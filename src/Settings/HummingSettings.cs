namespace Pulsar.Settings;

/// <summary>
/// Configuration for Humming mode audio-reactive haptics.
/// </summary>
public class HummingSettings
{
    /// <summary>
    /// Whether Humming mode is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Current analysis strategy.
    /// </summary>
    public AnalysisMode AnalysisMode { get; set; } = AnalysisMode.Bass;

    /// <summary>
    /// Sensitivity multiplier (0.5 = less sensitive, 2.0 = more sensitive).
    /// </summary>
    public float Sensitivity { get; set; } = 1.0f;

    /// <summary>
    /// Minimum threshold to trigger haptics (0.0 - 1.0).
    /// </summary>
    public float Threshold { get; set; } = 0.1f;

    /// <summary>
    /// FFT buffer size (1024 recommended for ~23ms at 44.1kHz).
    /// </summary>
    public int FftSize { get; set; } = 1024;
}
