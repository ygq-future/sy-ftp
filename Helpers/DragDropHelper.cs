using Avalonia.Input;

namespace sy_ftp.Helpers;

public static class DragDropHelper
{
    public static IEnumerable<string> GetDroppedFiles(DragEventArgs e)
    {
        if (e.DataTransfer.TryGetFiles() is not { } files)
            yield break;

        foreach (var item in files)
        {
            if (item.Path?.LocalPath is { } path)
                yield return path;
        }
    }
}
