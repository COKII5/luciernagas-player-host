using System.Collections.Generic;
using Fireflies.Config;
using Fireflies.Game.State;
using Fireflies.Session;
using Fireflies.UI.Widgets;
using UnityEngine;
using UnityEngine.UIElements;

namespace Fireflies.UI.Overlays
{
    public sealed class ToastFeed : SessionScreen
    {
        private const long FadeOutMilliseconds = 400;

        [SerializeField] private PlayerPalette _palette;
        [SerializeField, Min(0.5f)] private float _visibleSeconds = 3.5f;
        [SerializeField, Range(1, 10)] private int _maxToasts = 5;

        private readonly Queue<(VisualElement toast, float hideTime)> _active = new();

        protected override string RootElementName => "toast-stack";

        protected override void BindElements()
        {
        }

        protected override void OnSessionAttached(IMatchSession session)
        {
            session.PlayerJoined += AnnounceJoin;
            session.PlayerLeft += AnnounceLeave;
            session.GameEventRaised += AnnounceEvent;
        }

        protected override void OnSessionDetached(IMatchSession session)
        {
            session.PlayerJoined -= AnnounceJoin;
            session.PlayerLeft -= AnnounceLeave;
            session.GameEventRaised -= AnnounceEvent;
            Root.Clear();
            _active.Clear();
        }

        private void Update()
        {
            while (_active.Count > 0 && _active.Peek().hideTime <= Time.time)
            {
                VisualElement toast = _active.Dequeue().toast;
                toast.AddToClassList("toast--hidden");
                toast.schedule.Execute(toast.RemoveFromHierarchy).ExecuteLater(FadeOutMilliseconds);
            }
        }

        private void AnnounceJoin(PlayerInfo player)
        {
            Show("icon--player", $"{player.PlayerName} entró a la sala", player.Slot);
        }

        private void AnnounceLeave(PlayerInfo player)
        {
            Show("icon--player", $"{player.PlayerName} salió de la sala", player.Slot);
        }

        private void AnnounceEvent(GameEvent gameEvent)
        {
            PlayerInfo player = Session.FindPlayer(gameEvent.PlayerId);
            switch (gameEvent.Type)
            {
                case GameEventType.Deposited when player != null:
                    Show("icon--firefly", $"{player.PlayerName} llevó {gameEvent.Amount} al altar", player.Slot);
                    break;
                case GameEventType.DashHit when player != null:
                    PlayerInfo victim = Session.FindPlayer(gameEvent.OtherPlayerId);
                    string lost = gameEvent.Amount > 0 ? $" y le tiró {gameEvent.Amount}" : string.Empty;
                    Show("icon--dash", $"{player.PlayerName} embistió a {victim?.PlayerName}{lost}", player.Slot);
                    break;
                case GameEventType.GoldenSpawned:
                    Show("icon--crown", $"¡Apareció una luciérnaga dorada! Vale {gameEvent.Amount}", -1);
                    break;
            }
        }

        private void Show(string iconClass, string message, int slot)
        {
            VisualElement toast = UiFactory.Element("panel-chip", "toast", "toast--hidden");
            VisualElement icon = UiFactory.Element("icon", "icon--small", iconClass);
            if (slot >= 0)
            {
                icon.style.unityBackgroundImageTintColor = _palette.GetColor(slot);
            }

            toast.Add(icon);
            toast.Add(UiFactory.Label(message));
            Root.Add(toast);
            toast.schedule.Execute(() => toast.RemoveFromClassList("toast--hidden"));
            _active.Enqueue((toast, Time.time + _visibleSeconds));

            while (_active.Count > _maxToasts)
            {
                _active.Dequeue().toast.RemoveFromHierarchy();
            }
        }
    }
}
