namespace sy_ftp.Models;

public record RemoteFile(
    string Name,
    string FullPath,
    long Size,
    bool IsDirectory,
    DateTimeOffset LastModified)
{
    public bool IsParentEntry => Name == "..";
}
