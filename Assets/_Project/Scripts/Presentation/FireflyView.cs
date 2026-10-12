using UnityEngine;

namespace Fireflies.Presentation
{
    public sealed class FireflyView : MonoBehaviour
    {
        [SerializeField] private Transform _visual;
        [SerializeField] private float _hoverHeight = 0.9f;
        [SerializeField] private float _bobAmplitude = 0.18f;
        [SerializeField] private float _bobFrequency = 2.4f;
        [SerializeField] private float _spinDegreesPerSecond = 90f;
        [SerializeField] private float _appearDuration = 0.35f;

        private float _phase;
        private float _age;

        public bool IsGolden { get; set; }

        public void Show(Vector2 position)
        {
            _phase = Random.value * Mathf.PI * 2f;
            _age = 0f;
            gameObject.SetActive(true);
            MoveTo(position);
            Animate();
        }

        public void MoveTo(Vector2 position)
        {
            transform.position = new Vector3(position.x, 0f, position.y);
        }

        private void Update()
        {
            _age += Time.deltaTime;
            Animate();
        }

        private void Animate()
        {
            float bob = Mathf.Sin(Time.time * _bobFrequency + _phase) * _bobAmplitude;
            float appear = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_age / _appearDuration));
            _visual.localPosition = new Vector3(0f, _hoverHeight + bob, 0f);
            _visual.localRotation = Quaternion.Euler(0f, Time.time * _spinDegreesPerSecond + _phase * Mathf.Rad2Deg, 0f);
            _visual.localScale = Vector3.one * appear;
        }
    }
}
