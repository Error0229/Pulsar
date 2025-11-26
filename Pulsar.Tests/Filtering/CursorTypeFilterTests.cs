using Xunit;
using Pulsar.Filtering;
using Pulsar.Native;

namespace Pulsar.Tests.Filtering;

public class CursorTypeFilterTests
{
    [Fact]
    public void ShouldAllow_EnabledTransition_ReturnsTrue()
    {
        // Arrange
        var filter = new CursorTypeFilter();
        filter.EnableTransition(CursorType.Arrow, CursorType.Hand);

        // Act
        var result = filter.ShouldAllow(CursorType.Arrow, CursorType.Hand);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldAllow_DisabledTransition_ReturnsFalse()
    {
        // Arrange
        var filter = new CursorTypeFilter();
        // Don't enable any transitions

        // Act
        var result = filter.ShouldAllow(CursorType.Arrow, CursorType.Hand);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void EnableAll_AllowsAllTransitions()
    {
        // Arrange
        var filter = new CursorTypeFilter();
        filter.EnableAll();

        // Act & Assert
        Assert.True(filter.ShouldAllow(CursorType.Arrow, CursorType.Hand));
        Assert.True(filter.ShouldAllow(CursorType.Hand, CursorType.IBeam));
        Assert.True(filter.ShouldAllow(CursorType.IBeam, CursorType.Arrow));
    }

    [Fact]
    public void DisableTransition_PreviouslyEnabled_ReturnsFalse()
    {
        // Arrange
        var filter = new CursorTypeFilter();
        filter.EnableTransition(CursorType.Arrow, CursorType.Hand);

        // Act
        filter.DisableTransition(CursorType.Arrow, CursorType.Hand);
        var result = filter.ShouldAllow(CursorType.Arrow, CursorType.Hand);

        // Assert
        Assert.False(result);
    }
}
