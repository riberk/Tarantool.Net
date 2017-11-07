using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Tarantool.Net.Driver.Serialization;

namespace Tarantool.Net.Driver
{
    public class BinaryConnection : IBinaryConnection
    {
        [NotNull] private readonly ConcurrentDictionary<ulong, TaskCompletionSource<ResponseContinuationResult>> _continuations =
            new ConcurrentDictionary<ulong, TaskCompletionSource<ResponseContinuationResult>>();

        [NotNull] private readonly IDeserializerResolver _deserializerResolver;
        [NotNull] private readonly byte[] _emptyBuffer;
        [NotNull] private readonly IDeserializer<ErrorResponse> _errorDeserializer;
        [NotNull] private readonly IDeserializer<Header> _headerDeserializer;
        [NotNull] private readonly ISerializer<Header> _headerSerializer;

        [NotNull] private readonly SemaphoreSlim _writerSemaphore;
        [NotNull] private readonly SemaphoreSlim _readerSemaphore;
        [NotNull] private readonly byte[] _readSizeEmptyBuffer;
        [NotNull] private StreamInformer _readStream;
        [NotNull] private Stream _writeStream;
        [NotNull] private NetworkStream _networkStream;
        [NotNull] private readonly ISerializerResolver _serializerResolver;
        [NotNull] private readonly IDeserializer<int> _sizeDeserializer;
        [NotNull] private readonly ISerializer<int> _sizeSerializer;
        [NotNull] private readonly Socket _socket;
        private bool _isAuthenticated;
        private bool _isConnected;

        private long _syncCounter;

        public BinaryConnection(
                [NotNull] ISerializerResolver serializerResolver,
                [NotNull] IDeserializerResolver deserializerResolver
            )
        {
            _serializerResolver = serializerResolver ?? throw new ArgumentNullException(nameof(serializerResolver));
            _deserializerResolver = deserializerResolver ?? throw new ArgumentNullException(nameof(deserializerResolver));
            _socket = new Socket(SocketType.Stream, ProtocolType.IP);

            _readerSemaphore = new SemaphoreSlim(1);
            _writerSemaphore = new SemaphoreSlim(1);
            _headerSerializer = serializerResolver.Resolve<Header>();
            _headerDeserializer = deserializerResolver.Resolve<Header>();
            _sizeSerializer = serializerResolver.Resolve<int>();
            _sizeDeserializer = deserializerResolver.Resolve<int>();
            _errorDeserializer = _deserializerResolver.Resolve<ErrorResponse>();
            _emptyBuffer = new byte[1024 * 16];
            _readSizeEmptyBuffer = new byte[8];
        }

        public ConnectionInfo ConnectionInfo { get; private set; }
        public AuthenticationInfo AuthenticationInfo { get; private set; }

        public async Task<ConnectionInfo> OpenAsync(string host, int port)
        {
            if (_isConnected)
            {
                return ConnectionInfo;
            }
            await _socket.ConnectAsync(host, port);
            _networkStream = new NetworkStream(_socket, true);
            _writeStream = _networkStream;
            _readStream = new StreamInformer(new BufferedStream(_networkStream, 1024 * 16));

            var buffer = new byte[128];
            var readed = await _readStream.ReadAsync(buffer, 0, buffer.Length);
            if (readed != buffer.Length)
            {
                throw new InvalidOperationException($"Invalid greeting message. Expected 128 bytes, actual {readed} bytes");
            }
            var version = Encoding.ASCII.GetString(buffer, 0, 63);
            var saltBase64 = Encoding.ASCII.GetString(buffer, 64, 63);
            var salt = Convert.FromBase64String(saltBase64);
            ConnectionInfo = new ConnectionInfo(version, salt);
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
            var buffer = new byte[40];
            using (var sha1 = SHA1.Create())
            {
                var step1 = sha1.ComputeHash(Encoding.UTF8.GetBytes(password));
                var step2 = sha1.ComputeHash(step1);
                Array.Copy(ConnectionInfo.Salt, buffer, 20);
                Array.Copy(step2, 0, buffer, 20, 20);
                var step3 = sha1.ComputeHash(buffer);

                for (var i = 0; i < step1.Length; i++)
                {
                    buffer[i] = (byte) (step1[i] ^ step3[i]);
                }
            }
            var authenticationInfo = new AuthenticationInfo(userName, buffer);
            var responseTask = await SendRequestAsync(RequestType.Auth, null, authenticationInfo, ct);
            using (var result = await responseTask)
            {
                await ReadResponse(result, ct);
            }
            _isAuthenticated = true;
            AuthenticationInfo = authenticationInfo;
            return AuthenticationInfo;
        }

