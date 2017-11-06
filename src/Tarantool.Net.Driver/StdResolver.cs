using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Tarantool.Net.Driver.Serialization;

namespace Tarantool.Net.Driver
{
    public class StdResolver : ISerializerResolver, IDeserializerResolver
    {
        [NotNull] private readonly Dictionary<Type, object> _serializers = new Dictionary<Type, object>();
        [NotNull] private readonly Dictionary<Type, object> _deserializers = new Dictionary<Type, object>();

        public StdResolver(
                ISerializer<Header> headerSerializer,
                ISerializer<int> intSerializer,
                IDeserializer<Header> headerDeserializer,
                IDeserializer<int> intDeserializer,
                IDeserializer<ErrorResponse> errorResponseDeserializer
            )
        {
            Add(headerSerializer);
            Add(intSerializer);
            Add(headerDeserializer);
            Add(intDeserializer);
            Add(errorResponseDeserializer);
        }

        private void Add<T>(ISerializer<T> serializer)
        {
            _serializers.Add(typeof(T), serializer);
        }

        private void Add<T>(IDeserializer<T> serializer)
        {
            _deserializers.Add(typeof(T), serializer);
        }

        ISerializer<T> ISerializerResolver.Resolve<T>()
        {
            var type = typeof(T);
            if (!_serializers.TryGetValue(type, out var s))
            {
                throw new KeyNotFoundException($"Serializer for {type} not registred");
            }
            return (ISerializer<T>) s;
        }

        IDeserializer<T> IDeserializerResolver.Resolve<T>()
        {
            var type = typeof(T);
            if (!_deserializers.TryGetValue(type, out var s))
            {
                throw new KeyNotFoundException($"Deserializer for {type} not registred");
            }
            return (IDeserializer<T>)s;
        }
    }
}
