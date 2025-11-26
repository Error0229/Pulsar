# Humming Mode Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Add a "Humming" mode that uses FFT analysis on system audio output to trigger haptic feedback on MX Master 4 mouse, making the mouse vibrate in sync with music/audio.

**Architecture:** WASAPI loopback capture → FFT analysis → analysis strategy (Bass/Multi-band/Beat/Amplitude) → waveform mapper → Logitech haptic event system. Independent toggle from cursor haptics - both can coexist.

**Tech Stack:** NAudio (WASAPI loopback), System.Numerics (FFT), existing Logitech Actions SDK infrastructure

---

## Design Decisions

1. **Analysis modes:** Implement all 4 strategies (Bass, Multi-band, Beat Detection, Amplitude) - user cycles through them via action command
2. **Interaction with cursor haptics:** Fully independent - separate toggles, both can be active
3. **Responsiveness:** ~20-30ms update rate (1024 sample buffer at 44.1kHz ≈ 23ms)
4. **Audio source:** System loopback (WASAPI) - captures all audio output
5. **Configuration:** Action commands only - toggle on/off, cycle presets
6. **Collision handling:** If both pipelines fire simultaneously, cursor haptics take priority

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────┐
│                    HapticController                      │
│  ┌─────────────────────┐  ┌─────────────────────────┐   │
│  │  Cursor Pipeline    │  │   Humming Pipeline      │   │
│  │  (existing)         │  │   (new)                 │   │
│  │                     │  │                         │   │
│  │  CursorMonitor      │  │  AudioCaptureService    │   │
│  │       ↓             │  │       ↓                 │   │
│  │  CursorTypeFilter   │  │  FFTAnalyzer            │   │
│  │       ↓             │  │       ↓                 │   │
│  │  WaveformMapper     │  │  IAnalysisStrategy      │   │
│  │       ↓             │  │  (Bass/Band/Beat/Amp)   │   │
│  │                     │  │       ↓                 │   │
│  │                     │  │  AudioWaveformMapper    │   │
│  └────────┬────────────┘  └───────────┬─────────────┘   │
│           │                           │                  │
│           └───────────┬───────────────┘                  │
│                       ↓                                  │
│              Logitech SDK → MX Master 4                  │
└─────────────────────────────────────────────────────────┘
```

---

## Prerequisites

Before starting implementation:

1. **NAudio NuGet package**: Audio capture library with WASAPI support
2. **Existing Pulsar codebase**: This builds on the existing plugin infrastructure

---

## Task 1: Add NAudio Dependency

**Goal:** Add NAudio NuGet package for WASAPI loopback capture

**Step 1: Add package reference**

Run:
```bash
cd src
dotnet add package NAudio
```

**Step 2: Verify build**

Run:
```bash
dotnet build
```

Expected: Build succeeds with NAudio available

---

## Task 2: Create Humming Settings

**Goal:** Add settings infrastructure for Humming mode

**Step 1: Create AnalysisMode enum**

File: `src/Settings/AnalysisMode.cs`

```csharp
namespace Pulsar.Settings;

/// <summary>
/// Audio analysis strategies for Humming mode.
/// </summary>
public enum AnalysisMode
{
    /// <summary>
    /// Trigger on low-frequency energy (bass kicks, sub-bass).
    /// </summary>
    Bass,

    /// <summary>
    /// Split audio into frequency bands, map each to different intensities.
    /// </summary>
    MultiBand,

    /// <summary>
    /// Detect sudden volume spikes (drum hits, transients).
    /// </summary>
    BeatDetection,

    /// <summary>
    /// Continuous mapping of overall loudness to haptic intensity.
    /// </summary>
    Amplitude
}
```

**Step 2: Create HummingSettings class**

File: `src/Settings/HummingSettings.cs`

```csharp
namespace Pulsar.Settings;

