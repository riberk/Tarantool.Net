using System;
using System.Threading;
using JetBrains.Annotations;

namespace Tarantool.Net.Driver
{
    public struct ResponseInfo : IDisposable
    {
        [NotNull] private readonly SemaphoreSlim _semaphore;

        public ResponseInfo(
            int fullSize,
            int headerSize,
            [NotNull] Header header,
            [NotNull] SemaphoreSlim semaphore)
        {
            _semaphore = semaphore ?? throw new ArgumentNullException(nameof(semaphore));
            Header = header ?? throw new ArgumentNullException(nameof(header));
            BodySize = fullSize - headerSize;
        }

        [NotNull]
        public Header Header { get; }

        public int BodySize { get; }

        public void Dispose()
        {
            _semaphore.Release();
        }
    }
}
