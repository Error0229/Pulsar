namespace Pulsar.Filtering;

using System;
using System.Diagnostics;

using Pulsar.Native;

/// <summary>
/// Throttles haptic events to prevent overwhelming the user
/// </summary>
public class ThrottleFilter
{
    private readonly Int32 _throttleMs;
    private readonly Stopwatch _stopwatch;
    private Int64 _lastAllowedTimestamp;

    public ThrottleFilter(Int32 throttleMs)
    {
        this._throttleMs = throttleMs;
        this._stopwatch = Stopwatch.StartNew();
        this._lastAllowedTimestamp = -throttleMs; // Ensure first event always allowed
    }

    /// <summary>
    /// Check if event should be allowed based on throttle window
    /// </summary>
    public Boolean ShouldAllow(CursorType from, CursorType to)
    {
        var currentMs = this._stopwatch.ElapsedMilliseconds;

        if (currentMs - this._lastAllowedTimestamp >= this._throttleMs)
        {
            this._lastAllowedTimestamp = currentMs;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Reset throttle timer
    /// </summary>
    public void Reset() => this._lastAllowedTimestamp = 0;
}