namespace Pulsar.Settings;

/// <summary>
/// Audio analysis strategies for Humming mode.
/// </summary>
public enum AnalysisMode
{
    /// <summary>
    /// Trigger on low-frequency energy (bass kicks, sub-bass).
    /// </summary>
    Bass,

    /// <summary>
    /// Split audio into frequency bands, map each to different intensities.
    /// </summary>
    MultiBand,

    /// <summary>
    /// Detect sudden volume spikes (drum hits, transients).
    /// </summary>
    BeatDetection,

    /// <summary>
    /// Continuous mapping of overall loudness to haptic intensity.
    /// </summary>
    Amplitude
}