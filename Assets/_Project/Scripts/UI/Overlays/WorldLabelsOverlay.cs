using System.Collections.Generic;
using Fireflies.Config;
using Fireflies.Game.Simulation;
using Fireflies.Game.State;
using Fireflies.Presentation;
using Fireflies.Session;
using Fireflies.Shared;
using Fireflies.Sync;
using Fireflies.UI.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fireflies.UI.Overlays
{
    public sealed class WorldLabelsOverlay : SessionScreen
    {
        private const string LocalPlayerName = "Tú";

        [SerializeField] private PlayerPalette _palette;
        [SerializeField] private WorldPresenter _world;
        [SerializeField] private Camera _camera;
        [SerializeField] private float _nameHeight = 2.1f;
        [SerializeField] private float _scoreHeight = 2.4f;

        private readonly Dictionary<byte, Label> _nameLabels = new();
        private readonly Label[] _altarLabels = new Label[ArenaLayout.SlotCount];
        private readonly List<byte> _staleLabels = new();
        private readonly HashSet<byte> _seen = new();

        protected override string RootElementName => "world-labels";

        protected override void BindElements()
        {
            for (int slot = 0; slot < _altarLabels.Length; slot++)
            {
                _altarLabels[slot] = UiFactory.Label(string.Empty, "world-label", "world-label--score");
                _altarLabels[slot].pickingMode = PickingMode.Ignore;
                Root.Add(_altarLabels[slot]);
            }
        }

        private void LateUpdate()
        {
            bool hasWorld = Session != null && Session.IsReady && Session.Phase != MatchPhase.Results;
            UpdateNameLabels(hasWorld ? Runner.RenderState : null);
            UpdateAltarLabels(hasWorld ? Runner.RenderState : null);
        }

        private void UpdateNameLabels(WorldRenderState state)
        {
            _seen.Clear();
            if (state != null)
            {
                foreach (PlayerRenderState player in state.Players)
                {
                    PlayerInfo info = Session.FindPlayer(player.PlayerId);
                    if (info == null || !_world.TryGetPlayerPosition(player.PlayerId, out Vector3 position))
                    {
                        continue;
                    }

                    _seen.Add(player.PlayerId);
                    Label label = GetNameLabel(player.PlayerId);
                    label.text = player.PlayerId == Session.LocalPlayerId ? LocalPlayerName : info.PlayerName;
                    label.style.color = _palette.GetColor(info.Slot);
                    Place(label, position + Vector3.up * _nameHeight);
                }
            }

            StaleEntryRemover.RemoveUnseen(_nameLabels, _seen, _staleLabels, label => label.RemoveFromHierarchy());
        }

        private void UpdateAltarLabels(WorldRenderState state)
        {
            for (int slot = 0; slot < _altarLabels.Length; slot++)
            {
                PlayerInfo owner = state == null ? null : Session.FindPlayerInSlot(slot);
                PlayerRenderState player = default;
                bool isShown = owner != null && state.TryGetPlayer(owner.PlayerId, out player);
                Label label = _altarLabels[slot];
                UiFactory.SetDisplayed(label, isShown);
                if (!isShown)
                {
                    continue;
                }

                label.text = player.Score.ToString();
                label.style.color = _palette.GetColor(slot);
                Place(label, _world.GetAltarPosition(slot) + Vector3.up * _scoreHeight);
            }
        }

        private Label GetNameLabel(byte playerId)
        {
            if (!_nameLabels.TryGetValue(playerId, out Label label))
            {
                label = UiFactory.Label(string.Empty, "world-label");
                label.pickingMode = PickingMode.Ignore;
                Root.Add(label);
                _nameLabels.Add(playerId, label);
            }

            return label;
        }

        private void Place(VisualElement label, Vector3 worldPosition)
        {
            Vector2 panelPosition = RuntimePanelUtils.CameraTransformWorldToPanel(Root.panel, worldPosition, _camera);
            label.style.left = panelPosition.x;
            label.style.top = panelPosition.y;
        }
    }
}
