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
