using System;
using System.Collections.Generic;
using Fireflies.Config;
using Fireflies.Session;
using UnityEngine.UIElements;

namespace Fireflies.UI.Widgets
{
    // Filas con retrato, nombre, color y ping. En modo administrador agrega expulsar y banear.
    public sealed class PlayerListView
    {
        private const string LocalSuffix = " (tú)";
        private const string HostCaption = "Anfitrión";

        private readonly VisualElement _container;
        private readonly PlayerPalette _palette;
        private readonly Action<byte> _onKick;
        private readonly Action<byte> _onBan;
        private readonly Dictionary<byte, Label> _pingLabels = new();

        public PlayerListView(VisualElement container, PlayerPalette palette, Action<byte> onKick = null, Action<byte> onBan = null)
        {
            _container = container;
            _palette = palette;
            _onKick = onKick;
            _onBan = onBan;
        }

        private bool ShowsAdminActions => _onKick != null && _onBan != null;

        public void Rebuild(IReadOnlyList<PlayerInfo> players, byte localPlayerId)
        {
            _container.Clear();
            _pingLabels.Clear();
            foreach (PlayerInfo player in players)
            {
                _container.Add(CreateRow(player, player.PlayerId == localPlayerId));
            }
        }

        public void RefreshPings(IReadOnlyList<PlayerInfo> players)
        {
            foreach (PlayerInfo player in players)
            {
                if (_pingLabels.TryGetValue(player.PlayerId, out Label pingLabel))
                {
                    UiFormat.ApplyPing(pingLabel, player.RoundTripMs, player.IsHost);
                }
            }
        }

        private VisualElement CreateRow(PlayerInfo player, bool isLocal)
        {
            VisualElement row = UiFactory.Element("player-row");
            VisualElement portrait = UiFactory.Element("player-row__portrait");
            portrait.style.backgroundImage = new StyleBackground(_palette.GetPortrait(player.Slot));
            row.Add(portrait);

            VisualElement info = UiFactory.Element("player-row__info");
            Label name = UiFactory.Label(player.PlayerName + (isLocal ? LocalSuffix : string.Empty), "player-row__name");
            name.style.color = _palette.GetColor(player.Slot);
            info.Add(name);
            string caption = player.IsHost ? $"{HostCaption} · {_palette.GetColorName(player.Slot)}" : _palette.GetColorName(player.Slot);
            info.Add(UiFactory.Label(caption, "player-row__meta"));
            row.Add(info);

            VisualElement ping = UiFactory.Element("player-row__ping");
            ping.Add(UiFactory.Element("icon", "icon--small", "icon--ping", "icon--muted"));
            Label pingLabel = UiFactory.Label(string.Empty);
            UiFormat.ApplyPing(pingLabel, player.RoundTripMs, player.IsHost);
            _pingLabels[player.PlayerId] = pingLabel;
            ping.Add(pingLabel);
            row.Add(ping);

            if (ShowsAdminActions && !player.IsHost)
            {
                byte playerId = player.PlayerId;
                row.Add(UiFactory.IconButton("icon--kick", "Expulsar", () => _onKick(playerId)));
                Button ban = UiFactory.IconButton("icon--ban", "Banear IP", () => _onBan(playerId));
                ban.Q(className: "icon").AddToClassList("icon--danger");
                row.Add(ban);
            }

            return row;
        }
    }
}
