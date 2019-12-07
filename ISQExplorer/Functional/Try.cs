using System;

namespace ISQExplorer.Functional
{
    public class Try<T, TException> where TException : Exception
    {
        private T _value;
        private TException _ex;
        public bool HasValue { get; }
        
        public Try(T val)
        {
            (_value, _ex, HasValue) = (val, default, true);
        }

        public Try(TException ex)
        {
            (_value, _ex, HasValue) = (default, ex, false);
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

        public TRes Match<TRes>(Func<T, TRes> val, Func<TException, TRes> ex) => HasValue ? val(Value) : ex(Exception);

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

        public static implicit operator Try<T, TException>(T val) => new Try<T, TException>(val);
        
        public static implicit operator Try<T, TException>(TException ex) => new Try<T, TException>(ex);

        public static explicit operator T(Try<T, TException> t) => t.Value;

        public static explicit operator TException(Try<T, TException> t) => t.Exception;

        public static implicit operator bool(Try<T, TException> t) => t.HasValue;
    }
}