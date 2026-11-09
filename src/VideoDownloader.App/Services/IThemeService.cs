using VideoDownloader.Core.Models;

namespace VideoDownloader.App.Services;

public interface IThemeService
{
    void Apply(AppTheme theme);
}