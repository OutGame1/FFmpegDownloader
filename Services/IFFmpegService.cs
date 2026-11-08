using FFmpegDownloader.ViewModels;

namespace FFmpegDownloader.Services;

public interface IFFmpegService
{
    Task<string?> GetVersionAsync();

    Task<bool> InstallAsync();

    Task<int> RunAsync(DownloadItemViewModel download,
                       IProgress<string> output,
                       IProgress<double> progress,
                       CancellationToken cancellationToken = default);
}
