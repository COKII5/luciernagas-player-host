using System;
using UnityEngine;

namespace Fireflies.Config
{
    [CreateAssetMenu(fileName = "PlayerPalette", menuName = "Fireflies/Player Palette")]
    public sealed class PlayerPalette : ScriptableObject
    {
        [SerializeField] private SlotAppearance[] _slots = Array.Empty<SlotAppearance>();

        public int SlotCount => _slots.Length;

        public Color GetColor(int slot)
        {
            return IsValid(slot) ? _slots[slot].Color : Color.white;
        }

        public string GetColorName(int slot)
        {
            return IsValid(slot) ? _slots[slot].DisplayName : string.Empty;
        }

        public Sprite GetPortrait(int slot)
        {
            return IsValid(slot) ? _slots[slot].Portrait : null;
        }

        public void Configure(SlotAppearance[] slots)
        {
            _slots = slots;
        }

        private bool IsValid(int slot)
        {
            return slot >= 0 && slot < _slots.Length;
        }

        [Serializable]
        public struct SlotAppearance
        {
            [SerializeField] private string _displayName;
            [SerializeField] private Color _color;
            [SerializeField] private Sprite _portrait;

            public SlotAppearance(string displayName, Color color, Sprite portrait)
            {
                _displayName = displayName;
                _color = color;
                _portrait = portrait;
            }

            public string DisplayName => _displayName;
            public Color Color => _color;
            public Sprite Portrait => _portrait;
        }
    }
}
