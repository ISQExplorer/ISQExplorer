using System;

namespace ISQExplorer.Functional
{
    public class Optional<T>
    {
        private T _value;
        
        public bool HasValue { get; }

        public Optional()
        {
            (_value, HasValue) = (default, false);
        }

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

        public TRes Match<TRes>(Func<T, TRes> val, Func<TRes> empty) => HasValue ? val(Value) : empty();

        public TRes Match<TRes>(Func<T, TRes> val, TRes empty) => HasValue ? val(Value) : empty;
        
        public void Match(Action<T> val, Action empty)
        {
            if (HasValue)
            {
                val(Value);
            }
            else
            {
                empty();
            }
        }

        public static implicit operator Optional<T>(T val) => new Optional<T>(val);
        
        public static explicit operator T(Optional<T> val) => val.Value;

        public static implicit operator bool(Optional<T> val) => val.HasValue;
    }
}