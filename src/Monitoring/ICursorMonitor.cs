namespace Pulsar.Monitoring;

using Pulsar.Native;

/// <summary>
/// Interface for cursor monitoring implementations
/// </summary>
public interface ICursorMonitor : IDisposable
{
    /// <summary>
    /// Event raised when cursor type changes
    /// </summary>
    event Action<CursorType, CursorType> CursorChanged;

    /// <summary>
    /// Start monitoring cursor changes
    /// </summary>
    void Start();

    /// <summary>
    /// Stop monitoring cursor changes
    /// </summary>
    void Stop();
}