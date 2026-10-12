using System.Net;
using Fireflies.Game.State;
using Fireflies.Net.Connection;
using Fireflies.Session;
using UnityEngine;

namespace Fireflies.App
{
    // Permite abrir host o cliente desde la consola para probar varias instancias (incluso con -batchmode).
    public sealed class CommandLineLauncher : MonoBehaviour
    {
        private const string DefaultNamePrefix = "Jugador";
        private const int MinRandomNameSuffix = 100;
        private const int MaxRandomNameSuffix = 1000;

        [SerializeField] private SessionRunner _runner;
        [SerializeField, Range(10, 120)] private int _headlessFrameRate = 60;
        [SerializeField, Min(1f)] private float _statusLogInterval = 10f;
        [SerializeField, Min(0f)] private float _autoRestartDelay = 5f;

        private LaunchOptions _options;
        private float _nextStatusLogTime;
        private float _resultsShownTime;

        public bool StartedSession => _options != null && _options.HasSessionRequest;

        private void Start()
        {
            _options = LaunchOptions.FromEnvironment();
            ScheduleTimedActions();
            if (!_options.HasSessionRequest)
            {
                return;
            }

            if (Application.isBatchMode)
            {
                Application.targetFrameRate = _headlessFrameRate;
            }

            if (_options.Autopilot)
            {
                _runner.UseAutopilot();
            }

            _runner.SessionEnded += reason => Debug.Log($"[Launcher] Session ended: {reason}");
            StartRequestedSession();
        }

        private void ScheduleTimedActions()
        {
            if (_options.QuitAfterSeconds > 0f)
            {
                Invoke(nameof(Quit), _options.QuitAfterSeconds);
            }

            if (!string.IsNullOrEmpty(_options.CapturePath))
            {
                Invoke(nameof(CaptureScreenshot), _options.CaptureAfterSeconds);
            }
        }

        private void CaptureScreenshot()
        {
            ScreenCapture.CaptureScreenshot(_options.CapturePath);
            Debug.Log($"[Launcher] Screenshot saved to {_options.CapturePath}");
        }

        private void Update()
        {
            IMatchSession session = _runner.Session;
            if (!StartedSession || session == null || !session.IsReady)
            {
                return;
            }

            if (session is IHostAdministration host)
            {
                AutoStartMatches(session, host);
            }

            if (Time.time >= _nextStatusLogTime)
            {
                _nextStatusLogTime = Time.time + _statusLogInterval;
                LogStatus(session);
            }
        }

        private void StartRequestedSession()
        {
            int port = _options.Port ?? _runner.NetworkSettings.DefaultPort;
            string requestedName = PlayerNameRules.Normalize(_options.PlayerName);
            string playerName = PlayerNameRules.IsValid(requestedName) ? requestedName : DefaultNamePrefix + Random.Range(MinRandomNameSuffix, MaxRandomNameSuffix);
            byte slot = _options.Slot ?? 0;

            if (_options.HostRequested)
            {
                if (!_runner.TryHost(playerName, slot, port, out string error))
                {
                    Debug.LogError($"[Launcher] {error}");
                }

                return;
            }

            if (!_runner.TryJoin(new IPEndPoint(_options.JoinAddress, port), playerName, slot, out string joinError))
            {
                Debug.LogError($"[Launcher] {joinError}");
            }
        }

        private void AutoStartMatches(IMatchSession session, IHostAdministration host)
        {
            if (_options.AutoStartPlayerCount <= 0)
            {
                return;
            }

            bool enoughPlayers = session.Players.Count >= _options.AutoStartPlayerCount;
            if (session.Phase == MatchPhase.Lobby && enoughPlayers)
            {
                host.StartMatch();
            }
            else if (session.Phase == MatchPhase.Results)
            {
                RestartAfterDelay(host);
            }
            else
            {
                _resultsShownTime = 0f;
            }
        }

        private void RestartAfterDelay(IHostAdministration host)
        {
            if (_resultsShownTime <= 0f)
            {
                _resultsShownTime = Time.time;
            }
            else if (Time.time - _resultsShownTime >= _autoRestartDelay)
            {
                host.StartMatch();
            }
        }

        private void LogStatus(IMatchSession session)
        {
            string role = session.IsHost ? "host" : "client";
            string diagnostics = session is ISyncDiagnostics sync ? $" rtt={sync.RoundTripMs}ms buffered={sync.BufferedSnapshots}" : string.Empty;
            Debug.Log($"[Launcher] {role} players={session.Players.Count} phase={session.Phase} remaining={session.PhaseSecondsRemaining:0}s{diagnostics}");
        }

        private void Quit()
        {
            _runner.Leave();
            Application.Quit();
        }
    }
}
