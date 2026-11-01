namespace VideoDownloader.App.Services;

public interface IFolderPickerService
{
    Task<string?> PickFolderAsync();
}