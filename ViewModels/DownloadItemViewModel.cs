using CommunityToolkit.Mvvm.ComponentModel;
using FFmpegDownloader.Enums;
using System.IO;

namespace FFmpegDownloader.ViewModels;

public partial class DownloadItemViewModel(string url,
                                           string outputPath,
                                           DownloadOptionsSnapshot options) : ObservableObject
{
    public readonly string Url = url;

    public readonly string OutputPath = outputPath;

    public readonly DownloadOptionsSnapshot Options = options;

    [ObservableProperty]
    public partial double Progress { get; set; } = 0;

    [ObservableProperty]
    public partial TimeSpan CurrentTime { get; set; } = TimeSpan.Zero;

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
