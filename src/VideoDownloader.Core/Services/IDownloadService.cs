using VideoDownloader.Core.Models;

namespace VideoDownloader.Core.Services;

public interface IDownloadService
{
    Task InitializeAsync(CancellationToken ct = default);

    Task<MediaInfo> GetInfoAsync(
        string url,
        string? cookiesFromBrowser = null,
        CancellationToken ct = default);

    Task<string> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgressInfo>? progress = null,
        CancellationToken ct = default);

    Task<PlaylistDownloadResult> DownloadPlaylistAsync(
        PlaylistDownloadRequest request,
        IProgress<PlaylistProgressInfo>? progress = null,
        CancellationToken ct = default);
}