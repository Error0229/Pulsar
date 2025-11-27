namespace Pulsar.Audio;

/// <summary>
/// Service for capturing system audio output.
/// </summary>
public interface IAudioCaptureService : IDisposable
{
    /// <summary>
    /// Fired when audio samples are ready for processing.
    /// </summary>
    event Action<Single[]>? SamplesReady;

    /// <summary>
    /// Start capturing audio.
    /// </summary>
    void Start();

    /// <summary>
    /// Stop capturing audio.
    /// </summary>
    void Stop();

    /// <summary>
    /// Whether capture is currently active.
    /// </summary>
    Boolean IsCapturing { get; }

    /// <summary>
    /// Sample rate of captured audio.
    /// </summary>
    Int32 SampleRate { get; }
}