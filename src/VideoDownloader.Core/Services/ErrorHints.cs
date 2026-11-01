namespace VideoDownloader.Core.Services;

public static class ErrorHints
{
    private static readonly string[] LoginMarkers = ["login", "sign in", "cookies", "rate-limit"];

    public static string Describe(string message)
    {
        bool needsLogin = LoginMarkers.Any(
            marker => message.Contains(marker, StringComparison.OrdinalIgnoreCase));

        return needsLogin
            ? $"{message} Hint: pick your browser under \"Browser cookies\" and try again."
            : message;
    }
}