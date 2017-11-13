using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Tarantool.Net.Driver.Serialization;

namespace Tarantool.Net.Driver
{
    public class BinaryConnection : IBinaryConnection
    {
        [NotNull] private readonly IDeserializerResolver _deserializerResolver;
        [NotNull] private readonly IMessagePackStructureReaderFactory _messagePackStructureReaderFactory;
        [NotNull] private readonly IAuthenticationInfoFactory _authenticationInfoFactory;
        [NotNull] private readonly IReaderFactory _readerFactory;

        [NotNull] private readonly ConcurrentDictionary<ulong, TaskCompletionSource<ResponseInfo>> _continuations =
            new ConcurrentDictionary<ulong, TaskCompletionSource<ResponseInfo>>();

        [NotNull] private readonly ISerializer<Header> _headerSerializer;

        [NotNull] private readonly SemaphoreSlim _writerSemaphore;
        [NotNull] private Stream _writeStream;
        [NotNull] private NetworkStream _networkStream;
        [NotNull] private readonly ISerializerResolver _serializerResolver;
        [NotNull] private readonly ISerializer<int> _sizeSerializer;
        [NotNull] private readonly Socket _socket;
        private bool _isAuthenticated;
        private bool _isConnected;

        private long _syncCounter;
        [NotNull] private IReader _reader;

        public BinaryConnection(
                [NotNull] ISerializerResolver serializerResolver,
                [NotNull] IDeserializerResolver deserializerResolver,
                [NotNull] IReaderFactory readerFactory,
                [NotNull] IAuthenticationInfoFactory authenticationInfoFactory,
                [NotNull] IMessagePackStructureReaderFactory messagePackStructureReaderFactory
            )
        {
            _deserializerResolver = deserializerResolver;
            _messagePackStructureReaderFactory = messagePackStructureReaderFactory ?? throw new ArgumentNullException(nameof(messagePackStructureReaderFactory));
            _authenticationInfoFactory = authenticationInfoFactory ?? throw new ArgumentNullException(nameof(authenticationInfoFactory));
            _readerFactory = readerFactory ?? throw new ArgumentNullException(nameof(readerFactory));
            ConnectionId = Guid.NewGuid();
            _serializerResolver = serializerResolver ?? throw new ArgumentNullException(nameof(serializerResolver));
            _socket = new Socket(SocketType.Stream, ProtocolType.IP);
            _writerSemaphore = new SemaphoreSlim(1);
            _headerSerializer = serializerResolver.Resolve<Header>();
            _sizeSerializer = serializerResolver.Resolve<int>();
        }

        public Guid ConnectionId { get; }

        public ConnectionInfo ConnectionInfo { get; private set; }
        public AuthenticationInfo AuthenticationInfo { get; private set; }

        public async Task<ConnectionInfo> OpenAsync(string host, int port, CancellationToken ct)
        {
            if (_isConnected)
            {
                return ConnectionInfo;
            }
            await _socket.ConnectAsync(host, port);
            _networkStream = new NetworkStream(_socket, true);
            _writeStream = _networkStream;
            _reader = await _readerFactory.CreateAsync(_networkStream, _deserializerResolver, _messagePackStructureReaderFactory, ConnectionId, ct);
            ConnectionInfo = await _reader.ReadConnectionInfo(ct);
            _isConnected = true;
            ReadPacketAsync().FireAndForget();
            return ConnectionInfo;
        }

        public async Task<AuthenticationInfo> AuthenticateAsync(string userName, string password, CancellationToken ct)
        {
            if (!_isConnected)
            {
                throw new InvalidOperationException("Connection not opened");
            }
            if (_isAuthenticated)
            {
                return AuthenticationInfo;
            }

            var salt = new ArraySegment<byte>(ConnectionInfo.Salt, 0, ConnectionInfo.Salt.Length);
            var authenticationInfo = await _authenticationInfoFactory.Create(userName, password, salt);
            var responseTask = await SendRequestAsync(RequestType.Auth, null, authenticationInfo, ct);
            using (var result = await responseTask)
            {
                await _reader.ReadBodyAsync(result, ct);
            }
            _isAuthenticated = true;
            AuthenticationInfo = authenticationInfo;
            return AuthenticationInfo;
        }

        public async Task<IAsyncEnumerator<TResult>> Select<TKey, TResult>(SelectRequest<TKey> request, CancellationToken ct)
        {
            var responseTask = await SendRequestAsync(RequestType.Select, null, request, ct);
            var result = await responseTask;
            return new SelectBodyEnumerator<TResult>(_deserializerResolver.Resolve<TResult>(), result, _reader);
        }

        public void Dispose()
        {
            _networkStream?.Dispose();
            _socket?.Dispose();
        }

