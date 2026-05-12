using System;
using System.Runtime.InteropServices;

namespace sy_ftp.Helpers;

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

    /// <summary>
    /// 通过 ObjC 的 [NSString stringWithUTF8String:] 创建 NSString。
    /// 返回 autoreleased NSString 指针。
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

    public static void Apply(IntPtr nsWindow)
    {
        if (!OperatingSystem.IsMacOS()) return;

        try
        {
            var cls = objc_getClass("NSToolbar");
            if (cls == IntPtr.Zero) return;

            // alloc
            var allocSel = sel_registerName("alloc");
            var allocMethod = class_getClassMethod(cls, allocSel);
            if (allocMethod == IntPtr.Zero) return;
            var allocImp = method_getImplementation(allocMethod);
            if (allocImp == IntPtr.Zero) return;
            var allocFn = Marshal.GetDelegateForFunctionPointer<Fn_id_sel>(allocImp);
            var instance = allocFn(cls, allocSel);
            if (instance == IntPtr.Zero) return;

            // initWithIdentifier: — 需要 NSString*
            var initSel = sel_registerName("initWithIdentifier:");
            var initMethod = class_getInstanceMethod(cls, initSel);
            if (initMethod == IntPtr.Zero) return;
            var initImp = method_getImplementation(initMethod);
            if (initImp == IntPtr.Zero) return;
            var initFn = Marshal.GetDelegateForFunctionPointer<Fn_id_sel_id>(initImp);
            var nsId = CreateNSString("SyFtpToolbar");
            if (nsId == IntPtr.Zero) return;
            var toolbar = initFn(instance, initSel, nsId);
            if (toolbar == IntPtr.Zero) return;

            // setToolbar: on NSWindow
            var setToolbarSel = sel_registerName("setToolbar:");
            var setToolbarMethod = class_getInstanceMethod(objc_getClass("NSWindow"), setToolbarSel);
            if (setToolbarMethod == IntPtr.Zero) return;
            var setToolbarImp = method_getImplementation(setToolbarMethod);
            if (setToolbarImp == IntPtr.Zero) return;
            var setToolbarFn = Marshal.GetDelegateForFunctionPointer<Fn_void_sel_id>(setToolbarImp);
            setToolbarFn(nsWindow, setToolbarSel, toolbar);

            // setTitlebarAppearsTransparent:
            var transparentSel = sel_registerName("setTitlebarAppearsTransparent:");
            var transparentMethod = class_getInstanceMethod(objc_getClass("NSWindow"), transparentSel);
            if (transparentMethod == IntPtr.Zero) return;
            var transparentImp = method_getImplementation(transparentMethod);
            if (transparentImp == IntPtr.Zero) return;
            var transparentFn = Marshal.GetDelegateForFunctionPointer<Fn_void_sel_byte>(transparentImp);
            transparentFn(nsWindow, transparentSel, 1);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MacTitleBar] Apply failed: {ex.Message}");
        }
    }
}
