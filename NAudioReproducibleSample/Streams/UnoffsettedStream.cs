﻿
using LanguageExt;

namespace NAudioReproducibleSample.Streams;

public sealed class UnoffsettedStream : System.IO.Stream
{
    public const int ChunkSize = 2 * 2 * 128 * 1024;

    private static byte[] AUDIO_AES_IV = new byte[]
    {
        0x72, 0xe0, 0x67, 0xfb, 0xdd, 0xcb, 0xcf, 0x77, 0xeb, 0xe8, 0xbc, 0x64, 0x3f, 0x63, 0x0d, 0x93,
    };

    private readonly Dictionary<int, byte[]> _decryptedChunks = new();
    private readonly Func<int, ValueTask<byte[]>> _getChunkFunc;
    private readonly Option<IAudioDecrypt> _decrypt;

    private long _pos;

    public UnoffsettedStream(long totalSize, Func<int, ValueTask<byte[]>> getChunkFunc, Option<byte[]> audioKey,
        int offset)
    {
        _getChunkFunc = getChunkFunc;
        _decrypt = audioKey.Map(x => (IAudioDecrypt)new BouncyCastleDecrypt(x, AUDIO_AES_IV, ChunkSize));
        Length = totalSize - offset;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var chunkIndex = (int)(_pos / ChunkSize);
        var chunkOffset = (int)(_pos % ChunkSize);

        if (!_decryptedChunks.TryGetValue(chunkIndex, out var chunk))
        {
            int attempt = 0;
            const int maxAttempts = 10;
            while (attempt < maxAttempts)
            {
                try
                {
                    chunk = _getChunkFunc(chunkIndex).Result.ToArray(); //create a copy
                    _decrypt.IfSome(x => x.Decrypt(chunkIndex, chunk));
                    _decryptedChunks[chunkIndex] = chunk;

                    //Preload next 2 chunks ahead
                    _ = Task.Run(async () => await _getChunkFunc(chunkIndex + 1));
                    _ = Task.Run(async () => await _getChunkFunc(chunkIndex + 2));
                    break;
                }
                catch (Exception e)
                {
                    //Log: Failed to get chunk {chunkIndex}, retrying in 2 seconds
                    Console.WriteLine($"Failed to get chunk {chunkIndex}, retrying in 2 seconds");
                    Console.WriteLine(e);
                    attempt++;
                    Thread.Sleep(2000);
                    //
                    //throw new Exception($"Failed to get chunk {chunkIndex}", e);
                }
            }
        }


        var bytesToRead = Math.Max(0, Math.Min(count, chunk.Length - chunkOffset));
        Array.Copy(chunk, chunkOffset, buffer, offset, bytesToRead);
        _pos += bytesToRead;
        return bytesToRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        var to = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.Current => Position + offset,
            SeekOrigin.End => Length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null)
        };

        Position = to;
        return Position;
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
    public override long Length { get; }

    public override long Position
    {
        get => _pos;
        set
        {
            //add offset
            //load chunk
            var chunkIndex = (int)(value / ChunkSize);
            _ = Task.Run(async () => await _getChunkFunc(chunkIndex));
            //set position
            _pos = value;
        }
    }
}