namespace VideoDownloader.App.Models;

public sealed record BrowserOption(string Label, string? Value)
{
    public static BrowserOption None { get; } = new("None", null);

    public static IReadOnlyList<BrowserOption> GetAvailable()
    {
        List<BrowserOption> options =
        [
            None,
            new("Chrome", "chrome"),
            new("Firefox", "firefox"),
            new("Edge", "edge"),
            new("Brave", "brave"),
            new("Chromium", "chromium"),
        ];

        if (OperatingSystem.IsMacOS())
        {
            options.Add(new BrowserOption("Safari", "safari"));
        }

        return options;
    }
}