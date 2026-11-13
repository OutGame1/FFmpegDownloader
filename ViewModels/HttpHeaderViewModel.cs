using CommunityToolkit.Mvvm.ComponentModel;

namespace FFmpegDownloader.ViewModels;

public sealed partial class HttpHeaderViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Value { get; set; } = string.Empty;
}
