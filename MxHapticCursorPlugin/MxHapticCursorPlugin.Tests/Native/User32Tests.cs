using Xunit;
using MxHapticCursorPlugin.Native;

namespace MxHapticCursorPlugin.Tests.Native;

public class User32Tests
{
    [Fact]
    public void GetCursorInfo_ReturnsValidCursorHandle()
    {
        // Act
        var cursorInfo = User32.GetCurrentCursor();

        // Assert
        Assert.NotEqual(IntPtr.Zero, cursorInfo.hCursor);
        Assert.True(cursorInfo.flags == 0 || cursorInfo.flags == 1); // Hidden or showing
    }

    [Fact]
    public void DetectCursorType_Arrow_ReturnsArrow()
    {
        // Arrange - assume test runs with normal arrow cursor
        var cursorInfo = User32.GetCurrentCursor();

        // Act
        var cursorType = User32.GetCursorType(cursorInfo.hCursor);

        // Assert
        Assert.Equal(CursorType.Arrow, cursorType);
    }
}
