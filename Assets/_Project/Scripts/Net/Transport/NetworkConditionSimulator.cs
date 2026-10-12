using System;
using System.Collections.Generic;

namespace Fireflies.Net.Transport
{
    // Solo para pruebas: agrega latencia y pérdida para ver el suavizado en un solo PC.
    public sealed class NetworkConditionSimulator
    {
        private readonly Queue<(double releaseTime, ReceivedDatagram datagram)> _delayed = new();
        private readonly Random _random = new();
        private readonly double _latencySeconds;
        private readonly double _lossProbability;

        public NetworkConditionSimulator(int latencyMs, int packetLossPercent)
        {
            _latencySeconds = latencyMs / 1000.0;
            _lossProbability = packetLossPercent / 100.0;
        }

        public bool IsActive => _latencySeconds > 0 || _lossProbability > 0;

        public void Admit(ReceivedDatagram datagram, double now)
        {
            if (_random.NextDouble() < _lossProbability)
            {
                return;
            }

            _delayed.Enqueue((now + _latencySeconds, datagram));
        }

        public bool TryRelease(double now, out ReceivedDatagram datagram)
        {
            if (_delayed.Count > 0 && _delayed.Peek().releaseTime <= now)
            {
                datagram = _delayed.Dequeue().datagram;
                return true;
            }

            datagram = default;
            return false;
        }
    }
}
