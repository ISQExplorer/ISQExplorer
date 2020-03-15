using System.Linq;
using ISQExplorer.Misc;
using NUnit.Framework;

namespace ISQExplorerTests.Misc
{
    public class LinqTests
    {
        [Test]
        public void TestAtLeastPercent()
        {
            var ints = Linq.Range(1, 10).ToShuffledList(0);
            Assert.True(ints.AtLeastPercent(0.5, x => x >= 5));
            Assert.False(ints.AtLeastPercent(0.5, x => x > 5));
        }

        [Test]
        public void TestDistinct()
        {
            var ints = Linq.Range(0, 10).ToList();
            Assert.True(ints.Distinct((a, b) => a == b).Count() == 10);

            var ints2 = new[] {1, 1, 2, 2, 3, 2, 2};
            Assert.True(ints2.Distinct((a, b) => a == b).Count() == 3);
        }
        
        [Test]
        public void TestRange()
        {
            CollectionAssert.AreEqual(Linq.Range(5), new[] {0, 1, 2, 3, 4});
            CollectionAssert.AreEqual(Linq.Range(2, 5), new[] {2, 3, 4});
            CollectionAssert.AreEqual(Linq.Range(5, 2), new[] {5, 4, 3});
            CollectionAssert.AreEqual(Linq.Range(2, 5, 2), new[] {2, 4});
            CollectionAssert.AreEqual(Linq.Range(0, 5, 2), new[] {0, 2, 4});
            CollectionAssert.AreEqual(Linq.Range(5, 0, -2), new[] {5, 3, 1});
            CollectionAssert.AreEqual(Linq.Range(0), new int[] { });
            CollectionAssert.AreEqual(Linq.Range(1, 1), new int[] { });
            CollectionAssert.AreEqual(Linq.Range(5, 2, 1), new int[] { });
            CollectionAssert.AreEqual(Linq.Range(2, 5, -1), new int[] { });
        }
    }
}