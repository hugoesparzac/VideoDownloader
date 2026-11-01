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
}