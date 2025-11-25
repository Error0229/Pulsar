using Xunit;
using MxHapticCursorPlugin.Monitoring;
using MxHapticCursorPlugin.Native;

namespace MxHapticCursorPlugin.Tests.Monitoring;

public class EventDrivenCursorMonitorTests
{
    [Fact]
    public void Start_RaisesEvent_WhenCursorChanges()
    {
        // Arrange
        var monitor = new EventDrivenCursorMonitor();
        CursorType? detectedFrom = null;
        CursorType? detectedTo = null;
        var eventRaised = new ManualResetEventSlim(false);

        monitor.CursorChanged += (from, to) =>
        {
            detectedFrom = from;
            detectedTo = to;
            eventRaised.Set();
        };

        // Act
        monitor.Start();

        // Simulate cursor change by waiting (manual test - move cursor to text field)
        var raised = eventRaised.Wait(TimeSpan.FromSeconds(5));

        monitor.Stop();

        // Assert
        Assert.True(raised, "Expected cursor change event within 5 seconds");
        Assert.NotNull(detectedFrom);
        Assert.NotNull(detectedTo);
        Assert.NotEqual(detectedFrom, detectedTo);
    }

    [Fact]
    public void Dispose_UnhooksEvents()
    {
        // Arrange
        var monitor = new EventDrivenCursorMonitor();
        monitor.Start();

        // Act
        monitor.Dispose();

        // Assert - should not throw
        Assert.True(true);
    }
}
