namespace VideoDownloader.Core.Models;

public sealed record MediaInfo(
    string Title,
    string? ThumbnailUrl,
    TimeSpan? Duration,
    bool IsPlaylist,
    IReadOnlyList<int> AvailableHeights,
    IReadOnlyList<PlaylistItem> Items);