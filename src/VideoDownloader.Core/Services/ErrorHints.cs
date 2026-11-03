namespace VideoDownloader.Core.Services;

public static class ErrorHints
{
    private static readonly string[] CookieReadMarkers =
        ["could not copy", "cookie database", "failed to decrypt", "dpapi", "failed to load cookies"];

    private static readonly string[] LoginMarkers =
        ["login", "sign in", "cookies", "rate-limit", "unauthorized", "401"];

    private static readonly string[] OutdatedMarkers =
        ["unable to extract", "extraction failed", "nsig", "http error 403", "no video formats"];

    public static string Describe(string message)
    {
        if (ContainsAny(message, CookieReadMarkers))
        {
            return $"{message} Hint: the browser's cookies could not be read. Chrome, Edge and Brave protect them on Windows; sign in to the site with Firefox and pick Firefox under \"Browser cookies\".";
        }

        if (ContainsAny(message, LoginMarkers))
        {
            return OperatingSystem.IsWindows()
                ? $"{message} Hint: sign in to the site in Firefox and pick Firefox under \"Browser cookies\" (Chrome, Edge and Brave cannot be read on Windows)."
                : $"{message} Hint: pick a browser where you are signed in under \"Browser cookies\" and try again.";
        }

        return ContainsAny(message, OutdatedMarkers)
            ? $"{message} Hint: the site may have changed. Try \"Update tools\"."
            : message;
    }

    private static bool ContainsAny(string message, string[] markers) =>
        markers.Any(marker => message.Contains(marker, StringComparison.OrdinalIgnoreCase));
}