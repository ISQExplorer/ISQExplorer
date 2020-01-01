using System;

namespace ISQExplorer.Functional
{
    /// <summary>
    /// Contains a value, or an exception detailing why said value is not present.
    /// </summary>
    /// <typeparam name="T">The underlying value.</typeparam>
    public interface ITry<out T> : ITry<T, Exception>
    {
    }

    /// <summary>
    /// Contains a value, or an exception detailing why said value is not present.
    /// </summary>
    /// <typeparam name="T">The underlying value.</typeparam>
    /// <typeparam name="TException">The type of the exception. This is Exception by default.</typeparam>
    public interface ITry<out T, out TException> where TException : Exception
    {
        /// <summary>
        /// True if this Try has a value, false if not.
        /// </summary>
        bool HasValue { get; }

        /// <summary>
        /// The value if there is one. Otherwise this throws.
        /// </summary>
        T Value { get; }

        /// <summary>
        /// The exception if there is one. Otherwise this throws.
        /// </summary>
        TException Exception { get; }

        /// <summary>
        /// Executes one of two functions depending on if a value or an exception is present in this Try.
        /// </summary>
        /// <param name="val">The function to execute if a value is present.</param>
        /// <param name="ex">The function to execute if a value is not present.</param>
        /// <typeparam name="TRes">The value to return. Both functions must have the same return type.</typeparam>
        /// <returns>The return value of the function executed.</returns>
        TRes Match<TRes>(Func<T, TRes> val, Func<TException, TRes> ex);

        /// <summary>
        /// Executes one of two functions depending on if a value or an exception is present in this Try.
        /// </summary>
        /// <param name="val">The function to execute if a value is present.</param>
        /// <param name="ex">The function to execute if a value is not present.</param>
        void Match(Action<T> val, Action<TException> ex);

        /// <summary>
        /// Maps this Try to another type using the supplied function.
        /// </summary>
        /// <param name="func">A function converting this type to the desired type. If this function throws, the new Try will be constructed out of the thrown exception.</param>
        /// <typeparam name="TRes">The new type of the try.</typeparam>
        /// <returns>A new Try of the given type containing a value if this Try contains a value and the conversion function didn't throw, or the applicable exception if not.</returns>
        ITry<TRes, TException> Select<TRes>(Func<T, TRes> func);
    }
}