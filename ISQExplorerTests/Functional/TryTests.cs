using System;
using System.Threading.Tasks;
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

        [Test]
        public async Task AsyncTest()
        {
            var tmp1 = await Try.OfAsync(() => Task.Run(() => "abc"));
            Assert.True(tmp1.HasValue);
            Assert.AreEqual(tmp1.Value, "abc");

            var tmp2 = await tmp1.SelectAsync(async x => await Task.Run(() => x.Length));
            Assert.True(tmp2.HasValue);
            Assert.AreEqual(tmp2.Value, 3);

            var tmp3 = await Try.OfAsync(() => Task.Run(() =>
            {
                throw new Exception("yeet");
#pragma warning disable 162
                return "abc";
#pragma warning restore 162
            }));
            Assert.False(tmp3.HasValue);
            Assert.AreEqual(tmp3.Exception.Message, "yeet");

            var tmp4 = await tmp1.SelectAsync(async x => await Task.Run(() =>
            {
                throw new Exception("yeetus");
#pragma warning disable 162
                return 3;
#pragma warning restore 162
            }));
            Assert.False(tmp4.HasValue);
            Assert.AreEqual(tmp4.Exception.Message, "yeetus");
        }
    }
}