using CommunityToolkit.Mvvm.ComponentModel;
using FFmpegDownloader.Enums;
using System.IO;

namespace FFmpegDownloader.ViewModels;

public partial class DownloadItemViewModel(string url,
                                           string outputPath,
                                           DownloadOptionsSnapshot options) : ObservableObject
{
    public string Url { get; } = url;

    public string OutputPath { get; } = outputPath;

    public double Progress { get; set; } = 0;

    public string FileName => Path.GetFileName(OutputPath);

    public DownloadOptionsSnapshot Options { get; } = options;

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
