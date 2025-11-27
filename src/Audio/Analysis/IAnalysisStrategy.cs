namespace Pulsar.Audio.Analysis;

using Pulsar.Haptics;

/// <summary>
/// Analyzes FFT spectrum and determines if haptic should trigger.
/// </summary>
public interface IAnalysisStrategy
{
    /// <summary>
    /// Analyze spectrum and return haptic trigger result.
    /// </summary>
    /// <param name="spectrum">FFT magnitude spectrum</param>
    /// <param name="sampleRate">Audio sample rate</param>
    /// <param name="threshold">Trigger threshold (0-1)</param>
    /// <param name="sensitivity">Sensitivity multiplier</param>
    /// <returns>Waveform to trigger, or null if no trigger</returns>
    HapticTriggerResult? Analyze(Single[] spectrum, Int32 sampleRate, Single threshold, Single sensitivity);
}

/// <summary>
/// Result of audio analysis.
/// </summary>
public record HapticTriggerResult(WaveformType Waveform, Single Intensity);