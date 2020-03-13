using System.Collections.Generic;
using ISQExplorer.Functional;

namespace ISQExplorer.Misc
{
    public class OptionalDictionary<TKey, TValue> : Dictionary<TKey, Optional<TValue>> where TKey : notnull
    {
        public new Optional<TValue> this[TKey key]
        {
            get
            {
                if (!ContainsKey(key))
                {
                    return new Optional<TValue>();
                }

                return base[key];
            }

            set => base[key] = value;
        }
    }
}