using System.Globalization;

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

    public async Task<MediaInfo> GetInfoAsync(
        string url,
        string? cookiesFromBrowser = null,
        CancellationToken ct = default)
    {
        VideoData data = Unwrap(await GetClient().RunVideoDataFetch(
            url,
            ct: ct,
            overrideOptions: BuildOptions(cookiesFromBrowser)));
        bool isPlaylist = data.ResultType == MetadataType.Playlist;

        var heights = (data.Formats ?? [])
            .Where(f => f.VideoCodec is not (null or "none"))
            .Select(f => f.Height)
            .OfType<int>()
            .Where(h => h > 0)
            .Distinct()
            .OrderDescending()
            .ToList();

        IReadOnlyList<PlaylistItem> items = isPlaylist
            ? (data.Entries ?? []).Select(ToPlaylistItem).OfType<PlaylistItem>().ToList()
            : [];

        TimeSpan? duration = data.Duration is float seconds
            ? TimeSpan.FromSeconds(seconds)
            : null;

        return new MediaInfo(
            data.Title ?? "(untitled)",
            data.Thumbnail,
            duration,
            isPlaylist,
            heights,
            items);
    }

    public Task<string> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgressInfo>? progress = null,
        CancellationToken ct = default) =>
        DownloadCoreAsync(request, string.Empty, progress, ct);

    public async Task<PlaylistDownloadResult> DownloadPlaylistAsync(
        PlaylistDownloadRequest request,
        IProgress<PlaylistProgressInfo>? progress = null,
        CancellationToken ct = default)
    {
        string folder = Path.Combine(
            request.OutputFolder,
            FileNaming.SanitizeFileName(request.PlaylistTitle, "Playlist"));
        Directory.CreateDirectory(folder);

        int total = request.Items.Count;
        int succeeded = 0;
        var failed = new List<FailedItem>();

        for (int i = 0; i < total; i++)
        {
            ct.ThrowIfCancellationRequested();

            PlaylistItem item = request.Items[i];
            int number = i + 1;
            string prefix = $"{FileNaming.FormatIndex(number, total)} ";

            var itemProgress = progress is null
                ? null
                : new Progress<DownloadProgressInfo>(p =>
                    progress.Report(new PlaylistProgressInfo(number, total, item.Title, p)));

            try
            {
                var single = new DownloadRequest(item.Url, folder, request.Kind, request.MaxHeight, request.CookiesFromBrowser);
                await DownloadCoreAsync(single, prefix, itemProgress, ct);
                succeeded++;
            }
            catch (Exception ex) when (ex is DownloadException or IOException)
            {
                failed.Add(new FailedItem(item.Title, FirstLine(ex.Message)));
            }
        }

        return new PlaylistDownloadResult(folder, succeeded, failed);
    }

    private async Task<string> DownloadCoreAsync(
        DownloadRequest request,
        string fileNamePrefix,
        IProgress<DownloadProgressInfo>? progress,
        CancellationToken ct)
    {
        var client = GetClient();
        Directory.CreateDirectory(request.OutputFolder);

        string stagingFolder = Path.Combine(request.OutputFolder, $".vd-tmp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagingFolder);
        client.OutputFolder = stagingFolder;

        var ytProgress = progress is null
            ? null
            : new Progress<DownloadProgress>(p =>
                progress.Report(new DownloadProgressInfo(p.Progress * 100, p.DownloadSpeed, p.ETA)));

        var options = BuildOptions(request.CookiesFromBrowser);

        try
        {
            RunResult<string> result = request.Kind switch
            {
                DownloadKind.Video => await client.RunVideoDownload(
                    request.Url,
                    format: FormatSelector.ForVideo(request.MaxHeight),
                    mergeFormat: DownloadMergeFormat.Mp4,
                    ct: ct,
                    progress: ytProgress,
                    overrideOptions: options),

                DownloadKind.Audio => await client.RunAudioDownload(
                    request.Url,
                    AudioConversionFormat.Mp3,
                    ct: ct,
                    progress: ytProgress,
                    overrideOptions: options),

                _ => throw new ArgumentOutOfRangeException(nameof(request)),
            };

            string stagedPath = Unwrap(result);
            string finalPath = FileNaming.GetUniquePath(
                Path.Combine(request.OutputFolder, fileNamePrefix + Path.GetFileName(stagedPath)));

            File.Move(stagedPath, finalPath);
            return finalPath;
        }
        finally
        {
            TryDeleteFolder(stagingFolder);
        }
    }

    private static PlaylistItem? ToPlaylistItem(VideoData entry)
    {
        string? url = entry.Url ?? entry.WebpageUrl;
        return url is null ? null : new PlaylistItem(entry.Title ?? url, url);
    }

    private static string FirstLine(string message) =>
        message.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault() ?? message;

    private static void TryDeleteFolder(string folder)
    {
        try
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }

    private static T Unwrap<T>(RunResult<T> result) =>
        result.Success
            ? result.Data
            : throw new DownloadException(string.Join(Environment.NewLine, result.ErrorOutput));

    private static OptionSet? BuildOptions(string? cookiesFromBrowser) =>
        string.IsNullOrWhiteSpace(cookiesFromBrowser)
            ? null
            : new OptionSet { CookiesFromBrowser = cookiesFromBrowser };

    private YoutubeDL GetClient() =>
        _client ?? throw new InvalidOperationException("Call InitializeAsync first.");
}