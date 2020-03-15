using System.Collections.Generic;
using ISQExplorer.Functional;

namespace ISQExplorer.Misc
{
    public class OptionalDictionary<TKey, TValue> : Dictionary<TKey, Optional<TValue>> where TKey : notnull
    {
        public new Optional<TValue> this[TKey key]
        {
            get => !ContainsKey(key) ? new Optional<TValue>() : base[key];

            set => base[key] = value;
        }
    }
}