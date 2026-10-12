using Fireflies.Session;
using Fireflies.UI.Widgets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Fireflies.UI.Overlays
{
    // F3 muestra datos de red; con F4 un cliente apaga la interpolación para comparar a simple vista.
    public sealed class SyncDebugOverlay : SessionScreen
    {
        [SerializeField, Min(0.05f)] private float _refreshInterval = 0.25f;

        private Label _text;
        private bool _isOpen;
        private float _nextRefreshTime;

        protected override string RootElementName => "debug-overlay";

        protected override void BindElements()
        {
            _text = Root.Q<Label>("debug-text");
        }

        protected override void OnSessionDetached(IMatchSession session)
        {
            if (session is ISyncDiagnostics diagnostics)
            {
                diagnostics.InterpolationEnabled = true;
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f3Key.wasPressedThisFrame)
            {
                _isOpen = !_isOpen;
                UiFactory.SetDisplayed(Root, _isOpen);
            }

            if (keyboard.f4Key.wasPressedThisFrame && Session is ISyncDiagnostics toggled)
            {
                toggled.InterpolationEnabled = !toggled.InterpolationEnabled;
            }

            if (_isOpen && Time.unscaledTime >= _nextRefreshTime)
            {
                _nextRefreshTime = Time.unscaledTime + _refreshInterval;
                _text.text = Describe();
            }
        }

        private string Describe()
        {
            if (Session == null)
            {
                return "Sin sesión";
            }

            if (Session is not ISyncDiagnostics diagnostics)
            {
                return $"Anfitrión (autoritativo)  |  jugadores: {Session.Players.Count}  |  fase: {Session.Phase}";
            }

            string interpolation = diagnostics.InterpolationEnabled ? "ACTIVA" : "APAGADA (F4)";
            return $"RTT: {diagnostics.RoundTripMs} ms  |  snapshots en buffer: {diagnostics.BufferedSnapshots}\n" +
                   $"Interpolación: {interpolation}  |  predicción local: activa";
        }
    }
}
