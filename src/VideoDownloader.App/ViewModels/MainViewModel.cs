using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using VideoDownloader.Core.Models;
using VideoDownloader.Core.Services;

namespace VideoDownloader.App.ViewModels;

public sealed partial class MainViewModel(IDownloadService downloadService) : ViewModelBase
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    private string _url = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    private bool _isReady;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "Preparing tools...";

    [ObservableProperty]
    private string? _mediaTitle;

    public ObservableCollection<int> AvailableHeights { get; } = [];

    public async Task InitializeAsync()
    {
        IsBusy = true;
        try
        {
            await downloadService.InitializeAsync();
            IsReady = true;
            StatusMessage = "Ready. Paste a URL and press Analyze.";
        }
        catch (Exception ex) when (ex is HttpRequestException or IOException)
        {
            StatusMessage = $"Could not prepare yt-dlp/ffmpeg: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanAnalyze() => IsReady && !IsBusy && !string.IsNullOrWhiteSpace(Url);

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeAsync(CancellationToken ct)
    {
        IsBusy = true;
        StatusMessage = "Analyzing...";
        MediaTitle = null;
        AvailableHeights.Clear();

        try
        {
            MediaInfo info = await downloadService.GetInfoAsync(Url.Trim(), ct);

            MediaTitle = info.IsPlaylist ? $"Playlist: {info.Title}" : info.Title;
            foreach (int height in info.AvailableHeights)
            {
                AvailableHeights.Add(height);
            }

            StatusMessage = info.IsPlaylist
                ? "Playlist detected."
                : $"{info.AvailableHeights.Count} resolutions available.";
        }
        catch (DownloadException ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Cancelled.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}