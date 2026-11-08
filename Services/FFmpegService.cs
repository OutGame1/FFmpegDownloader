using FFmpegDownloader.ViewModels;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;

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

    public async Task<int> RunAsync(DownloadItemViewModel download,
                                    IProgress<string> output,
                                    IProgress<double> progress,
                                    CancellationToken cancellationToken = default)
    {
        ProcessStartInfo startInfo = new()
        {
            FileName = "ffmpeg",
            ArgumentList =
            {
                "-hide_banner",
                "-nostdin",
                "-y",
                "-progress",
                "pipe:1",
                "-nostats"
            },
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        // En-têtes HTTP
        if (download.Options.Headers.Count > 0)
        {
            string headers = string.Concat(
                download.Options.Headers.Select(h => $"{h.Name}: {h.Value}\r\n"));

            startInfo.ArgumentList.Add("-headers");
            startInfo.ArgumentList.Add(headers);
        }

        // Source
        startInfo.ArgumentList.Add("-i");
        startInfo.ArgumentList.Add(download.Url);

        // Copie des flux sans réencodage
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add("copy");

        // Métadonnées
        startInfo.ArgumentList.Add("-metadata");
        startInfo.ArgumentList.Add($"title={download.Options.Title}");

        startInfo.ArgumentList.Add("-metadata");
        startInfo.ArgumentList.Add($"artist={download.Options.Artist}");

        startInfo.ArgumentList.Add("-metadata");
        startInfo.ArgumentList.Add($"album={download.Options.Album}");

        startInfo.ArgumentList.Add("-metadata");
        startInfo.ArgumentList.Add($"date={download.Options.Date}");

        startInfo.ArgumentList.Add("-metadata");
        startInfo.ArgumentList.Add($"genre={download.Options.Genre}");

        startInfo.ArgumentList.Add("-metadata");
        startInfo.ArgumentList.Add($"comment={download.Options.Comment}");

        // Destination
        startInfo.ArgumentList.Add(download.OutputPath);

        using Process process = new()
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        process.Start();

        cancellationToken.ThrowIfCancellationRequested();

        using CancellationTokenRegistration registration = cancellationToken.Register(() =>
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // Le processus est déjà terminé.
            }
            catch (Win32Exception)
            {
                // Impossible de terminer le processus.
            }
        });

        long durationTicks = 0;

        // Lecture des messages FFmpeg (stderr)
        async Task ReadErrorAsync()
        {
            while (await process.StandardError.ReadLineAsync(cancellationToken) is { } line)
            {
                output.Report(line);

                // Récupération de la durée totale
                int index = line.IndexOf("Duration:", StringComparison.Ordinal);

                if (index >= 0)
                {
                    string durationText = line[(index + 9)..].Split(',')[0].Trim();

                    if (TimeSpan.TryParse(durationText, CultureInfo.InvariantCulture, out TimeSpan duration))
                        Interlocked.Exchange(ref durationTicks, duration.Ticks);
                }
            }
        }

        // Lecture de la progression (stdout)
        async Task ReadProgressAsync()
        {
            while (await process.StandardOutput.ReadLineAsync(cancellationToken) is { } line)
            {
                Debug.WriteLine($"[PROGRESS DEBUG] {line}");

                if (!line.StartsWith("out_time_us=", StringComparison.Ordinal))
                    continue;

                if (!long.TryParse(line.AsSpan("out_time_us=".Length), out long outputTimeUs))
                    continue;

                long totalTicks = Interlocked.Read(ref durationTicks);

                Debug.WriteLine($"[PROGRESS DEBUG] Durée : {totalTicks} ticks, temps traité : {outputTimeUs} µs");

                if (totalTicks <= 0)
                    continue;

                double percentage = outputTimeUs * 10.0 / totalTicks * 100.0;

                progress.Report(Math.Clamp(percentage, 0, 100));
            }
        }

        Task errorTask = ReadErrorAsync();
        Task progressTask = ReadProgressAsync();

        await Task.WhenAll(errorTask, progressTask);
        await process.WaitForExitAsync(cancellationToken);

        return process.ExitCode;
    }
}
