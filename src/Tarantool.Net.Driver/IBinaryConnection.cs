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

        Task<ConnectionInfo> OpenAsync(string host, int port);

        Task<AuthenticationInfo> AuthenticateAsync(string userName, string password, CancellationToken ct);

        Task<IAsyncEnumerable<TResult>> Select<TKey, TResult>(ISelectRequest<TKey> request, CancellationToken ct);
    }
}