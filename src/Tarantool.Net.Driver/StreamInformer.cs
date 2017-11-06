using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Tarantool.Net.Driver
{
    internal class StreamInformer : Stream
    {
        private class CurrentState
        {
            public int Readed { get; set; }

            public int Writed { get; set; }
        }

        [NotNull] private readonly CurrentState _currentState = new CurrentState();

        public int ReadedBytes => _currentState.Readed;

        public int WritedBytes => _currentState.Writed;

        [NotNull] private readonly Stream _baseStream;

        public StreamInformer([NotNull] Stream baseStream)
        {
            _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
        }

        public void ClearState()
        {
            _currentState.Readed = 0;
            _currentState.Writed = 0;
        }

        public override void Flush() => _baseStream.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) => _baseStream.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count)
        {
            var readed = _baseStream.Read(buffer, offset, count);
            _currentState.Readed += readed;
            return readed;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var readed = await base.ReadAsync(buffer, offset, count, cancellationToken);
            _currentState.Readed += readed;
            return readed;
        }

        public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);

        public override void SetLength(long value) => _baseStream.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            _baseStream.Write(buffer, offset, count);
            _currentState.Writed += count;
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _baseStream.WriteAsync(buffer, offset, count, cancellationToken);
            _currentState.Writed += count;
        }

        public override bool CanRead => _baseStream.CanRead;

        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => _baseStream.CanWrite;

        public override long Length => _baseStream.Length;

        public override long Position
        {
            get => _baseStream.Position;
            set => _baseStream.Position = value;
        }
    }
}
