using System.Collections.Generic;
using Fireflies.Config;
using Fireflies.Game.Rules;
using Fireflies.Game.State;
using UnityEngine;

namespace Fireflies.Game.Simulation
{
    // Solo existe en el host (autoritativo); los clientes reciben su estado por snapshots.
    public sealed class MatchSimulation
    {
        private readonly GameplaySettings _settings;
        private readonly List<SimPlayer> _players = new();
        private readonly List<GameEvent> _events = new();

        public MatchSimulation(GameplaySettings settings, MatchRules rules)
        {
            _settings = settings;
            Rules = rules;
            Layout = new ArenaLayout(settings);
            Field = new FireflyField(settings, Layout);
        }

        public MatchRules Rules { get; set; }
        public ArenaLayout Layout { get; }
        public FireflyField Field { get; }
        public IReadOnlyList<SimPlayer> Players => _players;
        public IReadOnlyList<GameEvent> PendingEvents => _events;

        public SimPlayer AddPlayer(byte playerId, int slot)
        {
            SimPlayer player = new SimPlayer(playerId, slot, CreateSpawnState(slot));
            _players.Add(player);
            return player;
        }

        public void RemovePlayer(byte playerId)
        {
            SimPlayer player = FindPlayer(playerId);
            if (player == null)
            {
                return;
            }

            Field.Scatter(player.Motion.Position, player.CarriedNormal, player.CarriedGolden);
            _players.Remove(player);
        }

        public SimPlayer FindPlayer(byte playerId)
        {
            return _players.Find(player => player.PlayerId == playerId);
        }

        public void ResetForNewMatch()
        {
            Field.Clear();
            foreach (SimPlayer player in _players)
            {
                player.ResetForMatch(CreateSpawnState(player.Slot));
            }
        }

        public void StepMovement(float deltaTime, bool canMove, int maxInputBacklog)
        {
            MovementTuning tuning = new MovementTuning(_settings, Rules);
            foreach (SimPlayer player in _players)
            {
                player.Step(deltaTime, tuning, canMove, maxInputBacklog);
            }

            Field.Advance(deltaTime);
        }

        public void StepGameplay(float deltaTime)
        {
            DashCollisionResolver.Resolve(_players, _settings, Field, _events);
            CollectFireflies();
            DepositAtAltars();
            if (Field.TrySpawn(deltaTime, Rules, out bool spawnedGolden) && spawnedGolden)
            {
                _events.Add(new GameEvent(GameEventType.GoldenSpawned, 0, 0, (byte)_settings.GoldenValue));
            }
        }

        public bool TryFindWinner(out SimPlayer winner)
        {
            winner = null;
            foreach (SimPlayer player in _players)
            {
                bool reachedTarget = player.Score >= Rules.TargetScore;
                if (reachedTarget && (winner == null || player.Score > winner.Score))
                {
                    winner = player;
                }
            }

            return winner != null;
        }

        public void ClearEvents()
        {
            _events.Clear();
        }

        private void CollectFireflies()
        {
            foreach (SimPlayer player in _players)
            {
                bool canCarryMore = player.CarriedCount < Rules.CarryCapacity;
                if (canCarryMore && !player.Motion.IsStunned && Field.TryCollect(player.Motion.Position, _settings.PickupRadius, out bool isGolden))
                {
                    player.AddCarried(isGolden);
                }
            }
        }

        private void DepositAtAltars()
        {
            foreach (SimPlayer player in _players)
            {
                Vector2 altar = Layout.GetAltarPosition(player.Slot);
                bool isAtAltar = Vector2.Distance(player.Motion.Position, altar) <= _settings.DepositRadius;
                if (isAtAltar && player.CarriedCount > 0)
                {
                    int points = player.DepositCarried(_settings.GoldenValue);
                    _events.Add(new GameEvent(GameEventType.Deposited, player.PlayerId, 0, (byte)points));
                }
            }
        }

        private PlayerMotionState CreateSpawnState(int slot)
        {
            return PlayerMotionState.At(Layout.GetSpawnPosition(slot), Layout.GetSpawnFacing(slot));
        }
    }
}
