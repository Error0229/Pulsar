using NAudio.Wave;

namespace Pulsar.Audio;

/// <summary>
/// Captures system audio output using WASAPI loopback.
/// </summary>
public class WasapiLoopbackCaptureService : IAudioCaptureService
{
    private readonly int _bufferSize;
    private WasapiLoopbackCapture _capture;
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

        var capture = _capture;
        if (capture != null)
        {
            capture.DataAvailable -= OnDataAvailable;
            capture.StopRecording();
            capture.Dispose();
            _capture = null;
        }

        IsCapturing = false;
    }

    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        var capture = _capture;
        if (capture == null) return;

        var bytesPerSample = capture.WaveFormat.BitsPerSample / 8;
        var channels = capture.WaveFormat.Channels;
        var samplesRecorded = e.BytesRecorded / bytesPerSample;

        for (var i = 0; i < samplesRecorded; i += channels)
        {
            // Convert to float and downmix to mono
            float sample = 0;
            for (var ch = 0; ch < channels; ch++)
            {
                var byteIndex = (i + ch) * bytesPerSample;
                if (byteIndex + bytesPerSample <= e.BytesRecorded)
                {
                    sample += BitConverter.ToSingle(e.Buffer, byteIndex);
                }
            }
            sample /= channels;

            float[]? bufferToFire = null;

            lock (_lock)
            {
                _buffer[_bufferIndex++] = sample;

                if (_bufferIndex >= _bufferSize)
                {
                    bufferToFire = new float[_bufferSize];
                    Array.Copy(_buffer, bufferToFire, _bufferSize);
                    _bufferIndex = 0;
                }
            }

            if (bufferToFire != null)
            {
                SamplesReady?.Invoke(bufferToFire);
            }
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
