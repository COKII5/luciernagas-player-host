using System;
using System.Globalization;
using System.Net;

namespace Fireflies.App
{
    public sealed class LaunchOptions
    {
        public bool HostRequested { get; private set; }
        public IPAddress JoinAddress { get; private set; }
        public int? Port { get; private set; }
        public string PlayerName { get; private set; }
        public byte? Slot { get; private set; }
        public bool Autopilot { get; private set; }
        public int AutoStartPlayerCount { get; private set; }
        public float QuitAfterSeconds { get; private set; }
        public string CapturePath { get; private set; }
        public float CaptureAfterSeconds { get; private set; }

        public bool HasSessionRequest => HostRequested || JoinAddress != null;

        public static LaunchOptions FromEnvironment()
        {
            return Parse(Environment.GetCommandLineArgs());
        }

        private static LaunchOptions Parse(string[] arguments)
        {
            LaunchOptions options = new LaunchOptions();
            for (int i = 0; i < arguments.Length; i++)
            {
                string value = i + 1 < arguments.Length ? arguments[i + 1] : null;
                if (options.TryApply(arguments[i].ToLowerInvariant(), value))
                {
                    i++;
                }
            }

            return options;
        }

        // El valor de retorno le dice al bucle de argumentos si debe saltarse el siguiente.
        private bool TryApply(string option, string value)
        {
            switch (option)
            {
                case "-host":
                    HostRequested = true;
                    return false;
                case "-autopilot":
                    Autopilot = true;
                    return false;
                case "-join" when IPAddress.TryParse(value, out IPAddress address):
                    JoinAddress = address;
                    return true;
                case "-port" when int.TryParse(value, out int port) && port > 0 && port <= IPEndPoint.MaxPort:
                    Port = port;
                    return true;
                case "-name" when !string.IsNullOrWhiteSpace(value):
                    PlayerName = value;
                    return true;
                case "-slot" when byte.TryParse(value, out byte slot):
                    Slot = slot;
                    return true;
                case "-autostart" when int.TryParse(value, out int count) && count > 0:
                    AutoStartPlayerCount = count;
                    return true;
                case "-quitafter" when TryParseSeconds(value, out float quitSeconds):
                    QuitAfterSeconds = quitSeconds;
                    return true;
                case "-capture" when !string.IsNullOrWhiteSpace(value):
                    CapturePath = value;
                    return true;
                case "-captureafter" when TryParseSeconds(value, out float captureSeconds):
                    CaptureAfterSeconds = captureSeconds;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryParseSeconds(string value, out float seconds)
        {
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds) && seconds > 0f;
        }
    }
}