        public Task<IAsyncEnumerable<TResult>> Select<TKey, TResult>(ISelectRequest<TKey> request, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _networkStream?.Dispose();
            _socket?.Dispose();
        }

        private async Task ReadPacketAsync()
        {
            await _readerSemaphore.WaitAsync();
            try
            {
                _readStream.ClearState();
                var sizePacket = await _sizeDeserializer.DeserializeAsync(_readStream, CancellationToken.None);
                if (_readStream.ReadedBytes < 5)
                {
                    await _readStream.ReadAsync(_readSizeEmptyBuffer, 0, 5 - _readStream.ReadedBytes);
                }
                var fullSize = sizePacket;
                var header = await _headerDeserializer.DeserializeAsync(_readStream, CancellationToken.None);
                var headerSize = _readStream.ReadedBytes - 5;
                var result = new ResponseContinuationResult(fullSize, headerSize, header, _readerSemaphore);
                if (_continuations.TryGetValue(header.Sync, out var cts))
                {
                    cts.SetResult(result);
                }
            }
            catch
            {
                //TODO exception
                _readerSemaphore.Release();
            }
        }

        private async Task EnsureSuccessResponse(ResponseContinuationResult result, CancellationToken ct)
        {
            if (result.Header.ErrorCode.HasValue)
            {
                var tarantoolError = await _errorDeserializer.DeserializeAsync(_readStream, ct);
                throw new TarantoolException(result.Header.ErrorCode.Value, tarantoolError.Message);
            }
        }

        private async Task ReadToEndResponse(ResponseContinuationResult result, int bodyReadedBytes, CancellationToken ct)
        {
            var unreaded = result.BodySize - bodyReadedBytes;
            while (unreaded != 0)
            {
                var needReadBytes = unreaded < _emptyBuffer.Length ? unreaded : _emptyBuffer.Length;
                unreaded -= await _readStream.ReadAsync(_emptyBuffer, 0, needReadBytes);
            }
        }

        private async Task ReadResponse(ResponseContinuationResult result, CancellationToken ct)
        {
            await EnsureSuccessResponse(result, ct);
            await ReadToEndResponse(result, 0, ct);
        }

        private async Task<T> ReadResponse<T>(ResponseContinuationResult result, CancellationToken ct)
        {
            await EnsureSuccessResponse(result, ct);
            var beforeBytesReaded = _readStream.ReadedBytes;
            var res = await _deserializerResolver.Resolve<T>().DeserializeAsync(_readStream, ct);
            
            await ReadToEndResponse(result, _readStream.ReadedBytes - beforeBytesReaded, ct);
            return res;
        }

        [ItemNotNull]
        private async Task<Task<ResponseContinuationResult>> SendRequestAsync<TRequest>(
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
            bufferStream.Position = 0;

            await _writerSemaphore.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                await bufferStream.CopyToAsync(_writeStream, 81920, ct);
                //await _writeStream.WriteAsync(buffer, 0, (int)bufferStream.Length, ct).ConfigureAwait(false);
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

        private Task<ResponseContinuationResult> GetResponseTask(ulong sync)
        {
            var cs = new TaskCompletionSource<ResponseContinuationResult>();
            if (!_continuations.TryAdd(sync, cs))
            {
                throw new InvalidOperationException($"Continuation with sync - {sync} already added");
            }
            return cs.Task;
        }

        private struct ResponseContinuationResult : IDisposable
        {
            [NotNull] private readonly SemaphoreSlim _semaphore;

            public ResponseContinuationResult(
                int fullSize,
                int headerSize,
                Header header,
                [NotNull] SemaphoreSlim semaphore)
            {
                _semaphore = semaphore ?? throw new ArgumentNullException(nameof(semaphore));
                Header = header;
                BodySize = fullSize - headerSize;
            }

            public Header Header { get; }

            public int BodySize { get; }

            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    }
}
