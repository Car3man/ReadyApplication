using System;
using System.Collections.Generic;

namespace ReadyApplication.Core
{
    public interface ICache
    {
        bool Contains(string key);
        bool TryGet<T>(string key, out T value);
        T Get<T>(string key);
        void Set<T>(string key, T value, TimeSpan ttl);
        void Invalidate(string key);
        void Invalidate();
    }

    public class Cache : ICache
    {
        private readonly CacheDictionary<string, object> _cache;

        public Cache(TimeSpan ttl)
        {
            _cache = new CacheDictionary<string, object>(new Dictionary<string, object>(), ttl);
        }

        public bool Contains(string key)
        {
            return _cache.ContainsKey(key);
        }

        public bool TryGet<T>(string key, out T value)
        {
            if (_cache.TryGetValue(key, out object cacheValue) && cacheValue is T typedCacheValue)
            {
                value = typedCacheValue;
                return true;
            }

            value = default;
            return false;
        }

        public T Get<T>(string key)
        {
            T cacheItem = (T)_cache[key];
            return cacheItem;
        }

        public void Set<T>(string key, T value, TimeSpan ttl)
        {
            _cache.Set(key, value, ttl);
        }

        public void Invalidate(string key)
        {
            _cache.Remove(key);
        }

        public void Invalidate()
        {
            _cache.Clear();
        }
    }
}