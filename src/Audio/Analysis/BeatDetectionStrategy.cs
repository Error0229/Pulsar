using Pulsar.Haptics;

namespace Pulsar.Audio.Analysis;

/// <summary>
/// Detects sudden volume spikes (transients/beats).
/// </summary>
public class BeatDetectionStrategy : IAnalysisStrategy
{
    private readonly Queue<float> _energyHistory = new();
    private const int HistorySize = 43; // ~1 second at 23ms per frame

    public HapticTriggerResult? Analyze(float[] spectrum, int sampleRate, float threshold, float sensitivity)
    {
        // Calculate total energy
        var energy = 0f;
        foreach (var mag in spectrum)
        {
            energy += mag * mag;
        }
        energy = MathF.Sqrt(energy / spectrum.Length) * sensitivity;

        // Maintain history
        _energyHistory.Enqueue(energy);
        if (_energyHistory.Count > HistorySize)
        {
            _energyHistory.Dequeue();
        }

        if (_energyHistory.Count < 10)
            return null;

        // Calculate average energy
        var averageEnergy = _energyHistory.Average();

        // Beat = current energy significantly above average
        var beatThreshold = averageEnergy * (1.5f + threshold);

        if (energy <= beatThreshold)
            return null;

        // Strong beat = SharpCollision, normal beat = Knock
        var waveform = energy > beatThreshold * 1.5f
            ? WaveformType.SharpCollision
            : WaveformType.Knock;

        return new HapticTriggerResult(waveform, Math.Min(energy / beatThreshold, 1f));
    }
}
