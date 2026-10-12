using Fireflies.Config;
using Fireflies.Game.State;
using Fireflies.Session;
using Fireflies.UI.Widgets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Fireflies.UI.Screens
{
    // Panel del anfitrión: ping de cada jugador, expulsar, banear IP, quitar bans, reglas y pausa.
    public sealed class AdminPanelScreen : SessionScreen
    {
        private const string PauseText = "Pausar partida";
        private const string ResumeText = "Reanudar partida";
        private const string NoBansText = "No hay direcciones baneadas.";

        [SerializeField] private PlayerPalette _palette;
        [SerializeField] private HudScreen _hud;

        private PlayerListView _playerList;
        private RulesEditorView _rulesEditor;
        private VisualElement _banList;
        private Button _pauseButton;
        private Button _lobbyButton;

        public bool IsRequestedOpen { get; private set; }

        protected override string RootElementName => "admin";

        protected override void BindElements()
        {
            _playerList = new PlayerListView(Root.Q("admin-player-list"), _palette, playerId => HostAdministration?.Kick(playerId), playerId => HostAdministration?.Ban(playerId));
            _rulesEditor = new RulesEditorView(Root.Q("admin-rules-editor"));
            _banList = Root.Q("admin-ban-list");
            _pauseButton = Root.Q<Button>("admin-pause");
            _lobbyButton = Root.Q<Button>("admin-lobby");

            _pauseButton.clicked += () => HostAdministration?.TogglePause();
            _lobbyButton.clicked += ReturnToLobby;
            Root.Q<Button>("admin-close").clicked += Close;
            _hud.AdminPanelRequested += Toggle;
            UiFactory.MakeButtonsUnfocusable(Root);
        }

        protected override void OnDisable()
        {
            _hud.AdminPanelRequested -= Toggle;
            base.OnDisable();
        }

        protected override void OnSessionAttached(IMatchSession session)
        {
            if (HostAdministration == null)
            {
                return;
            }

            session.RosterChanged += RefreshPlayers;
            session.RulesChanged += RefreshRules;
            session.PhaseChanged += RefreshPhaseButtons;
            HostAdministration.BanListChanged += RefreshBans;
        }

        protected override void OnSessionDetached(IMatchSession session)
        {
            IsRequestedOpen = false;
            if (session is not IHostAdministration host)
            {
                return;
            }

            session.RosterChanged -= RefreshPlayers;
            session.RulesChanged -= RefreshRules;
            session.PhaseChanged -= RefreshPhaseButtons;
            host.BanListChanged -= RefreshBans;
        }

        protected override void OnShown()
        {
            RefreshPlayers();
            RefreshRules();
            RefreshBans();
            RefreshPhaseButtons(Session.Phase);
        }

        private void Update()
        {
            bool togglePressed = Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame;
            if (togglePressed && HostAdministration != null)
            {
                Toggle();
            }
        }

        private void Toggle()
        {
            IsRequestedOpen = !IsRequestedOpen && HostAdministration != null;
        }

        private void Close()
        {
            IsRequestedOpen = false;
        }

        private void ReturnToLobby()
        {
            HostAdministration?.ReturnToLobby();
            Close();
        }

        private void RefreshPlayers()
        {
            if (IsVisible)
            {
                _playerList.Rebuild(Session.Players, Session.LocalPlayerId);
            }
        }

        private void RefreshRules()
        {
            IHostAdministration host = HostAdministration;
            if (IsVisible && host != null)
            {
                _rulesEditor.Show(Session.Rules, true, host.ApplyRules);
            }
        }

        private void RefreshBans()
        {
            IHostAdministration host = HostAdministration;
            if (!IsVisible || host == null)
            {
                return;
            }

            _banList.Clear();
            if (host.BannedAddresses.Count == 0)
            {
                _banList.Add(UiFactory.Label(NoBansText, "empty-hint"));
                return;
            }

            foreach (string address in host.BannedAddresses)
            {
                VisualElement row = UiFactory.Element("ban-row");
                row.Add(UiFactory.Label(address));
                row.Add(UiFactory.Button("Quitar ban", () => host.Unban(address), "button", "button--small"));
                _banList.Add(row);
            }
        }

        private void RefreshPhaseButtons(MatchPhase phase)
        {
            if (!IsVisible)
            {
                return;
            }

            bool canPause = phase == MatchPhase.Countdown || phase == MatchPhase.Playing || phase == MatchPhase.Paused;
            _pauseButton.SetEnabled(canPause);
            _pauseButton.text = phase == MatchPhase.Paused ? ResumeText : PauseText;
            _lobbyButton.SetEnabled(phase != MatchPhase.Lobby);
        }
    }
}
