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
        private const string ToastClass = "toast";
        private const string HiddenToastClass = "toast--hidden";
        private const string IconClass = "icon";
        private const string SmallIconClass = "icon--small";
        private const string PlayerIconClass = "icon--player";
        private const string FireflyIconClass = "icon--firefly";
        private const string DashIconClass = "icon--dash";
        private const string CrownIconClass = "icon--crown";

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
                toast.AddToClassList(HiddenToastClass);
                toast.schedule.Execute(toast.RemoveFromHierarchy).ExecuteLater(FadeOutMilliseconds);
            }
        }

        private void AnnounceJoin(PlayerInfo player)
        {
            Show(PlayerIconClass, $"{player.PlayerName} entró a la sala", player.Slot);
        }

        private void AnnounceLeave(PlayerInfo player)
        {
            Show(PlayerIconClass, $"{player.PlayerName} salió de la sala", player.Slot);
        }

        private void AnnounceEvent(GameEvent gameEvent)
        {
            PlayerInfo player = Session.FindPlayer(gameEvent.PlayerId);
            switch (gameEvent.Type)
            {
                case GameEventType.Deposited when player != null:
                    Show(FireflyIconClass, $"{player.PlayerName} llevó {gameEvent.Amount} al altar", player.Slot);
                    break;
                case GameEventType.DashHit when player != null:
                    PlayerInfo victim = Session.FindPlayer(gameEvent.OtherPlayerId);
                    string lost = gameEvent.Amount > 0 ? $" y le tiró {gameEvent.Amount}" : string.Empty;
                    Show(DashIconClass, $"{player.PlayerName} embistió a {victim?.PlayerName}{lost}", player.Slot);
                    break;
                case GameEventType.GoldenSpawned:
                    Show(CrownIconClass, $"¡Apareció una luciérnaga dorada! Vale {gameEvent.Amount}", -1);
                    break;
            }
        }

        private void Show(string iconClass, string message, int slot)
        {
            VisualElement toast = UiFactory.Element(UiFactory.ChipClass, ToastClass, HiddenToastClass);
            VisualElement icon = UiFactory.Element(IconClass, SmallIconClass, iconClass);
            if (slot >= 0)
            {
                icon.style.unityBackgroundImageTintColor = _palette.GetColor(slot);
            }

            toast.Add(icon);
            toast.Add(UiFactory.Label(message));
            Root.Add(toast);
            toast.schedule.Execute(() => toast.RemoveFromClassList(HiddenToastClass));
            _active.Enqueue((toast, Time.time + _visibleSeconds));

            while (_active.Count > _maxToasts)
            {
                _active.Dequeue().toast.RemoveFromHierarchy();
            }
        }
    }
}
