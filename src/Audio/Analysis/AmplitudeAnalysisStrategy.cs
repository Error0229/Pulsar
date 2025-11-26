using Pulsar.Haptics;

namespace Pulsar.Audio.Analysis;

/// <summary>
/// Continuous mapping of overall loudness to haptic intensity.
/// </summary>
public class AmplitudeAnalysisStrategy : IAnalysisStrategy
{
    private float _smoothedAmplitude;
    private const float SmoothingFactor = 0.3f;

    public HapticTriggerResult? Analyze(float[] spectrum, int sampleRate, float threshold, float sensitivity)
    {
        // Calculate RMS amplitude
        var sumSquares = 0f;
        foreach (var mag in spectrum)
        {
            sumSquares += mag * mag;
        }
        var amplitude = MathF.Sqrt(sumSquares / spectrum.Length) * sensitivity;

        // Smooth the amplitude
        _smoothedAmplitude = _smoothedAmplitude * (1 - SmoothingFactor) + amplitude * SmoothingFactor;

        if (_smoothedAmplitude < threshold)
            return null;

        // Map amplitude to waveform intensity
        WaveformType waveform;
        if (_smoothedAmplitude > threshold * 3)
        {
            waveform = WaveformType.SharpCollision;
        }
        else if (_smoothedAmplitude > threshold * 2)
        {
            waveform = WaveformType.SubtleCollision;
        }
        else
        {
            waveform = WaveformType.DampStateChange;
        }

        return new HapticTriggerResult(waveform, Math.Min(_smoothedAmplitude, 1f));
    }
}
