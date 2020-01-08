using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ISQExplorer.Functional;

namespace ISQExplorer.Misc
{
    public static class Linq
    {
        /// <summary>
        /// Enumerates through each element of the input with both the element and the 0-based index.
        /// Equivalent to Python's enumerate().
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <typeparam name="T">The type of the enumerable's elements.</typeparam>
        /// <returns>An enumerable containing tuples of (element, index).</returns>
        public static IEnumerable<(T elem, int index)> Enumerate<T>(this IEnumerable<T> enumerable)
        {
            var i = 0;
            foreach (var y in enumerable)
            {
                yield return (elem: y, index: i);
                i++;
            }
        }

        /// <summary>
        /// Executes the given action for each element of the input.
        /// </summary>
        /// <param name="enumerable">The input enumerable.</param>
        /// <param name="action">The action to execute.</param>
        /// <typeparam name="T"></typeparam>
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var y in enumerable)
            {
                action(y);
            }
        }

        /// <summary>
        /// Returns each line of the input reader as an enumerable.
        /// Useful for reading lines from a file.
        /// </summary>
        /// <param name="reader">The input reader.</param>
        /// <returns>An enumerable of lines from that reader.</returns>
        public static IEnumerable<string> Lines(this StreamReader reader)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                yield return line;
            }
        }

        /// <summary>
        /// Returns true of none of the elements return true for the given function. Opposite of Any().
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="predicate">The function to check each element against.</param>
        /// <typeparam name="T">The type of the enumerable.</typeparam>
        /// <returns>True if none of the elements satisfy the condition. False if not.</returns>
        public static bool None<T>(this IEnumerable<T> enumerable, Func<T, bool> predicate) =>
            !enumerable.Any(predicate);

        /// <summary>
        /// Returns true if the enumerable contains no elements. Opposite of Any().
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <typeparam name="T">The type of the enumerable.</typeparam>
        /// <returns>True if the enumerable contains no elements. False if not.</returns>
        public static bool None<T>(this IEnumerable<T> enumerable) => !enumerable.Any();

        /// <summary>
        /// Executes a function for each element of the input enumerable, indicating if any of them threw an exception.
        /// This fails fast if any of the instances throw.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="action">The function.</param>
        /// <typeparam name="T">The type of the enumerable.</typeparam>
        /// <returns>An Optional[Exception] containing the first exception thrown.</returns>
        public static async Task<Optional<Exception>> TryAllParallel<T>(this IEnumerable<T> enumerable,
            Func<T, Task<Optional<Exception>>> action)
        {
            try
            {
                var res = await Task.WhenAll(enumerable.AsParallel().Select(action));
                return res.Any(x => x.HasValue) ? res.First(x => x.HasValue) : new Optional<Exception>();
            }
            catch (Exception e)
            {
                return e;
            }
        }

        /// <summary>
        /// Executes a function for each element of the input enumerable, indicating if any of them threw an exception.
        /// This fails fast if any of the instances throw.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="action">The function.</param>
        /// <typeparam name="T">The type of the enumerable.</typeparam>
        /// <returns>An Optional[Exception] containing the first exception thrown.</returns>
        public static async Task<Optional<Exception>> TryAllParallel<T>(this IEnumerable<T> enumerable,
            Func<T, Task> action)
        {
            try
            {
                await Task.WhenAll(enumerable.AsParallel().Select(action));
                return new Optional<Exception>();
            }
            catch (Exception e)
            {
                return e;
            }
        }

        /// <summary>
        /// Executes a function for each element of the input enumerable, indicating if any of them threw an exception.
        /// This fails fast if any of the instances throw.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="action">The function.</param>
        /// <typeparam name="T">The type of the enumerable.</typeparam>
        /// <returns>An Optional[Exception] containing the first exception thrown.</returns>
        public static async Task<Optional<Exception>> TryAllParallel<T>(this IEnumerable<T> enumerable,
            Action<T> action)
        {
            return await enumerable.TryAllParallel(y => Task.Run(() => action(y)));
        }
    }
}