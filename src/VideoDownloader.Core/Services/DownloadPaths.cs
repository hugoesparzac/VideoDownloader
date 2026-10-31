using System.Diagnostics;

namespace VideoDownloader.Core.Services;

public static class DownloadPaths
{
    public static string GetDefaultDownloadFolder()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return OperatingSystem.IsLinux() && TryGetXdgDownloadDir(out var xdg)
            ? xdg
            : Path.Combine(home, "Downloads");
    }

    public static string GetBinariesFolder() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VideoDownloader",
            "bin");

    private static bool TryGetXdgDownloadDir(out string path)
    {
        path = string.Empty;
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-user-dir",
                Arguments = "DOWNLOAD",
                RedirectStandardOutput = true,
                UseShellExecute = false,
            });

            if (process is null)
            {
                return false;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (process.ExitCode == 0 && Directory.Exists(output))
            {
                path = output;
                return true;
            }
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
        }

        return false;
    }
}