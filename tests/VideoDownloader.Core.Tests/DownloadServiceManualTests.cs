using VideoDownloader.Core.Models;
using VideoDownloader.Core.Services;

namespace VideoDownloader.Core.Tests;

public class DownloadServiceManualTests
{
    [ManualFact]
    public async Task GetInfo_ReturnsHeights()
    {
        var service = new DownloadService();
        await service.InitializeAsync();

        MediaInfo info = await service.GetInfoAsync("https://youtu.be/3quYcNI3dOs");

        Assert.NotEmpty(info.AvailableHeights);
    }

    [ManualFact]
    public async Task GetToolVersion_ReturnsVersion()
    {
        var service = new DownloadService();
        await service.InitializeAsync();

        string version = await service.GetToolVersionAsync();

        Assert.False(string.IsNullOrWhiteSpace(version));
    }
}