using FFmpegDownloader.Models;

namespace FFmpegDownloader.Interfaces;

public interface IFFmpegService
{
    Task<string?> GetVersionAsync();

    Task<bool> InstallAsync();

    Task<bool> RunAsync(DownloadRequest download,
                        IProgress<string> outputNotifier,
                        IProgress<TimeSpan> currentTimeNotifier,
                        IProgress<TimeSpan> totalTimeNotifier,
                        IProgress<double> progressNotifier,
                        CancellationToken cancellationToken = default);
}
