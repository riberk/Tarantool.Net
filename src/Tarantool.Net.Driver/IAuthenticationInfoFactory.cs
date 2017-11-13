using System;
using System.Threading.Tasks;

namespace Tarantool.Net.Driver
{
    public interface IAuthenticationInfoFactory
    {
        Task<AuthenticationInfo> Create(string userName, string password, ArraySegment<byte> salt);
    }
}