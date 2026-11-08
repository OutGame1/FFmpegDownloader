namespace FFmpegDownloader.ViewModels;

public sealed record DownloadOptionsSnapshot(IReadOnlyList<HttpHeaderSnapshot> Headers,
                                             string Title,
                                             string Artist,
                                             string Album,
                                             string Date,
                                             string Genre,
                                             string Comment,
                                             string ExtraArguments);
