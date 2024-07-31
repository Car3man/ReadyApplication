using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ReadyApplication.Core
{
    public interface ILiveDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        event Action Updated;
        event Action<TKey> ItemUpdated;
    }
    
    public class LiveDictionary<TKey, TValue> : ILiveDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey,TValue> _dictionary;

        public event Action Updated;
        public event Action<TKey> ItemUpdated; 
        
        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set
            {
                _dictionary[key] = value;
                ItemUpdated?.Invoke(key);
                Updated?.Invoke();
            }
        }
        
        public ICollection<TKey> Keys => _dictionary.Keys;
        public ICollection<TValue> Values => _dictionary.Values;
        public int Count => _dictionary.Count;
        public bool IsReadOnly => false;

        public LiveDictionary()
        {
            _dictionary = new Dictionary<TKey, TValue>();
        }

        public LiveDictionary(IDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
        }

        public void Add(TKey key, TValue value)
        {
            _dictionary.Add(key, value);
            ItemUpdated?.Invoke(key);
            Updated?.Invoke();
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            _dictionary.Add(item);
            ItemUpdated?.Invoke(item.Key);
            Updated?.Invoke();
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            _dictionary.CopyTo(array, arrayIndex);
        }

        public bool ContainsKey(TKey key)
        {
            return _dictionary.ContainsKey(key);
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _dictionary.Contains(item);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return _dictionary.TryGetValue(key, out value);
        }

        public bool Remove(TKey key)
        {
            bool removeResult = _dictionary.Remove(key);
            if (removeResult)
            {
                ItemUpdated?.Invoke(key);
                Updated?.Invoke();
            }
            return removeResult;
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            bool removeResult = _dictionary.Remove(item);
            if (removeResult)
            {
                ItemUpdated?.Invoke(item.Key);
                Updated?.Invoke();
            }
            return removeResult;
        }

        public void Clear()
        {
            List<TKey> clearedKeys = _dictionary.Keys.ToList();
            _dictionary.Clear();
            clearedKeys.ForEach(key => ItemUpdated?.Invoke(key));
            Updated?.Invoke();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _dictionary.GetEnumerator();
        }
    }
}