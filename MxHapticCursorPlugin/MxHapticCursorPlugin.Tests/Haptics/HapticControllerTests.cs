using Xunit;
using MxHapticCursorPlugin.Haptics;
using MxHapticCursorPlugin.Settings;
using MxHapticCursorPlugin.Native;

namespace MxHapticCursorPlugin.Tests.Haptics;

public class HapticControllerTests
{
    [Fact]
    public void OnCursorChanged_FilteredOut_DoesNotTrigger()
    {
        // Arrange
        var settings = HapticSettings.CreatePreset(SensitivityPreset.Low);
        var triggeredEvents = new List<string>();
        var controller = new HapticController(settings, eventName => triggeredEvents.Add(eventName));

        // Act - Crosshair not enabled in Low preset
        controller.OnCursorChanged(CursorType.Arrow, CursorType.Crosshair);

        // Assert
        Assert.Empty(triggeredEvents);
    }

    [Fact]
    public void OnCursorChanged_AllowedTransition_TriggersHaptic()
    {
        // Arrange
        var settings = HapticSettings.CreatePreset(SensitivityPreset.Low);
        var triggeredEvents = new List<string>();
        var controller = new HapticController(settings, eventName => triggeredEvents.Add(eventName));
        controller.RecordDeviceActivity(); // Mark device as active

        // Act
        controller.OnCursorChanged(CursorType.Arrow, CursorType.Hand);

        // Assert
        Assert.Single(triggeredEvents);
        Assert.Equal("sharp_state_change", triggeredEvents[0]);
    }

    [Fact]
    public void OnCursorChanged_Throttled_DoesNotTrigger()
    {
        // Arrange
        var settings = HapticSettings.CreatePreset(SensitivityPreset.Low);
        var triggeredEvents = new List<string>();
        var controller = new HapticController(settings, eventName => triggeredEvents.Add(eventName));
        controller.RecordDeviceActivity();

        // Act
        controller.OnCursorChanged(CursorType.Arrow, CursorType.Hand);
        controller.OnCursorChanged(CursorType.Hand, CursorType.Arrow); // Immediate second event

        // Assert
        Assert.Single(triggeredEvents); // Only first event triggered
    }

    [Fact]
    public void OnCursorChanged_DeviceInactive_DoesNotTrigger()
    {
        // Arrange
        var settings = HapticSettings.CreatePreset(SensitivityPreset.Low);
        var triggeredEvents = new List<string>();
        var controller = new HapticController(settings, eventName => triggeredEvents.Add(eventName));
        // Don't record device activity

        // Act
        controller.OnCursorChanged(CursorType.Arrow, CursorType.Hand);

        // Assert
        Assert.Empty(triggeredEvents);
    }
}
