using FFmpegDownloader.Models;

namespace FFmpegDownloader.Interfaces;

public interface ICurlParserService
{
    CurlRequest Parse(string command);
}
