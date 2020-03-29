using ISQExplorer.Misc;
using NUnit.Framework;

namespace ISQExplorerTests.Misc
{
    public class AlgorithmsTests
    {
        public static object[] _lcsCases =
        {
            new object[] {"abcdef", "bcd", new[] {("bcd", 1)}},
            new object[] {"bcd", "abcdef", new[] {("bcd", 0)}},
            new object[] {"xabcxabcdx", "abcd", new[] {("abcd", 5)}},
            new object[] {"abcxabcdx", "abcd", new[] {("abcd", 4)}},
            new object[] {"abcxabcd", "abcd", new[] {("abcd", 4)}},
            new object[] {"abcabcd", "abcd", new[] {("abcd", 3)}},
            new object[] {"abcdabcd", "abcd", new[] {("abcd", 0), ("abcd", 4)}},
            new object[] {"abcdabc", "abcd", new[] {("abcd", 0)}},
            new object[] {"abcdabc", "xabcdx", new[] {("abcd", 0)}},
            new object[] {"abcdabc", "xxabcd", new[] {("abcd", 0)}},
            new object[] {"abcdabc", "abcxx", new[] {("abc", 0), ("abc", 4)}},
            new object[] {"abcdabc", "abcxxabcd", new[] {("abcd", 0)}},
            new object[] {"xabxcdx", "abcd", new[] {("ab", 1), ("cd", 4)}},
            new object[] {"abababab", "abxabx", new[] {("ab", 0), ("ab", 2), ("ab", 4), ("ab", 6)}},
            new object[] {"abxabxabxab", "abzabz", new[] {("ab", 0), ("ab", 3), ("ab", 6), ("ab", 9)}},
            new object[] {"aaa", "a", new[]{("a", 0), ("a", 1), ("a", 2)}}
        };

        [Test]
        [TestCaseSource(nameof(_lcsCases))]
        public void TestLongestCommonSubstring(string s1, string s2, (string Substring, int index)[] output)
        {
            CollectionAssert.AreEquivalent(output, Algorithms.LongestCommonSubstring(s1, s2));
        }
    }
}