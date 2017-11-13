using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Tarantool.Net.Driver
{
    public interface IBinaryConnection : IDisposable
    {
        ConnectionInfo ConnectionInfo { get; }

        AuthenticationInfo AuthenticationInfo { get; }

        Task<ConnectionInfo> OpenAsync(string host, int port, CancellationToken ct);

        Task<AuthenticationInfo> AuthenticateAsync(string userName, string password, CancellationToken ct);

        Task<IAsyncEnumerator<TResult>> Select<TKey, TResult>(SelectRequest<TKey> request, CancellationToken ct);
    }
}
