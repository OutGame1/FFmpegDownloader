using CommunityToolkit.Mvvm.ComponentModel;
using FFmpegDownloader.Interfaces;

namespace FFmpegDownloader.ViewModels;

public sealed partial class HttpHeaderViewModel : ObservableObject, IHttpHeader
{
    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Value { get; set; } = string.Empty;
}
