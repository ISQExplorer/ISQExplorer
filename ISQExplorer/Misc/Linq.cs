using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Moq;

namespace ISQExplorer.Misc
{
    public class PredicateComparer<T> : IComparer<T>
    {
        private readonly Func<T, T, int> _comp;

        public PredicateComparer(Func<T, T, int> comparer)
        {
            _comp = comparer;
        }

        public int Compare(T x, T y) => _comp(x, y);
    }

    public class PredicateEqualityComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, T, bool> _eq;
        private readonly Func<T, int> _hash;

        public PredicateEqualityComparer(Func<T, T, bool> equalityComparer, Func<T, int>? hashCodeGenerator = null)
        {
            _eq = equalityComparer;
            _hash = hashCodeGenerator ?? (e => e?.GetHashCode() ?? 0);
        }

        public bool Equals(T x, T y) => _eq(x, y);

        public int GetHashCode(T obj) => _hash(obj);
    }

    public static class Linq
    {
        /// <summary>
        /// Returns true if at least a certain number of the elements of an enumerable satisfy a predicate, false if not.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="number">The number of elements from the enumerable that must satisfy the predicate.</param>
        /// <param name="predicate">A function taking an element and returning true or false.</param>
        /// <typeparam name="T">The type of the elements in the enumerable.</typeparam>
        /// <returns>True if the predicate returned true for at least the given number of elements, false if not.</returns>
        public static bool AtLeast<T>(this IEnumerable<T> enumerable, int number, Func<T, bool> predicate)
        {
            if (number < 0)
            {
                return true;
            }

            var total = 0;
            foreach (var elem in enumerable)
            {
                if (predicate(elem))
                {
                    total++;
                }

                if (total >= number)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if at least a certain percentage of the elements of an enumerable satisfy a predicate, false if not.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="proportion">The proportion of elements that must satisfy the predicate. This is a value from 0.0 to 1.0.</param>
        /// <param name="predicate">A function taking an element and returning true or false.</param>
        /// <typeparam name="T">The type of the elements in the enumerable.</typeparam>
        /// <returns>True if the predicate returned true for at least the given number of elements, false if not.</returns>
        public static bool AtLeastPercent<T>(this IEnumerable<T> enumerable, double proportion, Func<T, bool> predicate)
        {
            if (proportion < 0 || proportion > 1)
            {
                throw new ArgumentException("Percent must be between 0 and 1.");
            }

            var accepted = 0;
            var total = 0;

            foreach (var elem in enumerable)
            {
                total++;
                if (predicate(elem))
                {
                    accepted++;
                }
            }

            return (double) accepted / total >= proportion;
        }

        /// <summary>
        /// Returns the distinct elements in the enumerable according to the given equality function.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="equalityPredicate">The function to use checking if two elements are equal.</param>
        /// <param name="hashPredicate">An optional hash function that should ensure that two equal elements have the same hashcode. By default this is T.GetHashCode().</param>
        /// <typeparam name="T">The type of the elements to compare.</typeparam>
        /// <returns>An enumerable containing the distinct elements.</returns>
        public static IEnumerable<T> Distinct<T>(this IEnumerable<T> enumerable, Func<T, T, bool> equalityPredicate, Func<T, int>? hashPredicate = null)
        {
            var seen = new HashSet<T>(new PredicateEqualityComparer<T>(equalityPredicate, hashPredicate));

            foreach (var elem in enumerable)
            {
                if (seen.Contains(elem))
                {
                    continue;
                }

                seen.Add(elem);
                yield return elem;
            }
        }

        /// <summary>
        /// Enumerates through each element of the input with both the element and the 0-based index.
        /// Equivalent to Python's enumerate().
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <typeparam name="T">The type of the enumerable's elements.</typeparam>
        /// <returns>An enumerable containing tuples of (element, index).</returns>
        public static IEnumerable<(int Index, T Elem)> Enumerate<T>(this IEnumerable<T> enumerable)
        {
            var i = 0;
            foreach (var y in enumerable)
            {
                yield return (Index: i, Elem: y);
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

        public static Task ForEachAsync<T>(this IEnumerable<T> enumerable, Func<T, Task> action) =>
            Task.WhenAll(enumerable.Select(action));

        /// <summary>
        /// Returns the index of a given element in an enumerable, or -1 if it was not found.
        /// </summary>
        /// <param name="enumerable">The input enumerable.</param>
        /// <param name="elem">The element to find.</param>
        /// <typeparam name="T">The type of the elements in the enumerable.</typeparam>
        /// <returns>The index of the given element, or -1 if it's not found.</returns>
        public static int Index<T>(this IEnumerable<T> enumerable, T elem)
        {
            var comp = EqualityComparer<T>.Default;
            foreach (var (i, e) in enumerable.Enumerate())
            {
                if (comp.Equals(e, elem))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Returns each line of the input reader as an enumerable.
        /// Useful for reading lines from a file.
        /// </summary>
        /// <param name="reader">The input reader.</param>
        /// <returns>An enumerable of lines from that reader.</returns>
        public static IEnumerable<string> Lines(this StreamReader reader)
        {
            string? line;
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
        /// Orders by a comparison function. 
        /// </summary>
        /// <param name="enumerable">The enumerable to order.</param>
        /// <param name="comparer">The comparison function. The function should return negative to put the left element before the right, positive to do the opposite, 0 if they are equal.</param>
        /// <typeparam name="T">The type of the enumerable's elements.</typeparam>
        /// <returns>A list of the elements sorted according to the given comparer.</returns>
        public static IList<T> OrderBy<T>(this IEnumerable<T> enumerable, Func<T, T, int> comparer)
        {
            var list = enumerable.ToList();
            list.Sort(new PredicateComparer<T>(comparer));
            return list;
        }

        /// <summary>
        /// Returns an enumerable of integers in a certain range. Equivalent to Python's range().
        /// </summary>
        /// <example>
        /// Range(5)        -> 0, 1, 2, 3, 4
        /// Range(2, 5)     -> 2, 3, 4
        /// Range(5, 2)     -> 5, 4, 3
        /// Range(2, 5, 2)  -> 2, 4
        /// Range(0, 5, 2)  -> 0, 2, 4
        /// Range(5, 0, -1) -> 5, 4, 3, 2, 1
        /// Range(0)        -> nothing
        /// Range(1, 1)     -> nothing
        /// Range(5, 2, 1)  -> nothing
        /// Range(2, 5, -1) -> nothing
        /// </example>
        /// The number to stop the range at.
        /// This number is not yielded, but all before it are.
        /// If this number is 0 or less, no integers are yielded.
        /// <returns>An enumerable of integers from 0 to stop.</returns>
        public static IEnumerable<int> Range(int stop)
        {
            for (var i = 0; i < stop; ++i)
            {
                yield return i;
            }
        }

        /// <summary>
        /// Returns an enumerable of integers in a certain range. Equivalent to Python's range().
        /// </summary>
        /// <example>
        /// Range(5)        -> 0, 1, 2, 3, 4
        /// Range(2, 5)     -> 2, 3, 4
        /// Range(5, 2)     -> 5, 4, 3
        /// Range(2, 5, 2)  -> 2, 4
        /// Range(0, 5, 2)  -> 0, 2, 4
        /// Range(5, 0, -1) -> 5, 4, 3, 2, 1
        /// Range(0)        -> nothing
        /// Range(1, 1)     -> nothing
        /// Range(5, 2, 1)  -> nothing
        /// Range(2, 5, -1) -> nothing
        /// </example>
        /// <param name="start">
        /// The number to start the range at. By default this is 0.
        /// </param>
        /// <param name="stop">
        /// The number to stop the range at. If this is equal to start, no integers are yielded.
        /// </param>
        /// <param name="step">
        /// The amount to advance each number in the range by.
        /// By default this is 1 if start is greater than stop, otherwise it is -1.
        /// If this value is positive and start is greater than stop, this will yield no numbers.
        /// This value can be negative, which will yield descending numbers if start is greater than stop, otherwise it will yield no numbers.
        /// If this value is set to 0, the default step is used.
        /// </param>
        /// <returns>An enumerable of integers from start to stop, going up/down by step.</returns>
        public static IEnumerable<int> Range(int start, int stop, int step = 0)
        {
            if (step == 0)
            {
                step = stop > start ? 1 : -1;
            }

            for (var i = start; step > 0 && i < stop || step < 0 && i > stop; i += step)
            {
                yield return i;
            }
        }

        /// <summary>
        /// Turns an enumerable into a randomly shuffled list.
        /// The order of the list is not cryptographically random.
        /// </summary>
        /// <param name="enumerable">The enumerable.</param>
        /// <param name="seed">The seed to use for the random number generator, or null to use a time-based seed.</param>
        /// <typeparam name="T">The type of the elements in the enumerable.</typeparam>
        /// <returns>A list containing the elements of the enumerable in shuffled order.</returns>
        public static IList<T> ToShuffledList<T>(this IEnumerable<T> enumerable, int? seed = null)
        {
            var list = enumerable.ToList();
            var rng = new Random(seed ?? (int) DateTime.UtcNow.Subtract(DateTime.UnixEpoch).TotalSeconds);

            for (var i = list.Count - 1; i > 0; --i)
            {
                var randIndex = rng.Next(0, i);
                var temp = list[i];
                list[i] = list[randIndex];
                list[randIndex] = temp;
            }

            return list;
        }
    }
}