using FFmpegDownloader.Services;
using FFmpegDownloader.ViewModels;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace FFmpegDownloader.Windows;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel mainViewModel;
    private readonly IFFmpegService ffmpegService;
    private readonly IContentDialogService dialogService;

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
    }

    private void BrowseOutputFile(object sender, RoutedEventArgs e)
    {
        SaveFileDialog dialog = new()
        {
            Filter = "Matroska (*.mkv)|*.mkv|MP4 (*.mp4)|*.mp4",
            AddExtension = true,
            FileName = Path.GetFileName(mainViewModel.OutputPath)
        };

        if (dialog.ShowDialog() == true)
        {
            mainViewModel.OutputPath = dialog.FileName;
        }
    }

    private void ParseCurlCommand(object sender, RoutedEventArgs e)
    {

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

    private void FFmpegOutputChanged(object sender, TextChangedEventArgs e)
    {
        FFmpegConsole.ScrollToEnd();
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;

        Closing -= OnClosing;

        try
        {
            mainViewModel.LoadingMessage = "Arrêt de la file d'attente...";
            await mainViewModel.StopQueueAsync();
        }
        finally
        {
            mainViewModel.LoadingMessage = string.Empty;
            await Dispatcher.InvokeAsync(Close);
        }
    }
}
