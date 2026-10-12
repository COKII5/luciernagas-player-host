using System;
using System.Collections.Generic;
using Fireflies.Config;
using Fireflies.Game.Flow;
using Fireflies.Game.Rules;
using Fireflies.Game.Simulation;
using Fireflies.Game.State;
using Fireflies.Net.Messages;
using Fireflies.Net.Protocol;
using Fireflies.Net.Connection;
using Fireflies.Sync;

namespace Fireflies.Session
{
    public sealed class HostMatchSession : IMatchSession, IHostAdministration
    {
        private readonly NetworkSettings _networkSettings;
        private readonly GameplaySettings _gameplaySettings;
        private readonly HostNetwork _network;
        private readonly HostRoster _roster = new();
        private readonly MatchSimulation _simulation;
        private readonly MatchStateMachine _stateMachine = new();
        private readonly HostWorldReader _worldReader = new();
        private readonly SimPlayer _localPlayer;
        private uint _localSequence;
        private uint _tick;
        private double _now;
        private double _nextPingTableTime;
        private bool _isDisposed;

        public HostMatchSession(NetworkSettings networkSettings, GameplaySettings gameplaySettings, string hostName, byte preferredSlot)
        {
            _networkSettings = networkSettings;
            _gameplaySettings = gameplaySettings;
            _simulation = new MatchSimulation(gameplaySettings, gameplaySettings.CreateDefaultRules());

            BanList banList = new BanList(networkSettings.BanListFileName);
            banList.Load();
            banList.Changed += () => BanListChanged?.Invoke();
            _network = new HostNetwork(networkSettings, banList);
            _network.ClientJoined += OnClientJoined;
            _network.ClientLeft += OnClientLeft;
            _network.GameplayPacketReceived += OnGameplayPacketReceived;
            _stateMachine.PhaseChanged += OnPhaseChanged;

            _roster.TryAssignSlot(preferredSlot, out byte slot);
            _roster.Add(ProtocolConstants.HostPlayerId, slot, hostName, true);
            _localPlayer = _simulation.AddPlayer(ProtocolConstants.HostPlayerId, slot);
        }

        public event Action<PlayerInfo> PlayerJoined;
        public event Action<PlayerInfo> PlayerLeft;
        public event Action RosterChanged;
        public event Action<MatchPhase> PhaseChanged;
        public event Action RulesChanged;
        public event Action<GameEvent> GameEventRaised;
        public event Action<IReadOnlyList<PlayerValue>> MatchFinished;
        public event Action<SessionEndReason> Ended;
        public event Action BanListChanged;

        public bool IsHost => true;
        public bool IsReady { get; private set; }
        public byte LocalPlayerId => ProtocolConstants.HostPlayerId;
        public MatchPhase Phase => _stateMachine.Phase;
        public float PhaseSecondsRemaining => _stateMachine.SecondsRemaining;
        public MatchRules Rules => _simulation.Rules;
        public IReadOnlyList<PlayerInfo> Players => _roster.Players;
        public IReadOnlyList<string> BannedAddresses => _network.BanList.Addresses;
        public int ListeningPort => _network.LocalPort;

        public void Start(int port)
        {
            _network.Start(port);
            IsReady = true;
        }

        public void Poll(double now, float frameDeltaTime)
        {
            _now = now;
            _network.Poll(now);
            BroadcastPingTableIfDue(now);
        }

        public void FixedTick(double now, float deltaTime, LocalInput input)
        {
            _now = now;
            _localPlayer.EnqueueCommand(new InputCommand(++_localSequence, input.Move, input.DashPressed));
            _stateMachine.Tick(deltaTime, Rules.DurationSeconds);
            _simulation.StepMovement(deltaTime, _stateMachine.AllowsMovement, _networkSettings.MaxInputBacklog);

            if (_stateMachine.Phase == MatchPhase.Playing)
            {
                _simulation.StepGameplay(deltaTime);
                PublishGameEvents();
                FinishMatchIfDecided();
            }

            _tick++;
            BroadcastSnapshotIfDue(now);
        }

        public void FillRenderState(WorldRenderState target, double now, float fixedStepAlpha)
        {
            HostWorldReader.FillRenderState(_simulation, fixedStepAlpha, target);
        }

        public void StartMatch()
        {
            if (!_stateMachine.CanStartMatch)
            {
                return;
            }

            _simulation.ResetForNewMatch();
            _stateMachine.StartCountdown(_gameplaySettings.CountdownSeconds);
        }

