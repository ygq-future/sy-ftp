namespace sy_ftp.Services;

public class FileWatcherService : IFileWatcherService
{
    private static readonly TimeSpan DebounceInterval = TimeSpan.FromMilliseconds(500);

    public IDisposable StartWatching(string filePath, Action<string> onChanged)
    {
        var dir = Path.GetDirectoryName(filePath) ?? ".";
        var name = Path.GetFileName(filePath);

        var watcher = new FileSystemWatcher(dir, name)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };

        var debouncer = new DebounceDispatcher(DebounceInterval);

        void handler(object _, FileSystemEventArgs e)
        {
            debouncer.Trigger(() => onChanged(e.FullPath));
        }

        watcher.Changed += handler;
        return new StopHandle(() =>
        {
            watcher.Changed -= handler;
            watcher.Dispose();
        });
    }
}

file sealed class DebounceDispatcher(TimeSpan interval)
{
    private readonly Lock _lock = new();
    private CancellationTokenSource? _cts;

    public void Trigger(Action action)
    {
        CancellationTokenSource? previous;
        lock (_lock)
        {
            previous = _cts;
            _cts = new CancellationTokenSource();
        }

        previous?.Cancel();
        previous?.Dispose();

        var captured = _cts;
        _ = Task.Delay(interval, captured.Token)
            .ContinueWith(t =>
            {
                if (!t.IsCanceled)
                    action();
            }, TaskScheduler.Default);
    }
}

file sealed class StopHandle(Action onDispose) : IDisposable
{
    public void Dispose() => onDispose();
}
