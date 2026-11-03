namespace VideoDownloader.App.Models;

public sealed record BrowserOption(string Label, string? Value)
{
    public static BrowserOption None { get; } = new("None", null);

    public static IReadOnlyList<BrowserOption> GetAvailable()
    {
        string suffix = OperatingSystem.IsWindows() ? " (may not work)" : string.Empty;

        List<BrowserOption> options =
        [
            None,
            new("Firefox", "firefox"),
            new($"Chrome{suffix}", "chrome"),
            new($"Edge{suffix}", "edge"),
            new($"Brave{suffix}", "brave"),
            new($"Chromium{suffix}", "chromium"),
        ];

        if (OperatingSystem.IsMacOS())
        {
            options.Add(new BrowserOption("Safari", "safari"));
        }

        return options;
    }
}