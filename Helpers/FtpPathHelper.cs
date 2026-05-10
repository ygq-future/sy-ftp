using System.IO;
using System.Runtime.InteropServices;

namespace sy_ftp.Helpers;

public static class FtpPathHelper
{
    public static string DefaultDownloadDir => Path.Combine(GetDownloadsFolder(), "SY-FTP");

    public static void Ensure() => Directory.CreateDirectory(DefaultDownloadDir);

    private static string GetDownloadsFolder()
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var hr = SHGetKnownFolderPath(
                    new Guid("374DE290-123F-4565-9164-39C4925E467B"),
                    0, 0, out var pszPath);
                if (hr == 0 && pszPath != 0)
                {
                    var dir = Marshal.PtrToStringUni(pszPath);
                    Marshal.FreeCoTaskMem(pszPath);
                    if (dir is not null && Directory.Exists(dir))
                        return dir;
                }
            }
            catch { }
        }
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern int SHGetKnownFolderPath(
        in Guid rfid, uint dwFlags, nint hToken, out nint pszPath);
}
