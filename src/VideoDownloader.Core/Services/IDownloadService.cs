using VideoDownloader.Core.Models;

namespace VideoDownloader.Core.Services;

public interface IDownloadService
{
    Task InitializeAsync(CancellationToken ct = default);

    Task<MediaInfo> GetInfoAsync(string url, CancellationToken ct = default);

    Task<string> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgressInfo>? progress = null,
        CancellationToken ct = default);
}