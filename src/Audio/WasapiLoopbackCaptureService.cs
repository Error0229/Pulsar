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

    public event Action<float[]> SamplesReady;
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

        if (_capture != null)
        {
            _capture.StopRecording();
            _capture.Dispose();
            _capture = null;
        }

        IsCapturing = false;
    }

    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        if (_capture == null) return;

        var bytesPerSample = _capture.WaveFormat.BitsPerSample / 8;
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

                    if (SamplesReady != null)
                    {
                        SamplesReady.Invoke(bufferCopy);
                    }
                }
            }
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