/// <summary>
/// Configuration for Humming mode audio-reactive haptics.
/// </summary>
public class HummingSettings
{
    /// <summary>
    /// Whether Humming mode is enabled.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Current analysis strategy.
    /// </summary>
    public AnalysisMode AnalysisMode { get; set; } = AnalysisMode.Bass;

    /// <summary>
    /// Sensitivity multiplier (0.5 = less sensitive, 2.0 = more sensitive).
    /// </summary>
    public float Sensitivity { get; set; } = 1.0f;

    /// <summary>
    /// Minimum threshold to trigger haptics (0.0 - 1.0).
    /// </summary>
    public float Threshold { get; set; } = 0.1f;

    /// <summary>
    /// FFT buffer size (1024 recommended for ~23ms at 44.1kHz).
    /// </summary>
    public int FftSize { get; set; } = 1024;
}
```

**Step 3: Add HummingSettings to HapticSettings**

File: `src/Settings/HapticSettings.cs`

Add property:
```csharp
public HummingSettings Humming { get; set; } = new();
```

---

## Task 3: Create Audio Capture Service

**Goal:** Implement WASAPI loopback capture for system audio

**Step 1: Create IAudioCaptureService interface**

File: `src/Audio/IAudioCaptureService.cs`

```csharp
namespace Pulsar.Audio;

/// <summary>
/// Service for capturing system audio output.
/// </summary>
public interface IAudioCaptureService : IDisposable
{
    /// <summary>
    /// Fired when audio samples are ready for processing.
    /// </summary>
    event Action<float[]>? SamplesReady;

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
    bool IsCapturing { get; }

    /// <summary>
    /// Sample rate of captured audio.
    /// </summary>
    int SampleRate { get; }
}
```

**Step 2: Create WasapiLoopbackCaptureService**

File: `src/Audio/WasapiLoopbackCaptureService.cs`

```csharp
using NAudio.Wave;

namespace Pulsar.Audio;

/// <summary>
/// Captures system audio output using WASAPI loopback.
/// </summary>
public class WasapiLoopbackCaptureService : IAudioCaptureService
{
    private readonly int _bufferSize;
    private WasapiLoopbackCapture? _capture;
    private float[] _buffer;
    private int _bufferIndex;
    private readonly object _lock = new();

    public event Action<float[]>? SamplesReady;
    public bool IsCapturing { get; private set; }
    public int SampleRate { get; private set; }

    public WasapiLoopbackCaptureService(int bufferSize = 1024)
    {
        _bufferSize = bufferSize;
        _buffer = new float[bufferSize];
    }

    public void Start()
    {
        if (IsCapturing) return;

        _capture = new WasapiLoopbackCapture();
        SampleRate = _capture.WaveFormat.SampleRate;
        _bufferIndex = 0;

        _capture.DataAvailable += OnDataAvailable;
        _capture.StartRecording();
        IsCapturing = true;
    }

