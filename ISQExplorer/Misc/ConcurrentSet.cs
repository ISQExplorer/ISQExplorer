using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ISQExplorer.Misc
{
    /// <summary>
    /// Contains a thread-safe set of elements.
    /// </summary>
    /// <typeparam name="T">The type of the contained elements.</typeparam>
    public class ConcurrentSet<T> : ICollection<T>
    {
        private readonly ConcurrentDictionary<T, byte> _dict;

        /// <summary>
        /// Constructs a blank ConcurrentSet.
        /// </summary>
        public ConcurrentSet()
        {
            _dict = new ConcurrentDictionary<T, byte>();
        }

        /// <summary>
        /// Constructs a blank ConcurrentSet out of the elements of another enumerable.
        /// </summary>
        /// <param name="other">The other enumerable to get elements out of.</param>
        public ConcurrentSet(IEnumerable<T> other) : this()
        {
            other.ForEach(x => Add(x));
        }
       
        /// <summary>
        /// Adds an item to the set.
        /// </summary>
        /// <param name="item">The item to add.</param>
        /// <returns>True if it was added, false if not.</returns>
        /// <exception cref="ArgumentException">The argument was null.</exception>
        public bool Add(T item)
        {
            if (item == null)
            {
                throw new ArgumentException("Cannot Add() a null value to a ConcurrentSet.");
            }

            if (Contains(item))
            {
                return false;
            }

            _dict[item] = 0;
            return true;
        }
        
        void ICollection<T>.Add(T item) => Add(item);

        /// <summary>
        /// Removes all items from the set.
        /// </summary>
        public void Clear() => _dict.Clear();

        /// <summary>
        /// Returns true if the set contains an item, false if not.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>True if it's in the set, false if not.</returns>
        public bool Contains(T item) => _dict.ContainsKey(item);

        /// <summary>
        /// Returns the number of elements in the set.
        /// </summary>
        public int Count => _dict.Count;

        /// <summary>
        /// Copies the elements in the set to an array.
        /// </summary>
        /// <param name="array">The array to copy to.</param>
        /// <param name="arrayIndex">The index to start copying to.</param>
        public void CopyTo(T[] array, int arrayIndex) => _dict.Keys.CopyTo(array, arrayIndex);

        /// <summary>
        /// Gets an enumerator of the elements in the set.
        /// </summary>
        /// <returns>An enumerator of the elements in the set.</returns>
        public IEnumerator<T> GetEnumerator() => _dict.Keys.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        
        public bool IsReadOnly => false;

        /// <summary>
        /// Removes an element from the set.
        /// </summary>
        /// <param name="item">The element to remove.</param>
        /// <returns>True if the element was successfully removed, false if it wasn't in the set.</returns>
        public bool Remove(T item) => _dict.Remove(item, out _);
    }
}