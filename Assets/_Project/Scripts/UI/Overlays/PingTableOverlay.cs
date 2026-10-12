using Fireflies.Config;
using Fireflies.Session;
using Fireflies.UI.Widgets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Fireflies.UI.Overlays
{
    // Mientras se mantiene TAB, cualquier jugador ve la latencia de todos.
    public sealed class PingTableOverlay : SessionScreen
    {
        [SerializeField] private PlayerPalette _palette;

        private PlayerListView _list;
        private bool _isOpen;

        protected override string RootElementName => "ping-table";

        protected override void BindElements()
        {
            _list = new PlayerListView(Root.Q("ping-table-list"), _palette);
        }

        protected override void OnSessionAttached(IMatchSession session)
        {
            session.RosterChanged += Refresh;
        }

        protected override void OnSessionDetached(IMatchSession session)
        {
            session.RosterChanged -= Refresh;
            SetOpen(false);
        }

        private void Update()
        {
            bool isHeld = Keyboard.current != null && Keyboard.current.tabKey.isPressed;
            SetOpen(isHeld && Session != null && Session.IsReady);
        }

        private void SetOpen(bool isOpen)
        {
            if (_isOpen == isOpen)
            {
                return;
            }

            _isOpen = isOpen;
            UiFactory.SetDisplayed(Root, isOpen);
            Refresh();
        }

        private void Refresh()
        {
            if (_isOpen && Session != null)
            {
                _list.Rebuild(Session.Players, Session.LocalPlayerId);
            }
        }
    }
}
