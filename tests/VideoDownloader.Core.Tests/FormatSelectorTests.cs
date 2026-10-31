using VideoDownloader.Core.Services;

namespace VideoDownloader.Core.Tests;

public class FormatSelectorTests
{
    [Fact]
    public void ForVideo_WithoutLimit_ReturnsBest() =>
        Assert.Equal("bestvideo+bestaudio/best", FormatSelector.ForVideo(null));

    [Fact]
    public void ForVideo_With1080_LimitsHeight() =>
        Assert.Equal(
            "bestvideo[height<=1080]+bestaudio/best[height<=1080]",
            FormatSelector.ForVideo(1080));
}