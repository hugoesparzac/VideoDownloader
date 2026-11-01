namespace VideoDownloader.Core.Services;

public static class ErrorHints
{
    private static readonly string[] LoginMarkers =
        ["login", "sign in", "cookies", "rate-limit", "unauthorized", "401"];

    private static readonly string[] OutdatedMarkers =
        ["unable to extract", "extraction failed", "nsig", "http error 403", "no video formats"];

    public static string Describe(string message) =>
        ContainsAny(message, LoginMarkers)
            ? $"{message} Hint: pick your browser under \"Browser cookies\" and try again."
            : ContainsAny(message, OutdatedMarkers)
                ? $"{message} Hint: the site may have changed. Try \"Update tools\"."
                : message;

    private static bool ContainsAny(string message, string[] markers) =>
        markers.Any(marker => message.Contains(marker, StringComparison.OrdinalIgnoreCase));
}