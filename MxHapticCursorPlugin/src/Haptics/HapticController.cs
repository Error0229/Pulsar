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
    private readonly ThrottleFilter _throttleFilter;
    private readonly DeviceActivityTracker _activityTracker;

    public HapticController(HapticSettings settings, Action<string> triggerHapticEvent)
    {
        _settings = settings;
        _triggerHapticEvent = triggerHapticEvent;
        _throttleFilter = new ThrottleFilter(settings.ThrottleMs);
        _activityTracker = new DeviceActivityTracker(settings.ActivityDetectionWindowMs);
    }

    /// <summary>
    /// Handle cursor change from monitor
    /// </summary>
    public void OnCursorChanged(CursorType from, CursorType to)
    {
        if (!_settings.Enabled)
            return;

        // Check if device is active (prevents haptics when using trackpad/other mouse)
        if (!_activityTracker.IsActive())
            return;

        // Check cursor type filter
        if (!_settings.CursorFilter.ShouldAllow(from, to))
            return;

        // Check throttle
        if (!_throttleFilter.ShouldAllow(from, to))
            return;

        // Get waveform and trigger haptic
        var waveform = _settings.WaveformMapper.GetWaveform(from, to);
        var eventName = WaveformMapper.ToEventName(waveform);

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
