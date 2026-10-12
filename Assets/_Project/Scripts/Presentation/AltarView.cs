using UnityEngine;

namespace Fireflies.Presentation
{
    public sealed class AltarView : MonoBehaviour
    {
        [SerializeField] private Renderer[] _tintedRenderers;
        [SerializeField] private Light _altarLight;
        [SerializeField] private Color _vacantColor = new Color(0.25f, 0.27f, 0.33f);
        [SerializeField] private float _occupiedLightIntensity = 3f;
        [SerializeField] private float _vacantLightIntensity = 0.4f;
        [SerializeField] private float _tintEmission = 1.2f;
        [SerializeField] private float _pulseIntensityBoost = 6f;
        [SerializeField] private float _pulseDecayRate = 4f;

        private float _pulse;
        private float _baseIntensity;
        private bool _isOccupied;

        public void SetOwner(bool isOccupied, Color color)
        {
            if (_isOccupied == isOccupied && _altarLight.color == color)
            {
                return;
            }

            _isOccupied = isOccupied;
            Color shown = isOccupied ? color : _vacantColor;
            TintApplier.Apply(_tintedRenderers, shown, isOccupied ? _tintEmission : 0f);
            _altarLight.color = shown;
            _baseIntensity = isOccupied ? _occupiedLightIntensity : _vacantLightIntensity;
        }

        public void Pulse()
        {
            _pulse = 1f;
        }

        private void Update()
        {
            _pulse = Mathf.MoveTowards(_pulse, 0f, _pulseDecayRate * Time.deltaTime);
            _altarLight.intensity = _baseIntensity + _pulse * _pulseIntensityBoost;
        }
    }
}
