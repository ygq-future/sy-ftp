namespace sy_ftp.Models;

public record PathSegment(string Name, string FullPath, bool IsLast)
{
    public bool ShowSeparator => !IsLast && FullPath != "/";
    public bool IsOverflow { get; init; }
}
