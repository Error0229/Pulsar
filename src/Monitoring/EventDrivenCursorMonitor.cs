using System;
using System.Threading;

using Pulsar.Native;

namespace Pulsar.Monitoring;

/// <summary>
/// Uses Windows event hooks to detect cursor changes.
/// More efficient than polling - zero overhead when cursor isn't changing.
/// </summary>
public class EventDrivenCursorMonitor : ICursorMonitor
{
    private IntPtr _hookHandle;
    private User32.WinEventDelegate _hookDelegate;
    private CursorType _lastCursorType;
    private IntPtr _lastCursorHandle;
    private readonly Timer _fallbackTimer;
    private readonly object _lock = new object();
    private bool _isRunning;

    public event Action<CursorType, CursorType> CursorChanged;

    public EventDrivenCursorMonitor()
    {
        // Initialize with current cursor
        var cursorInfo = User32.GetCurrentCursor();
        _lastCursorHandle = cursorInfo.hCursor;
        _lastCursorType = User32.GetCursorType(_lastCursorHandle);

        // Fallback polling at 200ms for cases where event hook misses changes
        _fallbackTimer = new Timer(OnFallbackTick, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_isRunning) return;

            _isRunning = true;

            // Create delegate and keep reference to prevent GC
            _hookDelegate = OnWinEvent;

            // Hook into cursor change events
            // Note: EVENT_OBJECT_NAMECHANGE doesn't always fire for cursor changes
            // So we also monitor foreground window changes as a trigger to check cursor
            _hookHandle = User32.SetWinEventHook(
                User32.EVENT_SYSTEM_FOREGROUND,
                User32.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero,
                _hookDelegate,
                0,
                0,
                User32.WINEVENT_OUTOFCONTEXT);

            // Start fallback polling at 200ms intervals
            _fallbackTimer.Change(200, 200);
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            if (!_isRunning) return;

            _isRunning = false;

            if (_hookHandle != IntPtr.Zero)
            {
                User32.UnhookWinEvent(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }

            _fallbackTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _hookDelegate = null;
        }
    }

    private void OnWinEvent(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime)
    {
        CheckCursorChange();
    }

    private void OnFallbackTick(object state)
    {
        lock (_lock)
        {
            if (!_isRunning) return;

            CheckCursorChange();
        }
    }

    private void CheckCursorChange()
    {
        try
        {
            var cursorInfo = User32.GetCurrentCursor();

            // Only process if cursor handle changed
            if (cursorInfo.hCursor == _lastCursorHandle)
                return;

            var newCursorType = User32.GetCursorType(cursorInfo.hCursor);

            // Only raise event if cursor TYPE changed
            if (newCursorType != _lastCursorType)
            {
                var oldType = _lastCursorType;
                _lastCursorType = newCursorType;
                _lastCursorHandle = cursorInfo.hCursor;

                CursorChanged?.Invoke(oldType, newCursorType);
            }
            else
            {
                _lastCursorHandle = cursorInfo.hCursor;
            }
        }
        catch
        {
            // Swallow exceptions to prevent crashes
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            Stop();
            CursorChanged = null; // Clear subscribers
            _fallbackTimer?.Dispose();
        }
    }
}
