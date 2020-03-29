using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ISQExplorer.Models;
using Microsoft.Extensions.Primitives;

namespace ISQExplorer.Misc
{
    public static class Alg
    {
        public static T Max<T>(params T[] elems) =>
            elems.Aggregate((a, c) => Comparer<T>.Default.Compare(a, c) >= 0 ? a : c);

        public static T Min<T>(params T[] elems) =>
            elems.Aggregate((a, c) => Comparer<T>.Default.Compare(a, c) <= 0 ? a : c);
    }

    public static class Algorithms
    {
        public enum UpdatedBy
        {
            None = 0,
            Top = 1,
            Left = 2,
        }

        public static ISet<(string Substring, int Index)> LongestCommonSubstring(string s1, string s2)
        {
            // ReSharper disable CommentTypo
            // DYNAMICCCCCCCCCCCCC PROGRAMMINGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG

            var table = new int[s2.Length + 1, s1.Length + 1];
            var buf = new HashSet<(int, int)>();
            var longest = 0;

            for (var i = 1; i <= s2.Length; ++i)
            {
                for (var j = 1; j <= s1.Length; ++j)
                {
                    if (s1[j - 1] == s2[i - 1])
                    {
                        table[i, j] = table[i - 1, j - 1] + 1;
                        if (table[i, j] > longest)
                        {
                            longest = table[i, j];
                            buf.Clear();
                            buf.Add((j - table[i, j], table[i, j]));
                        }
                        else if (table[i, j] == longest)
                        {
                            buf.Add((j - table[i, j], table[i, j]));
                        }
                    }
                }
            }
            
            var ret = buf.Select(x => (s1.Substring(x.Item1, x.Item2), x.Item1)).ToHashSet();
            return ret;
        }
    }
}