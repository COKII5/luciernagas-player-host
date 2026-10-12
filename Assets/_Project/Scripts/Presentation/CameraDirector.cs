using Fireflies.App;
using UnityEngine;

namespace Fireflies.Presentation
{
    public sealed class CameraDirector : MonoBehaviour
    {
        [SerializeField] private SessionRunner _runner;
        [SerializeField] private WorldPresenter _world;
        [SerializeField] private Transform _cameraTransform;

        [Header("Menu orbit")]
        [SerializeField] private float _orbitRadius = 26f;
        [SerializeField] private float _orbitHeight = 13f;
        [SerializeField] private float _orbitDegreesPerSecond = 4f;

        [Header("Gameplay framing")]
        [SerializeField] private Vector3 _gameplayOffset = new Vector3(0f, 27f, -13f);
        [SerializeField] private Vector3 _focusOffset = new Vector3(0f, 0f, -1.5f);
        [SerializeField, Range(0f, 1f)] private float _followFactor = 0.18f;
        [SerializeField] private float _sharpness = 3f;

        private float _orbitAngle;

        private void LateUpdate()
        {
            Vector3 focus = Vector3.zero;
            Vector3 desiredPosition;
            if (_runner.Session == null)
            {
                _orbitAngle += _orbitDegreesPerSecond * Time.deltaTime;
                float radians = _orbitAngle * Mathf.Deg2Rad;
                desiredPosition = new Vector3(Mathf.Cos(radians) * _orbitRadius, _orbitHeight, Mathf.Sin(radians) * _orbitRadius);
            }
            else
            {
                focus = _focusOffset;
                if (_world.TryGetPlayerPosition(_runner.Session.LocalPlayerId, out Vector3 localPosition))
                {
                    focus += localPosition * _followFactor;
                }

                desiredPosition = focus + _gameplayOffset;
            }

            float blend = 1f - Mathf.Exp(-_sharpness * Time.deltaTime);
            _cameraTransform.position = Vector3.Lerp(_cameraTransform.position, desiredPosition, blend);
            Quaternion desiredRotation = Quaternion.LookRotation(focus - desiredPosition);
            _cameraTransform.rotation = Quaternion.Slerp(_cameraTransform.rotation, desiredRotation, blend);
        }
    }
}
