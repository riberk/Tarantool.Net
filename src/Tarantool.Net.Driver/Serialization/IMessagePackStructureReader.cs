using System.Threading;
using System.Threading.Tasks;

namespace Tarantool.Net.Driver.Serialization
{
    public interface IMessagePackStructureReader
    {
        Task<long> ReadMapHeader(CancellationToken ct);

        Task<long> ReadArrayHeader(CancellationToken ct);
        Task<int> ReadInt(CancellationToken ct);
    }
}
