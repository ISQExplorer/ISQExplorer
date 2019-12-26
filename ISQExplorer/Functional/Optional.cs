#nullable enable
using System;
using System.Collections.Generic;

namespace ISQExplorer.Functional
{
    /// <summary>
    /// Represents a value that can or can not be present.
    /// </summary>
    /// <typeparam name="T">The type of the underlying optional value.</typeparam>
    public struct Optional<T> : IEquatable<Optional<T>>
    {
        private readonly T _value;

        public bool HasValue { get; }

        public Optional(T value)
        {
            (_value, HasValue) = (value, value != null);
        }

        public T Value
        {
            get
            {
                if (!HasValue)
                {
                    throw new InvalidOperationException("This Optional does not have a value.");
                }

                return _value;
            }
        }

        /// <summary>
        /// Executes one of two functions depending on if a value is or is not present in this Optional.
        /// </summary>
        /// <param name="val">The function to execute if a value is present.</param>
        /// <param name="empty">The function to execute if not.</param>
        /// <typeparam name="TRes">The return type of both functions. Both functions must have the same return type.</typeparam>
        /// <returns>The return value of the function executed.</returns>
        public TRes Match<TRes>(Func<T, TRes> val, Func<TRes> empty) => HasValue ? val(Value) : empty();

        /// <summary>
        /// Executes the given function if present, or returns the given value if not.
        /// </summary>
        /// <param name="val">The function to execute if a value is present.</param>
        /// <param name="empty">The value to return if not.</param>
        /// <typeparam name="TRes">The return value of the function and the type of the empty return. Both must have the same type.</typeparam>
        /// <returns>The return value of the function if a value is present, or the empty value if not.</returns>
        public TRes Match<TRes>(Func<T, TRes> val, TRes empty) => HasValue ? val(Value) : empty;

        /// <summary>
        /// Executes one of two functions depending on if a value is or is not present in this Optional.
        /// </summary>
        /// <param name="val">The function to execute if a value is present.</param>
        /// <param name="empty">The function to execute if not.</param>
        public void Match(Action<T> val, Action? empty = null)
        {
            if (HasValue)
            {
                val(Value);
            }
            else
            {
                empty?.Invoke();
            }
        }

        /// <summary>
        /// Maps this Optional to another type using the given function.
        /// </summary>
        /// <param name="func">The function converting T to TRes.</param>
        /// <typeparam name="TRes">The new optional type.</typeparam>
        /// <returns>An optional of the new type created using the supplied function. It will have a value if the current Optional has a value, and it will be empty if the current Optional is empty.</returns>
        public Optional<TRes> Select<TRes>(Func<T, TRes> func) => Match(
            val => func(val),
            () => new Optional<TRes>()
        );

        public static implicit operator Optional<T>(T val) => new Optional<T>(val);

        public static explicit operator T(Optional<T> val) => val.Value;

        public static implicit operator bool(Optional<T> val) => val.HasValue;

        public bool Equals(Optional<T> other)
        {
            return EqualityComparer<T>.Default.Equals(Value, other.Value) && HasValue == other.HasValue;
        }

        public override bool Equals(object? obj)
        {
            return obj is Optional<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_value, HasValue);
        }

        public static bool operator ==(Optional<T> left, Optional<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Optional<T> left, Optional<T> right)
        {
            return !left.Equals(right);
        }

        public static bool operator ==(Optional<T> left, T right)
        {
            return !left.HasValue && right == null ||
                   right != null && EqualityComparer<T>.Default.Equals(left.Value, right);
        }

        public static bool operator !=(Optional<T> left, T right)
        {
            return !(left == right);
        }

        public static bool operator ==(T left, Optional<T> right)
        {
            return right == left;
        }

        public static bool operator !=(T left, Optional<T> right)
        {
            return !(left == right);
        }
    }
}