using System.Globalization;

namespace VideoDownloader.Core.Services;

public static class FileNaming
{
    private static readonly char[] InvalidChars = [.. Path.GetInvalidFileNameChars(), .. "<>:\"/\\|?*"];

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
}