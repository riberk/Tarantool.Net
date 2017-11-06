using System.Threading.Tasks;

namespace Tarantool.Net.Driver
{
    public interface IResponseReader
    {
        Task WaitNext();
    }
}