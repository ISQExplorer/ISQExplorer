using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ISQExplorer.Functional;

namespace ISQExplorer.Web
{
    /// <summary>
    /// Limits the amount of tasks running to a certain number in a certain time period.
    /// </summary>
    public class RateLimiter
    {
        /// <summary>
        /// The maximum number of concurrent tasks this RateLimiter will run.
        /// </summary>
        public int MaxConcurrentTasks { get; }

        /// <summary>
        /// The minimum amount of time a Task spends running, in milliseconds.
        /// </summary>
        public int CycleTimeMillis { get; }

        private readonly SemaphoreSlim _semaphore;


        /// <summary>
        /// Constructs a RateLimiter.
        /// </summary>
        /// <param name="maxConcurrentTasks">The maximum number of Tasks that can run simultaneously. By default this is 2.</param>
        /// <param name="cycleTimeMillis">The minimum amount of time a Task spends running in milliseconds. By default this is 1000ms.</param>
        public RateLimiter(int maxConcurrentTasks = 10, int cycleTimeMillis = 1000)
        {
            (MaxConcurrentTasks, CycleTimeMillis) = (maxConcurrentTasks, cycleTimeMillis);
            _semaphore = new SemaphoreSlim(maxConcurrentTasks);
        }

        /// <summary>
        /// Registers a Task to run when the RateLimiter has an available spot.
        /// </summary>
        /// <param name="func">A function that returns the Task to be run.</param>
        public async Task Run(Func<Task> func) => await Run(async () =>
        {
            await func();
            return true;
        });

        /// <summary>
        /// Registers a Task to run when the RateLimiter has an available spot.
        /// </summary>
        /// <param name="func">A function that returns the Task to be run.</param>
        /// <typeparam name="T">The return type of the Task to run.</typeparam>
        /// <returns>The return value of the Task.</returns>
        public async Task<T> Run<T>(Func<Task<T>> func)
        {
            await _semaphore.WaitAsync();

            var watch = new Stopwatch();
            watch.Start();
            var res = await func();
            watch.Stop();

            if (watch.ElapsedMilliseconds < CycleTimeMillis)
            {
                await Task.Delay((int) (CycleTimeMillis - watch.ElapsedMilliseconds + 6));
            }

            _semaphore.Release();
            return res;
        }
    }
}