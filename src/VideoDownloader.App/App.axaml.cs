using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using Microsoft.Extensions.DependencyInjection;

using VideoDownloader.App.Services;
using VideoDownloader.App.ViewModels;
using VideoDownloader.App.Views;
using VideoDownloader.Core.Services;

namespace VideoDownloader.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection();
            services.AddSingleton<IDownloadService>(_ => new DownloadService());
            services.AddSingleton<IFolderPickerService>(_ => new FolderPickerService(() => desktop.MainWindow));
            services.AddTransient<MainViewModel>();
            services.AddSingleton<IImageDownloadService>(_ => new GalleryDlService());
            services.AddSingleton<ISettingsService>(_ => new JsonSettingsService());
            var provider = services.BuildServiceProvider();

            var viewModel = provider.GetRequiredService<MainViewModel>();
            viewModel.LoadSettings();

            var window = new MainWindow
            {
                DataContext = viewModel,
                Title = $"Video Downloader {GetVersion()}",
            };
            window.Opened += async (_, _) => await viewModel.InitializeAsync();
            window.Closing += (_, _) => viewModel.SaveSettings();

            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static string GetVersion()
    {
        Version? version = typeof(App).Assembly.GetName().Version;
        return version is null ? string.Empty : $"v{version.Major}.{version.Minor}.{version.Build}";
    }
}