    public void Stop()
    {
        if (!IsCapturing) return;

        _capture?.StopRecording();
        _capture?.Dispose();
        _capture = null;
        IsCapturing = false;
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        var bytesPerSample = _capture!.WaveFormat.BitsPerSample / 8;
        var channels = _capture.WaveFormat.Channels;
        var samplesRecorded = e.BytesRecorded / bytesPerSample;

        for (var i = 0; i < samplesRecorded; i += channels)
        {
            // Convert to float and downmix to mono
            float sample = 0;
            for (var ch = 0; ch < channels; ch++)
            {
                var byteIndex = (i + ch) * bytesPerSample;
                if (byteIndex + 3 < e.BytesRecorded)
                {
                    sample += BitConverter.ToSingle(e.Buffer, byteIndex);
                }
            }
            sample /= channels;

            lock (_lock)
            {
                _buffer[_bufferIndex++] = sample;

                if (_bufferIndex >= _bufferSize)
                {
                    var bufferCopy = new float[_bufferSize];
                    Array.Copy(_buffer, bufferCopy, _bufferSize);
                    _bufferIndex = 0;

                    SamplesReady?.Invoke(bufferCopy);
                }
            }
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
```

---

## Task 4: Create FFT Analyzer

**Goal:** Implement FFT analysis to extract frequency data from audio samples

**Step 1: Create FFTAnalyzer class**

File: `src/Audio/FFTAnalyzer.cs`

```csharp
using System.Numerics;

namespace Pulsar.Audio;

/// <summary>
/// Performs FFT analysis on audio samples.
/// </summary>
public class FFTAnalyzer
{
    private readonly int _fftSize;

    public FFTAnalyzer(int fftSize = 1024)
    {
        _fftSize = fftSize;
    }

    /// <summary>
    /// Compute magnitude spectrum from audio samples.
    /// </summary>
    /// <param name="samples">Audio samples (must match FFT size)</param>
    /// <returns>Magnitude spectrum (half of FFT size)</returns>
    public float[] ComputeSpectrum(float[] samples)
    {
        if (samples.Length != _fftSize)
        {
            throw new ArgumentException($"Expected {_fftSize} samples, got {samples.Length}");
        }

        // Apply Hanning window
        var windowed = ApplyHanningWindow(samples);

        // Convert to complex
        var complex = new Complex[_fftSize];
        for (var i = 0; i < _fftSize; i++)
        {
            complex[i] = new Complex(windowed[i], 0);
        }

        // Perform FFT (in-place Cooley-Tukey)
        FFT(complex);

        // Compute magnitudes (only need first half due to symmetry)
        var magnitudes = new float[_fftSize / 2];
        for (var i = 0; i < magnitudes.Length; i++)
        {
            magnitudes[i] = (float)complex[i].Magnitude;
        }

        return magnitudes;
    }

    /// <summary>
    /// Get the frequency for a given bin index.
    /// </summary>
    public float GetFrequency(int binIndex, int sampleRate)
    {
        return binIndex * sampleRate / (float)_fftSize;
    }

    /// <summary>
    /// Get frequency band energy.
    /// </summary>
    public float GetBandEnergy(float[] spectrum, int sampleRate, float minFreq, float maxFreq)
    {
        var minBin = (int)(minFreq * _fftSize / sampleRate);
        var maxBin = (int)(maxFreq * _fftSize / sampleRate);

        minBin = Math.Max(0, Math.Min(minBin, spectrum.Length - 1));
        maxBin = Math.Max(0, Math.Min(maxBin, spectrum.Length - 1));

        if (minBin >= maxBin) return 0;

        var sum = 0f;
        for (var i = minBin; i <= maxBin; i++)
        {
            sum += spectrum[i] * spectrum[i];
        }

        return (float)Math.Sqrt(sum / (maxBin - minBin + 1));
    }

    private static float[] ApplyHanningWindow(float[] samples)
    {
        var windowed = new float[samples.Length];
        for (var i = 0; i < samples.Length; i++)
        {
            var window = 0.5f * (1 - MathF.Cos(2 * MathF.PI * i / (samples.Length - 1)));
            windowed[i] = samples[i] * window;
        }
        return windowed;
    }

    private static void FFT(Complex[] data)
    {
        var n = data.Length;
        if (n <= 1) return;

        // Bit-reversal permutation
        for (int i = 1, j = 0; i < n; i++)
        {
            var bit = n >> 1;
            for (; (j & bit) != 0; bit >>= 1)
            {
                j ^= bit;
            }
            j ^= bit;

            if (i < j)
            {
                (data[i], data[j]) = (data[j], data[i]);
            }
        }

        // Cooley-Tukey iterative FFT
        for (var len = 2; len <= n; len <<= 1)
        {
            var angle = -2 * Math.PI / len;
            var wlen = new Complex(Math.Cos(angle), Math.Sin(angle));

            for (var i = 0; i < n; i += len)
            {
                var w = Complex.One;
                for (var j = 0; j < len / 2; j++)
                {
                    var u = data[i + j];
                    var v = data[i + j + len / 2] * w;
                    data[i + j] = u + v;
                    data[i + j + len / 2] = u - v;
                    w *= wlen;
                }
            }
        }
    }
}
```

---

## Task 5: Create Analysis Strategies

**Goal:** Implement the 4 audio analysis strategies

**Step 1: Create IAnalysisStrategy interface**

File: `src/Audio/Analysis/IAnalysisStrategy.cs`

```csharp
using Pulsar.Haptics;

namespace Pulsar.Audio.Analysis;

/// <summary>
/// Analyzes FFT spectrum and determines if haptic should trigger.
/// </summary>
public interface IAnalysisStrategy
{
    /// <summary>
    /// Analyze spectrum and return haptic trigger result.
    /// </summary>
    /// <param name="spectrum">FFT magnitude spectrum</param>
    /// <param name="sampleRate">Audio sample rate</param>
    /// <param name="threshold">Trigger threshold (0-1)</param>
    /// <param name="sensitivity">Sensitivity multiplier</param>
    /// <returns>Waveform to trigger, or null if no trigger</returns>
    HapticTriggerResult? Analyze(float[] spectrum, int sampleRate, float threshold, float sensitivity);
}

/// <summary>
/// Result of audio analysis.
/// </summary>
public record HapticTriggerResult(WaveformType Waveform, float Intensity);
```

**Step 2: Create BassAnalysisStrategy**

File: `src/Audio/Analysis/BassAnalysisStrategy.cs`

```csharp
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

        if (!isHit) return null;

        // Stronger bass = SharpCollision, lighter = SubtleCollision
        var waveform = energy > threshold * 2 ? WaveformType.SharpCollision : WaveformType.SubtleCollision;
        return new HapticTriggerResult(waveform, Math.Min(energy, 1f));
    }
}
```

**Step 3: Create MultiBandAnalysisStrategy**

File: `src/Audio/Analysis/MultiBandAnalysisStrategy.cs`

```csharp
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
        if (maxEnergy < threshold) return null;

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
```

**Step 4: Create BeatDetectionStrategy**

File: `src/Audio/Analysis/BeatDetectionStrategy.cs`

```csharp
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

