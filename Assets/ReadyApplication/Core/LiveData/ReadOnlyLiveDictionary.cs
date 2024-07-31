using System;
using System.Collections;
using System.Collections.Generic;

namespace ReadyApplication.Core
{
    public interface IReadOnlyLiveDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>
    {
        event Action Updated;
        event Action<TKey> ItemUpdated;
    }
    
    public class ReadOnlyLiveDictionary<TKey, TValue> : IReadOnlyLiveDictionary<TKey, TValue>
    {
        private readonly ILiveDictionary<TKey,TValue> _liveDictionary;
        
        public event Action Updated
        {
            add => _liveDictionary.Updated += value;
            remove => _liveDictionary.Updated -= value;
        }
        
        public event Action<TKey> ItemUpdated
        {
            add => _liveDictionary.ItemUpdated += value;
            remove => _liveDictionary.ItemUpdated -= value;
        }

        public TValue this[TKey key] => _liveDictionary[key];
        public IEnumerable<TKey> Keys => _liveDictionary.Keys;
        public IEnumerable<TValue> Values => _liveDictionary.Values;
        public int Count => _liveDictionary.Count;

        public ReadOnlyLiveDictionary(ILiveDictionary<TKey, TValue> liveDictionary)
        {
            _liveDictionary = liveDictionary;
        }

        public bool ContainsKey(TKey key) 
            => _liveDictionary.ContainsKey(key);

        public bool TryGetValue(TKey key, out TValue value) 
            => _liveDictionary.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() 
            => GetEnumerator();
        
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() 
            => _liveDictionary.GetEnumerator();
    }
}