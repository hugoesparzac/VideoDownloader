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
    IImageDownloadService imageService,
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
    [NotifyCanExecuteChangedFor(nameof(UpdateToolsCommand))]
    public partial bool IsReady { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AnalyzeCommand))]
    [NotifyCanExecuteChangedFor(nameof(DownloadCommand))]
    [NotifyCanExecuteChangedFor(nameof(UpdateToolsCommand))]
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

    [ObservableProperty]
    public partial BrowserOption SelectedBrowser { get; set; } = BrowserOption.None;

    [ObservableProperty]
    public partial bool IsImagePost { get; set; }

    [ObservableProperty]
    public partial bool IsProgressIndeterminate { get; set; } = true;

    [ObservableProperty]
    public partial string ToolsStatus { get; set; } = string.Empty;

    public IReadOnlyList<BrowserOption> Browsers { get; } = BrowserOption.GetAvailable();

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
            await RefreshToolsStatusAsync();
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

    private bool CanUpdateTools() => IsReady && !IsBusy;

    [RelayCommand(CanExecute = nameof(CanUpdateTools))]
    private async Task UpdateToolsAsync(CancellationToken ct)
    {
        IsBusy = true;
        StatusMessage = "Updating tools...";

        try
        {
            string ytDlp = await TryUpdateAsync("yt-dlp", async () => await downloadService.UpdateToolAsync(ct));
            string gallery = await TryUpdateAsync("gallery-dl", () => imageService.UpdateToolAsync(ct));

            StatusMessage = $"{ytDlp} | {gallery}";
            await RefreshToolsStatusAsync();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Update cancelled.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static async Task<string> TryUpdateAsync(string name, Func<Task<string?>> update)
    {
        try
        {
            string? version = await update();
            return $"{name} {version ?? "updated"}";
        }
        catch (DownloadException ex)
        {
            return $"{name}: {ex.Message}";
        }
    }

    private async Task RefreshToolsStatusAsync()
    {
        try
        {
            string ytDlp = await downloadService.GetToolVersionAsync();
            string? gallery = await imageService.GetToolVersionAsync();
            ToolsStatus = $"yt-dlp {ytDlp}  |  gallery-dl {gallery ?? "not installed yet"}";
        }
        catch (DownloadException)
        {
            ToolsStatus = string.Empty;
        }
    }

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
            if (!UrlHelper.TryNormalize(Url, out string url))
            {
                StatusMessage = "That doesn't look like a valid http(s) URL.";
                return;
            }

            if (UrlHelper.IsTikTokPhotoPost(url))
            {
                ShowImagePost(url);
                return;
            }

            MediaInfo info;
            try
            {
                info = await downloadService.GetInfoAsync(url, SelectedBrowser.Value, ct);
            }
            catch (DownloadException) when (UrlHelper.IsInstagramPost(url))
            {
                ShowImagePost(url);
                return;
            }

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
            StatusMessage = $"Error: {ErrorHints.Describe(ex.Message)}";
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

        IsProgressIndeterminate = IsImagePost;

        int? maxHeight = SelectedKind == DownloadKind.Video ? SelectedResolution?.Height : null;

        try
        {
            if (IsImagePost)
            {
                await DownloadImagesAsync(ct);
            }
            else if (IsPlaylist)
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
            StatusMessage = $"Error: {ErrorHints.Describe(ex.Message)}";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Download cancelled.";
        }
        finally
        {
            IsBusy = false;
            IsDownloading = false;
            IsProgressIndeterminate = true;
        }
    }

    [RelayCommand]
    private void CancelDownload() => DownloadCommand.Cancel();

    private void ShowImagePost(string url)
    {
        _analyzedUrl = url;
        IsPlaylist = false;
        IsImagePost = true;
        MediaTitle = $"Image post: {UrlHelper.GetPostFolderName(url)}";
        StatusMessage = "Image post detected. Press Download to save the images.";
    }

    private async Task DownloadImagesAsync(CancellationToken ct)
    {
        var progress = new Progress<int>(count => ProgressText = $"{count} file(s) downloaded");

        var request = new ImageDownloadRequest(_analyzedUrl, OutputFolder, SelectedBrowser.Value);
        ImageDownloadResult result = await imageService.DownloadAsync(request, progress, ct);

        ProgressPercent = 100;
        StatusMessage = result.FileCount == 1
            ? $"Saved to: {result.Location}"
            : $"Saved {result.FileCount} images to: {result.Location}";
    }

    private async Task DownloadSingleAsync(int? maxHeight, CancellationToken ct)
    {
        var progress = new Progress<DownloadProgressInfo>(p =>
        {
            ProgressPercent = p.Percent;
            ProgressText = $"{p.Percent:0}%  {p.Speed}  ETA {p.Eta}";
        });

        var request = new DownloadRequest(_analyzedUrl, OutputFolder, SelectedKind, maxHeight, SelectedBrowser.Value);
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

        var request = new PlaylistDownloadRequest(_playlistTitle, _playlistItems, OutputFolder, SelectedKind, maxHeight, SelectedBrowser.Value);

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