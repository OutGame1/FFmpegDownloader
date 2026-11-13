using FFmpegDownloader.Interfaces;
using FFmpegDownloader.Services;
using FFmpegDownloader.ViewModels;
using FFmpegDownloader.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Windows;
using Wpf.Ui;

namespace FFmpegDownloader;

public sealed partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<MainViewModel>()
                        .AddSingleton<MainWindow>()
                        .AddSingleton<AdvancedOptionsViewModel>()
                        .AddTransient<AdvancedOptionsWindow>()
                        .AddTransient<CurlImportViewModel>()
                        .AddTransient<CurlImportWindow>()
                        .AddSingleton<IFFmpegService, FFmpegService>()
                        .AddSingleton<ICurlParserService, CurlParserService>()
                        .AddSingleton<IContentDialogService, ContentDialogService>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e)
    {
        await _host.StartAsync();

        MainWindow mainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();

        await mainWindow.InitializeAsync();

        base.OnStartup(e);
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();

        base.OnExit(e);
    }
}
