using System;
using System.Diagnostics;
using MxHapticCursorPlugin.Native;

namespace MxHapticCursorPlugin.Filtering;

/// <summary>
/// Throttles haptic events to prevent overwhelming the user
/// </summary>
public class ThrottleFilter
{
    private readonly int _throttleMs;
    private readonly Stopwatch _stopwatch;
    private long _lastAllowedTimestamp;

    public ThrottleFilter(int throttleMs)
    {
        _throttleMs = throttleMs;
        _stopwatch = Stopwatch.StartNew();
        _lastAllowedTimestamp = -throttleMs; // Ensure first event always allowed
    }

    /// <summary>
    /// Check if event should be allowed based on throttle window
    /// </summary>
    public bool ShouldAllow(CursorType from, CursorType to)
    {
        var currentMs = _stopwatch.ElapsedMilliseconds;

        if (currentMs - _lastAllowedTimestamp >= _throttleMs)
        {
            _lastAllowedTimestamp = currentMs;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Reset throttle timer
    /// </summary>
    public void Reset()
    {
        _lastAllowedTimestamp = 0;
    }
}
