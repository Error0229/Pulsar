namespace Pulsar.Audio.Analysis;

using Pulsar.Haptics;

/// <summary>
/// Triggers on low-frequency energy (20-150Hz bass).
/// </summary>
public class BassAnalysisStrategy : IAnalysisStrategy
{
    private readonly FFTAnalyzer _fft;
    private Single _previousEnergy;
    private const Single MinFreq = 20f;
    private const Single MaxFreq = 150f;

    public BassAnalysisStrategy(FFTAnalyzer fft) => this._fft = fft;

    public HapticTriggerResult? Analyze(Single[] spectrum, Int32 sampleRate, Single threshold, Single sensitivity)
    {
        var energy = this._fft.GetBandEnergy(spectrum, sampleRate, MinFreq, MaxFreq);
        energy *= sensitivity;

        // Detect bass hit (energy spike above threshold)
        var isHit = energy > threshold && energy > this._previousEnergy * 1.5f;
        this._previousEnergy = energy;

        if (!isHit)
        {
            return null;
        }

        // Stronger bass = SharpCollision, lighter = SubtleCollision
        var waveform = energy > threshold * 2
            ? WaveformType.SharpCollision
            : WaveformType.SubtleCollision;

        return new HapticTriggerResult(waveform, Math.Min(energy, 1f));
    }
}