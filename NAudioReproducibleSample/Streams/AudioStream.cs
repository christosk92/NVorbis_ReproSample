using LanguageExt;

namespace NAudioReproducibleSample.Streams;

public sealed class AudioStream : System.IO.Stream
{
    private const int SPOTIFY_HEADER_SIZE = 0xa7;
    private readonly int _offset;
    private readonly UnoffsettedStream _stream;

    public AudioStream(long totalSize, Func<int, ValueTask<byte[]>> getChunkFunc, Option<byte[]> audioKey, bool isOgg)
    {
        _offset = isOgg ? SPOTIFY_HEADER_SIZE : 0;
        _stream = new UnoffsettedStream(totalSize, getChunkFunc, audioKey, isOgg ? SPOTIFY_HEADER_SIZE : 0);
    }

    internal UnoffsettedStream UnoffsettedStream => _stream;

    public override int Read(byte[] buffer, int offset, int count)
    {
        return _stream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var to = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length - offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
        };

        Position = to;
        return Position;
    }

    public override long Position
    {
        get => Math.Max(0, _stream.Position - _offset);
        set
        {
            var to = Math.Min(_stream.Length, value + _offset);
            _stream.Position = to;
        }
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Flush()
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => _stream.Length - _offset;
    public bool IsOgg => _offset == SPOTIFY_HEADER_SIZE;
}