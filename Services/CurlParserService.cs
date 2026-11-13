using FFmpegDownloader.Interfaces;
using FFmpegDownloader.Models;
using System.Text.RegularExpressions;

namespace FFmpegDownloader.Services;

public sealed partial class CurlParserService : ICurlParserService
{
    [GeneratedRegex(@"--url\s+'([^']*)'")]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"-H\s+'([^']*)'")]
    private static partial Regex HeaderRegex();

    public CurlRequest Parse(string command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command);

        Match urlMatch = UrlRegex().Match(command);

        if (!urlMatch.Success)
            throw new FormatException("URL introuvable dans la commande cURL.");

        CurlRequest request = new(urlMatch.Groups[1].Value);

        foreach (Match match in HeaderRegex().Matches(command))
        {
            string header = match.Groups[1].Value;

            int separatorIndex = header.IndexOf(':');

            if (separatorIndex < 0)
                continue;

            string name = header[..separatorIndex].Trim();
            string value = header[(separatorIndex + 1)..].Trim();

            if (name.Length == 0 || value.Length == 0)
                continue;

            request.Headers[name] = value;
        }

        return request;
    }
}
