using System.Threading;
using ISQExplorer.Functional;
using ISQExplorer.Misc;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ISQExplorerTests
{
    public class StringsTest
    {
        private static object[] _captureTestCases =
        {
            new object[] { "abcdef", "a(bcd)e", "bcd", 1},
            new object[] { "abcdef", "a(bcd)ef", "bcd", 1},
            new object[] { "abcdef", "abcd(.*)$", "ef", 1},
            new object[] { "abcdef", "(a)", "a", 1},
            new object[] { "abcdef", "(ab).*(ef)", "ef", 2},
            new object[] { "abcdef", "a(bcd)efg", null, 1},
            new object[] { "abcdef", "a", null, 1},
            new object[] { "abcdef", "(g)", null, 1},
            new object[] { "abcdef", "(a)", null, 2},
        };
        
        [Test]
        [TestCaseSource(nameof(_captureTestCases))]
        public void CaptureTest(string input, string pattern, string? result, int number = 1)
        {
            var res = input.Capture(pattern, number);
            if (result == null)
            {
                Assert.False(res.HasValue);
            }
            else
            {
                Assert.True(res.HasValue);
                Assert.AreEqual(result, res.Value);
            }
        }
    }
}