using System;

namespace ISQExplorer.Functional
{
    /// <summary>
    /// Contains a value, or an exception detailing why said value is not present.
    /// </summary>
    /// <typeparam name="T">The underlying value.</typeparam>
    public class Try<T> : Try<T, Exception>
    {
        /// <summary>
        /// Constructs a Try out of the given value.
        /// </summary>
        /// <param name="val">The value.</param>
        public Try(T val) : base(val)
        {
        }

        /// <summary>
        /// Constructs a Try out of the given exception.
        /// </summary>
        /// <param name="ex">The exception.</param>
        public Try(Exception ex) : base(ex)
        {
        }

        /// <summary>
        /// Executes the given function, constructing the Try out of the return value, or the exception the function might throw.
        /// </summary>
        /// <param name="func">The function to execute.</param>
        public Try(Func<T> func) : base(func)
        {
        }
    }

    /// <summary>
    /// Contains a value, or an exception detailing why said value is not present.
    /// </summary>
    /// <typeparam name="T">The underlying value.</typeparam>
    /// <typeparam name="TException">The type of the exception. This is Exception by default.</typeparam>
    public class Try<T, TException> where TException : Exception
    {
        private T _value;
        private TException _ex;
        public bool HasValue { get; }

        /// <summary>
        /// Constructs a Try out of the given value.
        /// </summary>
        /// <param name="val">The value.</param>
        public Try(T val)
        {
            (_value, _ex, HasValue) = (val, default, true);
        }

        /// <summary>
        /// Constructs a Try out of the given exception.
        /// </summary>
        /// <param name="ex">The exception.</param>
        public Try(TException ex)
        {
            (_value, _ex, HasValue) = (default, ex, false);
        }

        /// <summary>
        /// Executes the given function, constructing the Try out of the return value, or the exception the function might throw.
        /// </summary>
        /// <param name="func">The function to execute.</param>
        public Try(Func<T> func)
        {
            try
            {
                (_value, _ex, HasValue) = (func(), default, true);
            }
            catch (TException ex)
            {
                (_value, _ex, HasValue) = (default, ex, false);
            }
        }

        public T Value
        {
            get
            {
                if (HasValue)
                {
                    return _value;
                }

                throw new InvalidOperationException($"This Try has an exception, not a value. Exception: '{_ex}'.");
            }
        }

        public TException Exception
        {
            get
            {
                if (!HasValue)
                {
                    return _ex;
                }

                throw new InvalidOperationException($"This Try has a value, not an exception. Value: '{_value}'.");
            }
        }

        /// <summary>
        /// Executes one of two functions depending on if a value or an exception is present in this Try.
        /// </summary>
        /// <param name="val">The function to execute if a value is present.</param>
        /// <param name="ex">The function to execute if a value is not present.</param>
        /// <typeparam name="TRes">The value to return. Both functions must have the same return type.</typeparam>
        /// <returns>The return value of the function executed.</returns>
        public TRes Match<TRes>(Func<T, TRes> val, Func<TException, TRes> ex) => HasValue ? val(Value) : ex(Exception);

        /// <summary>
        /// Executes one of two functions depending on if a value or an exception is present in this Try.
        /// </summary>
        /// <param name="val">The function to execute if a value is present.</param>
        /// <param name="ex">The function to execute if a value is not present.</param>
        public void Match(Action<T> val, Action<TException> ex)
        {
            if (HasValue)
            {
                val(Value);
            }
            else
            {
                ex(Exception);
            }
        }

        /// <summary>
        /// Maps this Try to another type using the supplied function.
        /// </summary>
        /// <param name="func">A function converting this type to the desired type. If this function throws, the new Try will be constructed out of the thrown exception.</param>
        /// <typeparam name="TRes">The new type of the try.</typeparam>
        /// <returns>A new Try of the given type containing a value if this Try contains a value and the conversion function didn't throw, or the applicable exception if not.</returns>
        public Try<TRes, TException> Select<TRes>(Func<T, TRes> func) => Match(
            val =>
            {
                try
                {
                    return new Try<TRes, TException>(func(Value));
                }
                catch (TException ex)
                {
                    return new Try<TRes, TException>(ex);
                }
            },
            ex => ex
        );

        public static implicit operator Try<T, TException>(T val) => new Try<T, TException>(val);

        public static implicit operator Try<T, TException>(TException ex) => new Try<T, TException>(ex);

        public static explicit operator T(Try<T, TException> t) => t.Value;

        public static explicit operator TException(Try<T, TException> t) => t.Exception;

        public static implicit operator bool(Try<T, TException> t) => t.HasValue;
    }
}