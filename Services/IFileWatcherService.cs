namespace sy_ftp.Services;

public interface IFileWatcherService
{
    /// <summary>
    /// Starts watching a file for changes. Returns an IDisposable to stop watching.
    /// </summary>
    IDisposable StartWatching(string filePath, Action<string> onChanged);
}
