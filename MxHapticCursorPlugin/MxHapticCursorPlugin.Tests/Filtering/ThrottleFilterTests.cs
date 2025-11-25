using Xunit;
using MxHapticCursorPlugin.Filtering;
using MxHapticCursorPlugin.Native;

namespace MxHapticCursorPlugin.Tests.Filtering;

public class ThrottleFilterTests
{
    [Fact]
    public void ShouldAllow_FirstEvent_ReturnsTrue()
    {
        // Arrange
        var filter = new ThrottleFilter(throttleMs: 500);

        // Act
        var result = filter.ShouldAllow(CursorType.Arrow, CursorType.Hand);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldAllow_WithinThrottleWindow_ReturnsFalse()
    {
        // Arrange
        var filter = new ThrottleFilter(throttleMs: 500);
        filter.ShouldAllow(CursorType.Arrow, CursorType.Hand); // First event

        // Act
        var result = filter.ShouldAllow(CursorType.Hand, CursorType.IBeam); // Immediate second

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldAllow_AfterThrottleWindow_ReturnsTrue()
    {
        // Arrange
        var filter = new ThrottleFilter(throttleMs: 100);
        filter.ShouldAllow(CursorType.Arrow, CursorType.Hand); // First event

        // Act
        Thread.Sleep(150); // Wait past throttle window
        var result = filter.ShouldAllow(CursorType.Hand, CursorType.IBeam);

        // Assert
        Assert.True(result);
    }
}
