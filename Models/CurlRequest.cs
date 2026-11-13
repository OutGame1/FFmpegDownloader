namespace FFmpegDownloader.Models;

public sealed class CurlRequest(string url)
{
    public readonly string Url = url;

    public readonly Dictionary<string, string> Headers = [];
}
