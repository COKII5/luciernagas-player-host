using System;
using System.Collections.Generic;
using Fireflies.Game.Rules;

namespace Fireflies.Session
{
    public interface IHostAdministration
    {
        event Action BanListChanged;

        IReadOnlyList<string> BannedAddresses { get; }
        int ListeningPort { get; }

        void StartMatch();

        void ReturnToLobby();

        void TogglePause();

        void ApplyRules(MatchRules rules);

        void Kick(byte playerId);

        void Ban(byte playerId);

        void Unban(string address);
    }
}
