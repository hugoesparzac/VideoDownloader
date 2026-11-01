namespace VideoDownloader.Core.Models;

public sealed record PlaylistItem(string Title, string Url);

public sealed record PlaylistDownloadRequest(
    string PlaylistTitle,
    IReadOnlyList<PlaylistItem> Items,
    string OutputFolder,
    DownloadKind Kind,
    int? MaxHeight = null);

public sealed record PlaylistProgressInfo(
    int Index,
    int Total,
    string Title,
    DownloadProgressInfo Item);

public sealed record FailedItem(string Title, string Reason);

public sealed record PlaylistDownloadResult(
    string FolderPath,
    int Succeeded,
    IReadOnlyList<FailedItem> Failed);