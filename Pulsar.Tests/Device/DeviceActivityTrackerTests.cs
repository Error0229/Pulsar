using Xunit;
using Pulsar.Device;

namespace Pulsar.Tests.Device;

public class DeviceActivityTrackerTests
{
    [Fact]
    public void IsActive_NoActivity_ReturnsFalse()
    {
        // Arrange
        var tracker = new DeviceActivityTracker(activityWindowMs: 1000);

        // Act
        var result = tracker.IsActive();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsActive_AfterRecordActivity_ReturnsTrue()
    {
        // Arrange
        var tracker = new DeviceActivityTracker(activityWindowMs: 1000);

        // Act
        tracker.RecordActivity();
        var result = tracker.IsActive();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsActive_AfterActivityWindowExpires_ReturnsFalse()
    {
        // Arrange
        var tracker = new DeviceActivityTracker(activityWindowMs: 100);

        // Act
        tracker.RecordActivity();
        Thread.Sleep(150);
        var result = tracker.IsActive();

        // Assert
        Assert.False(result);
    }
}
