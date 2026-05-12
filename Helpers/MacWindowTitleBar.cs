using System;
using System.Runtime.InteropServices;

namespace sy_ftp.Helpers;

/// <summary>
/// macOS 厚标题栏 — 向 NSWindow 附加空 NSToolbar，
/// 让 macOS 自动增加标题栏高度并将红绿灯竖直居中。
///
/// 替代 Avalonia 12 中已移除的 OSXThickTitleBar。
/// 参考: https://github.com/AvaloniaUI/Avalonia/issues/21119
///
/// 支持全屏切换：进入全屏时移除 Toolbar（避免灰色条遮挡），
/// 退出全屏后恢复。
/// </summary>
internal static class MacWindowTitleBar
{
    private const string ObjCLib = "/usr/lib/libobjc.dylib";

    [DllImport(ObjCLib)]
    private static extern IntPtr objc_getClass(string name);

    [DllImport(ObjCLib)]
    private static extern IntPtr sel_registerName(string name);

    [DllImport(ObjCLib)]
    private static extern IntPtr class_getClassMethod(IntPtr cls, IntPtr sel);

    [DllImport(ObjCLib)]
    private static extern IntPtr class_getInstanceMethod(IntPtr cls, IntPtr sel);

    [DllImport(ObjCLib)]
    private static extern IntPtr method_getImplementation(IntPtr method);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr Fn_id_sel(IntPtr self, IntPtr sel);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate IntPtr Fn_id_sel_id(IntPtr self, IntPtr sel, IntPtr arg);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Fn_void_sel_id(IntPtr self, IntPtr sel, IntPtr arg);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void Fn_void_sel_byte(IntPtr self, IntPtr sel, byte arg);

    // 当前保存的工具栏引用，用于全屏后恢复
    private static IntPtr _toolbar;

    /// <summary>
    /// 通过 ObjC 的 [NSString stringWithUTF8String:] 创建 NSString。
    /// </summary>
    private static IntPtr CreateNSString(string value)
    {
        var cls = objc_getClass("NSString");
        var sel = sel_registerName("stringWithUTF8String:");
        var method = class_getClassMethod(cls, sel);
        if (method == IntPtr.Zero) return IntPtr.Zero;
        var imp = method_getImplementation(method);
        if (imp == IntPtr.Zero) return IntPtr.Zero;
        var fn = Marshal.GetDelegateForFunctionPointer<Fn_id_sel_id>(imp);
        var utf8 = Marshal.StringToCoTaskMemUTF8(value);
        var result = fn(cls, sel, utf8);
        Marshal.FreeCoTaskMem(utf8);
        return result;
    }

    /// <summary>
    /// 创建空 NSToolbar 实例并返回其指针。
    /// </summary>
    private static IntPtr CreateToolbar()
    {
        var cls = objc_getClass("NSToolbar");
        if (cls == IntPtr.Zero) return IntPtr.Zero;

        var allocSel = sel_registerName("alloc");
        var allocMethod = class_getClassMethod(cls, allocSel);
        if (allocMethod == IntPtr.Zero) return IntPtr.Zero;
        var allocImp = method_getImplementation(allocMethod);
        if (allocImp == IntPtr.Zero) return IntPtr.Zero;
        var allocFn = Marshal.GetDelegateForFunctionPointer<Fn_id_sel>(allocImp);
        var instance = allocFn(cls, allocSel);
        if (instance == IntPtr.Zero) return IntPtr.Zero;

        var initSel = sel_registerName("initWithIdentifier:");
        var initMethod = class_getInstanceMethod(cls, initSel);
        if (initMethod == IntPtr.Zero) return IntPtr.Zero;
        var initImp = method_getImplementation(initMethod);
        if (initImp == IntPtr.Zero) return IntPtr.Zero;
        var initFn = Marshal.GetDelegateForFunctionPointer<Fn_id_sel_id>(initImp);
        var nsId = CreateNSString("SyFtpToolbar");
        if (nsId == IntPtr.Zero) return IntPtr.Zero;
        return initFn(instance, initSel, nsId);
    }

    /// <summary>
    /// 为窗口附加空 NSToolbar。
    /// 幂等：重复调用不会创建多个工具栏。
    /// </summary>
    public static void Apply(IntPtr nsWindow)
    {
        if (!OperatingSystem.IsMacOS()) return;
        if (nsWindow == IntPtr.Zero) return;
        if (_toolbar != IntPtr.Zero) return; // 已应用

        try
        {
            var toolbar = CreateToolbar();
            if (toolbar == IntPtr.Zero) return;

            var cls = objc_getClass("NSWindow");
            if (cls == IntPtr.Zero) return;

            // setToolbar:
            var setToolbarSel = sel_registerName("setToolbar:");
            var setToolbarMethod = class_getInstanceMethod(cls, setToolbarSel);
            if (setToolbarMethod == IntPtr.Zero) return;
            var setToolbarImp = method_getImplementation(setToolbarMethod);
            if (setToolbarImp == IntPtr.Zero) return;
            var setToolbarFn = Marshal.GetDelegateForFunctionPointer<Fn_void_sel_id>(setToolbarImp);
            setToolbarFn(nsWindow, setToolbarSel, toolbar);

            // setTitlebarAppearsTransparent:
            var transparentSel = sel_registerName("setTitlebarAppearsTransparent:");
            var transparentMethod = class_getInstanceMethod(cls, transparentSel);
            if (transparentMethod == IntPtr.Zero) return;
            var transparentImp = method_getImplementation(transparentMethod);
            if (transparentImp == IntPtr.Zero) return;
            var transparentFn = Marshal.GetDelegateForFunctionPointer<Fn_void_sel_byte>(transparentImp);
            transparentFn(nsWindow, transparentSel, 1);

            _toolbar = toolbar;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MacTitleBar] Apply failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 移除窗口上的 NSToolbar（如进入全屏前）。
    /// Apply 可在之后再次调用以恢复。
    /// </summary>
    public static void Remove(IntPtr nsWindow)
    {
        if (!OperatingSystem.IsMacOS()) return;
        if (nsWindow == IntPtr.Zero) return;
        if (_toolbar == IntPtr.Zero) return; // 未应用

        try
        {
            var cls = objc_getClass("NSWindow");
            if (cls == IntPtr.Zero) return;

            var setToolbarSel = sel_registerName("setToolbar:");
            var setToolbarMethod = class_getInstanceMethod(cls, setToolbarSel);
            if (setToolbarMethod == IntPtr.Zero) return;
            var setToolbarImp = method_getImplementation(setToolbarMethod);
            if (setToolbarImp == IntPtr.Zero) return;
            var setToolbarFn = Marshal.GetDelegateForFunctionPointer<Fn_void_sel_id>(setToolbarImp);
            setToolbarFn(nsWindow, setToolbarSel, IntPtr.Zero);

            // 恢复透明标题栏
            var transparentSel = sel_registerName("setTitlebarAppearsTransparent:");
            var transparentMethod = class_getInstanceMethod(cls, transparentSel);
            if (transparentMethod == IntPtr.Zero) return;
            var transparentImp = method_getImplementation(transparentMethod);
            if (transparentImp == IntPtr.Zero) return;
            var transparentFn = Marshal.GetDelegateForFunctionPointer<Fn_void_sel_byte>(transparentImp);
            transparentFn(nsWindow, transparentSel, 1);

            // 清除状态标记，允许后续 Apply 重新创建工具栏
            _toolbar = IntPtr.Zero;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MacTitleBar] Remove failed: {ex.Message}");
        }
    }
}
