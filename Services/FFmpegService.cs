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
        FFMpegArgumentProcessor processor = FFMpegArguments
            .FromUrlInput(download.Url, options => BuildHeaders(download, options))
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
            .NotifyOnPercentageProgress(progress);

        FFMpegResult result = await processor.ProcessAsynchronously(
            throwOnError: false,
            cancellationToken: cancellationToken);

        return result.Success;
    }

    private static void BuildHeaders(DownloadItemViewModel download, FFMpegInputOptions options)
    {
        if (download.Options.Headers.Count == 0)
        {
            return;
        }

        string headers = string.Concat(
            download.Options.Headers.Select(h => $"{h.Name}: {h.Value}\r\n"));
        options.WithCustomArgument($"-headers \"{headers}\"");
    }
}
