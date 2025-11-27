namespace Pulsar.Audio.Analysis;

using Pulsar.Haptics;

/// <summary>
/// Detects beats using aubio library with fallback to energy-based detection.
/// </summary>
public class BeatDetectionStrategy : IAnalysisStrategy, IDisposable
{
    private readonly Queue<Single> _energyHistory = new();
    private const Int32 HistorySize = 43; // ~1 second at 23ms per frame

    private AubioTempoTracker? _aubioTracker;
    private Boolean _aubioAvailable;
    private Boolean _aubioInitialized;
    private Boolean _disposed;

    /// <summary>
    /// Current BPM detected by aubio (0 if not available)
    /// </summary>
    public Single CurrentBpm { get; private set; }

    /// <summary>
    /// Whether aubio beat detection is active
    /// </summary>
    public Boolean IsAubioActive => this._aubioAvailable && this._aubioTracker != null;

    /// <summary>
    /// Initializes aubio beat tracker. Call once when sample rate is known.
    /// </summary>
    public void InitializeAubio(UInt32 sampleRate)
    {
        if (this._aubioInitialized)
        {
            return;
        }

        this._aubioInitialized = true;

        try
        {
            this._aubioTracker = new AubioTempoTracker(sampleRate, 1024, 512);
            this._aubioTracker.Silence = -70f; // dB threshold for silence
            this._aubioAvailable = true;
        }
        catch (Exception)
        {
            // Aubio DLL not available or failed to initialize
            this._aubioAvailable = false;
            this._aubioTracker = null;
        }
    }

    /// <summary>
    /// Process raw audio samples using aubio for beat detection.
    /// Returns a result if a beat was detected.
    /// </summary>
    public HapticTriggerResult? ProcessWithAubio(Single[] samples, Single threshold, Single sensitivity)
    {
        if (!this._aubioAvailable || this._aubioTracker == null || samples.Length == 0)
        {
            return null;
        }

        try
        {
            var beatDetected = this._aubioTracker.Process(samples, out var bpm);
            this.CurrentBpm = bpm;

            if (!beatDetected)
            {
                return null;
            }

            // Calculate intensity based on BPM confidence
            var confidence = this._aubioTracker.Confidence;
            var intensity = Math.Clamp(confidence * sensitivity, 0f, 1f);

            // Higher BPM = faster music = more energetic feedback
            var waveform = bpm > 140
                ? WaveformType.SharpCollision
                : bpm > 100
                    ? WaveformType.Knock
                    : WaveformType.Knock;

            return new HapticTriggerResult(waveform, intensity);
        }
        catch
        {
            // If aubio fails, disable it and use fallback
            this._aubioAvailable = false;
            return null;
        }
    }

    /// <summary>
    /// Fallback energy-based beat detection using spectrum data.
    /// </summary>
    public HapticTriggerResult? Analyze(Single[] spectrum, Int32 sampleRate, Single threshold, Single sensitivity)
    {
        // Initialize aubio if not done yet
        if (!this._aubioInitialized)
        {
            this.InitializeAubio((UInt32)sampleRate);
        }

        // Calculate total energy
        var energy = 0f;
        foreach (var mag in spectrum)
        {
            energy += mag * mag;
        }
        energy = MathF.Sqrt(energy / spectrum.Length) * sensitivity;

        // Maintain history
        this._energyHistory.Enqueue(energy);
        if (this._energyHistory.Count > HistorySize)
        {
            this._energyHistory.Dequeue();
        }

        if (this._energyHistory.Count < 10)
        {
            return null;
        }

        // Calculate average energy
        var averageEnergy = this._energyHistory.Average();

        // Beat = current energy significantly above average
        var beatThreshold = averageEnergy * (1.5f + threshold);

        if (energy <= beatThreshold)
        {
            return null;
        }

        // Strong beat = SharpCollision, normal beat = Knock
        var waveform = energy > beatThreshold * 1.5f
            ? WaveformType.SharpCollision
            : WaveformType.Knock;

        return new HapticTriggerResult(waveform, Math.Min(energy / beatThreshold, 1f));
    }

    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        this._disposed = true;

        this._aubioTracker?.Dispose();
        this._aubioTracker = null;
    }
}