        if (_energyHistory.Count < 10) return null;

        // Calculate average energy
        var averageEnergy = _energyHistory.Average();

        // Beat = current energy significantly above average
        var beatThreshold = averageEnergy * (1.5f + threshold);
        if (energy <= beatThreshold) return null;

        // Strong beat = SharpCollision, normal beat = Knock
        var waveform = energy > beatThreshold * 1.5f ? WaveformType.SharpCollision : WaveformType.Knock;
        return new HapticTriggerResult(waveform, Math.Min(energy / beatThreshold, 1f));
    }
}
```

**Step 5: Create AmplitudeAnalysisStrategy**

File: `src/Audio/Analysis/AmplitudeAnalysisStrategy.cs`

```csharp
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

        if (_smoothedAmplitude < threshold) return null;

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
```

---

## Task 6: Create Humming Controller

**Goal:** Orchestrate audio capture, analysis, and haptic triggering

**Step 1: Create HummingController class**

File: `src/Audio/HummingController.cs`

```csharp
using Loupedeck;
using Pulsar.Audio.Analysis;
using Pulsar.Haptics;
using Pulsar.Settings;

namespace Pulsar.Audio;

/// <summary>
/// Orchestrates audio-reactive haptic feedback.
/// </summary>
public class HummingController : IDisposable
{
    private readonly IAudioCaptureService _captureService;
    private readonly FFTAnalyzer _fftAnalyzer;
    private readonly HapticSettings _settings;
    private readonly Plugin _plugin;
    private readonly Dictionary<AnalysisMode, IAnalysisStrategy> _strategies;

    private DateTime _lastHapticTime = DateTime.MinValue;
    private readonly TimeSpan _minHapticInterval = TimeSpan.FromMilliseconds(50);

