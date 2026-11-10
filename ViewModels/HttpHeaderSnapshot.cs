namespace FFmpegDownloader.ViewModels;

public sealed record HttpHeaderSnapshot(string Name,
                                        string Value)
{
    public override string ToString() => $"{Name}: {Value}";
}
