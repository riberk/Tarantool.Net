using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Tarantool.Net.Driver.Serialization;

namespace Tarantool.Net.Driver
{
    internal class Reader : IReader
    {
        [NotNull] private readonly IMessagePackStructureReader _messagePackStructureReader;
        [NotNull] private readonly IDeserializerResolver _deserializerResolver;
        [NotNull] private readonly StreamInformer _readStream;
        [NotNull] private readonly SemaphoreSlim _readerSemaphore;
        [NotNull] private readonly byte[] _readSizeBuffer;
        [NotNull] private readonly MemoryStream _readSizeStream;
        [NotNull] private readonly IDeserializer<Header> _headerDeserializer;
        [NotNull] private readonly IDeserializer<int> _intDeserializer;
        [NotNull] private readonly IDeserializer<uint> _uintDeserializer;
        [NotNull] private readonly byte[] _emptyBuffer;
        [NotNull] private readonly IDeserializer<ErrorResponse> _errorDeserializer;
        [NotNull] private readonly IDeserializer<ulong> _ulongDeserializer;


        public Reader(
            [NotNull] Stream stream,
            [NotNull] IDeserializerResolver deserializerResolver,
            [NotNull] IMessagePackStructureReader messagePackStructureReader
        )
        {
            _messagePackStructureReader = messagePackStructureReader;
            _messagePackStructureReader = messagePackStructureReader ?? throw new ArgumentNullException(nameof(messagePackStructureReader));
            _deserializerResolver = deserializerResolver ?? throw new ArgumentNullException(nameof(deserializerResolver));
            _readStream = new StreamInformer(stream);
            _readerSemaphore = new SemaphoreSlim(1);
            _readSizeBuffer = new byte[8];
            _readSizeStream = new MemoryStream(_readSizeBuffer);
            _headerDeserializer = deserializerResolver.Resolve<Header>();
            _intDeserializer = deserializerResolver.Resolve<int>();
            _uintDeserializer = deserializerResolver.Resolve<uint>();
            _emptyBuffer = new byte[1024 * 64];
            _errorDeserializer = _deserializerResolver.Resolve<ErrorResponse>();
            _ulongDeserializer = _deserializerResolver.Resolve<ulong>();
        }



        public async Task EnsureSuccessResponse(ResponseInfo result, CancellationToken ct)
        {
            var errorRes = await TryReadError(result, ct);
            if (errorRes.HasValue)
            {
                throw new TarantoolException(result.Header.ErrorCode.Value, errorRes.Value?.Message ?? "Unexpected error: tarantool error deserialized as null");
            }
        }

        private async Task ReadToEndResponse(ResponseInfo result, int bodyReadedBytes, CancellationToken ct)
        {
            var unreaded = result.BodySize - bodyReadedBytes;
            while (unreaded != 0)
            {
                var needReadBytes = unreaded < _emptyBuffer.Length ? unreaded : _emptyBuffer.Length;
                unreaded -= await _readStream.ReadAsync(_emptyBuffer, 0, needReadBytes, ct);
            }
        }

