using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Tarantool.Net.Driver.Serialization;

namespace Tarantool.Net.Driver
{
    public interface IReaderFactory
    {
        [NotNull, ItemNotNull]
        Task<IReader> CreateAsync(
            [NotNull] Stream source,
            [NotNull] IDeserializerResolver deserializerResolver,
            [NotNull] IMessagePackStructureReaderFactory messagePackStructureReaderFactory,
            Guid connectionId,
            CancellationToken ct);
    }
}
