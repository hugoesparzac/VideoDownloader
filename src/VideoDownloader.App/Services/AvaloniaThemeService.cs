using Avalonia;
using Avalonia.Styling;

using VideoDownloader.Core.Models;

namespace VideoDownloader.App.Services;

public sealed class AvaloniaThemeService : IThemeService
{
    public void Apply(AppTheme theme)
    {
        if (Application.Current is null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant = theme switch
        {
            AppTheme.Light => ThemeVariant.Light,
            AppTheme.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }
}