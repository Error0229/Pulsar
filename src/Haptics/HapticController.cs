namespace Pulsar.Haptics;

using System;

using Pulsar.Device;
using Pulsar.Filtering;
using Pulsar.Native;
using Pulsar.Settings;

/// <summary>
/// Coordinates cursor monitoring, filtering, and haptic triggering
/// </summary>
public class HapticController
{
    private readonly HapticSettings _settings;
    private readonly Action<String> _triggerHapticEvent;
    private readonly Action<String> _logDebug;
    private readonly ThrottleFilter _throttleFilter;
    private readonly DeviceActivityTracker _activityTracker;

    public HapticController(HapticSettings settings, Action<String> triggerHapticEvent, Action<String> logDebug = null)
    {
        this._settings = settings;
        this._triggerHapticEvent = triggerHapticEvent;
        this._logDebug = logDebug ?? (_ => { });
        this._throttleFilter = new ThrottleFilter(settings.ThrottleMs);
        this._activityTracker = new DeviceActivityTracker(settings.ActivityDetectionWindowMs);
    }

    /// <summary>
    /// Handle cursor change from monitor
    /// </summary>
    public void OnCursorChanged(CursorType from, CursorType to)
    {
        if (this._settings.VerboseCursorLogging)
        {
            this._logDebug($"Cursor changed: {from} -> {to}");
        }

        if (!this._settings.Enabled)
        {
            if (this._settings.VerboseCursorLogging)
            {
                this._logDebug("Haptics disabled, skipping");
            }
            return;
        }

        // Cursor movement implies mouse activity
        this._activityTracker.RecordActivity();

        // Check cursor type filter
        if (!this._settings.CursorFilter.ShouldAllow(from, to))
        {
            if (this._settings.VerboseCursorLogging)
            {
                this._logDebug($"Filter blocked transition: {from} -> {to}");
            }
            return;
        }

        // Check throttle
        if (!this._throttleFilter.ShouldAllow(from, to))
        {
            if (this._settings.VerboseCursorLogging)
            {
                this._logDebug("Throttle blocked");
            }
            return;
        }

        // Get waveform and trigger haptic
        var waveform = this._settings.WaveformMapper.GetWaveform(from, to);
        var eventName = WaveformMapper.ToEventName(waveform);

        this._logDebug($"Triggering haptic event: {eventName}");
        this._triggerHapticEvent(eventName);
    }

    /// <summary>
    /// Record MX Master 4 activity (call from plugin when buttons/wheel used)
    /// </summary>
    public void RecordDeviceActivity() => this._activityTracker.RecordActivity();

    /// <summary>
    /// Update settings (throttle, filters, etc.)
    /// </summary>
    public void UpdateSettings(HapticSettings newSettings)
    {
        // Settings object is mutable, no need to replace
        // Just signal that filters may have changed
    }
}