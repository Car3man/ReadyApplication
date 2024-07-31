using System;
using System.Collections.Generic;

namespace ReadyApplication.Standard
{
    public class ServiceLocator
    {
        private readonly IDictionary<Type, object> _services = new Dictionary<Type, object>();

        public void Register<TService>(TService service)
        {
            var serviceType = typeof(TService);
            if (_services.ContainsKey(serviceType))
            {
                throw new ArgumentException("Service type already registered");
            }
            _services.Add(serviceType, service);
        }

        public TService Get<TService>()
        {
            var serviceType = typeof(TService);
            if (_services.TryGetValue(serviceType, out var service))
            {
                return (TService)service;
            }
            throw new InvalidOperationException("No service registered for type " + serviceType);
        }
    }
}