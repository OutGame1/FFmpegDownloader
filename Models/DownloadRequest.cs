namespace FFmpegDownloader.Models;

public sealed record DownloadRequest(Uri Url,
                                     string OutputPath,
                                     DownloadOptions Options);
