using Xunit;
using Pulsar.Settings;
using Pulsar.Native;

namespace Pulsar.Tests.Settings;

public class HapticSettingsTests
{
    [Fact]
    public void CreateLowPreset_ConfiguresCorrectly()
    {
        // Act
        var settings = HapticSettings.CreatePreset(SensitivityPreset.Low);

        // Assert
        Assert.Equal(SensitivityPreset.Low, settings.Preset);
        Assert.Equal(500, settings.ThrottleMs);
        Assert.True(settings.CursorFilter.ShouldAllow(CursorType.Arrow, CursorType.Hand));
        Assert.True(settings.CursorFilter.ShouldAllow(CursorType.Arrow, CursorType.IBeam));
        Assert.False(settings.CursorFilter.ShouldAllow(CursorType.Arrow, CursorType.Crosshair));
    }

    [Fact]
    public void CreateMediumPreset_ConfiguresCorrectly()
    {
        // Act
        var settings = HapticSettings.CreatePreset(SensitivityPreset.Medium);

        // Assert
        Assert.Equal(SensitivityPreset.Medium, settings.Preset);
        Assert.Equal(250, settings.ThrottleMs);
        Assert.True(settings.CursorFilter.ShouldAllow(CursorType.Arrow, CursorType.Crosshair));
    }

    [Fact]
    public void CreateHighPreset_EnablesAllTransitions()
    {
        // Act
        var settings = HapticSettings.CreatePreset(SensitivityPreset.High);

        // Assert
        Assert.Equal(SensitivityPreset.High, settings.Preset);
        Assert.Equal(100, settings.ThrottleMs);
        Assert.True(settings.CursorFilter.ShouldAllow(CursorType.Arrow, CursorType.Custom));
    }
}
