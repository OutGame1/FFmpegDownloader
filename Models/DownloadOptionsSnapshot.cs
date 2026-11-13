using static FFmpegDownloader.Helpers.StringHelper;

namespace FFmpegDownloader.Models;

public sealed record DownloadOptionsSnapshot(IReadOnlyList<HttpHeaderSnapshot> Headers,
                                             string Title,
                                             string Artist,
                                             string Album,
                                             string Date,
                                             string Genre,
                                             string Comment,
                                             string ExtraArguments)
{
    public string HeadersString() => string.Join(CrLf, Headers) + CrLf;
};
