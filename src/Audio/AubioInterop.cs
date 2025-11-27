namespace Pulsar.Audio;

using System;
using System.Runtime.InteropServices;

/// <summary>
/// P/Invoke bindings for aubio_wrapper.dll - provides beat and onset detection
/// </summary>
public static class AubioInterop
{
    private const String DllName = "aubio_wrapper";

    #region fvec_t (float vector) functions

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr wrapper_new_fvec(UInt32 length);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void wrapper_del_fvec(IntPtr fvec);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Single wrapper_fvec_get_sample(IntPtr fvec, UInt32 position);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void wrapper_fvec_set_sample(IntPtr fvec, Single data, UInt32 position);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr wrapper_fvec_get_data(IntPtr fvec);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void wrapper_fvec_zeros(IntPtr fvec);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern UInt32 wrapper_fvec_get_length(IntPtr fvec);

    #endregion

    #region aubio_tempo_t (beat tracking) functions

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr wrapper_new_aubio_tempo(
        [MarshalAs(UnmanagedType.LPStr)] String method,
        UInt32 bufSize,
        UInt32 hopSize,
        UInt32 samplerate);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void wrapper_aubio_tempo_do(IntPtr tempo, IntPtr input, IntPtr output);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern UInt32 wrapper_aubio_tempo_get_last(IntPtr tempo);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Single wrapper_aubio_tempo_get_last_s(IntPtr tempo);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Single wrapper_aubio_tempo_get_last_ms(IntPtr tempo);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern UInt32 wrapper_aubio_tempo_set_silence(IntPtr tempo, Single silence);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Single wrapper_aubio_tempo_get_silence(IntPtr tempo);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern UInt32 wrapper_aubio_tempo_set_threshold(IntPtr tempo, Single threshold);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Single wrapper_aubio_tempo_get_threshold(IntPtr tempo);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Single wrapper_aubio_tempo_get_period(IntPtr tempo);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Single wrapper_aubio_tempo_get_period_s(IntPtr tempo);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Single wrapper_aubio_tempo_get_bpm(IntPtr tempo);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Single wrapper_aubio_tempo_get_confidence(IntPtr tempo);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void wrapper_del_aubio_tempo(IntPtr tempo);

    #endregion

    #region aubio_onset_t (onset detection) functions

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr wrapper_new_aubio_onset(
        [MarshalAs(UnmanagedType.LPStr)] String method,
        UInt32 bufSize,
        UInt32 hopSize,
        UInt32 samplerate);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void wrapper_aubio_onset_do(IntPtr onset, IntPtr input, IntPtr output);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern UInt32 wrapper_aubio_onset_get_last(IntPtr onset);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Single wrapper_aubio_onset_get_last_s(IntPtr onset);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Single wrapper_aubio_onset_get_last_ms(IntPtr onset);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern UInt32 wrapper_aubio_onset_set_silence(IntPtr onset, Single silence);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Single wrapper_aubio_onset_get_silence(IntPtr onset);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Single wrapper_aubio_onset_get_descriptor(IntPtr onset);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Single wrapper_aubio_onset_get_thresholded_descriptor(IntPtr onset);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern UInt32 wrapper_aubio_onset_set_threshold(IntPtr onset, Single threshold);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Single wrapper_aubio_onset_get_threshold(IntPtr onset);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern UInt32 wrapper_aubio_onset_set_minioi_ms(IntPtr onset, Single minioi);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Single wrapper_aubio_onset_get_minioi_ms(IntPtr onset);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void wrapper_aubio_onset_reset(IntPtr onset);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void wrapper_del_aubio_onset(IntPtr onset);

    #endregion

    #region Convenience functions

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr wrapper_create_tempo_tracker(UInt32 samplerate);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Int32 wrapper_process_tempo(
        IntPtr tempo,
        [In] Single[] samples,
        UInt32 numSamples,
        out Single outBpm);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr wrapper_create_onset_detector(
        [MarshalAs(UnmanagedType.LPStr)] String? method,
        UInt32 samplerate);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern Int32 wrapper_process_onset(
        IntPtr onset,
        [In] Single[] samples,
        UInt32 numSamples);

    #endregion
}

/// <summary>
/// Managed wrapper for aubio tempo (beat) tracker
/// </summary>
public sealed class AubioTempoTracker : IDisposable
{
    private IntPtr _handle;
    private readonly UInt32 _hopSize;
    private Boolean _disposed;

    public AubioTempoTracker(UInt32 sampleRate, UInt32 bufferSize = 1024, UInt32 hopSize = 512, String method = "default")
    {
        this._hopSize = hopSize;
        this._handle = AubioInterop.wrapper_new_aubio_tempo(method, bufferSize, hopSize, sampleRate);
        if (this._handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create aubio tempo tracker");
        }
    }