    public HummingController(
        IAudioCaptureService captureService,
        HapticSettings settings,
        Plugin plugin)
    {
        _captureService = captureService;
        _settings = settings;
        _plugin = plugin;
        _fftAnalyzer = new FFTAnalyzer(settings.Humming.FftSize);

        _strategies = new Dictionary<AnalysisMode, IAnalysisStrategy>
        {
            [AnalysisMode.Bass] = new BassAnalysisStrategy(_fftAnalyzer),
            [AnalysisMode.MultiBand] = new MultiBandAnalysisStrategy(_fftAnalyzer),
            [AnalysisMode.BeatDetection] = new BeatDetectionStrategy(),
            [AnalysisMode.Amplitude] = new AmplitudeAnalysisStrategy()
        };

        _captureService.SamplesReady += OnSamplesReady;
    }

    public void Start()
    {
        if (!_settings.Humming.Enabled) return;
        _captureService.Start();
    }

    public void Stop()
    {
        _captureService.Stop();
    }

    public void SetEnabled(bool enabled)
    {
        _settings.Humming.Enabled = enabled;
        if (enabled)
        {
            Start();
        }
        else
        {
            Stop();
        }
    }

    public void CycleAnalysisMode()
    {
        var modes = Enum.GetValues<AnalysisMode>();
        var currentIndex = Array.IndexOf(modes, _settings.Humming.AnalysisMode);
        var nextIndex = (currentIndex + 1) % modes.Length;
        _settings.Humming.AnalysisMode = modes[nextIndex];
    }

    private void OnSamplesReady(float[] samples)
    {
        if (!_settings.Humming.Enabled) return;

        // Throttle haptic triggers
        var now = DateTime.UtcNow;
        if (now - _lastHapticTime < _minHapticInterval) return;

        var spectrum = _fftAnalyzer.ComputeSpectrum(samples);
        var strategy = _strategies[_settings.Humming.AnalysisMode];

        var result = strategy.Analyze(
            spectrum,
            _captureService.SampleRate,
            _settings.Humming.Threshold,
            _settings.Humming.Sensitivity);

        if (result == null) return;

        _lastHapticTime = now;
        TriggerHaptic(result.Waveform);
    }

    private void TriggerHaptic(WaveformType waveform)
    {
        var eventName = waveform switch
        {
            WaveformType.SharpCollision => "sharp_collision",
            WaveformType.SubtleCollision => "subtle_collision",
            WaveformType.SharpStateChange => "sharp_state_change",
            WaveformType.DampStateChange => "damp_state_change",
            WaveformType.Ringing => "ringing",
            WaveformType.Knock => "knock",
            WaveformType.Mad => "mad",
            _ => "subtle_collision"
        };

        _plugin.RaiseEvent(eventName);
    }

    public void Dispose()
    {
        _captureService.SamplesReady -= OnSamplesReady;
        Stop();
    }
}
```

---

## Task 7: Create Action Commands

**Goal:** Add mouse button actions to toggle Humming mode

**Step 1: Create HummingToggleCommand**

File: `src/Actions/HummingToggleCommand.cs`

```csharp
using Loupedeck;
using Pulsar.Audio;

namespace Pulsar.Actions;

/// <summary>
/// Action command to toggle Humming mode on/off.
/// </summary>
public class HummingToggleCommand : PluginDynamicCommand
{
    private PulsarPlugin Plugin => (PulsarPlugin)base.Plugin;

    public HummingToggleCommand()
        : base("Humming Toggle", "Toggle audio-reactive haptics", "Haptics")
    {
    }

    protected override void RunCommand(string actionParameter)
    {
        var controller = Plugin.HummingController;
        if (controller == null) return;

        var newState = !Plugin.Settings.Humming.Enabled;
        controller.SetEnabled(newState);

        var status = newState ? "ON" : "OFF";
        ActionImageChanged();
    }

