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

    [Fact]
    public void SanitizeFileName_ReplacesInvalidCharacters() =>
        Assert.Equal("Mix_ Rock_Pop_", FileNaming.SanitizeFileName("Mix: Rock/Pop?"));

    [Fact]
    public void SanitizeFileName_EmptyOrDots_ReturnsFallback()
    {
        Assert.Equal("Untitled", FileNaming.SanitizeFileName("   "));
        Assert.Equal("Playlist", FileNaming.SanitizeFileName("...", "Playlist"));
    }

    [Theory]
    [InlineData(1, 4, "1")]
    [InlineData(1, 9, "1")]
    [InlineData(1, 10, "01")]
    [InlineData(9, 10, "09")]
    [InlineData(10, 10, "10")]
    [InlineData(1, 99, "01")]
    [InlineData(1, 100, "001")]
    [InlineData(99, 150, "099")]
    [InlineData(100, 100, "100")]
    public void FormatIndex_PadsToWidthOfTotal(int index, int total, string expected) =>
        Assert.Equal(expected, FileNaming.FormatIndex(index, total));

    [Fact]
    public void GetUniqueFolderPath_FolderExists_AddsCounter()
    {
        string path = Path.Combine(_folder, "Photos");
        Directory.CreateDirectory(path);

        Assert.Equal($"{path} (1)", FileNaming.GetUniqueFolderPath(path));
    }
}