using System.Collections.Generic;
using Fireflies.Net.Messages;

namespace Fireflies.Sync
{
    public sealed class SnapshotBuffer
    {
        private const int Capacity = 32;

        private readonly List<SnapshotMessage> _snapshots = new(Capacity);

        public int Count => _snapshots.Count;
        public SnapshotMessage Latest => _snapshots.Count > 0 ? _snapshots[_snapshots.Count - 1] : null;

        public SnapshotMessage this[int index] => _snapshots[index];

        // UDP no garantiza orden: un snapshot viejo o repetido no sirve y se descarta.
        public bool TryAdd(SnapshotMessage snapshot)
        {
            if (Latest != null && snapshot.Tick <= Latest.Tick)
            {
                return false;
            }

            _snapshots.Add(snapshot);
            if (_snapshots.Count > Capacity)
            {
                _snapshots.RemoveAt(0);
            }

            return true;
        }
    }
}
