using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FFmpegDownloader.Models;
using System.Collections.ObjectModel;

namespace FFmpegDownloader.ViewModels;

public sealed partial class AdvancedOptionsViewModel : ObservableObject
{

    #region "Propriétés observables"

    public ObservableCollection<HttpHeaderViewModel> Headers { get; } = [];

    [ObservableProperty]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Artist { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Album { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Date { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Genre { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Comment { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ExtraArguments { get; set; } = string.Empty;

    #endregion

    #region Commandes

    [RelayCommand]
    private void AddHeader()
    {
        Headers.Add(new HttpHeaderViewModel());
    }

    [RelayCommand]
    private void RemoveHeader(HttpHeaderViewModel header)
    {
        Headers.Remove(header);
    }

    #endregion

    public DownloadOptions CreateSnapshot() =>
        new(Headers,
            Title,
            Artist,
            Album,
            Date,
            Genre,
            Comment,
            ExtraArguments);

    public void Reset()
    {
        Headers.Clear();
        Title = string.Empty;
        Artist = string.Empty;
        Album = string.Empty;
        Date = string.Empty;
        Genre = string.Empty;
        Comment = string.Empty;
        ExtraArguments = string.Empty;
    }
}
