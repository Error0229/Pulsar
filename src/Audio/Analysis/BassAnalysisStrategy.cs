using Pulsar.Haptics;

namespace Pulsar.Audio.Analysis;

/// <summary>
/// Triggers on low-frequency energy (20-150Hz bass).
/// </summary>
public class BassAnalysisStrategy : IAnalysisStrategy
{
    private readonly FFTAnalyzer _fft;
    private float _previousEnergy;
    private const float MinFreq = 20f;
    private const float MaxFreq = 150f;

    public BassAnalysisStrategy(FFTAnalyzer fft)
    {
        _fft = fft;
    }

    public HapticTriggerResult? Analyze(float[] spectrum, int sampleRate, float threshold, float sensitivity)
    {
        var energy = _fft.GetBandEnergy(spectrum, sampleRate, MinFreq, MaxFreq);
        energy *= sensitivity;

        // Detect bass hit (energy spike above threshold)
        var isHit = energy > threshold && energy > _previousEnergy * 1.5f;
        _previousEnergy = energy;

        if (!isHit)
            return null;

        // Stronger bass = SharpCollision, lighter = SubtleCollision
        var waveform = energy > threshold * 2
            ? WaveformType.SharpCollision
            : WaveformType.SubtleCollision;

        return new HapticTriggerResult(waveform, Math.Min(energy, 1f));
    }
}
