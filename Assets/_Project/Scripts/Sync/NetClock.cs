namespace Fireflies.Sync
{
    // Estima el reloj del host para ubicar los snapshots en una misma línea de tiempo.
    public sealed class NetClock
    {
        private const double OffsetSmoothing = 0.1;

        private double _offset;
        private bool _isSynchronized;

        public bool IsSynchronized => _isSynchronized;

        public void SynchronizeRoughly(double hostTime, double localNow)
        {
            if (_isSynchronized)
            {
                return;
            }

            _offset = hostTime - localNow;
            _isSynchronized = true;
        }

        public void AddSample(double hostTime, int roundTripMs, double localNow)
        {
            double sampleOffset = hostTime + roundTripMs / 2000.0 - localNow;
            if (!_isSynchronized)
            {
                _offset = sampleOffset;
                _isSynchronized = true;
                return;
            }

            _offset += (sampleOffset - _offset) * OffsetSmoothing;
        }

        public double HostTimeAt(double localNow)
        {
            return localNow + _offset;
        }
    }
}
