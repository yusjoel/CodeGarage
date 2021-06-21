using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
// ReSharper disable PossibleMultipleEnumeration

namespace UnityReleaseNotesTool
{
    public static class EnumerableExtension
    {
        public static IOrderedEnumerable<T> OrderByAlphaNumeric<T>(this IEnumerable<T> source, Func<T, string> selector)
        {
            int max = source
                .SelectMany(i => Regex.Matches(selector(i), @"\d+").Select(m => (int?) m.Value.Length))
                .Max() ?? 0;

            return source.OrderByDescending(i =>
            {
                string replace = Regex.Replace(selector(i), @"\d+", m => m.Value.PadLeft(max, '0'));
                return replace;
            });
        }
    }
}