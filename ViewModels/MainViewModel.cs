using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFmpegDownloader.Enums;
using FFmpegDownloader.Services;
using FFmpegDownloader.Windows;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;

namespace FFmpegDownloader.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider serviceProvider;
    private readonly IFFmpegService ffmpegService;
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

    [ObservableProperty]
    public partial string FFmpegOutput { get; set; } = string.Empty;

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
                         AdvancedOptionsViewModel advancedOptionsViewModel)
    {
        this.serviceProvider = serviceProvider;
        this.ffmpegService = ffmpegService;
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

        IProgress<string> output = new Progress<string>(message => FFmpegOutput += message + Environment.NewLine);

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

        Task? completionTask = queueCompletion?.Task;

        queueCancellation?.Cancel();

        if (completionTask is not null)
            await completionTask;
    }
}
