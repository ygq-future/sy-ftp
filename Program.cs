using Avalonia;
using Avalonia.Native;
using System;

namespace sy_ftp;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect();

        if (OperatingSystem.IsMacOS())
        {
            builder = builder.With(new AvaloniaNativePlatformOptions
            {
                RenderingMode = [AvaloniaNativeRenderingMode.Software]
            });
        }

        if (OperatingSystem.IsWindows())
        {
            builder = builder.With(new Win32PlatformOptions
            {
                CompositionMode = [Win32CompositionMode.WinUIComposition],
                RenderingMode = [Win32RenderingMode.AngleEgl]
            });
        }

#if DEBUG
        builder = builder.WithDeveloperTools();
#endif
        return builder
            .WithInterFont()
            .LogToTrace();
    }
}