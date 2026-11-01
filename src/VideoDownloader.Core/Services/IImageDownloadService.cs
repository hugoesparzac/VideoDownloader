using VideoDownloader.Core.Models;

namespace VideoDownloader.Core.Services;

public interface IImageDownloadService
{
    Task<ImageDownloadResult> DownloadAsync(
        ImageDownloadRequest request,
        IProgress<int>? progress = null,
        CancellationToken ct = default);
}