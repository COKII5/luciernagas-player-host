using System;
using System.Collections.Generic;
using Fireflies.Config;
using Fireflies.Game.State;
using Fireflies.Session;
using Fireflies.Sync;
using Fireflies.UI.Widgets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Fireflies.UI.Screens
{
    public sealed class HudScreen : SessionScreen
    {
        private const float UrgentSeconds = 10f;
        private const float GoMessageSeconds = 1.2f;
        private const string GoMessage = "¡A brillar!";
        private const string HostPauseText = "Pausaste la partida. Todos ven este aviso hasta que la reanudes.";
        private const string ClientPauseText = "El anfitrión pausó la partida.";

        [SerializeField] private PlayerPalette _palette;

        private readonly Dictionary<byte, Label> _scoreValues = new();
        private VisualElement _scoreboard;
        private VisualElement _timerBox;
        private Label _timer;
        private Label _objective;
        private VisualElement _carryBox;
        private Label _carried;
        private VisualElement _dashBox;
        private VisualElement _dashFill;
        private Label _countdown;
        private VisualElement _pauseOverlay;
        private Label _pauseText;
        private Button _resumeButton;
        private VisualElement _hostTools;
        private int _lastCountdownValue;
        private float _goMessageTimeLeft;

        public event Action AdminPanelRequested;

        protected override string RootElementName => "hud";

        protected override void BindElements()
        {
            _scoreboard = Root.Q("hud-scoreboard");
            _timerBox = Root.Q("hud-timer-box");
            _timer = Root.Q<Label>("hud-timer");
            _objective = Root.Q<Label>("hud-objective");
            _carryBox = Root.Q("hud-carry");
            _carried = Root.Q<Label>("hud-carried");
            _dashBox = Root.Q("hud-dash");
            _dashFill = Root.Q("hud-dash-fill");
            _countdown = Root.Q<Label>("hud-countdown");
            _pauseOverlay = Root.Q("hud-pause-overlay");
            _pauseText = Root.Q<Label>("hud-pause-text");
            _resumeButton = Root.Q<Button>("hud-resume");
            _hostTools = Root.Q("hud-host-tools");

            _resumeButton.clicked += () => HostAdministration?.TogglePause();
            Root.Q<Button>("hud-pause").clicked += () => HostAdministration?.TogglePause();
            Root.Q<Button>("hud-admin").clicked += () => AdminPanelRequested?.Invoke();
            UiFactory.MakeButtonsUnfocusable(Root);
        }

        protected override void OnSessionAttached(IMatchSession session)
        {
            session.RosterChanged += RebuildScoreboard;
            session.PhaseChanged += OnPhaseChanged;
        }

        protected override void OnSessionDetached(IMatchSession session)
        {
            session.RosterChanged -= RebuildScoreboard;
            session.PhaseChanged -= OnPhaseChanged;
        }

        protected override void OnShown()
        {
            UiFactory.SetDisplayed(_hostTools, Session != null && Session.IsHost);
            RebuildScoreboard();
        }

        private void Update()
        {
            if (!IsVisible || Session == null)
            {
                return;
            }

            if (Session.IsHost && Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
            {
                HostAdministration.TogglePause();
            }

            UpdateTimer();
            UpdateCountdown();
            UpdatePause();
            UpdatePlayerStats(Runner.RenderState);
        }

        private void RebuildScoreboard()
        {
            if (!IsVisible || Session == null)
            {
                return;
            }

            _scoreboard.Clear();
            _scoreValues.Clear();
            foreach (PlayerInfo player in Session.Players)
            {
                VisualElement chip = UiFactory.Element("panel-chip", "score-chip");
                chip.EnableInClassList("score-chip--local", player.PlayerId == Session.LocalPlayerId);
                VisualElement dot = UiFactory.Element("score-chip__dot");
                dot.style.backgroundColor = _palette.GetColor(player.Slot);
                chip.Add(dot);
                chip.Add(UiFactory.Label(player.PlayerName, "score-chip__name"));
                Label value = UiFactory.Label("0", "score-chip__value");
                chip.Add(value);
                _scoreValues[player.PlayerId] = value;
                _scoreboard.Add(chip);
            }

            _objective.text = $"Lleva luciérnagas a tu altar  |  Meta: {Session.Rules.TargetScore} puntos";
        }

        private void UpdateTimer()
        {
            bool isCountingDown = Session.Phase == MatchPhase.Countdown;
            float seconds = isCountingDown ? Session.Rules.DurationSeconds : Session.PhaseSecondsRemaining;
            _timer.text = UiFormat.Clock(seconds);
            _timerBox.EnableInClassList("timer--urgent", Session.Phase == MatchPhase.Playing && seconds <= UrgentSeconds);
        }

        private void UpdateCountdown()
        {
            _goMessageTimeLeft = Mathf.Max(0f, _goMessageTimeLeft - Time.deltaTime);
            if (Session.Phase == MatchPhase.Countdown)
            {
                int value = Mathf.CeilToInt(Session.PhaseSecondsRemaining);
                _countdown.EnableInClassList("countdown--pop", value != _lastCountdownValue);
                _lastCountdownValue = value;
                _countdown.text = value > 0 ? value.ToString() : string.Empty;
                return;
            }

            _lastCountdownValue = 0;
            _countdown.RemoveFromClassList("countdown--pop");
            _countdown.text = _goMessageTimeLeft > 0f ? GoMessage : string.Empty;
        }

        private void OnPhaseChanged(MatchPhase phase)
        {
            if (phase == MatchPhase.Playing && _lastCountdownValue != 0)
            {
                _goMessageTimeLeft = GoMessageSeconds;
            }
        }

        private void UpdatePause()
        {
            bool isPaused = Session.Phase == MatchPhase.Paused;
            UiFactory.SetDisplayed(_pauseOverlay, isPaused);
            if (isPaused)
            {
                _pauseText.text = Session.IsHost ? HostPauseText : ClientPauseText;
                UiFactory.SetDisplayed(_resumeButton, Session.IsHost);
            }
        }

        private void UpdatePlayerStats(WorldRenderState renderState)
        {
            foreach (PlayerRenderState player in renderState.Players)
            {
                if (_scoreValues.TryGetValue(player.PlayerId, out Label value))
                {
                    value.text = player.Score.ToString();
                }

                if (player.PlayerId == Session.LocalPlayerId)
                {
                    UpdateLocalStats(player);
                }
            }
        }

        private void UpdateLocalStats(PlayerRenderState local)
        {
            int carried = local.CarriedNormal + local.CarriedGolden;
            int capacity = Session.Rules.CarryCapacity;
            _carried.text = $"{carried} / {capacity}";
            _carryBox.EnableInClassList("carry--full", carried >= capacity);

            bool dashEnabled = Session.Rules.DashEnabled;
            float charge = dashEnabled ? 1f - Mathf.Clamp01(local.DashCooldownLeft / Session.Rules.DashCooldown) : 0f;
            _dashFill.style.width = Length.Percent(charge * 100f);
            _dashBox.EnableInClassList("dash--ready", dashEnabled && charge >= 1f);
            _dashBox.EnableInClassList("dash--disabled", !dashEnabled);
        }
    }
}
