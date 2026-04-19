using System.Security.Cryptography;

namespace PortaBox.Application.Abstractions.Storage;

public static class Sha256StreamHasher
{
    public static HashingReadStream Wrap(Stream stream, bool leaveOpen = true)
    {
        ArgumentNullException.ThrowIfNull(stream);

        return new HashingReadStream(stream, leaveOpen);
    }

    public sealed class HashingReadStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly IncrementalHash _incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        private readonly bool _leaveOpen;
        private bool _completed;
        private string? _hash;

        internal HashingReadStream(Stream innerStream, bool leaveOpen)
        {
            _innerStream = innerStream;
            _leaveOpen = leaveOpen;
        }

        public long BytesRead { get; private set; }

        public override bool CanRead => _innerStream.CanRead;

        public override bool CanSeek => _innerStream.CanSeek;

        public override bool CanWrite => false;

        public override long Length => _innerStream.Length;

        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        public string GetComputedHashHex()
        {
            EnsureHashCompleted();
            return _hash!;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesRead = _innerStream.Read(buffer, offset, count);
            AppendBytes(buffer.AsSpan(offset, bytesRead));

            return bytesRead;
        }

        public override int Read(Span<byte> buffer)
        {
            var bytesRead = _innerStream.Read(buffer);
            AppendBytes(buffer[..bytesRead]);

            return bytesRead;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            var bytesRead = await _innerStream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
            AppendBytes(buffer.AsSpan(offset, bytesRead));

            return bytesRead;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var bytesRead = await _innerStream.ReadAsync(buffer, cancellationToken);
            AppendBytes(buffer.Span[..bytesRead]);

            return bytesRead;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException("Hashing read stream does not support write operations.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException("Hashing read stream does not support write operations.");
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                CompleteHash();
                _incrementalHash.Dispose();

                if (!_leaveOpen)
                {
                    _innerStream.Dispose();
                }
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            CompleteHash();
            _incrementalHash.Dispose();

            if (!_leaveOpen)
            {
                await _innerStream.DisposeAsync();
            }

            await base.DisposeAsync();
        }

        private void AppendBytes(ReadOnlySpan<byte> bytes)
        {
            if (bytes.IsEmpty)
            {
                CompleteHash();
                return;
            }

            _incrementalHash.AppendData(bytes);
            BytesRead += bytes.Length;
        }

        private void EnsureHashCompleted()
        {
            CompleteHash();
        }

        private void CompleteHash()
        {
            if (_completed)
            {
                return;
            }

            _hash = Convert.ToHexString(_incrementalHash.GetHashAndReset()).ToLowerInvariant();
            _completed = true;
        }
    }
}
