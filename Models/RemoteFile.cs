namespace sy_ftp.Models;

public record RemoteFile(
    string Name,
    string FullPath,
    long Size,
    bool IsDirectory,
    DateTimeOffset LastModified,
    string Owner = "N/A",
    string Permissions = "N/A")
{
    public bool IsParentEntry => Name == "..";
}
