using System;
using System.Collections.Generic;
using Fireflies.Game.Rules;
using Fireflies.Game.State;
using Fireflies.Net.Messages;
using Fireflies.Sync;

namespace Fireflies.Session
{
    // Lo implementan el host y el cliente: la UI no necesita saber cuál de los dos es.
    public interface IMatchSession : IDisposable
    {
        event Action<PlayerInfo> PlayerJoined;
        event Action<PlayerInfo> PlayerLeft;
        event Action RosterChanged;
        event Action<MatchPhase> PhaseChanged;
        event Action RulesChanged;
        event Action<GameEvent> GameEventRaised;
        event Action<IReadOnlyList<PlayerValue>> MatchFinished;
        event Action<SessionEndReason> Ended;

        bool IsHost { get; }
        bool IsReady { get; }
        byte LocalPlayerId { get; }
        MatchPhase Phase { get; }
        float PhaseSecondsRemaining { get; }
        MatchRules Rules { get; }
        IReadOnlyList<PlayerInfo> Players { get; }

        void Poll(double now, float frameDeltaTime);

        void FixedTick(double now, float deltaTime, LocalInput input);

        void FillRenderState(WorldRenderState target, double now, float fixedStepAlpha);
    }
}