    protected override string GetCommandDisplayName(string actionParameter, PluginImageSize imageSize)
    {
        var status = Plugin.Settings.Humming.Enabled ? "ON" : "OFF";
        return $"Humming: {status}";
    }
}
```

**Step 2: Create HummingModeCommand**

File: `src/Actions/HummingModeCommand.cs`

```csharp
using Loupedeck;
using Pulsar.Audio;
using Pulsar.Settings;

namespace Pulsar.Actions;

/// <summary>
/// Action command to cycle through Humming analysis modes.
/// </summary>
public class HummingModeCommand : PluginDynamicCommand
{
    private PulsarPlugin Plugin => (PulsarPlugin)base.Plugin;

    public HummingModeCommand()
        : base("Humming Mode", "Cycle audio analysis mode", "Haptics")
    {
    }

    protected override void RunCommand(string actionParameter)
    {
        var controller = Plugin.HummingController;
        if (controller == null) return;

        controller.CycleAnalysisMode();
        ActionImageChanged();
    }

    protected override string GetCommandDisplayName(string actionParameter, PluginImageSize imageSize)
    {
        var mode = Plugin.Settings.Humming.AnalysisMode;
        var modeName = mode switch
        {
            AnalysisMode.Bass => "Bass",
            AnalysisMode.MultiBand => "Multi-Band",
            AnalysisMode.BeatDetection => "Beat",
            AnalysisMode.Amplitude => "Amplitude",
            _ => "Unknown"
        };
        return $"Mode: {modeName}";
    }
}
```

---

## Task 8: Integrate with Plugin

**Goal:** Wire up HummingController in the main plugin class

**Step 1: Add HummingController property to PulsarPlugin**

File: `src/PulsarPlugin.cs`

Add field and property:
```csharp
private HummingController? _hummingController;
public HummingController? HummingController => _hummingController;
```

**Step 2: Initialize HummingController in plugin load**

In the plugin initialization (after settings are loaded):
```csharp
var captureService = new WasapiLoopbackCaptureService(Settings.Humming.FftSize);
_hummingController = new HummingController(captureService, Settings, this);

if (Settings.Humming.Enabled)
{
    _hummingController.Start();
}
```

**Step 3: Dispose HummingController on unload**

In plugin unload:
```csharp
_hummingController?.Dispose();
```

---

## Task 9: Testing

**Goal:** Verify Humming mode works end-to-end

**Step 1: Build and deploy**

```bash
dotnet build
```

**Step 2: Manual testing checklist**

1. [ ] Plugin loads without errors in Logi Options+
2. [ ] Humming Toggle command appears and toggles on/off
3. [ ] Humming Mode command cycles through Bass → Multi-Band → Beat → Amplitude
4. [ ] Playing music triggers haptic feedback when Humming is ON
5. [ ] No haptics when Humming is OFF
6. [ ] Cursor haptics still work independently
7. [ ] No crashes when switching modes during playback
8. [ ] No excessive CPU usage (check Task Manager)

---

## File Structure Summary

```
src/
├── Audio/
│   ├── IAudioCaptureService.cs
│   ├── WasapiLoopbackCaptureService.cs
│   ├── FFTAnalyzer.cs
│   ├── HummingController.cs
│   └── Analysis/
│       ├── IAnalysisStrategy.cs
│       ├── BassAnalysisStrategy.cs
│       ├── MultiBandAnalysisStrategy.cs
│       ├── BeatDetectionStrategy.cs
│       └── AmplitudeAnalysisStrategy.cs
├── Settings/
│   ├── AnalysisMode.cs
│   ├── HummingSettings.cs
│   └── HapticSettings.cs (modified)
├── Actions/
│   ├── HummingToggleCommand.cs
│   └── HummingModeCommand.cs
└── PulsarPlugin.cs (modified)
```

---

## Summary

- **9 tasks** total
- **12 new files** to create
- **2 existing files** to modify
- **NAudio** dependency for audio capture
- **Strategy pattern** for swappable analysis modes
- **Independent toggle** from cursor haptics
