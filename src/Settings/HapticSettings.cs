namespace Pulsar.Settings;

using Pulsar.Filtering;
using Pulsar.Haptics;
using Pulsar.Native;

/// <summary>
/// Configuration for haptic cursor feedback
/// </summary>
public class HapticSettings
{
    public Boolean Enabled { get; set; } = true;
    public SensitivityPreset Preset { get; set; } = SensitivityPreset.Medium;
    public MonitoringMode MonitoringMode { get; set; } = MonitoringMode.Polling;
    public Int32 ThrottleMs { get; set; } = 250;
    public Int32 ActivityDetectionWindowMs { get; set; } = 5000;

    /// <summary>
    /// Enable verbose logging for cursor changes (default: false to reduce log noise)
    /// </summary>
    public Boolean VerboseCursorLogging { get; set; } = false;

    public CursorTypeFilter CursorFilter { get; set; } = new();
    public WaveformMapper WaveformMapper { get; set; } = WaveformMapper.CreateDefault();
    public HummingSettings Humming { get; set; } = new();

    /// <summary>
    /// Create settings from a preset
    /// </summary>
    public static HapticSettings CreatePreset(SensitivityPreset preset)
    {
        var settings = new HapticSettings
        {
            Preset = preset,
            CursorFilter = new CursorTypeFilter(),
            WaveformMapper = WaveformMapper.CreateDefault()
        };

        switch (preset)
        {
            case SensitivityPreset.Low:
                settings.ThrottleMs = 500;
                // Only major transitions
                settings.CursorFilter.EnableTransition(CursorType.Arrow, CursorType.Hand);
                settings.CursorFilter.EnableTransition(CursorType.Hand, CursorType.Arrow);
                settings.CursorFilter.EnableTransition(CursorType.Arrow, CursorType.IBeam);
                settings.CursorFilter.EnableTransition(CursorType.IBeam, CursorType.Arrow);
                break;

            case SensitivityPreset.Medium:
                settings.ThrottleMs = 250;
                // Add resize handles and crosshair
                settings.CursorFilter.EnableTransition(CursorType.Arrow, CursorType.Hand);
                settings.CursorFilter.EnableTransition(CursorType.Hand, CursorType.Arrow);
                settings.CursorFilter.EnableTransition(CursorType.Arrow, CursorType.IBeam);
                settings.CursorFilter.EnableTransition(CursorType.IBeam, CursorType.Arrow);
                settings.CursorFilter.EnableTransition(CursorType.Arrow, CursorType.Crosshair);
                settings.CursorFilter.EnableTransition(CursorType.Crosshair, CursorType.Arrow);
                settings.CursorFilter.EnableTransition(CursorType.Arrow, CursorType.ResizeHorizontal);
                settings.CursorFilter.EnableTransition(CursorType.ResizeHorizontal, CursorType.Arrow);
                settings.CursorFilter.EnableTransition(CursorType.Arrow, CursorType.ResizeVertical);
                settings.CursorFilter.EnableTransition(CursorType.ResizeVertical, CursorType.Arrow);
                settings.CursorFilter.EnableTransition(CursorType.Arrow, CursorType.ResizeDiagonalNESW);
                settings.CursorFilter.EnableTransition(CursorType.ResizeDiagonalNESW, CursorType.Arrow);
                settings.CursorFilter.EnableTransition(CursorType.Arrow, CursorType.ResizeDiagonalNWSE);
                settings.CursorFilter.EnableTransition(CursorType.ResizeDiagonalNWSE, CursorType.Arrow);
                break;

            case SensitivityPreset.High:
                settings.ThrottleMs = 100;
                // Enable all transitions
                settings.CursorFilter.EnableAll();
                break;

            case SensitivityPreset.Custom:
                // User configures manually
                break;
        }

        return settings;
    }
}