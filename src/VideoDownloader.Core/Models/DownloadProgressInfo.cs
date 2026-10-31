namespace VideoDownloader.Core.Models;

public sealed record DownloadProgressInfo(double Percent, string? Speed, string? Eta);