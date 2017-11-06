using JetBrains.Annotations;
using Tarantool.Net.Driver.Serialization;

namespace Tarantool.Net.Driver
{
    public interface IDeserializerResolver
    {
        [NotNull]
        IDeserializer<T> Resolve<T>();
    }
}
