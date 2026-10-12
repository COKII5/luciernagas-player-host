using System;
using Fireflies.Game.State;

namespace Fireflies.Game.Flow
{
    // Lobby -> Countdown -> Playing <-> Paused -> Results -> (Countdown | Lobby). Solo corre en el host.
    public sealed class MatchStateMachine
    {
        private const float MinSecondsAfterRuleChange = 1f;

        private MatchPhase _phaseBeforePause;

        public event Action<MatchPhase> PhaseChanged;

        public MatchPhase Phase { get; private set; } = MatchPhase.Lobby;
        public float SecondsRemaining { get; private set; }

        public bool AllowsMovement => MatchPhaseRules.AllowsMovement(Phase);
        public bool HasTimeExpired => Phase == MatchPhase.Playing && SecondsRemaining <= 0f;
        public bool CanStartMatch => MatchPhaseRules.CanStartMatch(Phase);

        private bool IsClockRunningOrPaused => Phase == MatchPhase.Playing || (Phase == MatchPhase.Paused && _phaseBeforePause == MatchPhase.Playing);

        public void Tick(float deltaTime, int matchDurationSeconds)
        {
            if (Phase != MatchPhase.Countdown && Phase != MatchPhase.Playing)
            {
                return;
            }

            SecondsRemaining = Math.Max(0f, SecondsRemaining - deltaTime);
            if (Phase == MatchPhase.Countdown && SecondsRemaining <= 0f)
            {
                Enter(MatchPhase.Playing, matchDurationSeconds);
            }
        }

        public void StartCountdown(int countdownSeconds)
        {
            if (CanStartMatch)
            {
                Enter(MatchPhase.Countdown, countdownSeconds);
            }
        }

        public void TogglePause()
        {
            if (Phase == MatchPhase.Paused)
            {
                Enter(_phaseBeforePause, SecondsRemaining);
            }
            else if (MatchPhaseRules.IsMatchInProgress(Phase))
            {
                _phaseBeforePause = Phase;
                Enter(MatchPhase.Paused, SecondsRemaining);
            }
        }

        // Si el host cambia la duración en plena partida, el reloj se corre en la misma diferencia.
        public void ShiftMatchTime(float deltaSeconds)
        {
            if (IsClockRunningOrPaused)
            {
                SecondsRemaining = Math.Max(MinSecondsAfterRuleChange, SecondsRemaining + deltaSeconds);
            }
        }

        public void Finish()
        {
            Enter(MatchPhase.Results, 0f);
        }

        public void ReturnToLobby()
        {
            Enter(MatchPhase.Lobby, 0f);
        }

        private void Enter(MatchPhase phase, float secondsRemaining)
        {
            Phase = phase;
            SecondsRemaining = secondsRemaining;
            PhaseChanged?.Invoke(phase);
        }
    }
}
