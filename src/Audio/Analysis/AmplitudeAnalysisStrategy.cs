namespace Pulsar.Audio.Analysis;

using Pulsar.Haptics;

/// <summary>
/// Continuous mapping of overall loudness to haptic intensity.
/// </summary>
public class AmplitudeAnalysisStrategy : IAnalysisStrategy
{
    private Single _smoothedAmplitude;
    private const Single SmoothingFactor = 0.3f;

    public HapticTriggerResult? Analyze(Single[] spectrum, Int32 sampleRate, Single threshold, Single sensitivity)
    {
        // Calculate RMS amplitude
        var sumSquares = 0f;
        foreach (var mag in spectrum)
        {
            sumSquares += mag * mag;
        }
        var amplitude = MathF.Sqrt(sumSquares / spectrum.Length) * sensitivity;

        // Smooth the amplitude
        this._smoothedAmplitude = this._smoothedAmplitude * (1 - SmoothingFactor) + amplitude * SmoothingFactor;

        if (this._smoothedAmplitude < threshold)
        {
            return null;
        }

        // Map amplitude to waveform intensity
        WaveformType waveform;
        if (this._smoothedAmplitude > threshold * 3)
        {
            waveform = WaveformType.SharpCollision;
        }
        else
        {
            waveform = this._smoothedAmplitude > threshold * 2 ? WaveformType.SubtleCollision : WaveformType.DampStateChange;
        }

        return new HapticTriggerResult(waveform, Math.Min(this._smoothedAmplitude, 1f));
    }
}