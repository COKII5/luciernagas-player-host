namespace Fireflies.Net.Reliability
{
    public static class SequenceMath
    {
        private const int HalfRange = 32768;

        // Tiene en cuenta el desborde: 2 es más nuevo que 65530 porque el contador dio la vuelta.
        public static bool IsNewer(ushort candidate, ushort reference)
        {
            int difference = candidate - reference;
            return (difference > 0 && difference <= HalfRange) || difference < -HalfRange;
        }
    }
}
