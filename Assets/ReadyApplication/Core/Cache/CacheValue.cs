using System;
using System.Collections.Generic;

namespace ReadyApplication.Core
{
    public readonly struct CacheValue<T>
    {
        private readonly bool _hasValue;
        private readonly T _value;
        private readonly DateTime _expireTime;

        public CacheValue(T value, TimeSpan ttl)
        {
            _hasValue = true;
            _value = value;
            _expireTime = DateTime.UtcNow.Add(ttl);
        }

        public bool HasValue => _hasValue && _expireTime >= DateTime.UtcNow;

        public T Value => HasValue
            ? _value
            : throw new InvalidOperationException("The cache value has expired or null.");

        public override bool Equals(object other)
        {
            if (!_hasValue) return other == null;
            if (other == null) return false;
            return other is T otherConcrete && EqualityComparer<T>.Default.Equals(_value, otherConcrete);
        }

        public override int GetHashCode()
        {
            return _hasValue ? _value.GetHashCode() : 0;
        }

        public T GetValueOrDefault()
        {
            return _hasValue ? _value : default;
        }

        public T GetValueOrDefault(T defaultValue)
        {
            return _hasValue ? _value : defaultValue;
        }

        public static explicit operator T(CacheValue<T> value)
        {
            return value.Value;
        }

        public override string ToString()
        {
            return _hasValue ? _value.ToString() : "";
        }
    }
}