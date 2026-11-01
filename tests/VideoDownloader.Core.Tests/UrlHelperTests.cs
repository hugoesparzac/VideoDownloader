using VideoDownloader.Core.Services;

namespace VideoDownloader.Core.Tests;

public class UrlHelperTests
{
    [Fact]
    public void TryNormalize_ValidUrl_TrimsAndReturnsTrue()
    {
        bool ok = UrlHelper.TryNormalize("  https://youtu.be/abc  ", out string url);

        Assert.True(ok);
        Assert.Equal("https://youtu.be/abc", url);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    [InlineData("ftp://example.com/file")]
    public void TryNormalize_InvalidInput_ReturnsFalse(string input) =>
        Assert.False(UrlHelper.TryNormalize(input, out _));

    [Fact]
    public void IsTikTokPhotoPost_PhotoUrl_ReturnsTrue() =>
        Assert.True(UrlHelper.IsTikTokPhotoPost("https://www.tiktok.com/@kdmr98/photo/7671877560778771733"));

    [Fact]
    public void IsTikTokPhotoPost_VideoUrl_ReturnsFalse() =>
        Assert.False(UrlHelper.IsTikTokPhotoPost("https://www.tiktok.com/@user/video/123"));
}