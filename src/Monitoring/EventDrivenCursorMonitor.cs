namespace Pulsar.Monitoring;

using System;
using System.Threading;

using Pulsar.Native;

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
    private readonly Object _lock = new Object();
    private Boolean _isRunning;

    public event Action<CursorType, CursorType> CursorChanged;

    public EventDrivenCursorMonitor()
    {
        // Initialize with current cursor
        var cursorInfo = User32.GetCurrentCursor();
        this._lastCursorHandle = cursorInfo.hCursor;
        this._lastCursorType = User32.GetCursorType(this._lastCursorHandle);

        // Fallback polling at 200ms for cases where event hook misses changes
        this._fallbackTimer = new Timer(this.OnFallbackTick, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start()
    {
        lock (this._lock)
        {
            if (this._isRunning)
            {
                return;
            }

            this._isRunning = true;

            // Create delegate and keep reference to prevent GC
            this._hookDelegate = this.OnWinEvent;

            // Hook into cursor change events
            // Note: EVENT_OBJECT_NAMECHANGE doesn't always fire for cursor changes
            // So we also monitor foreground window changes as a trigger to check cursor
            this._hookHandle = User32.SetWinEventHook(
                User32.EVENT_SYSTEM_FOREGROUND,
                User32.EVENT_SYSTEM_FOREGROUND,
                IntPtr.Zero,
                this._hookDelegate,
                0,
                0,
                User32.WINEVENT_OUTOFCONTEXT);

            // Start fallback polling at 200ms intervals
            this._fallbackTimer.Change(200, 200);
        }
    }

    public void Stop()
    {
        lock (this._lock)
        {
            if (!this._isRunning)
            {
                return;
            }

            this._isRunning = false;

            if (this._hookHandle != IntPtr.Zero)
            {
                User32.UnhookWinEvent(this._hookHandle);
                this._hookHandle = IntPtr.Zero;
            }

            this._fallbackTimer.Change(Timeout.Infinite, Timeout.Infinite);
            this._hookDelegate = null;
        }
    }

    private void OnWinEvent(
        IntPtr hWinEventHook,
        UInt32 eventType,
        IntPtr hwnd,
        Int32 idObject,
        Int32 idChild,
        UInt32 dwEventThread,
        UInt32 dwmsEventTime) => this.CheckCursorChange();

    private void OnFallbackTick(Object state)
    {
        lock (this._lock)
        {
            if (!this._isRunning)
            {
                return;
            }

            this.CheckCursorChange();
        }
    }

    private void CheckCursorChange()
    {
        try
        {
            var cursorInfo = User32.GetCurrentCursor();

            // Only process if cursor handle changed
            if (cursorInfo.hCursor == this._lastCursorHandle)
            {
                return;
            }

            var newCursorType = User32.GetCursorType(cursorInfo.hCursor);

            // Only raise event if cursor TYPE changed
            if (newCursorType != this._lastCursorType)
            {
                var oldType = this._lastCursorType;
                this._lastCursorType = newCursorType;
                this._lastCursorHandle = cursorInfo.hCursor;

                CursorChanged?.Invoke(oldType, newCursorType);
            }
            else
            {
                this._lastCursorHandle = cursorInfo.hCursor;
            }
        }
        catch
        {
            // Swallow exceptions to prevent crashes
        }
    }

    public void Dispose()
    {
        lock (this._lock)
        {
            this.Stop();
            CursorChanged = null; // Clear subscribers
            this._fallbackTimer?.Dispose();
        }
    }
}