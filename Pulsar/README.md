# Pulsar

Haptic cursor feedback for Logitech MX Master 4. Feel subtle vibrations when your cursor type changes.

## Features

- **Haptic cursor feedback**: Feel subtle vibrations when cursor changes (arrow → hand, arrow → text field, etc.)
- **Configurable sensitivity**: Low/Medium/High presets or fully custom settings
- **Smart device detection**: Only triggers when using MX Master 4 (not trackpad/other mice)
- **Two monitoring modes**: Polling (simple) vs Event-Driven (efficient) - compare and choose
- **Semantic waveforms**: Different vibration patterns for different cursor types

## Requirements

- Windows 10/11
- Logitech Options+ 1.95 or later
- MX Master 4 mouse
- .NET 8 Runtime

## Installation

1. Download latest release from [Releases](https://github.com/yourusername/mx-haptic-cursor/releases)
2. Extract to `%LOCALAPPDATA%\Logi\LogiPluginService\Plugins\`
3. Restart Logitech Options+
4. Plugin appears in Options+ → Plugins

## Usage

### Default Behavior

Plugin starts with **Medium sensitivity**:
- Haptics for: clickable links, text fields, resize handles, crosshairs
- Throttle: 250ms between haptics
- Mode: Polling

### Changing Sensitivity

In Logitech Options+:
1. Go to device settings
2. Find "Sensitivity Preset" action
3. Assign to a button
4. Click to cycle: Low → Medium → High

**Low**: Only major cursor changes (hand, text)
**Medium**: + resize handles, crosshairs
**High**: All cursor types including custom cursors

### Monitoring Mode Comparison

**Polling Mode** (default):
- ✅ Simple, reliable
- ❌ Constant CPU usage (~1-2%)
- Best for: Desktop PCs with good power

**Event-Driven Mode**:
- ✅ Zero overhead when idle
- ❌ More complex, may miss some cursor changes
- Best for: Laptops, battery life optimization

Toggle via "Monitoring Mode" action in Options+.

### Advanced Customization

Currently requires code changes. Future versions will add UI for:
- Per-cursor-type waveform selection
- Custom throttle values
- Individual cursor type enable/disable

## Cursor Type → Waveform Mapping

| Cursor Change | Waveform | Feel |
|---------------|----------|------|
| Arrow → Hand (link) | Sharp State Change | Clear click feedback |
| Arrow → I-beam (text) | Subtle Collision | Gentle boundary |
| Arrow → Resize handles | Sharp Collision | Hit an edge |
| Arrow → Crosshair | Sharp State Change | Precision mode |
| Arrow → Wait/Busy | Ringing | Ongoing process |
| Arrow → Not allowed | Mad | Strong negative |
| Return to Arrow | Damp State Change | Soft return |

## Troubleshooting

**No haptics felt:**
1. Ensure MX Master 4 is connected and recognized in Options+
2. Click a button on the mouse (activates 5-second activity window)
3. Move cursor over a link or text field
4. Check plugin is enabled in Options+

**Too many haptics:**
- Reduce sensitivity to Low
- Increase throttle window (requires code change currently)

**Draining battery:**
- Switch to Event-Driven monitoring mode
- Reduce sensitivity to Low

**Plugin not loading:**
- Check .NET 8 Runtime is installed
- Check Windows Event Viewer for errors
- Enable debug logging (see DEVELOPMENT.md)

## Development

See [DEVELOPMENT.md](docs/DEVELOPMENT.md) for build instructions and architecture.

## License

MIT License - see LICENSE file

## Credits

Built with [Logitech Actions SDK](https://logitech.github.io/actions-sdk-docs/)
