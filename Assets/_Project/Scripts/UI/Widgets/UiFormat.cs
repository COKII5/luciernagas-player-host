using UnityEngine;
using UnityEngine.UIElements;

namespace Fireflies.UI.Widgets
{
    public static class UiFormat
    {
        private const int GoodPingMs = 60;
        private const int MediumPingMs = 150;
        private const string PingGoodClass = "ping--good";
        private const string PingMediumClass = "ping--medium";
        private const string PingBadClass = "ping--bad";

        public static string Clock(float seconds)
        {
            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            return $"{totalSeconds / 60}:{totalSeconds % 60:00}";
        }

        public static void ApplyPing(Label label, int roundTripMs, bool isHost)
        {
            label.RemoveFromClassList(PingGoodClass);
            label.RemoveFromClassList(PingMediumClass);
            label.RemoveFromClassList(PingBadClass);
            if (isHost)
            {
                label.text = "host";
                label.AddToClassList(PingGoodClass);
                return;
            }

            label.text = $"{roundTripMs} ms";
            string pingClass = roundTripMs <= GoodPingMs ? PingGoodClass : roundTripMs <= MediumPingMs ? PingMediumClass : PingBadClass;
            label.AddToClassList(pingClass);
        }
    }
}
