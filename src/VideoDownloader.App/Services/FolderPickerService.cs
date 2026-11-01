using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace VideoDownloader.App.Services;

public sealed class FolderPickerService(Func<TopLevel?> getOwner) : IFolderPickerService
{
    public async Task<string?> PickFolderAsync()
    {
        TopLevel? owner = getOwner();
        if (owner is null)
        {
            return null;
        }

        var folders = await owner.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select download folder",
            AllowMultiple = false,
        });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }
}