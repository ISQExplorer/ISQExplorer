using System;
using ISQExplorer.Functional;

namespace ISQExplorer.Misc
{
    public static class Parse
    {
        /// <summary>
        /// Parses an integer, returning a Try[int] which contains the integer on success, or the exception on failure.
        /// </summary>
        /// <param name="s">The string to parse.</param>
        /// <returns>A Try containing the parsed int, or the exception thrown if the string could not be parsed.</returns>
        public static Try<int> Int(string s) => new Try<int>(() => int.Parse(s));
    }
}