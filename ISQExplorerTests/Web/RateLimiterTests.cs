using System.Diagnostics;
using System.Threading.Tasks;
using ISQExplorer.Web;
using NUnit.Framework;

namespace ISQExplorerTests
{
    public class RateLimiterTests
    {
        [Test]
        public async Task TestRateLimiter()
        {
            var rl = new RateLimiter(maxConcurrentTasks: 2, cycleTimeMillis: 500);
            var watch = new Stopwatch();

            async Task<T> TaskGen<T>(int ms, T returnVal)
            {
                await Task.Delay(ms);
                return returnVal;
            }

            async Task TaskGenVoid(int ms)
            {
                await Task.Delay(ms);
            }

            watch.Start();
            await rl.Run(() => TaskGenVoid(100));
            watch.Stop();
            Assert.GreaterOrEqual(watch.ElapsedMilliseconds, 500);


            watch.Restart();
            await rl.Run(() => TaskGenVoid(100));
            await rl.Run(() => TaskGenVoid(100));
            watch.Stop();
            Assert.GreaterOrEqual(watch.ElapsedMilliseconds, 500);

            watch.Restart();
            await Task.WhenAll(
                rl.Run(() => TaskGenVoid(100)),
                rl.Run(() => TaskGenVoid(100)),
                rl.Run(() => TaskGenVoid(100))
            );
            watch.Stop();
            Assert.GreaterOrEqual(watch.ElapsedMilliseconds, 1000);

            watch.Restart();
            await Task.WhenAll(
                rl.Run(() => TaskGenVoid(1000)),
                rl.Run(() => TaskGenVoid(900))
            );
            watch.Stop();
            Assert.GreaterOrEqual(watch.ElapsedMilliseconds, 1000);
            Assert.Less(watch.ElapsedMilliseconds, 1300);

            watch.Restart();
            await Task.WhenAll(
                rl.Run(() => TaskGenVoid(1000)),
                rl.Run(() => TaskGenVoid(1000)),
                rl.Run(() => TaskGenVoid(1000))
            );
            watch.Stop();
            Assert.GreaterOrEqual(watch.ElapsedMilliseconds, 2000);
            Assert.Less(watch.ElapsedMilliseconds, 2500);

            watch.Restart();
            var res = await Task.WhenAll(
                rl.Run(() => TaskGen(100, 0)),
                rl.Run(() => TaskGen(100, 1))
            );
            watch.Stop();
            Assert.GreaterOrEqual(watch.ElapsedMilliseconds, 500);
            Assert.Contains(0, res);
            Assert.Contains(1, res);
        }
    }
}