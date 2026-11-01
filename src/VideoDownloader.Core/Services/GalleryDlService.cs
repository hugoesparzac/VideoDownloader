using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

using VideoDownloader.Core.Models;

namespace VideoDownloader.Core.Services;

public sealed class GalleryDlService(string binariesFolder) : IImageDownloadService
{
    private static readonly HttpClient Http = CreateHttpClient();

    private string? _executable;

    private const string ReleasesApiUrl = "https://codeberg.org/api/v1/repos/mikf/gallery-dl/releases/latest";
    private const string FallbackVersion = "v1.32.18";

    public GalleryDlService()
        : this(DownloadPaths.GetBinariesFolder())
    {
    }

    public async Task<ImageDownloadResult> DownloadAsync(
        ImageDownloadRequest request,
        IProgress<int>? progress = null,
        CancellationToken ct = default)
    {
        string executable = await EnsureExecutableAsync(ct);

        Directory.CreateDirectory(request.OutputFolder);
        string stagingFolder = Path.Combine(request.OutputFolder, $".vd-tmp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(stagingFolder);

        try
        {
            List<string> downloaded = await RunAsync(executable, request, stagingFolder, progress, ct);

            List<string> files = [.. downloaded.Where(File.Exists)];
            if (files.Count == 0)
            {
                files = [.. Directory.GetFiles(stagingFolder).Order(StringComparer.OrdinalIgnoreCase)];
            }

            return files.Count == 0
                ? throw new DownloadException("No images were found in this post.")
                : MoveResults(files, request);
        }
        finally
        {
            FileNaming.TryDeleteFolder(stagingFolder);
        }
    }

    public async Task<string?> GetToolVersionAsync(CancellationToken ct = default)
    {
        string? executable = File.Exists(GetLocalPath()) ? GetLocalPath() : FindOnPath(GetPathName());
        if (executable is null)
        {
            return null;
        }

        var (exitCode, output, _) = await ToolRunner.RunAsync(executable, ["--version"], ct);
        return exitCode == 0 ? output : null;
    }

    public async Task<string?> UpdateToolAsync(CancellationToken ct = default)
    {
        if (!File.Exists(GetLocalPath()) && FindOnPath(GetPathName()) is not null)
        {
            throw new DownloadException(
                "gallery-dl comes from your system. Update it with your package manager.");
        }

        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
        {
            throw new DownloadException(
                "gallery-dl was not found. Install it (for example with 'brew install gallery-dl') and restart the app.");
        }

        await DownloadExecutableAsync(GetLocalFileName(), GetLocalPath(), ct);
        _executable = GetLocalPath();

        return await GetToolVersionAsync(ct);
    }

    private static string GetLocalFileName() =>
        OperatingSystem.IsWindows() ? "gallery-dl.exe" : "gallery-dl.bin";

    private static string GetPathName() =>
        OperatingSystem.IsWindows() ? "gallery-dl.exe" : "gallery-dl";

    private string GetLocalPath() => Path.Combine(binariesFolder, GetLocalFileName());

    private static ImageDownloadResult MoveResults(List<string> files, ImageDownloadRequest request)
    {
        if (files.Count == 1)
        {
            string single = FileNaming.GetUniquePath(
                Path.Combine(request.OutputFolder, Path.GetFileName(files[0])));
            File.Move(files[0], single);
            return new ImageDownloadResult(single, 1);
        }

        string folder = FileNaming.GetUniqueFolderPath(Path.Combine(
            request.OutputFolder,
            FileNaming.SanitizeFileName(UrlHelper.GetPostFolderName(request.Url), "Images")));
        Directory.CreateDirectory(folder);

        for (int i = 0; i < files.Count; i++)
        {
            string name = $"{FileNaming.FormatIndex(i + 1, files.Count)} {Path.GetFileName(files[i])}";
            File.Move(files[i], Path.Combine(folder, name));
        }

        return new ImageDownloadResult(folder, files.Count);
    }

    private static async Task<List<string>> RunAsync(
        string executable,
        ImageDownloadRequest request,
        string stagingFolder,
        IProgress<int>? progress,
        CancellationToken ct)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        startInfo.ArgumentList.Add("-D");
        startInfo.ArgumentList.Add(stagingFolder);

        if (!string.IsNullOrWhiteSpace(request.CookiesFromBrowser))
        {
            startInfo.ArgumentList.Add("--cookies-from-browser");
            startInfo.ArgumentList.Add(request.CookiesFromBrowser);
        }

        startInfo.ArgumentList.Add(request.Url);

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
        }
        catch (Win32Exception ex)
        {
            throw new DownloadException($"Could not start gallery-dl: {ex.Message}", ex);
        }

