using System;

namespace Fireflies.Sync
{
    public sealed class NetClock
    {
        private const double OffsetSmoothing = 0.1;
        private const double MillisecondsPerSecond = 1000.0;
        private const double OneWayFraction = 0.5;

        private readonly double _resyncThreshold;
        private double _offset;
        private bool _isSynchronized;

        public NetClock(double resyncThreshold)
        {
            _resyncThreshold = resyncThreshold;
        }

        // Un snapshot no puede llegar "del futuro" ni con mucho atraso; si pasa, alguno de los dos
        // equipos se trabó (p. ej. al arrastrar la ventana) y conviene saltar en vez de corregir de a poco.
        public void ObserveSnapshot(double snapshotHostTime, double localNow)
        {
            double drift = HostTimeAt(localNow) - snapshotHostTime;
            if (!_isSynchronized || Math.Abs(drift) > _resyncThreshold)
            {
                Resynchronize(snapshotHostTime - localNow);
            }
        }

        public void AddSample(double hostTime, int roundTripMs, double localNow)
        {
            double sampleOffset = hostTime + roundTripMs * OneWayFraction / MillisecondsPerSecond - localNow;
            if (!_isSynchronized || Math.Abs(sampleOffset - _offset) > _resyncThreshold)
            {
                Resynchronize(sampleOffset);
                return;
            }

            _offset += (sampleOffset - _offset) * OffsetSmoothing;
        }

        public double HostTimeAt(double localNow)
        {
            return localNow + _offset;
        }

        private void Resynchronize(double offset)
        {
            _offset = offset;
            _isSynchronized = true;
        }
    }
}
