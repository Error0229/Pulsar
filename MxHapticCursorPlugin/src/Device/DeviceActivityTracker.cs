using System;
using System.Diagnostics;

namespace MxHapticCursorPlugin.Device;

/// <summary>
/// Tracks MX Master 4 activity to determine if haptics should trigger
/// Based on recent button clicks, wheel scrolls, etc.
/// </summary>
public class DeviceActivityTracker
{
    private readonly int _activityWindowMs;
    private readonly Stopwatch _stopwatch;
    private long? _lastActivityTimestamp;

    public DeviceActivityTracker(int activityWindowMs = 5000)
    {
        _activityWindowMs = activityWindowMs;
        _stopwatch = Stopwatch.StartNew();
        // Start active - assume mouse is being used until inactivity proves otherwise
        _lastActivityTimestamp = 0;
    }

    /// <summary>
    /// Record that MX Master 4 was used (button click, wheel scroll, etc.)
    /// </summary>
    public void RecordActivity()
    {
        _lastActivityTimestamp = _stopwatch.ElapsedMilliseconds;
    }

    /// <summary>
    /// Check if MX Master 4 has been used recently
    /// </summary>
    public bool IsActive()
    {
        if (!_lastActivityTimestamp.HasValue)
        {
            return false;
        }

        var currentMs = _stopwatch.ElapsedMilliseconds;
        return (currentMs - _lastActivityTimestamp.Value) <= _activityWindowMs;
    }

    /// <summary>
    /// Reset activity tracking
    /// </summary>
    public void Reset()
    {
        _lastActivityTimestamp = null;
    }
}
