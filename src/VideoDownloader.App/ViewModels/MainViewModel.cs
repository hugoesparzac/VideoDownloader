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
    private static readonly int[] PlaylistHeights = [2160, 1440, 1080, 720, 480, 360, 240, 144];

    private string _analyzedUrl = string.Empty;
    private string _playlistTitle = string.Empty;
    private IReadOnlyList<PlaylistItem> _playlistItems = [];

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

    [ObservableProperty]
    public partial bool HasFailures { get; set; }

    public IReadOnlyList<DownloadKind> Kinds { get; } = [DownloadKind.Video, DownloadKind.Audio];

    public ObservableCollection<ResolutionOption> AvailableResolutions { get; } = [];

    public ObservableCollection<string> FailedItems { get; } = [];

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
        && MediaTitle is not null
        && (!IsPlaylist || _playlistItems.Count > 0)
        && !string.IsNullOrWhiteSpace(OutputFolder);

    [RelayCommand(CanExecute = nameof(CanAnalyze))]
    private async Task AnalyzeAsync(CancellationToken ct)
    {
        IsBusy = true;
        StatusMessage = "Analyzing...";
        MediaTitle = null;
        IsPlaylist = false;
        HasFailures = false;
        FailedItems.Clear();
        AvailableResolutions.Clear();

        try
        {
            string url = Url.Trim();
            MediaInfo info = await downloadService.GetInfoAsync(url, ct);

            _analyzedUrl = url;
            _playlistTitle = info.Title;
            _playlistItems = info.Items;
            IsPlaylist = info.IsPlaylist;

            AvailableResolutions.Add(new ResolutionOption("Best available", null));
            IEnumerable<int> heights = info.IsPlaylist ? PlaylistHeights : info.AvailableHeights;
            foreach (int height in heights)
            {
                AvailableResolutions.Add(new ResolutionOption($"{height}p", height));
            }

            SelectedResolution = AvailableResolutions[0];

            MediaTitle = info.IsPlaylist
                ? $"Playlist: {info.Title} ({info.Items.Count} items)"
                : info.Title;

            StatusMessage = info.IsPlaylist
                ? info.Items.Count > 0
                    ? "Playlist detected. It will be saved in its own folder."
                    : "This playlist has no downloadable items."
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
        HasFailures = false;
        FailedItems.Clear();
        StatusMessage = "Downloading...";

        int? maxHeight = SelectedKind == DownloadKind.Video ? SelectedResolution?.Height : null;

        try
        {
            if (IsPlaylist)
            {
                await DownloadPlaylistAsync(maxHeight, ct);
            }
            else
            {
                await DownloadSingleAsync(maxHeight, ct);
            }
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

    private async Task DownloadSingleAsync(int? maxHeight, CancellationToken ct)
    {
        var progress = new Progress<DownloadProgressInfo>(p =>
        {
            ProgressPercent = p.Percent;
            ProgressText = $"{p.Percent:0}%  {p.Speed}  ETA {p.Eta}";
        });

        var request = new DownloadRequest(_analyzedUrl, OutputFolder, SelectedKind, maxHeight);
        string path = await downloadService.DownloadAsync(request, progress, ct);

        ProgressPercent = 100;
        StatusMessage = $"Saved to: {path}";
    }

    private async Task DownloadPlaylistAsync(int? maxHeight, CancellationToken ct)
    {
        var progress = new Progress<PlaylistProgressInfo>(p =>
        {
            ProgressPercent = ((p.Index - 1) + (p.Item.Percent / 100)) / p.Total * 100;
            ProgressText = $"[{p.Index}/{p.Total}] {p.Title} - {p.Item.Percent:0}%  {p.Item.Speed}";
        });

        var request = new PlaylistDownloadRequest(
            _playlistTitle, _playlistItems, OutputFolder, SelectedKind, maxHeight);

        PlaylistDownloadResult result = await downloadService.DownloadPlaylistAsync(request, progress, ct);

        foreach (FailedItem failure in result.Failed)
        {
            FailedItems.Add($"{failure.Title}: {failure.Reason}");
        }

        HasFailures = result.Failed.Count > 0;
        ProgressPercent = 100;

        string skipped = HasFailures ? $", {result.Failed.Count} skipped" : string.Empty;
        StatusMessage = $"Done: {result.Succeeded} of {_playlistItems.Count} downloaded{skipped}. Folder: {result.FolderPath}";
    }
}