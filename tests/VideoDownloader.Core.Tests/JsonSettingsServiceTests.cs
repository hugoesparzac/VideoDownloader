using VideoDownloader.Core.Models;
using VideoDownloader.Core.Services;

namespace VideoDownloader.Core.Tests;

public sealed class JsonSettingsServiceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(_folder, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void Load_MissingFile_ReturnsDefaults() =>
        Assert.Equal(new AppSettings(), new JsonSettingsService(FilePath).Load());

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        var service = new JsonSettingsService(FilePath);
        var settings = new AppSettings
        {
            OutputFolder = "/tmp/videos",
            Kind = DownloadKind.Audio,
            CookiesFromBrowser = "brave",
            CheckUpdatesOnStartup = false,
        };

        service.Save(settings);

        Assert.Equal(settings, service.Load());
    }

    [Fact]
    public void Load_CorruptedFile_ReturnsDefaults()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(FilePath, "{ this is not json");

        Assert.Equal(new AppSettings(), new JsonSettingsService(FilePath).Load());
    }
}