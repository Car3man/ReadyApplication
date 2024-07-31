using System;
using System.Collections;
using System.Collections.Generic;

namespace ReadyApplication.Core
{
    public interface IRepositoryEntityCache<TKey, TValue> : IDictionary<TKey, TValue>
    {
        event Action Updated;
        event Action<TKey> EntityUpdated;
        public TimeSpan DefaultTtl { get; set; }
        void Add(KeyValuePair<TKey, TValue> item, TimeSpan ttl);
        void Set(TKey key, TValue value, TimeSpan ttl);
        bool TryGetNotExpiredValue(TKey key, out TValue value);
        bool HasMissedOrExpired(TKey key);
        DateTimeOffset GetEntityLifetime(TKey key);
        TimeSpan GetEntityAge(TKey key);
        TimeSpan GetEntityTtl(TKey key);
    }
    
    public class RepositoryEntityCache<TKey, TValue> : IRepositoryEntityCache<TKey, TValue>
    {
        private readonly ILiveDictionary<TKey, TValue> _entities;
        private readonly IDictionary<TKey, DateTimeOffset> _cacheTime;
        private readonly IDictionary<TKey, TimeSpan> _ttl;
        
        public TValue this[TKey key]
        {
            get => _entities[key];
            set
            {
                _entities[key] = value;
                _cacheTime[key] = DateTimeOffset.UtcNow;
                _ttl[key] = DefaultTtl;
            }
        }

        public ICollection<TKey> Keys => _entities.Keys;
        public ICollection<TValue> Values => _entities.Values;
        public int Count => _entities.Count;
        public bool IsReadOnly => false;
        
        public TimeSpan DefaultTtl { get; set; }

        public event Action Updated
        {
            add => _entities.Updated += value;
            remove => _entities.Updated -= value;
        }
        
        public event Action<TKey> EntityUpdated
        {
            add => _entities.ItemUpdated += value;
            remove => _entities.ItemUpdated -= value;
        }

        public RepositoryEntityCache()
        {
            _entities = new LiveDictionary<TKey, TValue>();
            _cacheTime = new Dictionary<TKey, DateTimeOffset>();
            _ttl = new Dictionary<TKey, TimeSpan>();
            DefaultTtl = TimeSpan.MaxValue;
        }

        public RepositoryEntityCache(TimeSpan ttl)
        {
            _entities = new LiveDictionary<TKey, TValue>();
            _cacheTime = new Dictionary<TKey, DateTimeOffset>();
            _ttl = new Dictionary<TKey, TimeSpan>();
            DefaultTtl = ttl;
        }

        public RepositoryEntityCache(ILiveDictionary<TKey, TValue> entities, TimeSpan ttl)
        {
            _entities = entities;
            _cacheTime = new Dictionary<TKey, DateTimeOffset>();
            _ttl = new Dictionary<TKey, TimeSpan>();
            DefaultTtl = ttl;
        }
        
        public DateTimeOffset GetEntityLifetime(TKey key)
        {
            return _cacheTime[key];
        }
        
        public TimeSpan GetEntityAge(TKey key)
        {
            return DateTimeOffset.UtcNow - _cacheTime[key];
        }
        
        public TimeSpan GetEntityTtl(TKey key)
        {
            return _ttl[key];
        }
        
        public void Add(TKey key, TValue value)
        {
            _entities.Add(key, value);
            _cacheTime.Add(key, DateTimeOffset.UtcNow);
            _ttl.Add(key, DefaultTtl);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _entities.Add(item);
            _cacheTime.Add(item.Key, DateTimeOffset.UtcNow);
            _ttl.Add(item.Key, DefaultTtl);
        }
        
        public void Add(KeyValuePair<TKey, TValue> item, TimeSpan ttl)
        {
            _entities.Add(item);
            _cacheTime.Add(item.Key, DateTimeOffset.UtcNow);
            _ttl.Add(item.Key, ttl);
        }
        
        public void Set(TKey key, TValue value, TimeSpan ttl)
        {
            _entities[key] = value;
            _cacheTime[key] = DateTimeOffset.UtcNow;
            _ttl[key] = ttl;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _entities.CopyTo(array, arrayIndex);
        }

        public bool ContainsKey(TKey key)
        {
            return _entities.ContainsKey(key);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _entities.Contains(item);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _entities.TryGetValue(key, out value);
        }
        
        public bool TryGetNotExpiredValue(TKey key, out TValue value)
        {
            if (_entities.TryGetValue(key, out value) && _cacheTime[key] + _ttl[key] >= DateTimeOffset.UtcNow)
            {
                return true;
            }
            value = default;
            return false;
        }
        
        public bool HasMissedOrExpired(TKey key)
        {
            if (!_entities.ContainsKey(key))
			{
				return true;
			}
            return _cacheTime[key] + _ttl[key] < DateTimeOffset.UtcNow;
        }

        public bool Remove(TKey key)
        {
            _cacheTime.Remove(key);
            _ttl.Remove(key);
            return _entities.Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (_entities.Remove(item))
            {
                _cacheTime.Remove(item.Key);
                _ttl.Remove(item.Key);
                return true;
            }
            return false;
        }

        public void Clear()
        {
            _entities.Clear();
            _cacheTime.Clear();
            _ttl.Clear();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() 
            => _entities.GetEnumerator();
    }
}