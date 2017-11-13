using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Tarantool.Net.Driver.Serialization
{
    public interface IMessagePackStructureReaderFactory
    {
        Task<IMessagePackStructureReader> Create(Stream s, Guid connectionId, CancellationToken ct);
    }
}