using System;
using System.Threading.Tasks;
using ISQExplorer.Functional;
using NUnit.Framework;

namespace ISQExplorerTests
{
    public class ResultTests
    {
        [Test]
        public void CatchesError()
        {
            try
            {
                var res = Result.Of(() => throw new Exception());
                Assert.True(res.IsError);
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [Test]
        public void SucceedsWhenNotThrowing()
        {
            var res = Result.Of(() => { });
            Assert.False(res.IsError);
        }

        [Test]
        public async Task CatchesErrorAsync()
        {
            try
            {
                var res = await Result.OfAsync(async () =>
                {
                    await Task.CompletedTask;
                    throw new Exception();
                });
                Assert.True(res.IsError);
            }
            catch (Exception)
            {
                Assert.Fail();
            }
        }

        [Test]
        public async Task SucceedsWhenNotThrowingAsync()
        {
            var res = await Result.OfAsync(async () => { await Task.CompletedTask; });
            Assert.False(res.IsError);
        }
    }
}