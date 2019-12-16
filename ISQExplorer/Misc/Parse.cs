using System;
using ISQExplorer.Functional;

namespace ISQExplorer.Misc
{
    public static class Parse
    {
        public static Try<int> Int(string s) => new Try<int>(() => int.Parse(s));
    }
}