        /// <inheritdoc />
        public async Task<ResponseInfo> ReadNextAsync(CancellationToken ct)
        {
            await _readerSemaphore.WaitAsync(ct);
            try
            {
                _readStream.ClearState();
                _readSizeStream.Position = 0;
                var readedBytes = await _readStream.ReadAsync(_readSizeBuffer, 0, 5, ct);
                if (readedBytes != 5)
                {
                    throw new InvalidOperationException("Could not read a packet size");
                }
                
                var fullSize = await _ulongDeserializer.DeserializeAsync(_readSizeStream, CancellationToken.None);
                var header = await _headerDeserializer.DeserializeAsync(_readStream, CancellationToken.None);
                if (header == null)
                {
                    throw new TarantoolProtocolException("Header is null");
                }
                var headerSize = _readStream.ReadedBytes - 5;
                return new ResponseInfo((int)fullSize, headerSize, header, _readerSemaphore);
            }
            catch
            {
                _readerSemaphore.Release();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task ReadBodyAsync(ResponseInfo responseInfo, CancellationToken ct)
        {
            await EnsureSuccessResponse(responseInfo, ct);
            await ReadToEndResponse(responseInfo, 0, ct);
        }

        public async Task ReadBodyToAsync(ResponseInfo responseInfo, Stream s, CancellationToken ct)
        {
            await EnsureSuccessResponse(responseInfo, ct);
            var unreaded = responseInfo.BodySize;
            while (unreaded != 0)
            {
                var needReadBytes = unreaded < _emptyBuffer.Length ? unreaded : _emptyBuffer.Length;
                var bytesReaded = await _readStream.ReadAsync(_emptyBuffer, 0, needReadBytes, ct);
                unreaded -= bytesReaded;
                await s.WriteAsync(_emptyBuffer, 0, bytesReaded, ct);
            }
        }

        /// <inheritdoc />
        public async Task<T> ReadBodyAsync<T>(ResponseInfo responseInfo, CancellationToken ct)
        {
            await EnsureSuccessResponse(responseInfo, ct);
            var beforeBytesReaded = _readStream.ReadedBytes;
            var res = await _deserializerResolver.Resolve<T>().DeserializeAsync(_readStream, ct);

            await ReadToEndResponse(responseInfo, _readStream.ReadedBytes - beforeBytesReaded, ct);
            return res;
        }

        public async Task<ConnectionInfo> ReadConnectionInfo(CancellationToken ct)
        {
            await _readerSemaphore.WaitAsync(ct);
            try
            {
                var buffer = new byte[128];
                var readed = await _readStream.ReadAsync(buffer, 0, buffer.Length, ct);
                if (readed != buffer.Length)
                {
                    throw new InvalidOperationException($"Invalid greeting message. Expected 128 bytes, actual {readed} bytes");
                }
                var version = Encoding.ASCII.GetString(buffer, 0, 63);
                var saltBase64 = Encoding.ASCII.GetString(buffer, 64, 63);
                var salt = Convert.FromBase64String(saltBase64);
                return new ConnectionInfo(version, salt);
            }
            finally
            {
                _readerSemaphore.Release();
            }
            
        }

        public async Task<AsyncResult<ErrorResponse>> TryReadError(ResponseInfo result, CancellationToken ct)
        {
            if (result.Header.ErrorCode.HasValue)
            {
                var tarantoolError = await _errorDeserializer.DeserializeAsync(_readStream, ct);
                return new AsyncResult<ErrorResponse>(tarantoolError);
            }
            return new AsyncResult<ErrorResponse>();
        }

        public async Task<int> ReadArrayHeader(CancellationToken ct) => (int)await _messagePackStructureReader.ReadArrayHeader(ct);

        public async Task<int> ReadMapHeader(CancellationToken ct) => (int)await _messagePackStructureReader.ReadMapHeader(ct);

        public Task<int> ReadInt(CancellationToken ct) => _messagePackStructureReader.ReadInt(ct);
        public Task<T> ReadAsync<T>(IDeserializer<T> deserializer, CancellationToken ct) => deserializer.DeserializeAsync(_readStream, ct);
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct UIntBinary
    {
        [FieldOffset(0)]
        public readonly uint value;

        [FieldOffset(0)]
        public readonly byte byte0;

        [FieldOffset(1)]
        public readonly byte byte1;

        [FieldOffset(2)]
        public readonly byte byte2;

        [FieldOffset(3)]
        public readonly byte byte3;

        public UIntBinary(uint f)
        {
            this = default(UIntBinary);
            value = f;
        }

        public byte[] ToArray()
        {
            byte[] bytes;
            if (BitConverter.IsLittleEndian)
            {
                bytes = new[]
                {
                    byte3,
                    byte2,
                    byte1,
                    byte0
                };
            }
            else
            {
                bytes = new[]
                {
                    byte0,
                    byte1,
                    byte2,
                    byte3,
                };
            }
            return bytes;
        }

        public UIntBinary(ArraySegment<byte> bytes)
        {
            value = 0;
            if (BitConverter.IsLittleEndian)
            {
                byte0 = bytes.Array[bytes.Offset + 7];
                byte1 = bytes.Array[bytes.Offset + 6];
                byte2 = bytes.Array[bytes.Offset + 5];
                byte3 = bytes.Array[bytes.Offset + 4];
            }
            else
            {
                byte0 = bytes.Array[bytes.Offset + 0];
                byte1 = bytes.Array[bytes.Offset + 1];
                byte2 = bytes.Array[bytes.Offset + 2];
                byte3 = bytes.Array[bytes.Offset + 3];
            }
        }
    }
}
