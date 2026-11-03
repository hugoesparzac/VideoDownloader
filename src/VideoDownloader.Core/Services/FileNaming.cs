using System.Globalization;

namespace VideoDownloader.Core.Services;

public static class FileNaming
{
    private static readonly char[] InvalidChars = [.. Path.GetInvalidFileNameChars(), .. "<>:\"/\\|?*"];

    private static readonly string[] TemporaryExtensions = [".part", ".ytdl", ".temp", ".tmp"];

    public static string? FindOutputFile(string folder, string? reportedPath)
    {
        if (!string.IsNullOrWhiteSpace(reportedPath) && File.Exists(reportedPath))
        {
            return reportedPath;
        }

        return Directory.EnumerateFiles(folder)
            .Where(f => !TemporaryExtensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.Length)
            .Select(f => f.FullName)
            .FirstOrDefault();
    }

    public static string SanitizeFileName(string name, string fallback = "Untitled")
    {
        string cleaned = string.Concat(name.Select(c => InvalidChars.Contains(c) ? '_' : c))
            .Trim()
            .TrimEnd('.');

        return cleaned.Length == 0 ? fallback : cleaned;
    }

    public static string GetUniquePath(string path)
    {
        if (!File.Exists(path))
        {
            return path;
        }

        string folder = Path.GetDirectoryName(path) ?? string.Empty;
        string name = Path.GetFileNameWithoutExtension(path);
        string extension = Path.GetExtension(path);

        for (int i = 1; ; i++)
        {
            string candidate = Path.Combine(folder, $"{name} ({i}){extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }

    public static string FormatIndex(int index, int total)
    {
        int digits = total.ToString(CultureInfo.InvariantCulture).Length;
        return index.ToString($"D{digits}", CultureInfo.InvariantCulture);
    }

    public static string GetUniqueFolderPath(string path)
    {
        if (!Directory.Exists(path))
        {
            return path;
        }

        for (int i = 1; ; i++)
        {
            string candidate = $"{path} ({i})";
            if (!Directory.Exists(candidate))
            {
                return candidate;
            }
        }
    }

    public static void TryDeleteFolder(string folder)
    {
        try
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}