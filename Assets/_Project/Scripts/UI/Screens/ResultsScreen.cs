using System.Collections.Generic;
using Fireflies.Config;
using Fireflies.Net.Messages;
using Fireflies.Session;
using Fireflies.UI.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fireflies.UI.Screens
{
    public sealed class ResultsScreen : SessionScreen
    {
        private const int PodiumPlaces = 3;
        private const float FirstPlaceBlockHeight = 150f;
        private const float BlockHeightDropPerPlace = 40f;
        private const float BlockColorAlpha = 0.85f;

        [SerializeField] private PlayerPalette _palette;

        private readonly List<PlayerValue> _standings = new();
        private Label _title;
        private Label _subtitle;
        private VisualElement _podium;
        private Button _rematchButton;
        private Button _lobbyButton;
        private Label _waiting;

        protected override string RootElementName => "results";

        protected override void BindElements()
        {
            _title = Root.Q<Label>("results-title");
            _subtitle = Root.Q<Label>("results-subtitle");
            _podium = Root.Q("results-podium");
            _rematchButton = Root.Q<Button>("results-rematch");
            _lobbyButton = Root.Q<Button>("results-lobby");
            _waiting = Root.Q<Label>("results-waiting");

            _rematchButton.clicked += () => HostAdministration?.StartMatch();
            _lobbyButton.clicked += () => HostAdministration?.ReturnToLobby();
            Root.Q<Button>("results-leave").clicked += Runner.Leave;
            UiFactory.MakeButtonsUnfocusable(Root);
        }

        protected override void OnSessionAttached(IMatchSession session)
        {
            session.MatchFinished += StoreStandings;
        }

        protected override void OnSessionDetached(IMatchSession session)
        {
            session.MatchFinished -= StoreStandings;
            _standings.Clear();
        }

        protected override void OnShown()
        {
            bool isHost = Session != null && Session.IsHost;
            UiFactory.SetDisplayed(_rematchButton, isHost);
            UiFactory.SetDisplayed(_lobbyButton, isHost);
            UiFactory.SetDisplayed(_waiting, !isHost);
            ShowStandings();
        }

        private void StoreStandings(IReadOnlyList<PlayerValue> standings)
        {
            _standings.Clear();
            _standings.AddRange(standings);
            if (IsVisible)
            {
                ShowStandings();
            }
        }

        private void ShowStandings()
        {
            _podium.Clear();
            if (Session == null || _standings.Count == 0)
            {
                _title.text = "Fin de la partida";
                _subtitle.text = string.Empty;
                return;
            }

            PlayerValue best = _standings[0];
            bool isTie = _standings.Count > 1 && _standings[1].Value == best.Value;
            PlayerInfo winner = Session.FindPlayer(best.PlayerId);
            _title.text = isTie ? "¡Empate!" : $"¡Gana {winner?.PlayerName ?? "?"}!";
            _subtitle.text = best.Value >= Session.Rules.TargetScore
                ? $"Alcanzó la meta de {Session.Rules.TargetScore} puntos"
                : "Se acabó el tiempo";

            int places = Mathf.Min(PodiumPlaces, _standings.Count);
            for (int place = 0; place < places; place++)
            {
                _podium.Add(CreatePodiumStep(_standings[place], place));
            }
        }

        private VisualElement CreatePodiumStep(PlayerValue standing, int place)
        {
            PlayerInfo player = Session.FindPlayer(standing.PlayerId);
            int slot = player?.Slot ?? 0;
            Color color = _palette.GetColor(slot);

            VisualElement step = UiFactory.Element("podium-step");
            VisualElement portrait = UiFactory.Element("podium-step__portrait");
            portrait.style.backgroundImage = new StyleBackground(_palette.GetPortrait(slot));
            step.Add(portrait);

            VisualElement block = UiFactory.Element("podium-step__block");
            block.style.height = FirstPlaceBlockHeight - place * BlockHeightDropPerPlace;
            block.style.backgroundColor = new Color(color.r, color.g, color.b, BlockColorAlpha);
            block.Add(UiFactory.Label($"{place + 1}", "podium-step__place"));
            step.Add(block);

            step.Add(UiFactory.Label(player?.PlayerName ?? "Jugador que salió", "podium-step__name"));
            step.Add(UiFactory.Label($"{standing.Value} pts", "podium-step__score"));
            return step;
        }
    }
}
