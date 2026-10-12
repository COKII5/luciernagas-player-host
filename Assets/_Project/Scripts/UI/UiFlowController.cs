using Fireflies.App;
using Fireflies.Game.State;
using Fireflies.Session;
using Fireflies.UI.Overlays;
using Fireflies.UI.Screens;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fireflies.UI
{
    // Decide qué pantalla se ve según el estado de la sesión: menú, conectando, sala, partida o resultados.
    public sealed class UiFlowController : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;
        [SerializeField] private SessionRunner _runner;
        [SerializeField] private MainMenuScreen _mainMenu;
        [SerializeField] private ConnectingOverlay _connecting;
        [SerializeField] private LobbyScreen _lobby;
        [SerializeField] private HudScreen _hud;
        [SerializeField] private ResultsScreen _results;
        [SerializeField] private AdminPanelScreen _adminPanel;
        [SerializeField] private DialogOverlay _dialog;

        private void OnEnable()
        {
            _runner.SessionEnded += ShowEndReason;
        }

        private void OnDisable()
        {
            _runner.SessionEnded -= ShowEndReason;
        }

        // Los contenedores de cada plantilla ocupan toda la pantalla y no deben tapar los clics de las capas de abajo.
        private void Start()
        {
            _document.rootVisualElement.Query<TemplateContainer>().ForEach(container => container.pickingMode = PickingMode.Ignore);
            _dialog.SetVisible(false);
        }

        private void Update()
        {
            IMatchSession session = _runner.Session;
            bool isReady = session != null && session.IsReady;
            MatchPhase phase = isReady ? session.Phase : MatchPhase.Lobby;
            bool isInMatch = isReady && (phase == MatchPhase.Countdown || phase == MatchPhase.Playing || phase == MatchPhase.Paused);

            _mainMenu.SetVisible(session == null);
            _connecting.SetVisible(session != null && !isReady);
            _lobby.SetVisible(isReady && phase == MatchPhase.Lobby);
            _hud.SetVisible(isInMatch);
            _results.SetVisible(isReady && phase == MatchPhase.Results);
            _adminPanel.SetVisible(isReady && session.IsHost && _adminPanel.IsRequestedOpen);
        }

        private void ShowEndReason(SessionEndReason reason)
        {
            if (reason != SessionEndReason.LeftByUser)
            {
                _dialog.ShowEndReason(reason);
            }
        }
    }
}
