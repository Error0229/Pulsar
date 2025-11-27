namespace Pulsar.Haptics;

using System.Collections.Generic;

using Pulsar.Native;

/// <summary>
/// Maps cursor transitions to haptic waveforms
/// </summary>
public class WaveformMapper
{
    private readonly Dictionary<(CursorType, CursorType), WaveformType> _mappings;

    public WaveformMapper() => this._mappings = new Dictionary<(CursorType, CursorType), WaveformType>();

    /// <summary>
    /// Create mapper with default semantic mappings
    /// </summary>
    public static WaveformMapper CreateDefault()
    {
        var mapper = new WaveformMapper();

        // Clickable elements - clear state change
        mapper.SetMapping(CursorType.Arrow, CursorType.Hand, WaveformType.SharpStateChange);

        // Text fields - gentle boundary crossing
        mapper.SetMapping(CursorType.Arrow, CursorType.IBeam, WaveformType.SubtleCollision);

        // Resize handles - hit an edge
        mapper.SetMapping(CursorType.Arrow, CursorType.ResizeHorizontal, WaveformType.SharpCollision);
        mapper.SetMapping(CursorType.Arrow, CursorType.ResizeVertical, WaveformType.SharpCollision);
        mapper.SetMapping(CursorType.Arrow, CursorType.ResizeDiagonalNESW, WaveformType.SharpCollision);
        mapper.SetMapping(CursorType.Arrow, CursorType.ResizeDiagonalNWSE, WaveformType.SharpCollision);

        // Precision mode
        mapper.SetMapping(CursorType.Arrow, CursorType.Crosshair, WaveformType.SharpStateChange);

        // Busy indicators
        mapper.SetMapping(CursorType.Arrow, CursorType.Wait, WaveformType.Ringing);
        mapper.SetMapping(CursorType.Arrow, CursorType.AppStarting, WaveformType.Ringing);

        // Blocked actions
        mapper.SetMapping(CursorType.Arrow, CursorType.NotAllowed, WaveformType.Mad);

        // Return to arrow - soft return
        mapper.SetMapping(CursorType.Hand, CursorType.Arrow, WaveformType.DampStateChange);
        mapper.SetMapping(CursorType.IBeam, CursorType.Arrow, WaveformType.DampStateChange);
        mapper.SetMapping(CursorType.Crosshair, CursorType.Arrow, WaveformType.DampStateChange);
        mapper.SetMapping(CursorType.ResizeHorizontal, CursorType.Arrow, WaveformType.DampStateChange);
        mapper.SetMapping(CursorType.ResizeVertical, CursorType.Arrow, WaveformType.DampStateChange);
        mapper.SetMapping(CursorType.ResizeDiagonalNESW, CursorType.Arrow, WaveformType.DampStateChange);
        mapper.SetMapping(CursorType.ResizeDiagonalNWSE, CursorType.Arrow, WaveformType.DampStateChange);

        // Custom cursors - generic indicator
        mapper.SetMapping(CursorType.Arrow, CursorType.Custom, WaveformType.SubtleCollision);
        mapper.SetMapping(CursorType.Custom, CursorType.Arrow, WaveformType.SubtleCollision);

        return mapper;
    }

    /// <summary>
    /// Set custom waveform for a transition
    /// </summary>
    public void SetMapping(CursorType from, CursorType to, WaveformType waveform) => this._mappings[(from, to)] = waveform;

    /// <summary>
    /// Get waveform for a cursor transition
    /// </summary>
    public WaveformType GetWaveform(CursorType from, CursorType to)
    {
        if (this._mappings.TryGetValue((from, to), out var waveform))
        {
            return waveform;
        }

        // Default fallback
        return WaveformType.SubtleCollision;
    }

    /// <summary>
    /// Convert WaveformType to SDK event name
    /// </summary>
    public static String ToEventName(WaveformType waveform)
    {
        return waveform switch
        {
            WaveformType.SharpCollision => "sharp_collision",
            WaveformType.SubtleCollision => "subtle_collision",
            WaveformType.SharpStateChange => "sharp_state_change",
            WaveformType.DampStateChange => "damp_state_change",
            WaveformType.Ringing => "ringing",
            WaveformType.Knock => "knock",
            WaveformType.Mad => "mad",
            _ => "subtle_collision"
        };
    }
}