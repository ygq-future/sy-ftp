using Avalonia.Input;
using Avalonia.Platform.Storage;

namespace sy_ftp.Helpers;

public static class DragDropHelper
{
    public static IEnumerable<IStorageItem> GetDroppedItems(DragEventArgs e)
    {
        if (e.DataTransfer.TryGetFiles() is not { } files)
            yield break;

        foreach (var item in files)
        {
            yield return item;
        }
    }

    public static IEnumerable<string> GetDroppedFiles(DragEventArgs e)
    {
        foreach (var item in GetDroppedItems(e))
        {
            if (item.Path?.LocalPath is { } path)
                yield return path;
        }
    }
}
