using System.Text;

namespace FFmpegDownloader.Helpers;

public static class StringHelper
{

    #region Constantes

    public const string CrLf = "\r\n";
    public const char Antislash = '\\';
    public const char DblQuote = '"';

    #endregion

    public static string QuoteArgument(string value)
    {
        StringBuilder result = new(DblQuote.ToString());
        int backslashes = 0;

        foreach (char c in value)
        {
            switch (c)
            {
                case Antislash:
                    backslashes++;
                    break;

                case DblQuote:
                    result.Append(Antislash, backslashes * 2 + 1)
                          .Append(DblQuote);
                    backslashes = 0;
                    break;

                default:
                    result.Append(Antislash, backslashes)
                          .Append(c);
                    backslashes = 0;
                    break;
            }
        }

        result.Append(Antislash, backslashes * 2)
              .Append(DblQuote);

        return result.ToString();
    }
}
