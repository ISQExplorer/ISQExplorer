using System;
using System.Collections.Generic;

namespace ISQExplorer.Misc
{
    public class DefaultDictionary<TKey, TValue> : Dictionary<TKey, TValue> where TKey : notnull
    {
        private readonly Func<TValue> _defaultFactory;

        public DefaultDictionary(TValue defaultValue) : this(() => defaultValue)
        {
        }

        public DefaultDictionary(Func<TValue> defaultFactory)
        {
            _defaultFactory = defaultFactory;
        }

        public new TValue this[TKey key]
        {
            get
            {
                if (!ContainsKey(key))
                {
                    base[key] = _defaultFactory();
                }

                return base[key];
            }

            set => base[key] = value;
        }
    }
}