    public Single Bpm => this._handle != IntPtr.Zero ? AubioInterop.wrapper_aubio_tempo_get_bpm(this._handle) : 0;

    public Single Confidence => this._handle != IntPtr.Zero ? AubioInterop.wrapper_aubio_tempo_get_confidence(this._handle) : 0;

    public Single Threshold
    {
        get => this._handle != IntPtr.Zero ? AubioInterop.wrapper_aubio_tempo_get_threshold(this._handle) : 0;
        set
        {
            if (this._handle != IntPtr.Zero)
            {
                AubioInterop.wrapper_aubio_tempo_set_threshold(this._handle, value);
            }
        }
    }

    public Single Silence
    {
        get => this._handle != IntPtr.Zero ? AubioInterop.wrapper_aubio_tempo_get_silence(this._handle) : 0;
        set
        {
            if (this._handle != IntPtr.Zero)
            {
                AubioInterop.wrapper_aubio_tempo_set_silence(this._handle, value);
            }
        }
    }

    /// <summary>
    /// Process audio samples and detect beats
    /// </summary>
    /// <param name="samples">Audio samples (should be hop_size length for best results)</param>
    /// <param name="bpm">Current BPM estimate</param>
    /// <returns>True if a beat was detected</returns>
    public Boolean Process(Single[] samples, out Single bpm)
    {
        bpm = 0;
        if (this._disposed || this._handle == IntPtr.Zero || samples.Length == 0)
        {
            return false;
        }

        var result = AubioInterop.wrapper_process_tempo(this._handle, samples, (UInt32)samples.Length, out bpm);
        return result != 0;
    }

    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        this._disposed = true;

        if (this._handle != IntPtr.Zero)
        {
            AubioInterop.wrapper_del_aubio_tempo(this._handle);
            this._handle = IntPtr.Zero;
        }
    }
}

/// <summary>
/// Managed wrapper for aubio onset detector
/// </summary>
public sealed class AubioOnsetDetector : IDisposable
{
    private IntPtr _handle;
    private Boolean _disposed;

    /// <summary>
    /// Create onset detector
    /// </summary>
    /// <param name="sampleRate">Audio sample rate</param>
    /// <param name="bufferSize">FFT buffer size</param>
    /// <param name="hopSize">Hop size between frames</param>
    /// <param name="method">Detection method: energy, hfc, complex, phase, wphase, specdiff, kl, mkl, specflux, default</param>
    public AubioOnsetDetector(UInt32 sampleRate, UInt32 bufferSize = 1024, UInt32 hopSize = 512, String method = "default")
    {
        this._handle = AubioInterop.wrapper_new_aubio_onset(method, bufferSize, hopSize, sampleRate);
        if (this._handle == IntPtr.Zero)
        {
            throw new InvalidOperationException("Failed to create aubio onset detector");
        }
    }

    public Single Threshold
    {
        get => this._handle != IntPtr.Zero ? AubioInterop.wrapper_aubio_onset_get_threshold(this._handle) : 0;
        set
        {
            if (this._handle != IntPtr.Zero)
            {
                AubioInterop.wrapper_aubio_onset_set_threshold(this._handle, value);
            }
        }
    }

    public Single Silence
    {
        get => this._handle != IntPtr.Zero ? AubioInterop.wrapper_aubio_onset_get_silence(this._handle) : 0;
        set
        {
            if (this._handle != IntPtr.Zero)
            {
                AubioInterop.wrapper_aubio_onset_set_silence(this._handle, value);
            }
        }
    }

    public Single MinInterOnsetMs
    {
        get => this._handle != IntPtr.Zero ? AubioInterop.wrapper_aubio_onset_get_minioi_ms(this._handle) : 0;
        set
        {
            if (this._handle != IntPtr.Zero)
            {
                AubioInterop.wrapper_aubio_onset_set_minioi_ms(this._handle, value);
            }
        }
    }

    /// <summary>
    /// Process audio samples and detect onsets
    /// </summary>
    /// <param name="samples">Audio samples</param>
    /// <returns>True if an onset was detected</returns>
    public Boolean Process(Single[] samples)
    {
        if (this._disposed || this._handle == IntPtr.Zero || samples.Length == 0)
        {
            return false;
        }

        var result = AubioInterop.wrapper_process_onset(this._handle, samples, (UInt32)samples.Length);
        return result != 0;
    }

    public void Reset()
    {
        if (this._handle != IntPtr.Zero)
        {
            AubioInterop.wrapper_aubio_onset_reset(this._handle);
        }
    }

    public void Dispose()
    {
        if (this._disposed)
        {
            return;
        }

        this._disposed = true;

        if (this._handle != IntPtr.Zero)
        {
            AubioInterop.wrapper_del_aubio_onset(this._handle);
            this._handle = IntPtr.Zero;
        }
    }
}