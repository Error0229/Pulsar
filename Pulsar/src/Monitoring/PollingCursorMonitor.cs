using System;
using System.Threading;

using Pulsar.Native;

namespace Pulsar.Monitoring;

/// <summary>
/// Polls Windows cursor state at regular intervals to detect changes
/// Simple but uses constant CPU. Good for comparison with event-driven approach.
/// </summary>
public class PollingCursorMonitor : ICursorMonitor
{
    private readonly int _pollIntervalMs;
    private readonly Timer _timer;
    private readonly object _lock = new object();
    private CursorType _lastCursorType;
    private IntPtr _lastCursorHandle;
    private bool _isRunning;

    public event Action<CursorType, CursorType> CursorChanged;

    public PollingCursorMonitor(int pollIntervalMs = 50)
    {
        _pollIntervalMs = pollIntervalMs;
        _timer = new Timer(OnTimerTick, null, Timeout.Infinite, Timeout.Infinite);

        // Initialize with current cursor
        var cursorInfo = User32.GetCurrentCursor();
        _lastCursorHandle = cursorInfo.hCursor;
        _lastCursorType = User32.GetCursorType(_lastCursorHandle);
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_isRunning) return;

            _isRunning = true;
            _timer.Change(0, _pollIntervalMs);
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            if (!_isRunning) return;

            _isRunning = false;
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    private void OnTimerTick(object state)
    {
        lock (_lock)
        {
            if (!_isRunning) return;

            try
            {
                var cursorInfo = User32.GetCurrentCursor();

                // Only process if cursor handle changed
                if (cursorInfo.hCursor == _lastCursorHandle)
                    return;

                var newCursorType = User32.GetCursorType(cursorInfo.hCursor);

                // Only raise event if cursor TYPE changed (not just handle)
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
                // Swallow exceptions in timer callback to prevent crashes
            }
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            Stop();
            CursorChanged = null; // Clear subscribers
            _timer?.Dispose();
        }
    }
}
