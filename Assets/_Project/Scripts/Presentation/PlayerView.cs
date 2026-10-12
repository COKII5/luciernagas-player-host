using Fireflies.Sync;
using UnityEngine;

namespace Fireflies.Presentation
{
    public sealed class PlayerView : MonoBehaviour
    {
        private const float MinSpeedForSway = 0.05f;

        [SerializeField] private Renderer[] _tintedRenderers;
        [SerializeField] private Transform _body;
        [SerializeField] private Transform _lantern;
        [SerializeField] private Light _lanternLight;
        [SerializeField] private CarriedFirefliesOrbit _carriedOrbit;
        [SerializeField] private TrailRenderer _dashTrail;
        [SerializeField] private GameObject _localPlayerMarker;

        [Header("Procedural animation")]
        [SerializeField] private float _turnSharpness = 14f;
        [SerializeField] private float _stepFrequency = 9f;
        [SerializeField] private float _stepBounce = 0.06f;
        [SerializeField] private float _lanternSwayDegrees = 22f;
        [SerializeField] private float _lanternSharpness = 8f;
        [SerializeField] private float _speedForFullSway = 6f;
        [SerializeField, Range(0f, 1f)] private float _idleSwayFactor = 0.3f;
        [SerializeField] private float _stunWobbleDegrees = 18f;
        [SerializeField] private float _stunWobbleFrequency = 30f;
        [SerializeField] private float _tintEmission = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _lanternWhiteBlend = 0.35f;

        private Vector3 _lastPosition;
        private float _walkCycle;
        private float _lanternAngle;
        private bool _hasPosition;

        public void Configure(Color color, bool isLocalPlayer)
        {
            TintApplier.Apply(_tintedRenderers, color, _tintEmission);
            _lanternLight.color = Color.Lerp(color, Color.white, _lanternWhiteBlend);
            _localPlayerMarker.SetActive(isLocalPlayer);
            _dashTrail.startColor = color;
            _dashTrail.endColor = new Color(color.r, color.g, color.b, 0f);
        }

        public void Apply(in PlayerRenderState state, float deltaTime)
        {
            Vector3 position = new Vector3(state.Position.x, 0f, state.Position.y);
            float speed = _hasPosition && deltaTime > 0f ? Vector3.Distance(position, _lastPosition) / deltaTime : 0f;
            _lastPosition = position;
            _hasPosition = true;
            transform.position = position;

            if (state.Facing.sqrMagnitude > 0f)
            {
                Quaternion target = Quaternion.LookRotation(new Vector3(state.Facing.x, 0f, state.Facing.y));
                transform.rotation = Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-_turnSharpness * deltaTime));
            }

            AnimateBody(speed, state.IsStunned, deltaTime);
            _dashTrail.emitting = state.IsDashing;
            _carriedOrbit.SetCarried(state.CarriedNormal, state.CarriedGolden);
        }

        public void ResetMotion()
        {
            _hasPosition = false;
            _dashTrail.Clear();
        }

        private void AnimateBody(float speed, bool isStunned, float deltaTime)
        {
            bool isMoving = speed > MinSpeedForSway;
            _walkCycle = isMoving ? _walkCycle + deltaTime * _stepFrequency : 0f;
            float bounce = Mathf.Abs(Mathf.Sin(_walkCycle)) * _stepBounce;
            float wobble = isStunned ? Mathf.Sin(Time.time * _stunWobbleFrequency) * _stunWobbleDegrees : 0f;
            _body.localPosition = new Vector3(0f, bounce, 0f);
            _body.localRotation = Quaternion.Euler(0f, 0f, wobble);

            float targetSway = isMoving ? -_lanternSwayDegrees * Mathf.Clamp01(speed / _speedForFullSway) : 0f;
            targetSway += Mathf.Sin(_walkCycle * 0.5f) * _lanternSwayDegrees * _idleSwayFactor;
            _lanternAngle = Mathf.Lerp(_lanternAngle, targetSway, 1f - Mathf.Exp(-_lanternSharpness * deltaTime));
            _lantern.localRotation = Quaternion.Euler(_lanternAngle, 0f, 0f);
        }
    }
}
