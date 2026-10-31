namespace VideoDownloader.Core.Services;

public static class FormatSelector
{
    public static string ForVideo(int? maxHeight) =>
        maxHeight is int h
            ? $"bestvideo[height<={h}]+bestaudio/best[height<={h}]"
            : "bestvideo+bestaudio/best";
}