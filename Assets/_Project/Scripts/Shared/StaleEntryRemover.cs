using System;
using System.Collections.Generic;

namespace Fireflies.Shared
{
    public static class StaleEntryRemover
    {
        public static void RemoveUnseen<TKey, TValue>(
            Dictionary<TKey, TValue> entries,
            HashSet<TKey> seenKeys,
            List<TKey> buffer,
            Action<TValue> onRemoved)
        {
            buffer.Clear();
            foreach (TKey key in entries.Keys)
            {
                if (!seenKeys.Contains(key))
                {
                    buffer.Add(key);
                }
            }

            foreach (TKey key in buffer)
            {
                onRemoved(entries[key]);
                entries.Remove(key);
            }
        }
    }
}
