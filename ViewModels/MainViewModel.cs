using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFmpegDownloader.Enums;
using FFmpegDownloader.Interfaces;
using FFmpegDownloader.Models;
using FFmpegDownloader.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace FFmpegDownloader.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider serviceProvider;
    private readonly IFFmpegService ffmpegService;
    private readonly ICurlParserService curlParserService;
    private readonly AdvancedOptionsViewModel advancedOptionsViewModel;
    private CancellationTokenSource? queueCancellation;
    private TaskCompletionSource? queueCompletion;

    public ObservableCollection<DownloadItemViewModel> Downloads { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddToQueueCommand))]
    public partial string VideoUrl { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddToQueueCommand))]
    public partial string OutputPath { get; set; } = string.Empty;

    public ObservableCollection<string> FFmpegOutput { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartQueueCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopQueueCommand))]
    public partial bool IsQueueRunning { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoading))]
    public partial string LoadingMessage { get; set; } = string.Empty;

    public bool IsLoading => LoadingMessage.Length != 0;

    public MainViewModel(IServiceProvider serviceProvider,
                         IFFmpegService ffmpegService,
                         ICurlParserService curlParserService,
                         AdvancedOptionsViewModel advancedOptionsViewModel)
    {
        this.serviceProvider = serviceProvider;
        this.ffmpegService = ffmpegService;
        this.curlParserService = curlParserService;
        this.advancedOptionsViewModel = advancedOptionsViewModel;

        Downloads.CollectionChanged += (_, _) =>
        {
            StartQueueCommand.NotifyCanExecuteChanged();
            StopQueueCommand.NotifyCanExecuteChanged();
        };
    }

    [RelayCommand]
    private void OpenAdvancedOptions()
    {
        AdvancedOptionsWindow window = serviceProvider.GetRequiredService<AdvancedOptionsWindow>();
        window.Owner = Application.Current.MainWindow;
        window.ShowDialog();
    }

    [RelayCommand(CanExecute = nameof(CanAddToQueue))]
    private void AddToQueue()
    {
        DownloadOptionsSnapshot options = advancedOptionsViewModel.CreateSnapshot();

        Downloads.Add(new DownloadItemViewModel(VideoUrl, OutputPath, options));

        VideoUrl = string.Empty;
        OutputPath = string.Empty;
        advancedOptionsViewModel.Reset();
    }

    private bool CanAddToQueue() =>
        Uri.TryCreate(VideoUrl, UriKind.Absolute, out Uri? uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && OutputPath.Length > 0;

    [RelayCommand(CanExecute = nameof(CanStartQueue))]
    private async Task StartQueueAsync()
    {
        if (IsQueueRunning)
            return;

        IsQueueRunning = true;

        CancellationTokenSource cancellationSource = new();
        queueCancellation = cancellationSource;
        queueCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        CancellationToken cancellationToken = cancellationSource.Token;

        FFmpegOutput.Clear();
        IProgress<string> output = new Progress<string>(FFmpegOutput.Add);

        try
        {
            // Utilisation d'une boucle for pour pouvoir ajouter
            // des téléchargements pendant l'exécution de la file.
            for (int i = 0; i < Downloads.Count; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                DownloadItemViewModel download = Downloads[i];

                if (download.Status != DownloadStatus.Pending)
                    continue;

                download.Status = DownloadStatus.Downloading;
                download.Progress = 0;

                output.Report($">>> Téléchargement : {download.Url}");

                IProgress<TimeSpan> currentTime = new Progress<TimeSpan>(
                        value => download.CurrentTime = value);

                IProgress<double> progress = new Progress<double>(
                    value => download.Progress = value);

                try
                {
                    bool success = await ffmpegService.RunAsync(download, output, currentTime, progress, cancellationToken);

                    if (success)
                    {
                        download.Status = DownloadStatus.Completed;
                        output.Report(">>> Téléchargement terminé.");
                    }
                    else
                    {
                        download.Status = DownloadStatus.Failed;
                        output.Report($">>> Échec du téléchargement.");
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    download.Status = DownloadStatus.Paused;
                    output.Report(">>> Téléchargement interrompu.");
                    break;
                }
                catch (Exception exception)
                {
                    download.Status = DownloadStatus.Failed;
                    output.Report($">>> Erreur : {exception.Message}");
                }
            }
        }
        finally
        {
            queueCancellation = null;
            cancellationSource.Dispose();
            IsQueueRunning = false;

            queueCompletion?.TrySetResult();
            queueCompletion = null;
        }
    }

    private bool CanStartQueue() => !IsQueueRunning && Downloads.Any(d => d.Status == DownloadStatus.Pending);

    [RelayCommand(CanExecute = nameof(IsQueueRunning))]
    public async Task StopQueueAsync()
    {
        if (!IsQueueRunning)
            return;

        try
        {
            LoadingMessage = "Arrêt de la file d'attente...";

            Task? completionTask = queueCompletion?.Task;

            queueCancellation?.Cancel();

            if (completionTask is not null)
                await completionTask;
        }
        finally
        {
            LoadingMessage = string.Empty;
        }
    }

    [RelayCommand]
    private void BrowseOutputFile()
    {
        SaveFileDialog dialog = new()
        {
            Filter = "Matroska (*.mkv)|*.mkv|MP4 (*.mp4)|*.mp4",
            AddExtension = true,
            FileName = Path.GetFileName(OutputPath)
        };

        if (dialog.ShowDialog() == true)
        {
            OutputPath = dialog.FileName;
        }
    }

    [RelayCommand]
    private void ImportCurl()
    {
        CurlImportWindow window = serviceProvider.GetRequiredService<CurlImportWindow>();
        window.Owner = Application.Current.MainWindow;

        if (window.ShowDialog() != true)
            return;

        try
        {
            CurlRequest request = curlParserService.Parse(window.CommandText);

            VideoUrl = request.Url;

            advancedOptionsViewModel.Headers.Clear();
            foreach (KeyValuePair<string, string> header in request.Headers)
            {
                advancedOptionsViewModel.Headers.Add(
                    new HttpHeaderViewModel
                    {
                        Name = header.Key,
                        Value = header.Value
                    });
            }
        } catch (Exception exception)
        {
            // TODO: Afficher un message d'erreur à l'utilisateur.
            Debug.WriteLine(exception.Message);
        }
    }
}
