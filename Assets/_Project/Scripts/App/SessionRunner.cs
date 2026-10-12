using System;
using System.Net;
using System.Net.Sockets;
using Fireflies.App.Input;
using Fireflies.Config;
using Fireflies.Session;
using Fireflies.Sync;
using UnityEngine;

namespace Fireflies.App
{
    public sealed class SessionRunner : MonoBehaviour
    {
        [SerializeField] private NetworkSettings _networkSettings;
        [SerializeField] private GameplaySettings _gameplaySettings;
        [SerializeField] private KeyboardGamepadInput _playerInput;

        private IInputSource _inputSource;

        public event Action<IMatchSession> SessionStarted;
        public event Action<SessionEndReason> SessionEnded;

        public IMatchSession Session { get; private set; }
        public WorldRenderState RenderState { get; } = new();
        public NetworkSettings NetworkSettings => _networkSettings;
        public GameplaySettings GameplaySettings => _gameplaySettings;

        private void Awake()
        {
            _inputSource = _playerInput;
        }

        private void Update()
        {
            IMatchSession session = Session;
            if (session == null)
            {
                RenderState.Clear();
                return;
            }

            double now = Time.timeAsDouble;
            session.Poll(now, Time.deltaTime);
            if (Session == null)
            {
                return;
            }

            float fixedStepAlpha = Mathf.Clamp01((Time.time - Time.fixedTime) / Time.fixedDeltaTime);
            session.FillRenderState(RenderState, now, fixedStepAlpha);
        }

        private void FixedUpdate()
        {
            Session?.FixedTick(Time.fixedTimeAsDouble, Time.fixedDeltaTime, _inputSource.ConsumeFixedStepInput());
        }

        private void OnApplicationQuit()
        {
            Leave();
        }

        public bool TryHost(string playerName, byte preferredSlot, int port, out string error)
        {
            HostMatchSession host = new HostMatchSession(_networkSettings, _gameplaySettings, playerName, preferredSlot);
            try
            {
                host.Start(port);
            }
            catch (SocketException exception)
            {
                error = exception.SocketErrorCode == SocketError.AddressAlreadyInUse
                    ? $"El puerto {port} ya está en uso."
                    : $"No se pudo abrir el puerto {port} ({exception.SocketErrorCode}).";
                return false;
            }

            error = null;
            Begin(host);
            return true;
        }

        public void Join(IPEndPoint hostEndPoint, string playerName, byte preferredSlot)
        {
            ClientMatchSession client = new ClientMatchSession(_networkSettings, _gameplaySettings, Time.fixedDeltaTime);
            Begin(client);
            client.Connect(hostEndPoint, playerName, preferredSlot, Time.timeAsDouble);
        }

        public void Leave()
        {
            Session?.Dispose();
        }

        public void UseAutopilot()
        {
            _inputSource = new AutopilotInput();
        }

        private void Begin(IMatchSession session)
        {
            Leave();
            Session = session;
            session.Ended += OnSessionEnded;
            SessionStarted?.Invoke(session);
        }

        private void OnSessionEnded(SessionEndReason reason)
        {
            IMatchSession ended = Session;
            Session = null;
            if (ended != null)
            {
                ended.Ended -= OnSessionEnded;
                ended.Dispose();
            }

            RenderState.Clear();
            SessionEnded?.Invoke(reason);
        }
    }
}
