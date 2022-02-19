using System;
using System.Collections.Generic;
using System.Linq;

namespace Striked3D.Core
{
    public class ServiceRegistry
    {
        private readonly Dictionary<Type, IService> _registeredServices = new();

        public IService[] All => _registeredServices.Values.ToArray();

        /// <inheritdoc />
        public T Register<T>() where T : class, IService
        {
            lock (_registeredServices)
            {
                T newService = Activator.CreateInstance<T>();
                _registeredServices.Add(typeof(T), newService);

                return newService;
            }
        }

        /// <inheritdoc />
        public bool TryGetService(Type type, out IService service)
        {
            if (type == null) { throw new ArgumentNullException(nameof(type)); }

            lock (_registeredServices)
            {
                return _registeredServices.TryGetValue(type, out service);
            }
        }

        /// <inheritdoc />
        public T Get<T>() where T : class, IService
        {
            if (!TryGetService(typeof(T), out IService service))
            {
                return null;
            }

            return (T)service;
        }
    }
}
