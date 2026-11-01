namespace VideoDownloader.Core.Services;

public static class UrlHelper
{
    public static bool TryNormalize(string? input, out string url)
    {
        url = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        string trimmed = input.Trim();
        if (Uri.TryCreate(trimmed, UriKind.Absolute, out Uri? uri) && uri.Scheme is "http" or "https")
        {
            url = trimmed;
            return true;
        }

        return false;
    }

    public static bool IsTikTokPhotoPost(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
        && uri.Host.EndsWith("tiktok.com", StringComparison.OrdinalIgnoreCase)
        && uri.AbsolutePath.Contains("/photo/", StringComparison.OrdinalIgnoreCase);

    public static bool IsInstagramPost(string url) =>
        Uri.TryCreate(url, UriKind.Absolute, out Uri? uri)
        && uri.Host.EndsWith("instagram.com", StringComparison.OrdinalIgnoreCase)
        && uri.AbsolutePath.Contains("/p/", StringComparison.OrdinalIgnoreCase);

    public static string GetPostFolderName(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
        {
            return "Images";
        }

        string host = uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase)
            ? uri.Host[4..]
            : uri.Host;
        string site = host.Split('.')[0];
        string postId = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault() ?? string.Empty;

        return postId.Length == 0 ? site : $"{site} {postId}";
    }
}