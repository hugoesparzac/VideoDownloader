namespace VideoDownloader.Core.Models;

public sealed record DownloadRequest(
    string Url,
    string OutputFolder,
    DownloadKind Kind,
    int? MaxHeight = null,
    string? CookiesFromBrowser = null);