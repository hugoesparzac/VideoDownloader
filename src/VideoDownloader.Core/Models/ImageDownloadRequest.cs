namespace VideoDownloader.Core.Models;

public sealed record ImageDownloadRequest(
    string Url,
    string OutputFolder,
    string? CookiesFromBrowser = null);

public sealed record ImageDownloadResult(string Location, int FileCount);