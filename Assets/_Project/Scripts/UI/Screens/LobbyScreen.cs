using System.Collections.Generic;
using Fireflies.Config;
using Fireflies.Game.Rules;
using Fireflies.Session;
using Fireflies.UI.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fireflies.UI.Screens
{
    public sealed class LobbyScreen : SessionScreen
    {
        private const string HostRulesHint = "Como anfitrión puedes ajustar las reglas antes de empezar.";
        private const string ClientRulesHint = "Solo el anfitrión puede cambiar las reglas.";
        private const string NoNetworkText = "No se encontró una red local; los demás no podrán unirse.";

        [SerializeField] private PlayerPalette _palette;

        private PlayerListView _playerList;
        private RulesEditorView _rulesEditor;
        private Label _address;
        private Label _rulesHint;
        private Label _waiting;
        private Button _startButton;

        protected override string RootElementName => "lobby";

        protected override void BindElements()
        {
            _playerList = new PlayerListView(Root.Q("lobby-player-list"), _palette);
            _rulesEditor = new RulesEditorView(Root.Q("lobby-rules-editor"));
            _address = Root.Q<Label>("lobby-address");
            _rulesHint = Root.Q<Label>("lobby-rules-hint");
            _waiting = Root.Q<Label>("lobby-waiting");
            _startButton = Root.Q<Button>("lobby-start");
            _startButton.clicked += () => HostAdministration?.StartMatch();
            Root.Q<Button>("lobby-leave").clicked += Runner.Leave;
            UiFactory.MakeButtonsUnfocusable(Root);
        }

        protected override void OnSessionAttached(IMatchSession session)
        {
            session.RosterChanged += RefreshPlayers;
            session.RulesChanged += RefreshRules;
        }

        protected override void OnSessionDetached(IMatchSession session)
        {
            session.RosterChanged -= RefreshPlayers;
            session.RulesChanged -= RefreshRules;
        }

        protected override void OnShown()
        {
            if (Session == null)
            {
                return;
            }

            bool isHost = Session.IsHost;
            UiFactory.SetDisplayed(_startButton, isHost);
            UiFactory.SetDisplayed(_waiting, !isHost);
            _rulesHint.text = isHost ? HostRulesHint : ClientRulesHint;
            _address.text = isHost ? DescribeHostAddress() : $"Conectado a la sala de {Session.FindHost()?.PlayerName}";
            _playerList.Rebuild(Session.Players, Session.LocalPlayerId);
            RefreshRules();
        }

        private void RefreshPlayers()
        {
            if (!IsVisible || Session == null)
            {
                return;
            }

            _playerList.Rebuild(Session.Players, Session.LocalPlayerId);
        }

        private void RefreshRules()
        {
            if (Session == null)
            {
                return;
            }

            IHostAdministration host = HostAdministration;
            _rulesEditor.Show(Session.Rules, host != null, host == null ? null : (MatchRules edited) => host.ApplyRules(edited));
        }

        private string DescribeHostAddress()
        {
            List<string> addresses = LocalAddressFinder.FindIPv4Addresses();
            int port = HostAdministration.ListeningPort;
            return addresses.Count == 0 ? NoNetworkText : $"Comparte tu IP: {string.Join("  o  ", addresses)}   |   Puerto {port}";
        }
    }
}
