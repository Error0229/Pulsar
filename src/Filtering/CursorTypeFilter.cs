namespace Pulsar.Filtering;

using System;
using System.Collections.Generic;

using Pulsar.Native;

/// <summary>
/// Filters cursor transitions based on user preferences
/// </summary>
public class CursorTypeFilter
{
    private readonly HashSet<(CursorType, CursorType)> _enabledTransitions;

    public CursorTypeFilter() => this._enabledTransitions = new HashSet<(CursorType, CursorType)>();

    /// <summary>
    /// Enable haptics for a specific cursor transition
    /// </summary>
    public void EnableTransition(CursorType from, CursorType to) => this._enabledTransitions.Add((from, to));

    /// <summary>
    /// Disable haptics for a specific cursor transition
    /// </summary>
    public void DisableTransition(CursorType from, CursorType to) => this._enabledTransitions.Remove((from, to));

    /// <summary>
    /// Enable all cursor transitions
    /// </summary>
    public void EnableAll()
    {
        var allCursorTypes = Enum.GetValues<CursorType>();
        foreach (var from in allCursorTypes)
        {
            foreach (var to in allCursorTypes)
            {
                if (from != to)
                {
                    this._enabledTransitions.Add((from, to));
                }
            }
        }
    }

    /// <summary>
    /// Disable all cursor transitions
    /// </summary>
    public void DisableAll() => this._enabledTransitions.Clear();

    /// <summary>
    /// Check if transition should trigger haptic
    /// </summary>
    public Boolean ShouldAllow(CursorType from, CursorType to) => this._enabledTransitions.Contains((from, to));
}