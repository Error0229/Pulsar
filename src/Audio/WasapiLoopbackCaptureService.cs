namespace Pulsar.Audio;

using System.Runtime.InteropServices;

using static Pulsar.Audio.WasapiInterop;

/// <summary>
/// Captures system audio output using WASAPI loopback via direct P/Invoke.
/// Avoids NAudio COM interop issues in plugin host environments.
/// </summary>
public class WasapiLoopbackCaptureService : IAudioCaptureService
{
    private readonly Int32 _bufferSize;
    private readonly Single[] _sampleBuffer;
    private Int32 _bufferIndex;
    private readonly Object _lock = new();

    private Thread _captureThread;
    private volatile Boolean _stopRequested;
    private readonly ManualResetEvent _startedEvent = new(false);
    private Exception _initException;

    // WASAPI resources (managed on capture thread)
    private IMMDevice _device;
    private IAudioClient _audioClient;
    private IAudioCaptureClient _captureClient;
    private IntPtr _formatPtr;
    private Int32 _frameSize;
    private Int32 _channels;
    private Boolean _isFloat;

    public event Action<Single[]> SamplesReady;
    public Boolean IsCapturing { get; private set; }
    public Int32 SampleRate { get; private set; }

    public WasapiLoopbackCaptureService(Int32 bufferSize = 1024)
    {
        this._bufferSize = bufferSize;
        this._sampleBuffer = new Single[bufferSize];
    }

    public void Start()
    {
        if (this.IsCapturing)
        {
            return;
        }

        this._stopRequested = false;
        this._startedEvent.Reset();
        this._initException = null;

        this._captureThread = new Thread(this.CaptureThreadProc)
        {
            Name = "WASAPI Capture Thread",
            IsBackground = true
        };
        this._captureThread.SetApartmentState(ApartmentState.STA);
        this._captureThread.Start();

        this._startedEvent.WaitOne(TimeSpan.FromSeconds(5));

        if (this._initException != null)
        {
            throw this._initException;
        }

        this.IsCapturing = true;
    }

    private void CaptureThreadProc()
    {
        try
        {
            // Initialize COM for this thread
            CoInitializeEx(IntPtr.Zero, COINIT_APARTMENTTHREADED);

            try
            {
                this.InitializeWasapi();
                this._startedEvent.Set();

                // Capture loop
                this.CaptureLoop();
            }
            finally
            {
                this.CleanupWasapi();
                CoUninitialize();
            }
        }
        catch (Exception ex)
        {
            this._initException = ex;
            this._startedEvent.Set();
        }
    }

    private void InitializeWasapi()
    {
        // Create device enumerator
        var clsid = CLSID_MMDeviceEnumerator;
        var iid = IID_IMMDeviceEnumerator;
        var hr = CoCreateInstance(ref clsid, IntPtr.Zero, CLSCTX_ALL, ref iid, out var enumeratorPtr);
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        var enumerator = (IMMDeviceEnumerator)Marshal.GetObjectForIUnknown(enumeratorPtr);

        // Get default render endpoint (for loopback capture)
        hr = enumerator.GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eConsole, out this._device);
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        Marshal.Release(enumeratorPtr);

        // Activate IAudioClient
        var audioClientIid = IID_IAudioClient;
        hr = this._device.Activate(ref audioClientIid, CLSCTX_ALL, IntPtr.Zero, out var audioClientPtr);
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        this._audioClient = (IAudioClient)Marshal.GetObjectForIUnknown(audioClientPtr);
        Marshal.Release(audioClientPtr);

        // Get mix format
        hr = this._audioClient.GetMixFormat(out this._formatPtr);
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        var format = Marshal.PtrToStructure<WAVEFORMATEX>(this._formatPtr);
        this.SampleRate = (Int32)format.nSamplesPerSec;
        this._channels = format.nChannels;
        this._frameSize = format.nBlockAlign;

        // Check if format is float
        if (format.wFormatTag == 0xFFFE) // WAVE_FORMAT_EXTENSIBLE
        {
            var extFormat = Marshal.PtrToStructure<WAVEFORMATEXTENSIBLE>(this._formatPtr);
            this._isFloat = extFormat.SubFormat == KSDATAFORMAT_SUBTYPE_IEEE_FLOAT;
        }
        else
        {
            this._isFloat = format.wFormatTag == 3; // WAVE_FORMAT_IEEE_FLOAT
        }

