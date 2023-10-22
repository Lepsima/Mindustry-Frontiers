using Frontiers.Content.Maps;
using System;
using System.Collections.Generic;
using System.Linq;

public static class MoreExtensions {
    public static IEnumerable<string> SplitToChunks(this string str, int n) {
        if (string.IsNullOrEmpty(str) || n < 1) {
            throw new ArgumentException();
        }

        return Enumerable.Range(0, str.Length / n).Select(i => str.Substring(i * n, n));
    }

    public static bool EqualsOrInherits(this Type type, Type other) {
         return type == other || type.IsSubclassOf(other);
    }

    public static T[] AddRange<T>(this T[] array, T[] other) {
        if (other == null || other.Length == 0) return array;
        if (array == null || array.Length == 0) return other;

        T[] result = new T[array.Length + other.Length];
        for (int i = 0; i < array.Length; i++) result[i] = array[i];
        for (int i = 0; i < other.Length; i++) result[i + array.Length] = other[i];
        return result;
    }

    public static char ToChar(this TileType tileType) => Convert.ToChar(tileType == null ? 255 : tileType.id);

    public static TileType ToType(this char data) => TileLoader.GetTileTypeById(Convert.ToInt16(data));
}