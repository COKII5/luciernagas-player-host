using System.Collections.Generic;
using Fireflies.App;
using Fireflies.Config;
using Fireflies.Session;
using Fireflies.Sync;
using UnityEngine;

namespace Fireflies.Presentation
{
    public sealed class WorldPresenter : MonoBehaviour
    {
        [SerializeField] private SessionRunner _runner;
        [SerializeField] private PlayerPalette _palette;
        [SerializeField] private PlayerView _playerPrefab;
        [SerializeField] private FireflyView _fireflyPrefab;
        [SerializeField] private FireflyView _goldenFireflyPrefab;
        [SerializeField] private AltarView[] _altars;

        private readonly Dictionary<byte, PlayerView> _playerViews = new();
        private readonly Dictionary<ushort, FireflyView> _fireflyViews = new();
        private readonly Stack<FireflyView> _normalPool = new();
        private readonly Stack<FireflyView> _goldenPool = new();
        private readonly HashSet<byte> _seenPlayers = new();
        private readonly HashSet<ushort> _seenFireflies = new();
        private readonly List<byte> _stalePlayers = new();
        private readonly List<ushort> _staleFireflies = new();

        public bool TryGetPlayerPosition(byte playerId, out Vector3 position)
        {
            bool found = _playerViews.TryGetValue(playerId, out PlayerView view);
            position = found ? view.transform.position : Vector3.zero;
            return found;
        }

        public Vector3 GetAltarPosition(int slot)
        {
            return slot >= 0 && slot < _altars.Length ? _altars[slot].transform.position : Vector3.zero;
        }

        public void PulseAltar(int slot)
        {
            if (slot >= 0 && slot < _altars.Length)
            {
                _altars[slot].Pulse();
            }
        }

        private void LateUpdate()
        {
            IMatchSession session = _runner.Session;
            WorldRenderState state = _runner.RenderState;
            SyncPlayers(session, state);
            SyncFireflies(state);
            SyncAltars(session);
        }

        private void SyncPlayers(IMatchSession session, WorldRenderState state)
        {
            _seenPlayers.Clear();
            foreach (PlayerRenderState player in state.Players)
            {
                PlayerInfo info = session.FindPlayer(player.PlayerId);
                if (info == null)
                {
                    continue;
                }

                _seenPlayers.Add(player.PlayerId);
                PlayerView view = GetOrCreatePlayerView(info, session.LocalPlayerId);
                view.Apply(player, Time.deltaTime);
            }

            RemoveStalePlayers();
        }

        private PlayerView GetOrCreatePlayerView(PlayerInfo info, byte localPlayerId)
        {
            if (_playerViews.TryGetValue(info.PlayerId, out PlayerView view))
            {
                return view;
            }

            view = Instantiate(_playerPrefab, transform);
            view.name = $"Player_{info.PlayerName}";
            view.Configure(_palette.GetColor(info.Slot), info.PlayerId == localPlayerId);
            view.ResetMotion();
            _playerViews.Add(info.PlayerId, view);
            return view;
        }

        private void RemoveStalePlayers()
        {
            _stalePlayers.Clear();
            foreach (byte playerId in _playerViews.Keys)
            {
                if (!_seenPlayers.Contains(playerId))
                {
                    _stalePlayers.Add(playerId);
                }
            }

            foreach (byte playerId in _stalePlayers)
            {
                Destroy(_playerViews[playerId].gameObject);
                _playerViews.Remove(playerId);
            }
        }

        private void SyncFireflies(WorldRenderState state)
        {
            _seenFireflies.Clear();
            foreach (FireflyRenderState firefly in state.Fireflies)
            {
                _seenFireflies.Add(firefly.FireflyId);
                if (_fireflyViews.TryGetValue(firefly.FireflyId, out FireflyView view))
                {
                    view.MoveTo(firefly.Position);
                    continue;
                }

                view = TakeFromPool(firefly.IsGolden);
                view.Show(firefly.Position);
                _fireflyViews.Add(firefly.FireflyId, view);
            }

            RemoveStaleFireflies();
        }

        private void RemoveStaleFireflies()
        {
            _staleFireflies.Clear();
            foreach (KeyValuePair<ushort, FireflyView> entry in _fireflyViews)
            {
                if (!_seenFireflies.Contains(entry.Key))
                {
                    _staleFireflies.Add(entry.Key);
                }
            }

            foreach (ushort fireflyId in _staleFireflies)
            {
                ReturnToPool(_fireflyViews[fireflyId]);
                _fireflyViews.Remove(fireflyId);
            }
        }

        private FireflyView TakeFromPool(bool isGolden)
        {
            Stack<FireflyView> pool = isGolden ? _goldenPool : _normalPool;
            if (pool.Count > 0)
            {
                return pool.Pop();
            }

            FireflyView view = Instantiate(isGolden ? _goldenFireflyPrefab : _fireflyPrefab, transform);
            view.IsGolden = isGolden;
            return view;
        }

        private void ReturnToPool(FireflyView view)
        {
            view.gameObject.SetActive(false);
            (view.IsGolden ? _goldenPool : _normalPool).Push(view);
        }

        private void SyncAltars(IMatchSession session)
        {
            for (int slot = 0; slot < _altars.Length; slot++)
            {
                bool isOccupied = session.FindPlayerInSlot(slot) != null;
                _altars[slot].SetOwner(isOccupied, _palette.GetColor(slot));
            }
        }
    }
}
