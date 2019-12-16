using System;
using System.Collections.Generic;
using System.IO;

namespace ISQExplorer.Misc
{
    public static class Linq
    {
        public static IEnumerable<string> Lines(this StreamReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }

        public static IEnumerable<(T elem, int index)> Enumerate<T>(this IEnumerable<T> x)
        {
            var i = 0;
            foreach (var y in x)
            {
                yield return (elem: y, index: i);
                i++;
            }
        }

        public static void ForEach<T>(this IEnumerable<T> x, Action<T> func)
        {
            foreach (var y in x)
            {
                func(y);
            }
        }

        public static string Join(this IEnumerable<string> x, string delim) => string.Join(delim, x);
    }
}