using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tarantool.Net.Driver.Serialization;

namespace Tarantool.Net.Driver
{
    public class ReaderFactory : IReaderFactory
    {
        public async Task<IReader> CreateAsync(
            Stream source,
            IDeserializerResolver deserializerResolver,
            IMessagePackStructureReaderFactory messagePackStructureReaderFactory,
            Guid connectionId,
            CancellationToken ct)
        {
            return new Reader(source, deserializerResolver, await messagePackStructureReaderFactory.Create(source, connectionId, ct));
        }
    }
}
