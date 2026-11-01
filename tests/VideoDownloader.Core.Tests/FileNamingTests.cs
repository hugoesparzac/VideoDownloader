using VideoDownloader.Core.Services;

namespace VideoDownloader.Core.Tests;

public sealed class FileNamingTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public FileNamingTests() => Directory.CreateDirectory(_folder);

    public void Dispose() => Directory.Delete(_folder, recursive: true);

    [Fact]
    public void GetUniquePath_FileDoesNotExist_ReturnsSamePath()
    {
        string path = Path.Combine(_folder, "video.mp4");

        Assert.Equal(path, FileNaming.GetUniquePath(path));
    }

    [Fact]
    public void GetUniquePath_FileExists_AddsCounter()
    {
        string path = Path.Combine(_folder, "video.mp4");
        File.WriteAllText(path, "x");

        Assert.Equal(Path.Combine(_folder, "video (1).mp4"), FileNaming.GetUniquePath(path));
    }

    [Fact]
    public void GetUniquePath_CounterTaken_SkipsToNext()
    {
        string path = Path.Combine(_folder, "video.mp4");
        File.WriteAllText(path, "x");
        File.WriteAllText(Path.Combine(_folder, "video (1).mp4"), "x");

        Assert.Equal(Path.Combine(_folder, "video (2).mp4"), FileNaming.GetUniquePath(path));
    }
}