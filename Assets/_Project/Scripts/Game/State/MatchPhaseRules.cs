namespace Fireflies.Game.State
{
    // Host y cliente consultan lo mismo; si difirieran, la predicción del cliente se desalinearía.
    public static class MatchPhaseRules
    {
        public static bool AllowsMovement(MatchPhase phase)
        {
            return phase == MatchPhase.Lobby || phase == MatchPhase.Playing;
        }

        public static bool IsMatchInProgress(MatchPhase phase)
        {
            return phase == MatchPhase.Countdown || phase == MatchPhase.Playing || phase == MatchPhase.Paused;
        }

        public static bool CanStartMatch(MatchPhase phase)
        {
            return phase == MatchPhase.Lobby || phase == MatchPhase.Results;
        }
    }
}
