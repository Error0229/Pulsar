using System;
using System.Runtime.InteropServices;

namespace MxHapticCursorPlugin.Native;

/// <summary>
/// Windows user32.dll P/Invoke declarations for cursor monitoring
/// </summary>
public static class User32
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CURSORINFO
    {
        public int cbSize;
        public int flags;
        public IntPtr hCursor;
        public POINT ptScreenPos;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorInfo(ref CURSORINFO pci);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

    // Standard cursor IDs from winuser.h
    private const int IDC_ARROW = 32512;
    private const int IDC_IBEAM = 32513;
    private const int IDC_WAIT = 32514;
    private const int IDC_CROSS = 32515;
    private const int IDC_UPARROW = 32516;
    private const int IDC_SIZE = 32640;
    private const int IDC_ICON = 32641;
    private const int IDC_SIZENWSE = 32642;
    private const int IDC_SIZENESW = 32643;
    private const int IDC_SIZEWE = 32644;
    private const int IDC_SIZENS = 32645;
    private const int IDC_SIZEALL = 32646;
    private const int IDC_NO = 32648;
    private const int IDC_HAND = 32649;
    private const int IDC_APPSTARTING = 32650;
    private const int IDC_HELP = 32651;

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
        if (!GetCursorInfo(ref cursorInfo))
        {
            throw new System.ComponentModel.Win32Exception();
        }
        return cursorInfo;
    }

    /// <summary>
    /// Detect cursor type from cursor handle
    /// </summary>
    public static CursorType GetCursorType(IntPtr hCursor)
    {
        if (_systemCursors.TryGetValue(hCursor, out var cursorType))
        {
            return cursorType;
        }

        return CursorType.Custom;
    }
}
