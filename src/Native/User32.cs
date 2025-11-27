namespace Pulsar.Native;

using System;
using System.Runtime.InteropServices;

/// <summary>
/// Windows user32.dll P/Invoke declarations for cursor monitoring
/// </summary>
public static class User32
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CURSORINFO
    {
        public Int32 cbSize;
        public Int32 flags;
        public IntPtr hCursor;
        public POINT ptScreenPos;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public Int32 X;
        public Int32 Y;
    }

    [DllImport("user32.dll")]
    private static extern Boolean GetCursorInfo(ref CURSORINFO pci);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr LoadCursor(IntPtr hInstance, Int32 lpCursorName);

    // Event hook support
    public delegate void WinEventDelegate(
        IntPtr hWinEventHook,
        UInt32 eventType,
        IntPtr hwnd,
        Int32 idObject,
        Int32 idChild,
        UInt32 dwEventThread,
        UInt32 dwmsEventTime);

    [DllImport("user32.dll")]
    public static extern IntPtr SetWinEventHook(
        UInt32 eventMin,
        UInt32 eventMax,
        IntPtr hmodWinEventProc,
        WinEventDelegate lpfnWinEventProc,
        UInt32 idProcess,
        UInt32 idThread,
        UInt32 dwFlags);

    [DllImport("user32.dll")]
    public static extern Boolean UnhookWinEvent(IntPtr hWinEventHook);

    // Event constants
    public const UInt32 EVENT_OBJECT_NAMECHANGE = 0x800C;
    public const UInt32 EVENT_SYSTEM_FOREGROUND = 0x0003;
    public const UInt32 WINEVENT_OUTOFCONTEXT = 0x0000;

    // Standard cursor IDs from winuser.h
    private const Int32 IDC_ARROW = 32512;
    private const Int32 IDC_IBEAM = 32513;
    private const Int32 IDC_WAIT = 32514;
    private const Int32 IDC_CROSS = 32515;
    private const Int32 IDC_UPARROW = 32516;
    private const Int32 IDC_SIZE = 32640;
    private const Int32 IDC_ICON = 32641;
    private const Int32 IDC_SIZENWSE = 32642;
    private const Int32 IDC_SIZENESW = 32643;
    private const Int32 IDC_SIZEWE = 32644;
    private const Int32 IDC_SIZENS = 32645;
    private const Int32 IDC_SIZEALL = 32646;
    private const Int32 IDC_NO = 32648;
    private const Int32 IDC_HAND = 32649;
    private const Int32 IDC_APPSTARTING = 32650;
    private const Int32 IDC_HELP = 32651;

    private static readonly Dictionary<IntPtr, CursorType> _systemCursors;

    static User32()
    {
        // Load system cursor handles once at startup
        _systemCursors = new Dictionary<IntPtr, CursorType>
        {
            { LoadCursor(IntPtr.Zero, IDC_ARROW), CursorType.Arrow },
            { LoadCursor(IntPtr.Zero, IDC_IBEAM), CursorType.IBeam },
            { LoadCursor(IntPtr.Zero, IDC_WAIT), CursorType.Wait },
            { LoadCursor(IntPtr.Zero, IDC_CROSS), CursorType.Crosshair },
            { LoadCursor(IntPtr.Zero, IDC_HAND), CursorType.Hand },
            { LoadCursor(IntPtr.Zero, IDC_SIZEWE), CursorType.ResizeHorizontal },
            { LoadCursor(IntPtr.Zero, IDC_SIZENS), CursorType.ResizeVertical },
            { LoadCursor(IntPtr.Zero, IDC_SIZENWSE), CursorType.ResizeDiagonalNWSE },
            { LoadCursor(IntPtr.Zero, IDC_SIZENESW), CursorType.ResizeDiagonalNESW },
            { LoadCursor(IntPtr.Zero, IDC_NO), CursorType.NotAllowed },
            { LoadCursor(IntPtr.Zero, IDC_APPSTARTING), CursorType.AppStarting },
        };
    }

    /// <summary>
    /// Get current cursor information from Windows
    /// </summary>
    public static CURSORINFO GetCurrentCursor()
    {
        var cursorInfo = new CURSORINFO { cbSize = Marshal.SizeOf(typeof(CURSORINFO)) };
        return !GetCursorInfo(ref cursorInfo) ? throw new System.ComponentModel.Win32Exception() : cursorInfo;
    }

    /// <summary>
    /// Detect cursor type from cursor handle
    /// </summary>
    public static CursorType GetCursorType(IntPtr hCursor) => _systemCursors.TryGetValue(hCursor, out var cursorType) ? cursorType : CursorType.Custom;
}