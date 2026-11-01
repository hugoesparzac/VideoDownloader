using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace VideoDownloader.Core.Services;

internal static class ToolRunner
{
    public static async Task<(int ExitCode, string Output, string Error)> RunAsync(
        string executable,
        IEnumerable<string> arguments,
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

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };
        try
        {
            process.Start();
        }
        catch (Win32Exception ex)
        {
            throw new DownloadException($"Could not start {Path.GetFileName(executable)}: {ex.Message}", ex);
        }

        await using CancellationTokenRegistration registration = ct.Register(() => Kill(process));

        Task<string> outputTask = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        Task<string> errorTask = process.StandardError.ReadToEndAsync(CancellationToken.None);

        await process.WaitForExitAsync(ct);

        return (process.ExitCode, (await outputTask).Trim(), (await errorTask).Trim());
    }

    private static void Kill(Process process)
    {
        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception)
        {
        }
    }
}