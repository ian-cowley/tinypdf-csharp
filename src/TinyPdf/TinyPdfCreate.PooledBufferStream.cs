namespace TinyPdf;

public partial class TinyPdfCreate
{
    private sealed class PooledBufferStream : Stream
    {
        private readonly PooledBufferWriter _writer;
        public PooledBufferStream(PooledBufferWriter writer) { _writer = writer; }
        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count)
        {
            int remaining = count;
            int src = offset;
            while (remaining > 0)
            {
                var span = _writer.GetSpan(remaining);
                int toCopy = Math.Min(span.Length, remaining);
                buffer.AsSpan(src, toCopy).CopyTo(span);
                _writer.Advance(toCopy);
                src += toCopy;
                remaining -= toCopy;
            }
        }
    }
}
