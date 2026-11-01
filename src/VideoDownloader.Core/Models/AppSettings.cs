namespace VideoDownloader.Core.Models;

public sealed record AppSettings
{
    public string? OutputFolder { get; init; }

    public DownloadKind Kind { get; init; } = DownloadKind.Video;

    public string? CookiesFromBrowser { get; init; }

    public bool CheckUpdatesOnStartup { get; init; } = true;
}