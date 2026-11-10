using FFMpegCore;
using FFmpegDownloader.ViewModels;
using System.ComponentModel;
using System.Diagnostics;

namespace FFmpegDownloader.Services;

public sealed class FFmpegService : IFFmpegService
{
    public async Task<string?> GetVersionAsync()
    {
        try
        {
            using Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };

            process.Start();

            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            string output = await outputTask;
            string error = await errorTask;

            if (process.ExitCode != 0)
                return null;

            // Première ligne : ffmpeg version X.X.X ...
            return new([.. output.TakeWhile(c => c != '\n')]);
        }
        catch (Exception ex) when (ex is Win32Exception
                                      or InvalidOperationException)
        {
            // FFmpeg absent ou impossible à exécuter.
            return null;
        }
    }

    public async Task<bool> InstallAsync()
    {
        try
        {
            using Process process = new()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "winget",
                    Arguments = "install --id Gyan.FFmpeg.Essentials --exact --accept-package-agreements --accept-source-agreements",
                    UseShellExecute = true,
                    CreateNoWindow = false
                }
            };

            process.Start();
            await process.WaitForExitAsync();

            // winget error --input -1978335189
            // 0x8a15002b : APPINSTALLER_CLI_ERROR_UPDATE_NOT_APPLICABLE
            // Aucune mise à jour applicable trouvée

            return process.ExitCode == 0 || process.ExitCode == -1978335189;
        }
        catch (Exception ex) when (ex is Win32Exception
                                      or InvalidOperationException)
        {
            return false;
        }
    }

    public async Task<bool> RunAsync(DownloadItemViewModel download,
                                     IProgress<string> output,
                                     IProgress<TimeSpan> currentTime,
                                     IProgress<double> progress,
                                     CancellationToken cancellationToken = default)
    {
        IMediaAnalysis mediaAnalysis = await AnalyseAsync(download, cancellationToken);

        download.TotalTime = mediaAnalysis.Duration;

        FFMpegArgumentProcessor processor = FFMpegArguments
            .FromUrlInput(download.Url, options =>
            {
                string headers = download.Options.HeadersString();
                if (headers.Length > 0)
                    options.WithCustomArgument($"-headers \"{headers}\"");
            })
            .OutputToFile(download.OutputPath, overwrite: true, options => options
                .CopyStreams()
                .WithMetadata("title", download.Options.Title)
                .WithMetadata("artist", download.Options.Artist)
                .WithMetadata("album", download.Options.Album)
                .WithMetadata("date", download.Options.Date)
                .WithMetadata("genre", download.Options.Genre)
                .WithMetadata("comment", download.Options.Comment))
            .NotifyOnStandardError(output.Report)
            .NotifyOnProgress(currentTime)
            .NotifyOnPercentageProgress(progress, mediaAnalysis.Duration);

        FFMpegResult result = await processor.ProcessAsynchronously(
            throwOnError: false,
            cancellationToken: cancellationToken);

        return result.Success;
    }

    private static async Task<IMediaAnalysis> AnalyseAsync(DownloadItemViewModel download, CancellationToken cancellationToken = default)
    {
        string headers = download.Options.HeadersString();
        string? customArguments = headers.Length > 0
            ? $"-headers \"{headers}\""
            : null;

        return await FFProbe.AnalyseAsync(download.Url,
                                          customArguments: customArguments,
                                          cancellationToken: cancellationToken);
    }
}
