namespace Pulsar.Audio;

using System;

using Pulsar.Audio.Analysis;
using Pulsar.Haptics;
using Pulsar.Settings;

/// <summary>
/// Coordinates audio capture, analysis, and haptic triggering for Humming mode.
/// </summary>
public class HummingController : IDisposable
{
    private readonly HummingSettings _settings;
    private readonly Action<String> _triggerHapticEvent;
    private readonly Action<String> _logDebug;

    private IAudioCaptureService? _captureService;
    private FFTAnalyzer? _fftAnalyzer;
    private IAnalysisStrategy? _currentStrategy;

    // Keep raw samples for aubio beat detection
    private Single[]? _rawSampleBuffer;
    private Boolean _disposed;
    private DateTime _lastHapticTime = DateTime.MinValue;

    // Strategies - created when FFTAnalyzer is available
    private BassAnalysisStrategy? _bassStrategy;
    private MultiBandAnalysisStrategy? _multiBandStrategy;
    private BeatDetectionStrategy? _beatStrategy;
    private AmplitudeAnalysisStrategy? _amplitudeStrategy;

    public Boolean IsRunning { get; private set; }

    /// <summary>
    /// Current BPM if beat detection is active with aubio
    /// </summary>
    public Single CurrentBpm => this._beatStrategy?.CurrentBpm ?? 0;

    /// <summary>
    /// Whether aubio beat detection is being used
    /// </summary>
    public Boolean IsAubioActive => this._beatStrategy?.IsAubioActive ?? false;

    public HummingController(HummingSettings settings, Action<String> triggerHapticEvent, Action<String>? logDebug = null)
    {
        this._settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this._triggerHapticEvent = triggerHapticEvent ?? throw new ArgumentNullException(nameof(triggerHapticEvent));
        this._logDebug = logDebug ?? (_ => { });
    }

    /// <summary>
    /// Start audio capture and analysis
    /// </summary>
    public void Start()
    {
        if (this.IsRunning)
        {
            return;
        }

        if (!this._settings.Enabled)
        {
            this._logDebug("Humming mode disabled in settings");
            return;
        }

        try
        {
            this._logDebug("Starting Humming mode...");

            // Create audio capture service
            this._captureService = new WasapiLoopbackCaptureService(this._settings.FftSize);
            this._captureService.SamplesReady += this.OnSamplesReady;

            // Create FFT analyzer
            this._fftAnalyzer = new FFTAnalyzer(this._settings.FftSize);

            // Select strategy based on mode
            this._currentStrategy = this.GetStrategy(this._settings.AnalysisMode);

            // Initialize aubio for beat detection
            if (this._currentStrategy is BeatDetectionStrategy beatStrategy)
            {
                beatStrategy.InitializeAubio((UInt32)this._captureService.SampleRate);
            }

            // Allocate buffer for raw samples (for aubio)
            this._rawSampleBuffer = new Single[this._settings.FftSize];

            // Start capture
            this._captureService.Start();
            this.IsRunning = true;

            this._logDebug($"Humming mode started. Mode: {this._settings.AnalysisMode}, SampleRate: {this._captureService.SampleRate}");
        }
        catch (Exception ex)
        {
            this._logDebug($"Failed to start Humming mode: {ex.Message}");
            this.Stop();
            throw;
        }
    }

    /// <summary>
    /// Stop audio capture and analysis
    /// </summary>
    public void Stop()
    {
        if (!this.IsRunning && this._captureService == null)
        {
            return;
        }

        this._logDebug("Stopping Humming mode...");

        this.IsRunning = false;

        if (this._captureService != null)
        {
            this._captureService.SamplesReady -= this.OnSamplesReady;
            this._captureService.Stop();
            this._captureService.Dispose();
            this._captureService = null;
        }

        this._fftAnalyzer = null;
        this._rawSampleBuffer = null;

        this._logDebug("Humming mode stopped");
    }

    /// <summary>
    /// Change the analysis mode
    /// </summary>
    public void SetMode(AnalysisMode mode)
    {
        this._settings.AnalysisMode = mode;
        this._logDebug($"Humming mode changed to: {mode}");

        // Only update strategy if already running (FFTAnalyzer is initialized)
        if (this.IsRunning && this._fftAnalyzer != null)
        {
            this._currentStrategy = this.GetStrategy(mode);

            // Initialize aubio if switching to beat detection
            if (this._currentStrategy is BeatDetectionStrategy beatStrategy && this._captureService != null)
            {
                beatStrategy.InitializeAubio((UInt32)this._captureService.SampleRate);
            }
        }
    }

    private IAnalysisStrategy GetStrategy(AnalysisMode mode)
    {
        if (this._fftAnalyzer == null)
        {
            throw new InvalidOperationException("FFTAnalyzer must be initialized before getting strategy");
        }

        return mode switch
        {
            AnalysisMode.Bass => this._bassStrategy ??= new BassAnalysisStrategy(this._fftAnalyzer),
            AnalysisMode.MultiBand => this._multiBandStrategy ??= new MultiBandAnalysisStrategy(this._fftAnalyzer),
            AnalysisMode.BeatDetection => this._beatStrategy ??= new BeatDetectionStrategy(),
            AnalysisMode.Amplitude => this._amplitudeStrategy ??= new AmplitudeAnalysisStrategy(),
            _ => this._bassStrategy ??= new BassAnalysisStrategy(this._fftAnalyzer)
        };
    }

    private void OnSamplesReady(Single[] samples)
    {
        if (!this.IsRunning || this._fftAnalyzer == null || this._currentStrategy == null)
        {
            return;
        }

        try
        {
            HapticTriggerResult? result = null;

            // For beat detection mode, try aubio first with raw samples
            if (this._currentStrategy is BeatDetectionStrategy beatStrategy && beatStrategy.IsAubioActive)
            {
                // Store raw samples for aubio processing
                Array.Copy(samples, this._rawSampleBuffer!, Math.Min(samples.Length, this._rawSampleBuffer!.Length));
                result = beatStrategy.ProcessWithAubio(this._rawSampleBuffer, this._settings.Threshold, this._settings.Sensitivity);
            }

            // If aubio didn't detect anything (or isn't active), use FFT-based analysis
            if (result == null)
            {
                // Compute FFT spectrum
                var spectrum = this._fftAnalyzer.ComputeSpectrum(samples);

                // Run analysis strategy
                result = this._currentStrategy.Analyze(
                    spectrum,
                    this._captureService.SampleRate,
                    this._settings.Threshold,
                    this._settings.Sensitivity);
            }

            if (result == null)
            {
                return;
            }

            // Apply minimum interval to avoid haptic spam
            var now = DateTime.UtcNow;
            var minInterval = TimeSpan.FromMilliseconds(50); // 50ms minimum between haptics
            if (now - this._lastHapticTime < minInterval)
            {
                return;
            }

            this._lastHapticTime = now;

            // Trigger haptic event
            var eventName = WaveformMapper.ToEventName(result.Waveform);
            this._logDebug($"Humming haptic: {eventName} (intensity: {result.Intensity:F2})");
            this._triggerHapticEvent(eventName);
        }
        catch (Exception ex)
        {
            this._logDebug($"Error in audio analysis: {ex.Message}");
        }
    }

    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        this._disposed = true;

        this.Stop();
        this._beatStrategy?.Dispose();
    }
}