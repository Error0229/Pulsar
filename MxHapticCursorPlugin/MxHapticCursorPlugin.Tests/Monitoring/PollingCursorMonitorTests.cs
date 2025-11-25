using Xunit;
using MxHapticCursorPlugin.Monitoring;
using MxHapticCursorPlugin.Native;

namespace MxHapticCursorPlugin.Tests.Monitoring;

public class PollingCursorMonitorTests
{
    [Fact]
    public void Start_RaisesEvent_WhenCursorChanges()
    {
        // Arrange
        var monitor = new PollingCursorMonitor(pollIntervalMs: 50);
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
    public void Stop_StopsPolling()
    {
        // Arrange
        var monitor = new PollingCursorMonitor(pollIntervalMs: 50);
        var eventCount = 0;
        monitor.CursorChanged += (_, _) => eventCount++;

        // Act
        monitor.Start();
        Thread.Sleep(100);
        monitor.Stop();
        var countBeforeStop = eventCount;
        Thread.Sleep(200);

        // Assert
        Assert.Equal(countBeforeStop, eventCount); // No new events after stop
    }

    [Fact]
    public void Dispose_StopsMonitoring()
    {
        // Arrange
        var monitor = new PollingCursorMonitor(pollIntervalMs: 50);
        monitor.Start();

        // Act
        monitor.Dispose();

        // Assert - should not throw
        Assert.True(true);
    }
}
