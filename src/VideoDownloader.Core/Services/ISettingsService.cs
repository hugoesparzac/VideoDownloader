using VideoDownloader.Core.Models;

namespace VideoDownloader.Core.Services;

public interface ISettingsService
{
    AppSettings Load();

    void Save(AppSettings settings);
}