        public void ReturnToLobby()
        {
            if (_stateMachine.Phase == MatchPhase.Lobby)
            {
                return;
            }

            _simulation.ResetForNewMatch();
            _stateMachine.ReturnToLobby();
        }

        public void TogglePause()
        {
            _stateMachine.TogglePause();
        }

        public void ApplyRules(MatchRules rules)
        {
            _simulation.Rules = rules.Clone();
            _network.BroadcastReliable(new RulesMessage(_simulation.Rules), _now);
            RulesChanged?.Invoke();
        }

        public void Kick(byte playerId)
        {
            _network.Kick(playerId, DisconnectReason.Kicked);
        }

        public void Ban(byte playerId)
        {
            _network.Ban(playerId);
        }

        public void Unban(string address)
        {
            _network.BanList.Remove(address);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;
            _network.Dispose();
            Ended?.Invoke(SessionEndReason.LeftByUser);
        }

        private void OnClientJoined(ConnectedClient client)
        {
            if (!_roster.TryAssignSlot(client.PreferredSlot, out byte slot))
            {
                _network.Kick(client.PlayerId, DisconnectReason.Kicked);
                return;
            }

            PlayerInfo info = _roster.Add(client.PlayerId, slot, client.PlayerName, false);
            _simulation.AddPlayer(client.PlayerId, slot);

            _network.SendReliable(client, new JoinAcceptedMessage(client.PlayerId, slot), _now);
            _network.SendReliable(client, new RulesMessage(Rules), _now);
            _network.SendReliable(client, new MatchPhaseMessage(Phase, PhaseSecondsRemaining), _now);
            _network.BroadcastReliable(_roster.ToRosterMessage(), _now);

            PlayerJoined?.Invoke(info);
            RosterChanged?.Invoke();
        }

        private void OnClientLeft(ConnectedClient client, DisconnectReason reason)
        {
            PlayerInfo info = _roster.Remove(client.PlayerId);
            _simulation.RemovePlayer(client.PlayerId);
            _network.BroadcastReliable(_roster.ToRosterMessage(), _now);

            if (info != null)
            {
                PlayerLeft?.Invoke(info);
            }

            RosterChanged?.Invoke();
        }

        private void OnGameplayPacketReceived(ConnectedClient client, DecodedPacket packet)
        {
            if (packet.Type != PacketType.Input || !PacketCodec.TryReadPayload(packet, InputMessage.Read, out InputMessage input))
            {
                return;
            }

            _simulation.FindPlayer(client.PlayerId)?.EnqueueCommands(input.Commands);
        }

        private void OnPhaseChanged(MatchPhase phase)
        {
            _network.BroadcastReliable(new MatchPhaseMessage(phase, _stateMachine.SecondsRemaining), _now);
            PhaseChanged?.Invoke(phase);
        }

        private void PublishGameEvents()
        {
            foreach (GameEvent gameEvent in _simulation.PendingEvents)
            {
                _network.BroadcastReliable(new GameEventMessage(gameEvent), _now);
                GameEventRaised?.Invoke(gameEvent);
            }

            _simulation.ClearEvents();
        }

        private void FinishMatchIfDecided()
        {
            if (!_simulation.TryFindWinner(out _) && !_stateMachine.HasTimeExpired)
            {
                return;
            }

            List<PlayerValue> standings = HostWorldReader.CalculateStandings(_simulation);
            _network.BroadcastReliable(new MatchResultsMessage(standings), _now);
            MatchFinished?.Invoke(standings);
            _stateMachine.Finish();
        }

        private void BroadcastSnapshotIfDue(double now)
        {
            if (_tick % _networkSettings.TicksPerSnapshot != 0)
            {
                return;
            }

            SnapshotMessage snapshot = _worldReader.ComposeSnapshot(_simulation, _tick, now, PhaseSecondsRemaining);
            _network.BroadcastUnreliable(snapshot);
        }

        private void BroadcastPingTableIfDue(double now)
        {
            if (now < _nextPingTableTime)
            {
                return;
            }

            _nextPingTableTime = now + _networkSettings.HeartbeatInterval;
            foreach (PlayerInfo player in _roster.Players)
            {
                if (!player.IsHost && _network.TryGetClient(player.PlayerId, out ConnectedClient client))
                {
                    player.RoundTripMs = client.Peer.RoundTripMs;
                }
            }

            _network.BroadcastUnreliable(_roster.ToPingTableMessage());
            RosterChanged?.Invoke();
        }
    }
}
