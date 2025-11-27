namespace Pulsar.Device;

using System;
using System.Diagnostics;

/// <summary>
/// Tracks MX Master 4 activity to determine if haptics should trigger
/// Based on recent button clicks, wheel scrolls, etc.
/// </summary>
public class DeviceActivityTracker
{
    private readonly Int32 _activityWindowMs;
    private readonly Stopwatch _stopwatch;
    private Int64? _lastActivityTimestamp;

    public DeviceActivityTracker(Int32 activityWindowMs = 5000)
    {
        this._activityWindowMs = activityWindowMs;
        this._stopwatch = Stopwatch.StartNew();
        // Start active - assume mouse is being used until inactivity proves otherwise
        this._lastActivityTimestamp = 0;
    }

    /// <summary>
    /// Record that MX Master 4 was used (button click, wheel scroll, etc.)
    /// </summary>
    public void RecordActivity() => this._lastActivityTimestamp = this._stopwatch.ElapsedMilliseconds;

    /// <summary>
    /// Check if MX Master 4 has been used recently
    /// </summary>
    public Boolean IsActive()
    {
        if (!this._lastActivityTimestamp.HasValue)
        {
            return false;
        }

        var currentMs = this._stopwatch.ElapsedMilliseconds;
        return (currentMs - this._lastActivityTimestamp.Value) <= this._activityWindowMs;
    }

    /// <summary>
    /// Reset activity tracking
    /// </summary>
    public void Reset() => this._lastActivityTimestamp = null;
}