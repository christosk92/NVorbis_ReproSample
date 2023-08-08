namespace NAudioReproducibleSample.Streams;

public interface IAudioDecrypt
{
    void Decrypt(int chunkIndex, byte[] buffer);
}