        await using CancellationTokenRegistration registration = ct.Register(() => KillQuietly(process));

        Task<string> errorTask = process.StandardError.ReadToEndAsync(CancellationToken.None);
        var downloaded = new List<string>();

        string? line;
        while ((line = await process.StandardOutput.ReadLineAsync(ct)) is not null)
        {
            if (line.Length > 0 && !line.StartsWith('#'))
            {
                downloaded.Add(line.Trim());
                progress?.Report(downloaded.Count);
            }
        }

        await process.WaitForExitAsync(ct);
        string error = await errorTask;

        return process.ExitCode != 0 && downloaded.Count == 0
            ? throw new DownloadException(PickErrorLine(error, process.ExitCode))
            : downloaded;
    }

    private static string PickErrorLine(string error, int exitCode)
    {
        string[] lines = error.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return lines.FirstOrDefault(l => l.Contains("error", StringComparison.OrdinalIgnoreCase))
            ?? lines.FirstOrDefault()
            ?? string.Create(CultureInfo.InvariantCulture, $"gallery-dl exited with code {exitCode}.");
    }

    private static void KillQuietly(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
        }
    }

    private async Task<string> EnsureExecutableAsync(CancellationToken ct)
    {
        if (_executable is not null)
        {
            return _executable;
        }

        string localName = OperatingSystem.IsWindows() ? "gallery-dl.exe" : "gallery-dl.bin";
        string localPath = Path.Combine(binariesFolder, localName);

        if (File.Exists(localPath))
        {
            return _executable = localPath;
        }

        string? onPath = FindOnPath(OperatingSystem.IsWindows() ? "gallery-dl.exe" : "gallery-dl");
        if (onPath is not null)
        {
            return _executable = onPath;
        }

        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux())
        {
            throw new DownloadException(
                "gallery-dl was not found. Install it (for example with 'brew install gallery-dl') and restart the app.");
        }

        await DownloadExecutableAsync(localName, localPath, ct);
        return _executable = localPath;
    }

    private async Task DownloadExecutableAsync(string assetName, string localPath, CancellationToken ct)
    {
        Directory.CreateDirectory(binariesFolder);
        string tempPath = $"{localPath}.download";
        Uri uri = await ResolveDownloadUriAsync(assetName, ct);

        try
        {
            await using (Stream source = await Http.GetStreamAsync(uri, ct))
            await using (FileStream target = File.Create(tempPath))
            {
                await source.CopyToAsync(target, ct);
            }

            File.Move(tempPath, localPath, overwrite: true);
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            throw new DownloadException($"Could not download gallery-dl: {ex.Message}", ex);
        }
        finally
        {
            File.Delete(tempPath);
        }

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                localPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    private static async Task<Uri> ResolveDownloadUriAsync(string assetName, CancellationToken ct)
    {
        try
        {
            await using Stream stream = await Http.GetStreamAsync(new Uri(ReleasesApiUrl), ct);
            using JsonDocument document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);

            if (document.RootElement.TryGetProperty("assets", out JsonElement assets))
            {
                foreach (JsonElement asset in assets.EnumerateArray())
                {
                    if (asset.TryGetProperty("name", out JsonElement name)
                        && name.GetString() == assetName
                        && asset.TryGetProperty("browser_download_url", out JsonElement url)
                        && Uri.TryCreate(url.GetString(), UriKind.Absolute, out Uri? address)
                        && address.Scheme == Uri.UriSchemeHttps
                        && address.Host == "codeberg.org")
                    {
                        return address;
                    }
                }
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException)
        {
        }

        return new Uri($"https://codeberg.org/mikf/gallery-dl/releases/download/{FallbackVersion}/{assetName}");
    }

    private static string? FindOnPath(string fileName)
    {
        string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        foreach (string directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            string candidate = Path.Combine(directory, fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("VideoDownloader");
        return client;
    }
}