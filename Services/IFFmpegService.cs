using FFmpegDownloader.ViewModels;

namespace FFmpegDownloader.Services;

public interface IFFmpegService
{
    Task<string?> GetVersionAsync();

    Task<bool> InstallAsync();

    Task<bool> RunAsync(DownloadItemViewModel download,
                       IProgress<string> output,
                       IProgress<TimeSpan> currentTime,
                       IProgress<double> progress,
                       CancellationToken cancellationToken = default);
}
