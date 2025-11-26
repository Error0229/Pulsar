using Pulsar.Haptics;

namespace Pulsar.Audio.Analysis;

/// <summary>
/// Splits audio into bass/mid/high bands, triggers different waveforms.
/// </summary>
public class MultiBandAnalysisStrategy : IAnalysisStrategy
{
    private readonly FFTAnalyzer _fft;

    public MultiBandAnalysisStrategy(FFTAnalyzer fft)
    {
        _fft = fft;
    }

    public HapticTriggerResult? Analyze(float[] spectrum, int sampleRate, float threshold, float sensitivity)
    {
        var bassEnergy = _fft.GetBandEnergy(spectrum, sampleRate, 20f, 150f) * sensitivity;
        var midEnergy = _fft.GetBandEnergy(spectrum, sampleRate, 150f, 2000f) * sensitivity;
        var highEnergy = _fft.GetBandEnergy(spectrum, sampleRate, 2000f, 8000f) * sensitivity;

        // Find dominant band
        var maxEnergy = Math.Max(bassEnergy, Math.Max(midEnergy, highEnergy));

        if (maxEnergy < threshold)
            return null;

        WaveformType waveform;
        if (bassEnergy >= midEnergy && bassEnergy >= highEnergy)
        {
            waveform = WaveformType.SharpCollision; // Bass = heavy thump
        }
        else if (midEnergy >= highEnergy)
        {
            waveform = WaveformType.SubtleCollision; // Mid = moderate pulse
        }
        else
        {
            waveform = WaveformType.SharpStateChange; // High = crisp tick
        }

        return new HapticTriggerResult(waveform, Math.Min(maxEnergy, 1f));
    }
}
