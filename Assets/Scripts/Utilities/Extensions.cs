using System;
using System.Collections.Generic;
using System.Linq;

public static class MoreExtensions {
    public static IEnumerable<string> Split(this string str, int n) {
        if (string.IsNullOrEmpty(str) || n < 1) {
            throw new ArgumentException();
        }

        return Enumerable.Range(0, str.Length / n).Select(i => str.Substring(i * n, n));
    }
}