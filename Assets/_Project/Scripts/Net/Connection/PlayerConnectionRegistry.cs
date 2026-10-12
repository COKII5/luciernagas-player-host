using System;
using System.Collections.Generic;
using System.Net;
using Fireflies.Net.Protocol;

namespace Fireflies.Net.Connection
{
    public sealed class PlayerConnectionRegistry
    {
        private readonly Dictionary<IPEndPoint, ConnectedClient> _clientsByEndPoint = new();
        private readonly Dictionary<byte, ConnectedClient> _clientsByPlayerId = new();
        private byte _lastAssignedPlayerId = ProtocolConstants.HostPlayerId;

        public int Count => _clientsByPlayerId.Count;

        public IReadOnlyCollection<ConnectedClient> Clients => _clientsByPlayerId.Values;

        public bool TryGetByEndPoint(IPEndPoint endPoint, out ConnectedClient client)
        {
            return _clientsByEndPoint.TryGetValue(endPoint, out client);
        }

        public bool TryGetByPlayerId(byte playerId, out ConnectedClient client)
        {
            return _clientsByPlayerId.TryGetValue(playerId, out client);
        }

        public ConnectedClient Register(RemotePeer peer, string playerName, byte preferredSlot)
        {
            byte playerId = AllocatePlayerId();
            ConnectedClient client = new ConnectedClient(playerId, playerName, preferredSlot, peer);
            _clientsByEndPoint.Add(peer.EndPoint, client);
            _clientsByPlayerId.Add(playerId, client);
            return client;
        }

        public void Remove(ConnectedClient client)
        {
            _clientsByEndPoint.Remove(client.Peer.EndPoint);
            _clientsByPlayerId.Remove(client.PlayerId);
            client.MarkDisconnected();
        }

        // Los ids no se reutilizan enseguida: así un paquete tardío de alguien que salió
        // no se confunde con el jugador nuevo.
        private byte AllocatePlayerId()
        {
            for (int attempt = 0; attempt < byte.MaxValue; attempt++)
            {
                _lastAssignedPlayerId = _lastAssignedPlayerId == byte.MaxValue ? (byte)1 : (byte)(_lastAssignedPlayerId + 1);
                if (!_clientsByPlayerId.ContainsKey(_lastAssignedPlayerId))
                {
                    return _lastAssignedPlayerId;
                }
            }

            throw new InvalidOperationException("No player ids left.");
        }
    }
}
