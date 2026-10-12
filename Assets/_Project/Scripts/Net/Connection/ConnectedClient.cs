namespace Fireflies.Net.Connection
{
    public sealed class ConnectedClient
    {
        public ConnectedClient(byte playerId, string playerName, byte preferredSlot, RemotePeer peer)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            PreferredSlot = preferredSlot;
            Peer = peer;
            IsConnected = true;
        }

        public byte PlayerId { get; }
        public string PlayerName { get; }
        public byte PreferredSlot { get; }
        public RemotePeer Peer { get; }
        public bool IsConnected { get; private set; }

        public void MarkDisconnected()
        {
            IsConnected = false;
        }
    }
}
