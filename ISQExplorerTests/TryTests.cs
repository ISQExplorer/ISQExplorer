using ISQExplorer.Functional;
using NUnit.Framework;

namespace ISQExplorerTests
{
    public class TryTests
    {
        [Test]
        public void EqualityTest()
        {
            var tmp1 = new Try<string>("abc");

            Assert.True(tmp1 == "abc", "tmp1 != abc");
            Assert.True("abc" == tmp1, "abc != tmp1");
            Assert.True(tmp1 != "def", "tmp1 != def");
            Assert.True("def" != tmp1, "def != tmp1");

            var tmp2 = Try.Of("abc");

            Assert.True(tmp1 == tmp2, "tmp1 != tmp2");
            Assert.True(tmp2 == "abc", "tmp2 != abc");

            Assert.AreEqual(tmp1, tmp2);
        }
    }
}