        // Initialize audio client with loopback flag
        // Buffer duration: 100ms in 100-nanosecond units
        var bufferDuration = 1000000L; // 100ms
        hr = this._audioClient.Initialize(
            AUDCLNT_SHAREMODE.AUDCLNT_SHAREMODE_SHARED,
            (UInt32)AUDCLNT_STREAMFLAGS.AUDCLNT_STREAMFLAGS_LOOPBACK,
            bufferDuration,
            0,
            this._formatPtr,
            IntPtr.Zero);
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        // Get capture client
        var captureClientIid = IID_IAudioCaptureClient;
        hr = this._audioClient.GetService(ref captureClientIid, out var captureClientPtr);
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        this._captureClient = (IAudioCaptureClient)Marshal.GetObjectForIUnknown(captureClientPtr);
        Marshal.Release(captureClientPtr);

        // Start capturing
        hr = this._audioClient.Start();
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }
    }

    private void CaptureLoop()
    {
        while (!this._stopRequested)
        {
            Thread.Sleep(10); // ~100 Hz polling

            try
            {
                // Get available packets
                var hr = this._captureClient.GetNextPacketSize(out var packetSize);
                if (hr < 0)
                {
                    continue;
                }

                while (packetSize > 0)
                {
                    hr = this._captureClient.GetBuffer(out var dataPtr, out var numFrames, out var flags, out _, out _);
                    if (hr < 0)
                    {
                        break;
                    }

                    if ((flags & AUDCLNT_BUFFERFLAGS_SILENT) == 0 && numFrames > 0)
                    {
                        this.ProcessAudioData(dataPtr, numFrames);
                    }

                    this._captureClient.ReleaseBuffer(numFrames);

                    hr = this._captureClient.GetNextPacketSize(out packetSize);
                    if (hr < 0)
                    {
                        break;
                    }
                }
            }
            catch
            {
                // Ignore capture errors, keep trying
            }
        }
    }

    private void ProcessAudioData(IntPtr dataPtr, UInt32 numFrames)
    {
        var bytesPerSample = this._isFloat ? 4 : 2;

        for (var i = 0; i < numFrames; i++)
        {
            // Downmix to mono
            Single sample = 0;
            for (var ch = 0; ch < this._channels; ch++)
            {
                var offset = (i * this._channels + ch) * bytesPerSample;
                if (this._isFloat)
                {
                    sample += Marshal.PtrToStructure<Single>(dataPtr + offset);
                }
                else
                {
                    var shortSample = Marshal.PtrToStructure<Int16>(dataPtr + offset);
                    sample += shortSample / 32768f;
                }
            }
            sample /= this._channels;

            Single[] bufferToFire = null;

            lock (this._lock)
            {
                this._sampleBuffer[this._bufferIndex++] = sample;

                if (this._bufferIndex >= this._bufferSize)
                {
                    bufferToFire = new Single[this._bufferSize];
                    Array.Copy(this._sampleBuffer, bufferToFire, this._bufferSize);
                    this._bufferIndex = 0;
                }
            }

            if (bufferToFire != null)
            {
                SamplesReady?.Invoke(bufferToFire);
            }
        }
    }

    private void CleanupWasapi()
    {
        try
        { this._audioClient?.Stop(); }
        catch { }

        if (this._formatPtr != IntPtr.Zero)
        {
            CoTaskMemFree(this._formatPtr);
            this._formatPtr = IntPtr.Zero;
        }

        if (this._captureClient != null)
        {
            Marshal.ReleaseComObject(this._captureClient);
            this._captureClient = null;
        }

        if (this._audioClient != null)
        {
            Marshal.ReleaseComObject(this._audioClient);
            this._audioClient = null;
        }

        if (this._device != null)
        {
            Marshal.ReleaseComObject(this._device);
            this._device = null;
        }
    }

    public void Stop()
    {
        if (!this.IsCapturing)
        {
            return;
        }

        this._stopRequested = true;

        if (this._captureThread != null && this._captureThread.IsAlive)
        {
            this._captureThread.Join(TimeSpan.FromSeconds(2));
            this._captureThread = null;
        }

        this.IsCapturing = false;
    }

    public void Dispose()
    {
        this.Stop();
        this._startedEvent.Dispose();
    }
}