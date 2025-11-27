namespace Pulsar.Audio.Analysis;

using Pulsar.Haptics;

/// <summary>
/// Splits audio into bass/mid/high bands, triggers different waveforms.
/// </summary>
public class MultiBandAnalysisStrategy : IAnalysisStrategy
{
    private readonly FFTAnalyzer _fft;

    public MultiBandAnalysisStrategy(FFTAnalyzer fft) => this._fft = fft;

    public HapticTriggerResult? Analyze(Single[] spectrum, Int32 sampleRate, Single threshold, Single sensitivity)
    {
        var bassEnergy = this._fft.GetBandEnergy(spectrum, sampleRate, 20f, 150f) * sensitivity;
        var midEnergy = this._fft.GetBandEnergy(spectrum, sampleRate, 150f, 2000f) * sensitivity;
        var highEnergy = this._fft.GetBandEnergy(spectrum, sampleRate, 2000f, 8000f) * sensitivity;

        // Find dominant band
        var maxEnergy = Math.Max(bassEnergy, Math.Max(midEnergy, highEnergy));

        if (maxEnergy < threshold)
        {
            return null;
        }

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