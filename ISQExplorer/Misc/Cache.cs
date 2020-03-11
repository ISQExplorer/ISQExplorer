using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace ISQExplorer.Misc
{
    public class Cache<TKey, TValue>
    {
        private readonly ConcurrentDictionary<TKey, TValue> _dict;

        public Cache()
        {
            _dict = new ConcurrentDictionary<TKey, TValue>();
        }

        public TValue GetOrMake(TKey key, Func<TValue> valueFactory)
        {
            if (!_dict.ContainsKey(key))
            {
                _dict[key] = valueFactory();
            }

            return _dict[key];
        }

        public async Task<TValue> GetOrMakeAsync(TKey key, Func<Task<TValue>> valueFactory)
        {
            if (!_dict.ContainsKey(key))
            {
                _dict[key] = await valueFactory();
            }

            return _dict[key];
        }

        public void KeepWhere(Func<TValue, bool> predicate) => _dict.ToList().ForEach(k =>
        {
            var (key, value) = k;
            try
            {
                if (!predicate(value))
                {
                    _dict.TryRemove(key, out _);
                }
            }
            catch (Exception e)
            {
                _dict.TryRemove(key, out _);
            }
        });
    }
}