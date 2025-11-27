namespace Pulsar.Monitoring;

using System;
using System.Threading;

using Pulsar.Native;

/// <summary>
/// Polls Windows cursor state at regular intervals to detect changes
/// Simple but uses constant CPU. Good for comparison with event-driven approach.
/// </summary>
public class PollingCursorMonitor : ICursorMonitor
{
    private readonly Int32 _pollIntervalMs;
    private readonly Timer _timer;
    private readonly Object _lock = new Object();
    private CursorType _lastCursorType;
    private IntPtr _lastCursorHandle;
    private Boolean _isRunning;

    public event Action<CursorType, CursorType> CursorChanged;

    public PollingCursorMonitor(Int32 pollIntervalMs = 50)
    {
        this._pollIntervalMs = pollIntervalMs;
        this._timer = new Timer(this.OnTimerTick, null, Timeout.Infinite, Timeout.Infinite);

        // Initialize with current cursor
        var cursorInfo = User32.GetCurrentCursor();
        this._lastCursorHandle = cursorInfo.hCursor;
        this._lastCursorType = User32.GetCursorType(this._lastCursorHandle);
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
            this._timer.Change(0, this._pollIntervalMs);
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
            this._timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    private void OnTimerTick(Object state)
    {
        lock (this._lock)
        {
            if (!this._isRunning)
            {
                return;
            }

            try
            {
                var cursorInfo = User32.GetCurrentCursor();

                // Only process if cursor handle changed
                if (cursorInfo.hCursor == this._lastCursorHandle)
                {
                    return;
                }

                var newCursorType = User32.GetCursorType(cursorInfo.hCursor);

                // Only raise event if cursor TYPE changed (not just handle)
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
                // Swallow exceptions in timer callback to prevent crashes
            }
        }
    }

    public void Dispose()
    {
        lock (this._lock)
        {
            this.Stop();
            CursorChanged = null; // Clear subscribers
            this._timer?.Dispose();
        }
    }
}