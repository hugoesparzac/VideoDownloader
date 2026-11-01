using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using VideoDownloader.App.Models;
using VideoDownloader.App.Services;
using VideoDownloader.Core.Models;
using VideoDownloader.Core.Services;

namespace VideoDownloader.App.ViewModels;

public sealed partial class MainViewModel(
    IDownloadService downloadService,
    IFolderPickerService folderPicker) : ViewModelBase
{
    private string _analyzedUrl = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    public partial string Url { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    public partial bool IsReady { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial bool IsDownloading { get; set; }

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Preparing tools...";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    public partial string? MediaTitle { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    public partial bool IsPlaylist { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVideoSelected))]
    public partial DownloadKind SelectedKind { get; set; } = DownloadKind.Video;

    [ObservableProperty]
    public partial ResolutionOption? SelectedResolution { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    public partial string OutputFolder { get; set; } = DownloadPaths.GetDefaultDownloadFolder();

    [ObservableProperty]
    public partial double ProgressPercent { get; set; }

    [ObservableProperty]
    public partial string ProgressText { get; set; } = string.Empty;

    public IReadOnlyList<DownloadKind> Kinds { get; } = [DownloadKind.Video, DownloadKind.Audio];

    public ObservableCollection<ResolutionOption> AvailableResolutions { get; } = [];

    public bool IsVideoSelected => SelectedKind == DownloadKind.Video;

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

    private bool CanDownload() =>
        IsReady
        && !IsBusy
        && !IsPlaylist
        && MediaTitle is not null
        && !string.IsNullOrWhiteSpace(OutputFolder);

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeAsync(CancellationToken ct)
    {
        IsBusy = true;
        StatusMessage = "Analyzing...";
        MediaTitle = null;
        IsPlaylist = false;
        AvailableResolutions.Clear();

        try
        {
            string url = Url.Trim();
            MediaInfo info = await downloadService.GetInfoAsync(url, ct);

            _analyzedUrl = url;
            IsPlaylist = info.IsPlaylist;

            AvailableResolutions.Add(new ResolutionOption("Best available", null));
            foreach (int height in info.AvailableHeights)
            {
                AvailableResolutions.Add(new ResolutionOption($"{height}p", height));
            }

            SelectedResolution = AvailableResolutions[0];
            MediaTitle = info.IsPlaylist ? $"Playlist: {info.Title}" : info.Title;

            StatusMessage = info.IsPlaylist
                ? "Playlist downloads arrive in the next phase."
                : "Choose format, resolution and folder, then press Download.";
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

    [RelayCommand]
    private async Task BrowseFolderAsync()
    {
        string? folder = await folderPicker.PickFolderAsync();
        if (folder is not null)
        {
            OutputFolder = folder;
        }
    }

    [RelayCommand(CanExecute = nameof(CanDownload))]
    private async Task DownloadAsync(CancellationToken ct)
    {
        IsBusy = true;
        IsDownloading = true;
        ProgressPercent = 0;
        ProgressText = string.Empty;
        StatusMessage = "Downloading...";

        var progress = new Progress<DownloadProgressInfo>(p =>
        {
            ProgressPercent = p.Percent;
            ProgressText = $"{p.Percent:0}%  {p.Speed}  ETA {p.Eta}";
        });

        try
        {
            int? maxHeight = SelectedKind == DownloadKind.Video ? SelectedResolution?.Height : null;
            var request = new DownloadRequest(_analyzedUrl, OutputFolder, SelectedKind, maxHeight);

            string path = await downloadService.DownloadAsync(request, progress, ct);

            ProgressPercent = 100;
            StatusMessage = $"Saved to: {path}";
        }
        catch (DownloadException ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Download cancelled.";
        }
        finally
        {
            IsBusy = false;
            IsDownloading = false;
        }
    }

    [RelayCommand]
    private void CancelDownload() => DownloadCommand.Cancel();
}