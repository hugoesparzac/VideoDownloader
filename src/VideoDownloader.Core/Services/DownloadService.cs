using VideoDownloader.Core.Models;

using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;

namespace VideoDownloader.Core.Services;

public sealed class DownloadService(string binariesFolder) : IDownloadService
{
    private YoutubeDL? _client;

    public DownloadService()
        : this(DownloadPaths.GetBinariesFolder())
    {
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        Directory.CreateDirectory(binariesFolder);
        await Utils.DownloadBinaries(skipExisting: true, directoryPath: binariesFolder);

        _client = new YoutubeDL
        {
            YoutubeDLPath = Path.Combine(binariesFolder, Utils.YtDlpBinaryName),
            FFmpegPath = Path.Combine(binariesFolder, Utils.FfmpegBinaryName),
        };
    }

    public async Task<MediaInfo> GetInfoAsync(string url, CancellationToken ct = default)
    {
        VideoData data = Unwrap(await GetClient().RunVideoDataFetch(url, ct: ct));

        var heights = (data.Formats ?? [])
            .Where(f => f.VideoCodec is not (null or "none"))
            .Select(f => f.Height)
            .OfType<int>()
            .Where(h => h > 0)
            .Distinct()
            .OrderDescending()
            .ToList();

        TimeSpan? duration = data.Duration is float seconds
            ? TimeSpan.FromSeconds(seconds)
            : null;

        return new MediaInfo(
            data.Title ?? "(untitled)",
            data.Thumbnail,
            duration,
            data.ResultType == MetadataType.Playlist,
            heights);
    }

    public async Task<string> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgressInfo>? progress = null,
        CancellationToken ct = default)
    {
        var client = GetClient();
        Directory.CreateDirectory(request.OutputFolder);
        client.OutputFolder = request.OutputFolder;

        var ytProgress = progress is null
            ? null
            : new Progress<DownloadProgress>(p =>
                progress.Report(new DownloadProgressInfo(p.Progress * 100, p.DownloadSpeed, p.ETA)));

        RunResult<string> result = request.Kind switch
        {
            DownloadKind.Video => await client.RunVideoDownload(
                request.Url,
                format: FormatSelector.ForVideo(request.MaxHeight),
                mergeFormat: DownloadMergeFormat.Mp4,
                ct: ct,
                progress: ytProgress),

            DownloadKind.Audio => await client.RunAudioDownload(
                request.Url,
                AudioConversionFormat.Mp3,
                ct: ct,
                progress: ytProgress),

            _ => throw new ArgumentOutOfRangeException(nameof(request)),
        };

        return Unwrap(result);
    }

    private static T Unwrap<T>(RunResult<T> result) =>
        result.Success
            ? result.Data
            : throw new DownloadException(string.Join(Environment.NewLine, result.ErrorOutput));

    private YoutubeDL GetClient() =>
        _client ?? throw new InvalidOperationException("Call InitializeAsync first.");
}