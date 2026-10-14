using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Fireflies.Config;
using UnityEngine;

namespace Fireflies.Net.Transport
{
    // Misma idea del UDPServer visto en clase, pero para varios clientes: una tarea en segundo plano
    // recibe y encola en una ConcurrentQueue, y el hilo principal la vacía en cada frame.
    public sealed class UdpTransport : IDisposable
    {
        // En Windows, cuando un cliente cierra, el siguiente Receive lanza ConnectionReset por el
        // ICMP "port unreachable". Con este control se desactiva ese aviso.
        private const int SioUdpConnectionReset = -1744830452;
        private const int ReceiveErrorBackoffMs = 50;

        private readonly ConcurrentQueue<ReceivedDatagram> _incoming = new();
        private readonly NetworkConditionSimulator _conditionSimulator;
        private UdpClient _socket;
        private CancellationTokenSource _receiveCancellation;

        public UdpTransport(NetworkConditionSimulator conditionSimulator)
        {
            _conditionSimulator = conditionSimulator;
        }

        public bool IsOpen => _socket != null;

        public int LocalPort => _socket == null ? 0 : ((IPEndPoint)_socket.Client.LocalEndPoint).Port;

        public static UdpTransport FromSettings(NetworkSettings settings)
        {
            return new UdpTransport(new NetworkConditionSimulator(settings.SimulatedLatencyMs, settings.SimulatedPacketLossPercent));
        }

        public void Open(int port)
        {
            if (IsOpen)
            {
                throw new InvalidOperationException("The transport is already open.");
            }

            UdpClient socket = new UdpClient(port);
            try
            {
                IgnoreConnectionResetErrors(socket);
            }
            catch (Exception)
            {
                socket.Dispose();
                throw;
            }

            _socket = socket;
            _receiveCancellation = new CancellationTokenSource();
            CancellationToken token = _receiveCancellation.Token;
            Task.Run(() => ReceiveLoop(token));
        }

        public void Send(byte[] datagram, IPEndPoint target)
        {
            if (!IsOpen)
            {
                return;
            }

            try
            {
                _socket.Send(datagram, datagram.Length, target);
            }
            catch (SocketException exception)
            {
                Debug.LogWarning($"[UdpTransport] Send to {target} failed: {exception.SocketErrorCode}");
            }
            catch (ObjectDisposedException)
            {
                // El socket se cerró justo al salir de la sesión; ese envío ya no importa.
            }
        }

        // Los avisos de salida van sin confirmación (el socket se cierra enseguida), por eso se mandan varias veces.
        public void SendRepeated(byte[] datagram, IPEndPoint target, int times)
        {
            for (int i = 0; i < times; i++)
            {
                Send(datagram, target);
            }
        }

        public bool TryReceive(double now, out ReceivedDatagram datagram)
        {
            if (!_conditionSimulator.IsActive)
            {
                return _incoming.TryDequeue(out datagram);
            }

            while (_incoming.TryDequeue(out ReceivedDatagram arrived))
            {
                _conditionSimulator.Admit(arrived, now);
            }

            return _conditionSimulator.TryRelease(now, out datagram);
        }

        public void Dispose()
        {
            if (!IsOpen)
            {
                return;
            }

            _receiveCancellation.Cancel();
            _socket.Close();
            _socket.Dispose();
            _socket = null;
            _receiveCancellation.Dispose();
            _receiveCancellation = null;
        }

        private static void IgnoreConnectionResetErrors(UdpClient socket)
        {
            if (Application.platform != RuntimePlatform.WindowsPlayer && Application.platform != RuntimePlatform.WindowsEditor)
            {
                return;
            }

            socket.Client.IOControl(SioUdpConnectionReset, new byte[] { 0 }, null);
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            UdpClient socket = _socket;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    UdpReceiveResult result = await socket.ReceiveAsync();
                    _incoming.Enqueue(new ReceivedDatagram(result.RemoteEndPoint, result.Buffer));
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                catch (Exception exception) when (!token.IsCancellationRequested)
                {
                    Debug.LogWarning($"[UdpTransport] Receive error ignored: {exception.GetType().Name} {exception.Message}");
                    await Task.Delay(ReceiveErrorBackoffMs);
                }
            }
        }
    }
}
