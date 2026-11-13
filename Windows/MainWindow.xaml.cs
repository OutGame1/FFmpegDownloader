using FFmpegDownloader.Interfaces;
using FFmpegDownloader.ViewModels;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace FFmpegDownloader.Windows;

public sealed partial class MainWindow : FluentWindow
{
    private readonly MainViewModel mainViewModel;
    private readonly IFFmpegService ffmpegService;
    private readonly IContentDialogService dialogService;

    private bool isConsoleAtBottom = true;

    public MainWindow(MainViewModel mainViewModel,
                      IFFmpegService ffmpegService,
                      IContentDialogService dialogService)
    {
        InitializeComponent();

        this.mainViewModel = mainViewModel;
        this.ffmpegService = ffmpegService;
        this.dialogService = dialogService;

        DataContext = mainViewModel;

        dialogService.SetDialogHost(RootContentDialogHost);

        Closing += OnClosing;
        FFmpegConsole.ScrollChanged += OnFFmpegConsoleScrollChanged;
        mainViewModel.FFmpegOutput.CollectionChanged += FFmpegOutputChanged;
    }

    public async Task InitializeAsync()
    {
        string? version = await ffmpegService.GetVersionAsync();
        if (version is not null)
            return;

        ContentDialog dialog = new()
        {
            Title = "FFmpeg manquant",
            Content =
            """
            FFmpeg n'a pas été détecté sur votre ordinateur.
            Ce logiciel est nécessaire pour que l'application fonctionne correctement.
            
            Voulez-vous l'installer automatiquement ?
            """,
            PrimaryButtonText = "Oui",
            CloseButtonText = "Non, fermer l'application",
            PrimaryButtonAppearance = ControlAppearance.Success,
            CloseButtonAppearance = ControlAppearance.Danger,
            DefaultButton = ContentDialogButton.Primary
        };

        ContentDialogResult result = await dialogService.ShowAsync(dialog, CancellationToken.None);

        if (result != ContentDialogResult.Primary)
        {
            Application.Current.Shutdown();
            return;
        }

        IsEnabled = false;
        bool installed = await ffmpegService.InstallAsync();
        IsEnabled = true;

        if (installed)
        {
            dialog = new()
            {
                Title = "Installation terminée",
                Content =
                """
                FFmpeg a été installé.
                
                Veuillez redémarrer l'application pour actualiser les variables d'environnement.
                """,
                CloseButtonText = "OK",
                CloseButtonAppearance = ControlAppearance.Info,
                DefaultButton = ContentDialogButton.Close
            };
        }
        else
        {
            dialog = new()
            {
                Title = "Erreur",
                Content = "L'installation de FFmpeg a échoué.",
                CloseButtonText = "OK",
                CloseButtonAppearance = ControlAppearance.Info,
                DefaultButton = ContentDialogButton.Close
            };
        }

        await dialogService.ShowAsync(dialog, CancellationToken.None);

        Application.Current.Shutdown();
    }

    private void OnFFmpegConsoleScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        isConsoleAtBottom =
            e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 1;
    }

    private void FFmpegOutputChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (isConsoleAtBottom)
            FFmpegConsole.ScrollToEnd();
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;

        Closing -= OnClosing;

        try
        {
            await mainViewModel.StopQueueAsync();
        }
        finally
        {
            await Dispatcher.InvokeAsync(Close);
        }
    }
}
