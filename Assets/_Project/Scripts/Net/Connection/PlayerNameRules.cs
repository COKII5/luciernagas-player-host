using Fireflies.Net.Protocol;

namespace Fireflies.Net.Connection
{
    public static class PlayerNameRules
    {
        public static bool IsValid(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName) || playerName.Length > ProtocolConstants.MaxPlayerNameLength)
            {
                return false;
            }

            foreach (char character in playerName)
            {
                bool isAllowed = char.IsLetterOrDigit(character) || character == ' ' || character == '_' || character == '-';
                if (!isAllowed)
                {
                    return false;
                }
            }

            return true;
        }

        public static string Normalize(string playerName)
        {
            return playerName == null ? string.Empty : playerName.Trim();
        }
    }
}
