using System;
using System.Collections.Generic;

/// <summary>Stable ordered prefixes without materializing and sorting a whole candidate pool.</summary>
internal static class StableSmallSelection {
    internal static T[] Take<T, TKey>(IEnumerable<T> source, int count, Func<T, TKey> key,
            IComparer<TKey> comparer, Func<T, string> tieKey = null) {
        var values = new T[count];
        var keys = new TKey[count];
        var ties = tieKey == null ? null : new string[count];
        int used = 0;
        foreach (var value in source) {
            TKey score = key(value);
            string tie = tieKey?.Invoke(value);
            int position = used;
            for (int i = 0; i < used; i++) {
                int order = comparer.Compare(score, keys[i]);
                if (order == 0 && ties != null) order = StringComparer.Ordinal.Compare(tie, ties[i]);
                if (order < 0) { position = i; break; }
            }
            if (position >= count) continue;
            for (int i = Math.Min(used, count - 1); i > position; i--) {
                values[i] = values[i - 1]; keys[i] = keys[i - 1];
                if (ties != null) ties[i] = ties[i - 1];
            }
            values[position] = value; keys[position] = score;
            if (ties != null) ties[position] = tie;
            if (used < count) used++;
        }
        if (used != count) Array.Resize(ref values, used);
        return values;
    }
}
