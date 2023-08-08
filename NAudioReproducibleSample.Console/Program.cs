// See https://aka.ms/new-console-template for more information

using LanguageExt;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudioReproducibleSample.Streams;

var keyHex = "25-44-A5-8E-88-4B-F8-16-26-DA-81-67-5C-2C-A0-81";
var key = keyHex.Split('-').Select(x => Convert.ToByte(x, 16)).ToArray();
using var fs = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Files", "sample_encrypted"));

const int firstChunkStart = 0;
const int chunkSize = UnoffsettedStream.ChunkSize;
const int firstChunkEnd = firstChunkStart + chunkSize - 1;


var numberOfChunks =
    (int)Math.Ceiling((double)fs.Length / chunkSize);
var requested = new TaskCompletionSource<byte[]>[numberOfChunks];

await GetChunkFunc(0);

ValueTask<byte[]> GetChunkFunc(int index)
{
    //check if chunk index is valid
    if (index < 0 || index >= numberOfChunks)
    {
        return new ValueTask<byte[]>(Array.Empty<byte>());
    }

    if (requested[index] is { Task.IsCompleted: true })
    {
        return new ValueTask<byte[]>(requested[index].Task.Result);
    }

    if (requested[index] is null)
    {
        Console.WriteLine($"Requesting chunk {index}");
        var start = index * chunkSize;
        var end = start + chunkSize - 1;
        requested[index] = new TaskCompletionSource<byte[]>();
        
     
        //Read chunk from file , add a delay of 200 ms to simulate network latency
        var fetchTask = Task.Run(async () =>
        {
            await Task.Delay(200);
            var buffer = new byte[chunkSize];
            fs.Seek(start, SeekOrigin.Begin);
            fs.Read(buffer, 0, chunkSize);
            requested[index].SetResult(buffer);
            
            return buffer;
        });
        return new ValueTask<byte[]>(fetchTask);
    }

    return new ValueTask<byte[]>(requested[index].Task);
}


var stream = new AudioStream(
    totalSize: fs.Length,
    getChunkFunc: GetChunkFunc,
    audioKey: Option<byte[]>.Some(key),
    isOgg: true
);

var vorbisReader = new VorbisWaveReader(stream);
var waveOut = new WaveOutEvent();
waveOut.Init(vorbisReader);

waveOut.Play();
Console.WriteLine("Playing...");
int iteration = 0;
while (waveOut.PlaybackState == PlaybackState.Playing)
{
    await Task.Delay(1000);
    iteration++;
    //At 2rd iteration, seek to half of duration
    if (iteration == 4)
    {
        var halfDuration = vorbisReader.TotalTime.TotalSeconds / 2;
        Console.WriteLine($"Seeking to {halfDuration}");
        vorbisReader.CurrentTime = TimeSpan.FromSeconds(halfDuration);
    }
}

Console.WriteLine("Done");