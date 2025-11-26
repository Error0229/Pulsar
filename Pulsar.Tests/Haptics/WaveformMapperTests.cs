using Xunit;
using Pulsar.Haptics;
using Pulsar.Native;

namespace Pulsar.Tests.Haptics;

public class WaveformMapperTests
{
    [Fact]
    public void GetWaveform_ArrowToHand_ReturnsSharpStateChange()
    {
        // Arrange
        var mapper = WaveformMapper.CreateDefault();

        // Act
        var waveform = mapper.GetWaveform(CursorType.Arrow, CursorType.Hand);

        // Assert
        Assert.Equal(WaveformType.SharpStateChange, waveform);
    }

    [Fact]
    public void GetWaveform_ArrowToIBeam_ReturnsSubtleCollision()
    {
        // Arrange
        var mapper = WaveformMapper.CreateDefault();

        // Act
        var waveform = mapper.GetWaveform(CursorType.Arrow, CursorType.IBeam);

        // Assert
        Assert.Equal(WaveformType.SubtleCollision, waveform);
    }

    [Fact]
    public void SetCustomMapping_OverridesDefault()
    {
        // Arrange
        var mapper = WaveformMapper.CreateDefault();

        // Act
        mapper.SetMapping(CursorType.Arrow, CursorType.Hand, WaveformType.Mad);
        var waveform = mapper.GetWaveform(CursorType.Arrow, CursorType.Hand);

        // Assert
        Assert.Equal(WaveformType.Mad, waveform);
    }

    [Fact]
    public void GetWaveform_NoMapping_ReturnsSubtleCollision()
    {
        // Arrange
        var mapper = new WaveformMapper();

        // Act
        var waveform = mapper.GetWaveform(CursorType.Custom, CursorType.Arrow);

        // Assert
        Assert.Equal(WaveformType.SubtleCollision, waveform);
    }
}
