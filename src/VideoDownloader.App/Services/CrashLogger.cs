namespace VideoDownloader.App.Services;

public static class CrashLogger
{
    public static void Register()
    {
        AppDomain.CurrentDomain.UnhandledException +=
            (_, e) => Write("UnhandledException", e.ExceptionObject as Exception);

        TaskScheduler.UnobservedTaskException +=
            (_, e) => Write("UnobservedTaskException", e.Exception);
    }

    public static void Write(string source, Exception? exception)
    {
        try
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "VideoDownloader",
                "logs");
            Directory.CreateDirectory(folder);

            string entry = $"[{DateTimeOffset.Now:O}] {source}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}";
            File.AppendAllText(Path.Combine(folder, "crash.log"), entry);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}