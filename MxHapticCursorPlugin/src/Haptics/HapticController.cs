using System;
using MxHapticCursorPlugin.Settings;
using MxHapticCursorPlugin.Native;
using MxHapticCursorPlugin.Filtering;
using MxHapticCursorPlugin.Device;

namespace MxHapticCursorPlugin.Haptics;

/// <summary>
/// Coordinates cursor monitoring, filtering, and haptic triggering
/// </summary>
public class HapticController
{
    private readonly HapticSettings _settings;
    private readonly Action<string> _triggerHapticEvent;
    private readonly Action<string> _logDebug;
    private readonly ThrottleFilter _throttleFilter;
    private readonly DeviceActivityTracker _activityTracker;

    public HapticController(HapticSettings settings, Action<string> triggerHapticEvent, Action<string> logDebug = null)
    {
        _settings = settings;
        _triggerHapticEvent = triggerHapticEvent;
        _logDebug = logDebug ?? (_ => { });
        _throttleFilter = new ThrottleFilter(settings.ThrottleMs);
        _activityTracker = new DeviceActivityTracker(settings.ActivityDetectionWindowMs);
    }

    /// <summary>
    /// Handle cursor change from monitor
    /// </summary>
    public void OnCursorChanged(CursorType from, CursorType to)
    {
        _logDebug($"Cursor changed: {from} -> {to}");

        if (!_settings.Enabled)
        {
            _logDebug("Haptics disabled, skipping");
            return;
        }

        // Cursor movement implies mouse activity
        _activityTracker.RecordActivity();

        // Check cursor type filter
        if (!_settings.CursorFilter.ShouldAllow(from, to))
        {
            _logDebug($"Filter blocked transition: {from} -> {to}");
            return;
        }

        // Check throttle
        if (!_throttleFilter.ShouldAllow(from, to))
        {
            _logDebug("Throttle blocked");
            return;
        }

        // Get waveform and trigger haptic
        var waveform = _settings.WaveformMapper.GetWaveform(from, to);
        var eventName = WaveformMapper.ToEventName(waveform);

        _logDebug($"Triggering haptic event: {eventName}");
        _triggerHapticEvent(eventName);
    }

    /// <summary>
    /// Record MX Master 4 activity (call from plugin when buttons/wheel used)
    /// </summary>
    public void RecordDeviceActivity()
    {
        _activityTracker.RecordActivity();
    }

    /// <summary>
    /// Update settings (throttle, filters, etc.)
    /// </summary>
    public void UpdateSettings(HapticSettings newSettings)
    {
        // Settings object is mutable, no need to replace
        // Just signal that filters may have changed
    }
}
