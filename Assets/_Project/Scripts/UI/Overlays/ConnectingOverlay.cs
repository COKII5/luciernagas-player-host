using UnityEngine;
using UnityEngine.UIElements;

namespace Fireflies.UI.Overlays
{
    public sealed class ConnectingOverlay : SessionScreen
    {
        private const string ConnectingText = "Buscando al anfitrión y entrando a la sala...";

        [SerializeField] private float _spinnerDegreesPerSecond = 180f;

        private VisualElement _spinner;
        private float _angle;

        protected override string RootElementName => "connecting";

        protected override void BindElements()
        {
            _spinner = Root.Q("connecting-spinner");
            Root.Q<Label>("connecting-text").text = ConnectingText;
            Root.Q<Button>("connecting-cancel").clicked += Runner.Leave;
        }

        private void Update()
        {
            if (!IsVisible)
            {
                return;
            }

            _angle = (_angle + _spinnerDegreesPerSecond * Time.deltaTime) % 360f;
            _spinner.style.rotate = new Rotate(_angle);
        }
    }
}
