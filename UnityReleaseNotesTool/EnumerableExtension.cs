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

            // 用 Ordinal 比较：默认比较器跟随当前区域设置，在中文 Windows 上没必要冒这个险。
            return source.OrderByDescending(
                i => Regex.Replace(selector(i), @"\d+", m => m.Value.PadLeft(max, '0')),
                StringComparer.Ordinal);
        }
    }
}