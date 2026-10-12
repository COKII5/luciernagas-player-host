using System.Collections.Generic;
using UnityEngine;

namespace Fireflies.Presentation
{
    public sealed class CarriedFirefliesOrbit : MonoBehaviour
    {
        [SerializeField] private Transform _normalOrbPrefab;
        [SerializeField] private Transform _goldenOrbPrefab;
        [SerializeField, Min(0.1f)] private float _radius = 0.75f;
        [SerializeField] private float _height = 1.1f;
        [SerializeField] private float _degreesPerSecond = 140f;
        [SerializeField] private float _bobAmplitude = 0.08f;
        [SerializeField] private float _bobFrequency = 3f;

        private readonly List<Transform> _normalOrbs = new();
        private readonly List<Transform> _goldenOrbs = new();
        private float _angle;

        public void SetCarried(int normalCount, int goldenCount)
        {
            Resize(_normalOrbs, _normalOrbPrefab, normalCount);
            Resize(_goldenOrbs, _goldenOrbPrefab, goldenCount);
        }

        private void Update()
        {
            _angle += _degreesPerSecond * Time.deltaTime;
            int total = _normalOrbs.Count + _goldenOrbs.Count;
            int index = 0;
            PlaceAll(_goldenOrbs, total, ref index);
            PlaceAll(_normalOrbs, total, ref index);
        }

        private void PlaceAll(List<Transform> orbs, int total, ref int index)
        {
            foreach (Transform orb in orbs)
            {
                float angle = (_angle + 360f * index / total) * Mathf.Deg2Rad;
                float bob = Mathf.Sin(Time.time * _bobFrequency + index) * _bobAmplitude;
                orb.localPosition = new Vector3(Mathf.Cos(angle) * _radius, _height + bob, Mathf.Sin(angle) * _radius);
                index++;
            }
        }

        private void Resize(List<Transform> orbs, Transform prefab, int count)
        {
            while (orbs.Count < count)
            {
                orbs.Add(Instantiate(prefab, transform));
            }

            while (orbs.Count > count)
            {
                Transform last = orbs[orbs.Count - 1];
                orbs.RemoveAt(orbs.Count - 1);
                Destroy(last.gameObject);
            }
        }
    }
}