        private async Task ReadPacketAsync()
        {
            try
            {
                var result = await _reader.ReadNextAsync(CancellationToken.None);
                if (_continuations.TryGetValue(result.Header.Sync, out var cts))
                {
                    var error = await _reader.TryReadError(result, CancellationToken.None);
                    if (error.HasValue)
                    {
                        cts.SetException(new TarantoolException(result.Header.ErrorCode.Value, error.Value?.Message));
                    }
                    else
                    {
                        cts.SetResult(result);
                    }
                }
                else
                {
                    //TODO if not exists continuation by sync
                }
            }
            finally
            {
                ReadPacketAsync().FireAndForget();
            }
        }

        [ItemNotNull]
        private async Task<Task<ResponseInfo>> SendRequestAsync<TRequest>(
            RequestType requestType,
            uint? schemaId,
            TRequest request,
            CancellationToken ct)
        {
            var sync = NextSync();
            var responseTask = GetResponseTask(sync);
            var header = new Header(requestType, sync, schemaId);
            var bufferStream = new MemoryStream(5+13);
            bufferStream.Seek(5, SeekOrigin.Begin);
            await _headerSerializer.SerializeAsync(bufferStream, header, ct).ConfigureAwait(false);
            await _serializerResolver.Resolve<TRequest>().SerializeAsync(bufferStream, request, ct).ConfigureAwait(false);
            var headerAndBodySize = (int)bufferStream.Position - 5;
            bufferStream.Position = 0;
            await _sizeSerializer.SerializeAsync(bufferStream, headerAndBodySize, ct).ConfigureAwait(false);
            var packetSizeBytes = (int)bufferStream.Position;
            bufferStream.Position = 0;

            await _writerSemaphore.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                var buffer = bufferStream.GetBuffer();
                var ms = new MemoryStream();
                await ms.WriteAsync(buffer, 0, packetSizeBytes, ct);
                await ms.WriteAsync(buffer, 5, headerAndBodySize, ct);
                var sb = new StringBuilder();
                ms.ToArray().Aggregate(sb, (b, v) => b.Append(v.ToString("X2") + " "));
                var str = sb.ToString();
                await _writeStream.WriteAsync(buffer, 0, packetSizeBytes, ct);
                await _writeStream.WriteAsync(buffer, 5, headerAndBodySize, ct);
            }
            finally
            {
                _writerSemaphore.Release();
            }
            return responseTask;
        }

        public ulong NextSync()
        {
            Interlocked.Increment(ref _syncCounter);
            return (ulong) _syncCounter;
        }

        private Task<ResponseInfo> GetResponseTask(ulong sync)
        {
            var cs = new TaskCompletionSource<ResponseInfo>();
            if (!_continuations.TryAdd(sync, cs))
            {
                throw new InvalidOperationException($"Continuation with sync - {sync} already added");
            }
            return cs.Task;
        }

        
    }

    internal class SelectBodyEnumerator<T> : IAsyncEnumerator<T>
    {
        [NotNull] private readonly IDeserializer<T> _deserializer;
        private readonly ResponseInfo _responseInfo;
        [NotNull] private readonly IReader _reader;
        private int _count;
        private int _current;

        public SelectBodyEnumerator(
            [NotNull] IDeserializer<T> deserializer,
            ResponseInfo responseInfo,
            [NotNull] IReader reader
        )
        {
            _deserializer = deserializer ?? throw new ArgumentNullException(nameof(deserializer));
            _responseInfo = responseInfo;
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _count = -1;
            _current = 0;
        }
        /// <inheritdoc />
        public void Dispose() => _responseInfo.Dispose();

        /// <inheritdoc />
        public async Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            if (_count == -1)
            {
                var ms = new MemoryStream();
                await _reader.ReadBodyToAsync(_responseInfo, ms, cancellationToken);
                var sb = new StringBuilder();
                ms.ToArray().Aggregate(sb, (b, v) => b.Append(v.ToString("X2") + " "));
                var str = sb.ToString();
                var mapCount = await _reader.ReadMapHeader(cancellationToken);
                if (mapCount != 1)
                {
                    throw new TarantoolProtocolException($"Expected data is map with one value, but values count is {mapCount}");
                }
                var val = (Key)await _reader.ReadInt(cancellationToken);
                if (val != Key.Data)
                {
                    throw new TarantoolProtocolException($"Expected data key {Key.Data} but was {val}");
                }
                _count = await _reader.ReadArrayHeader(cancellationToken);
            }

            if(_count == 0)
            {
                return false;
            }
            if (_current == _count)
            {
                return false;
            }
            Current = await _reader.ReadAsync(_deserializer, cancellationToken);
            _current++;
            return true;
        }

        /// <inheritdoc />
        public T Current { get; private set; }
    }
}
