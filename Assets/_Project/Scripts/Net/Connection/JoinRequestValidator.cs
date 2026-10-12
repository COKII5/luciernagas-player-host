using System.Net;
using Fireflies.Net.Messages;
using Fireflies.Net.Protocol;

namespace Fireflies.Net.Connection
{
    public sealed class JoinRequestValidator
    {
        private readonly BanList _banList;
        private readonly int _maxPlayers;

        public JoinRequestValidator(BanList banList, int maxPlayers)
        {
            _banList = banList;
            _maxPlayers = maxPlayers;
        }

        public JoinRejectReason? FindRejection(IPEndPoint sender, JoinRequestMessage request, string normalizedName, int connectedClients)
        {
            if (request.ProtocolVersion != ProtocolConstants.ProtocolVersion)
            {
                return JoinRejectReason.VersionMismatch;
            }

            if (_banList.Contains(sender.Address))
            {
                return JoinRejectReason.Banned;
            }

            int playersIncludingHost = connectedClients + 1;
            if (playersIncludingHost >= _maxPlayers)
            {
                return JoinRejectReason.SessionFull;
            }

            return PlayerNameRules.IsValid(normalizedName) ? null : JoinRejectReason.InvalidName;
        }
    }
}
