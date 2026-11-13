using CommunityToolkit.Mvvm.ComponentModel;

namespace FFmpegDownloader.ViewModels;

public sealed partial class CurlImportViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string CommandText { get; set; } = string.Empty;
}
