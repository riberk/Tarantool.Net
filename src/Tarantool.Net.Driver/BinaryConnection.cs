using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using JetBrains.Annotations;
using Tarantool.Net.Abstractions;
using Tarantool.Net.Abstractions.Serialization;

namespace Tarantool.Net.Driver
{
    public class TarantoolConnectionOptions
    {
        public TarantoolConnectionOptions(string host, int port)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Port = port;
        }

        public TarantoolConnectionOptions(string host, int port, string userName, string password)
        {
            Host = host ?? throw new ArgumentNullException(nameof(host));
            Port = port;
            UserName = userName ?? throw new ArgumentNullException(nameof(userName));
            Password = password ?? throw new ArgumentNullException(nameof(password));
        }

        public string Host { get; }

        public int Port { get; }

        public string UserName { get; }

        public string Password { get; }
    }

    public class TarantoolConnectionStringBuilder
    {
        
    }

    public class StdResolver : ISerializerResolver, IDeserializerResolver
    {
        [NotNull] private readonly Dictionary<Type, object> _serializers = new Dictionary<Type, object>();
        [NotNull] private readonly Dictionary<Type, object> _deserializers = new Dictionary<Type, object>();

        public StdResolver(
            ISerializer<Header> headerSerializer,
            ISerializer<int> intSerializer,
            IDeserializer<Header> headerDeserializer,
            IDeserializer<int> intDeserializer,
            IDeserializer<ErrorResponse> errorResponseDeserializer
            )
        {
            Add(headerSerializer);
            Add(intSerializer);
            Add(headerDeserializer);
            Add(intDeserializer);
            Add(errorResponseDeserializer);
        }

        private void Add<T>(ISerializer<T> serializer)
        {
            _serializers.Add(typeof(T), serializer);
        }

        private void Add<T>(IDeserializer<T> serializer)
        {
            _deserializers.Add(typeof(T), serializer);
        }

        ISerializer<T> ISerializerResolver.Resolve<T>()
        {
            var type = typeof(T);
            if (!_serializers.TryGetValue(type, out var s))
            {
                throw new KeyNotFoundException($"Serializer for {type} not registred");
            }
            return (ISerializer<T>) s;
        }

        IDeserializer<T> IDeserializerResolver.Resolve<T>()
        {
            var type = typeof(T);
            if (!_deserializers.TryGetValue(type, out var s))
            {
                throw new KeyNotFoundException($"Deserializer for {type} not registred");
            }
            return (IDeserializer<T>)s;
        }
    }

    public class BinaryConnection : IBinaryConnection
    {
        [NotNull] private readonly ISerializerResolver _serializerResolver;
        [NotNull] private readonly IDeserializerResolver _deserializerResolver;
        [NotNull] private readonly Socket _socket;
        [NotNull] private readonly NetworkStream _writeStream;
        [NotNull] private readonly BufferedStream _readStream;
        private bool _isConnected = false;
        private bool _isAuthenticated = false;

        public BinaryConnection(
                [NotNull] ISerializerResolver serializerResolver,
                [NotNull] IDeserializerResolver deserializerResolver
        )
        {
            _serializerResolver = serializerResolver ?? throw new ArgumentNullException(nameof(serializerResolver));
            _deserializerResolver = deserializerResolver ?? throw new ArgumentNullException(nameof(deserializerResolver));
            _socket = new Socket(SocketType.Stream, ProtocolType.IP);
            _writeStream = new NetworkStream(_socket, true);
            _readerSemaphore = new SemaphoreSlim(1);
            _writerSemaphore = new SemaphoreSlim(1);
            _headerSerializer = serializerResolver.Resolve<Header>();
            _headerDeserializer = deserializerResolver.Resolve<Header>();
            _sizeSerializer = serializerResolver.Resolve<int>();
            _sizeDeserializer = deserializerResolver.Resolve<int>();
            _errorDeserializer = _deserializerResolver.Resolve<ErrorResponse>();
            _emptyBuffer = new byte[1024*16];
            _readStream = new BufferedStream(_writeStream, 1024*16);
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
            var buffer = new byte[128];
            var version = Encoding.ASCII.GetString(buffer, 0, 63);
            var saltBase64 = Encoding.ASCII.GetString(buffer, 64, 44);
            var salt = Convert.FromBase64String(saltBase64);
            ConnectionInfo = new ConnectionInfo(version, salt);
            _isConnected = true;
            ReadPacketAsync().FireAndForget();
            return ConnectionInfo;
        }

        private async Task ReadPacketAsync()
        {
            await _readerSemaphore.WaitAsync();
            try
            {
                var sizePacket = await _sizeDeserializer.DeserializeAsync(_writeStream, CancellationToken.None);
                if (sizePacket.DeserializedBytes < 5)
                {
                    await _readStream.ReadAsync(_readSizeEmptyBuffer, 0, 5 - sizePacket.DeserializedBytes);
                }
                var fullSize = sizePacket.Value;
                var header = await _headerDeserializer.DeserializeAsync(_writeStream, CancellationToken.None);
                var result = new ResponseContinuationResult(fullSize, header.DeserializedBytes, header.Value, _readerSemaphore);
                if (_continuations.TryGetValue(header.Value.Sync, out var cts))
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

                for (int i = 0; i < step1.Length; i++)
                {
                    buffer[i] = (byte)(step1[i] ^ step3[i]);
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

        private async Task EnsureSuccessResponse(ResponseContinuationResult result, CancellationToken ct)
        {
            if (result.Header.ErrorCode.HasValue)
            {
                var tarantoolError = await _errorDeserializer.DeserializeAsync(_writeStream, ct);
                throw new TarantoolException(result.Header.ErrorCode.Value, tarantoolError.Value.Message);
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
            var res = await _deserializerResolver.Resolve<T>().DeserializeAsync(_writeStream, ct);
            return res.Value;
        }

        [ItemNotNull]
        private async Task<Task<ResponseContinuationResult>> SendRequestAsync<TRequest>(RequestType requestType, uint? schemaId, TRequest request, CancellationToken ct)
        {
            var sync = NextSync();
            var responseTask = GetResponseTask(sync);
            var header = new Header(requestType, sync, schemaId);
            var buffer = new byte[5 + 13];
            var bufferStream = new MemoryStream(buffer);
            bufferStream.Seek(5, SeekOrigin.Begin);
            var headerBytes = await _headerSerializer.SerializeAsync(bufferStream, header, ct).ConfigureAwait(false);
            var bodyBytes = await _serializerResolver.Resolve<TRequest>().SerializeAsync(bufferStream, request, ct).ConfigureAwait(false);
            await _sizeSerializer.SerializeAsync(bufferStream, headerBytes + bodyBytes, ct).ConfigureAwait(false);

            await _readerSemaphore.WaitAsync(ct).ConfigureAwait(false);
            bufferStream.Position = 0;
            try
            {
                await _writeStream.WriteAsync(buffer, 0, 5 + headerBytes + bodyBytes, ct).ConfigureAwait(false);
            }
            finally
            {
                _readerSemaphore.Release();
            }
            return responseTask;
        }

        public Task<IAsyncEnumerable<TResult>> Select<TKey, TResult>(ISelectRequest<TKey> request, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _writeStream?.Dispose();
            _socket?.Dispose();
        }

        private long _syncCounter = 0;

        public ulong NextSync()
        {
            Interlocked.Increment(ref _syncCounter);
            return (ulong) _syncCounter;
        }

        [NotNull] private readonly ConcurrentDictionary<ulong, TaskCompletionSource<ResponseContinuationResult>> _continuations =
            new ConcurrentDictionary<ulong, TaskCompletionSource<ResponseContinuationResult>>();

        [NotNull] private readonly SemaphoreSlim _readerSemaphore;
        [NotNull] private readonly ISerializer<Header> _headerSerializer;
        [NotNull] private readonly ISerializer<int> _sizeSerializer;
        [NotNull] private readonly IDeserializer<ErrorResponse> _errorDeserializer;
        [NotNull] private readonly byte[] _emptyBuffer;
        [NotNull] private readonly IDeserializer<Header> _headerDeserializer;
        [NotNull] private readonly IDeserializer<int> _sizeDeserializer;
        [NotNull] private readonly byte[] _readSizeEmptyBuffer;
        [NotNull] private readonly SemaphoreSlim _writerSemaphore;

        private Task<ResponseContinuationResult> GetResponseTask(ulong sync)
        {
            var cs = new TaskCompletionSource<ResponseContinuationResult>();
            if (!_continuations.TryAdd(sync, cs))
            {
                throw new InvalidOperationException($"Continuation with sync - {sync} already added");
            }
            return cs.Task;
        }

        private struct ResponseContinuationResult  : IDisposable
        {
            [NotNull] private readonly SemaphoreSlim _semaphore;

            public ResponseContinuationResult(
                int fullSize,
                int headerSize,
                Header header,
                [NotNull] SemaphoreSlim semaphore) 
            {
                _semaphore = semaphore ?? throw new ArgumentNullException(nameof(semaphore));
                FullSize = fullSize;
                HeaderSize = headerSize;
                Header = header;
                BodySize = FullSize - headerSize;
            }

            public int FullSize { get; }

            public int HeaderSize { get; }

            public Header Header { get; }

            public int BodySize { get; }

            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    }

    public class TarantoolException : Exception
    {
        public ErrorCode ErrorCode { get; }

        public string TarantoolMessage { get; }

        public TarantoolException(ErrorCode errorCode, string tarantoolMessage)
            : base($"Tarantool response error \nCode: {errorCode}\nMessage: {tarantoolMessage}")
        {
            ErrorCode = errorCode;
            TarantoolMessage = tarantoolMessage;
        }

    }
    public interface ISerializerResolver
    {
        [NotNull]
        ISerializer<T> Resolve<T>();
    }

    public interface IDeserializerResolver
    {
        [NotNull]
        IDeserializer<T> Resolve<T>();
    }

    public interface IResponseReader
    {
        Task WaitNext();
    }
    
    public interface IBinaryConnection : IDisposable
    {
        ConnectionInfo ConnectionInfo { get; }

        AuthenticationInfo AuthenticationInfo { get; }

        Task<ConnectionInfo> OpenAsync(string host, int port);

        Task<AuthenticationInfo> AuthenticateAsync(string userName, string password, CancellationToken ct);

        Task<IAsyncEnumerable<TResult>> Select<TKey, TResult>(ISelectRequest<TKey> request, CancellationToken ct);
    }

    public interface ISelectRequest<out TKey>
    {
        uint SpaceId { get; }

        uint IndexId { get; }

        uint? Limit { get; }

        uint? Offset { get; }

        IteratorType Iterator { get; }

        TKey Key { get; }
    }

    public struct SelectRequest<TKey>  : ISelectRequest<TKey>
    {
        public SelectRequest(uint spaceId, uint indexId, uint? limit, uint? offset, IteratorType iterator, TKey key)
        {
            SpaceId = spaceId;
            IndexId = indexId;
            Limit = limit;
            Offset = offset;
            Iterator = iterator;
            Key = key;
        }

        public uint SpaceId { get; }
        public uint IndexId { get; }
        public uint? Limit { get; }
        public uint? Offset { get; }
        public IteratorType Iterator { get; }
        public TKey Key { get; }
    }

    public struct ErrorResponse
    {
        public ErrorResponse(string message)
        {
            Message = message;
        }

        public string Message { get; }
    }

    public struct AuthenticationInfo
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Object"></see> class.</summary>
        public AuthenticationInfo(string userName, byte[] chapSha1)
        {
            UserName = userName ?? throw new ArgumentNullException(nameof(userName));
            ChapSha1 = chapSha1 ?? throw new ArgumentNullException(nameof(chapSha1));
        }

        public string UserName { get; }

        public byte[] ChapSha1 { get; }
    }

    public struct ConnectionInfo
    {
        /// <summary>Initializes a new instance of the <see cref="T:System.Object"></see> class.</summary>
        public ConnectionInfo(string version, byte[] salt)
        {
            Version = version ?? throw new ArgumentNullException(nameof(version));
            Salt = salt ?? throw new ArgumentNullException(nameof(salt));
        }

        public string Version { get; }

        public byte[] Salt { get; }

    }
}
