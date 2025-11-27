namespace Pulsar.Audio;

using System.Numerics;

/// <summary>
/// Performs FFT analysis on audio samples.
/// </summary>
public class FFTAnalyzer
{
    private readonly Int32 _fftSize;

    public FFTAnalyzer(Int32 fftSize = 1024)
    {
        if (fftSize <= 0 || (fftSize & (fftSize - 1)) != 0)
        {
            throw new ArgumentException($"FFT size must be power of 2, got {fftSize}");
        }

        this._fftSize = fftSize;
    }

    /// <summary>
    /// Compute magnitude spectrum from audio samples.
    /// </summary>
    /// <param name="samples">Audio samples (must match FFT size)</param>
    /// <returns>Magnitude spectrum (half of FFT size)</returns>
    public Single[] ComputeSpectrum(Single[] samples)
    {
        if (samples.Length != this._fftSize)
        {
            throw new ArgumentException($"Expected {this._fftSize} samples, got {samples.Length}");
        }

        // Apply Hanning window
        var windowed = ApplyHanningWindow(samples);

        // Convert to complex
        var complex = new Complex[this._fftSize];
        for (var i = 0; i < this._fftSize; i++)
        {
            complex[i] = new Complex(windowed[i], 0);
        }

        // Perform FFT (in-place Cooley-Tukey)
        FFT(complex);

        // Compute magnitudes (only need first half due to symmetry)
        var magnitudes = new Single[this._fftSize / 2];
        for (var i = 0; i < magnitudes.Length; i++)
        {
            magnitudes[i] = (Single)complex[i].Magnitude;
        }

        return magnitudes;
    }

    /// <summary>
    /// Get the frequency for a given bin index.
    /// </summary>
    public Single GetFrequency(Int32 binIndex, Int32 sampleRate) => binIndex * sampleRate / (Single)this._fftSize;

    /// <summary>
    /// Get frequency band energy.
    /// </summary>
    public Single GetBandEnergy(Single[] spectrum, Int32 sampleRate, Single minFreq, Single maxFreq)
    {
        var minBin = (Int32)(minFreq * this._fftSize / sampleRate);
        var maxBin = (Int32)(maxFreq * this._fftSize / sampleRate);

        minBin = Math.Max(0, Math.Min(minBin, spectrum.Length - 1));
        maxBin = Math.Max(0, Math.Min(maxBin, spectrum.Length - 1));

        if (minBin >= maxBin)
        {
            return 0;
        }

        var count = maxBin - minBin + 1;
        var sum = 0f;
        for (var i = minBin; i <= maxBin; i++)
        {
            sum += spectrum[i] * spectrum[i];
        }

        return (Single)Math.Sqrt(sum / count);
    }

    private static Single[] ApplyHanningWindow(Single[] samples)
    {
        if (samples.Length == 0)
        {
            throw new ArgumentException("Cannot apply window to empty buffer");
        }

        var windowed = new Single[samples.Length];

        if (samples.Length == 1)
        {
            windowed[0] = samples[0];
            return windowed;
        }

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
        if (n <= 1)
        {
            return;
        }

        // Bit-reversal permutation
        for (Int32 i = 1, j = 0; i < n; i++)
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