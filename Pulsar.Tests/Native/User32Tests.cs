using Xunit;
using Pulsar.Native;

namespace Pulsar.Tests.Native;

public class User32Tests
{
    [Fact]
    public void GetCursorInfo_ReturnsValidCursorInfo()
    {
        // Act & Assert - verify GetCurrentCursor() works without throwing and returns valid flags
        var cursorInfo = User32.GetCurrentCursor();

        // Verify the flags field contains a valid state (either hidden or showing)
        Assert.True(cursorInfo.flags == 0 || cursorInfo.flags == 1,
            $"Cursor flags should be 0 (hidden) or 1 (showing), got {cursorInfo.flags}");
    }

    [Fact]
    public void DetectCursorType_ReturnsValidCursorType()
    {
        // Arrange
        var cursorInfo = User32.GetCurrentCursor();

        // Act
        var cursorType = User32.GetCursorType(cursorInfo.hCursor);

        // Assert - just verify we get a valid enum value, not a specific type
        Assert.True(Enum.IsDefined(typeof(CursorType), cursorType));
    }
}
