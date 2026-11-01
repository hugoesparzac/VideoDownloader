using VideoDownloader.Core.Services;

namespace VideoDownloader.Core.Tests;

public class ErrorHintsTests
{
    [Fact]
    public void Describe_LoginError_AddsHint() =>
        Assert.Contains("Browser cookies", ErrorHints.Describe("ERROR: login required"));

    [Fact]
    public void Describe_OtherError_ReturnsUnchanged() =>
        Assert.Equal("ERROR: Video unavailable", ErrorHints.Describe("ERROR: Video unavailable"));

    [Fact]
    public void Describe_ExtractorError_SuggestsUpdate() =>
        Assert.Contains("Update tools", ErrorHints.Describe("ERROR: Unable to extract video data"));

    [Fact]
    public void Describe_LoginError_DoesNotSuggestUpdate() =>
        Assert.DoesNotContain("Update tools", ErrorHints.Describe("ERROR: login required"));
}