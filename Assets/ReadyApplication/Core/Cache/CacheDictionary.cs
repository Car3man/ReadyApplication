using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ReadyApplication.Core
{
    public interface ICacheDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        public TimeSpan DefaultTtl { get; set; }
        void Add(TKey key, TValue value, TimeSpan ttl);
        void Add(KeyValuePair<TKey, TValue> item, TimeSpan ttl);
        public void Set(TKey key, TValue value, TimeSpan ttl);
    }
    
    public class CacheDictionary<TKey, TValue> : ICacheDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> _dictionary;
        private readonly IDictionary<TKey, DateTimeOffset> _expireDictionary;

        public TValue this[TKey key]
        {
            get => ContainsNotExpiredKey(key)
                ? _dictionary[key]
                : throw new KeyNotFoundException($"The key '{key}' was not found in the cache dictionary.");
            set
            {
                _dictionary[key] = value;
                _expireDictionary[key] = DateTimeOffset.UtcNow.Add(DefaultTtl);
            }
        }

        public ICollection<TKey> Keys => new CacheKeyCollection<TKey>(this);
        public ICollection<TValue> Values => new CacheValueCollection<TValue>(this);
        public int Count => _dictionary.Keys.Count(ContainsNotExpiredKey);
        public bool IsReadOnly => false;
        public TimeSpan DefaultTtl { get; set; }

        public CacheDictionary(TimeSpan ttl)
        {
            _dictionary = new Dictionary<TKey, TValue>();
            _expireDictionary = new Dictionary<TKey, DateTimeOffset>();
            DefaultTtl = ttl;
        }

        public CacheDictionary(IDictionary<TKey, TValue> dictionary, TimeSpan ttl)
        {
            _dictionary = dictionary;
            _expireDictionary = dictionary.ToDictionary(kv => kv.Key, _ => DateTimeOffset.UtcNow.Add(DefaultTtl));
            DefaultTtl = ttl;
        }

        private bool ContainsNotExpiredKey(TKey key)
        {
            return _dictionary.ContainsKey(key) && _expireDictionary[key] > DateTimeOffset.UtcNow;
        }

        private bool ContainsNotExpiredKeyValuePair(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.Contains(item) && _expireDictionary[item.Key] > DateTimeOffset.UtcNow;
        }

        public void Add(TKey key, TValue value)
        {
            _dictionary.Add(key, value);
            _expireDictionary.Add(key, DateTimeOffset.UtcNow.Add(DefaultTtl));
        }

        public void Add(TKey key, TValue value, TimeSpan ttl)
        {
            _dictionary.Add(key, value);
            _expireDictionary.Add(key, DateTimeOffset.UtcNow.Add(ttl));
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _dictionary.Add(item);
            _expireDictionary.Add(new KeyValuePair<TKey, DateTimeOffset>(item.Key, DateTimeOffset.UtcNow.Add(DefaultTtl)));
        }

        public void Add(KeyValuePair<TKey, TValue> item, TimeSpan ttl)
        {
            _dictionary.Add(item);
            _expireDictionary.Add(new KeyValuePair<TKey, DateTimeOffset>(item.Key, DateTimeOffset.UtcNow.Add(ttl)));
        }

        public void Set(TKey key, TValue value, TimeSpan ttl)
        {
            _dictionary[key] = value;
            _expireDictionary[key] = DateTimeOffset.UtcNow.Add(ttl);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            foreach (var key in this)
            {
                array[arrayIndex++] = key;
            }
        }

        public bool ContainsKey(TKey key)
        {
            return ContainsNotExpiredKey(key);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return ContainsNotExpiredKeyValuePair(item);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (ContainsNotExpiredKey(key))
            {
                value = _dictionary[key];
                return true;
            }

            value = default;
            return false;
        }

        public bool Remove(TKey key)
        {
            if (ContainsNotExpiredKey(key))
            {
                _dictionary.Remove(key);
                _expireDictionary.Remove(key);
                return true;
            }

            return false;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (ContainsNotExpiredKeyValuePair(item))
            {
                _dictionary.Remove(item.Key);
                _expireDictionary.Remove(item.Key);
                return true;
            }

            return false;
        }

        public void Clear()
        {
            _dictionary.Clear();
            _expireDictionary.Clear();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            foreach (KeyValuePair<TKey, TValue> item in _dictionary)
            {
                if (ContainsNotExpiredKeyValuePair(item))
                {
                    yield return item;
                }
            }
        }

        private abstract class CacheCollection<TItem> : ICollection<TItem>
        {
            protected readonly CacheDictionary<TKey, TValue> Cache;

            protected CacheCollection(CacheDictionary<TKey, TValue> cache)
            {
                Cache = cache;
            }

            public int Count => Cache.Count;
            public bool IsReadOnly => true;

            public void Add(TItem item)
            {
                throw new NotSupportedException();
            }

            public void CopyTo(TItem[] array, int arrayIndex)
            {
                foreach (var key in this)
                {
                    array[arrayIndex++] = key;
                }
            }

            public bool Remove(TItem item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public abstract bool Contains(TItem item);
            public abstract IEnumerator<TItem> GetEnumerator();
        }

        private class CacheKeyCollection<TItem> : CacheCollection<TItem> where TItem : TKey
        {
            public CacheKeyCollection(CacheDictionary<TKey, TValue> cache) : base(cache)
            {
            }

            public override bool Contains(TItem item)
            {
                return item != null && Cache.ContainsNotExpiredKey(item);
            }

            public override IEnumerator<TItem> GetEnumerator()
            {
                foreach (TKey key in Cache.Keys)
                {
                    if (Cache.ContainsNotExpiredKey(key))
                    {
                        yield return (TItem)key;
                    }
                }
            }
        }

        private class CacheValueCollection<TItem> : CacheCollection<TItem> where TItem : TValue
        {
            public CacheValueCollection(CacheDictionary<TKey, TValue> cache) : base(cache)
            {
            }

            public override bool Contains(TItem item)
            {
                if (item == null)
                {
                    return false;
                }

                foreach (KeyValuePair<TKey, TValue> kv in Cache._dictionary)
                {
                    if (Equals(kv.Value, item) && Cache.ContainsNotExpiredKey(kv.Key))
                    {
                        return true;
                    }
                }

                return false;
            }

            public override IEnumerator<TItem> GetEnumerator()
            {
                foreach (KeyValuePair<TKey, TValue> item in Cache)
                {
                    if (Cache.ContainsNotExpiredKey(item.Key))
                    {
                        yield return (TItem)item.Value;
                    }
                }
            }
        }
    }
}