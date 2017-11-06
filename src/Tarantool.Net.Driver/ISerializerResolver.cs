using JetBrains.Annotations;
using Tarantool.Net.Driver.Serialization;

namespace Tarantool.Net.Driver
{
    public interface ISerializerResolver
    {
        [NotNull]
        ISerializer<T> Resolve<T>();
    }
}
