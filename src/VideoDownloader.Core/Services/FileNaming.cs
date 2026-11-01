namespace VideoDownloader.Core.Services;

public static class FileNaming
{
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
}