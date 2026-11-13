using CommunityToolkit.Mvvm.ComponentModel;
using FFmpegDownloader.Enums;
using FFmpegDownloader.Models;
using System.IO;

namespace FFmpegDownloader.ViewModels;

public partial class DownloadItemViewModel(string url,
                                           string outputPath,
                                           DownloadOptionsSnapshot options) : ObservableObject
{
    public Uri Url { get; } = new Uri(url);

    public string OutputPath { get; } = outputPath;

    public DownloadOptionsSnapshot Options { get; } = options;

    [ObservableProperty]
    public partial double Progress { get; set; } = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedTime))]
    public partial TimeSpan CurrentTime { get; set; } = TimeSpan.Zero;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FormattedTime))]
    public partial TimeSpan TotalTime { get; set; } = TimeSpan.Zero;

    public string FormattedTime => $"{CurrentTime:hh\\:mm\\:ss} / {TotalTime:hh\\:mm\\:ss}";

    public string FileName => Path.GetFileName(OutputPath);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StatusMessage))]
    public partial DownloadStatus Status { get; set; } = DownloadStatus.Pending;

    public string StatusMessage => Status switch
    {
        DownloadStatus.Pending => "En attente",
        DownloadStatus.Downloading => "En cours",
        DownloadStatus.Completed => "Terminé",
        DownloadStatus.Paused => "En pause",
        DownloadStatus.Failed => "Échec",
        _ => "Inconnu"
    };
}
