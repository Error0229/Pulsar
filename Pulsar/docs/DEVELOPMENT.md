# Development Guide

## Setup

### Prerequisites

1. **Install .NET 8 SDK**: https://dotnet.microsoft.com/download/dotnet/8.0
2. **Install Logitech Options+**: https://www.logitech.com/en-us/software/logi-options-plus.html
3. **Install LogiPluginTool**:
   ```bash
   dotnet tool install --global LogiPluginTool
   ```
4. **MX Master 4 mouse** (for testing)

### Clone and Build

```bash
git clone https://github.com/yourusername/pulsar.git
cd pulsar/Pulsar/src
dotnet build
```

### Development Workflow

**Hot Reload Mode:**
```bash
cd Pulsar/src
dotnet watch build
```

Changes are automatically reloaded in Logi Options+ (may require action reassignment).

## Testing

### Unit Tests

```bash
cd Pulsar.Tests
dotnet test
```

**Manual Cursor Tests** (require interaction):
- `PollingCursorMonitorTests.Start_RaisesEvent_WhenCursorChanges`
- `EventDrivenCursorMonitorTests.Start_RaisesEvent_WhenCursorChanges`

To run manually:
1. Start test
2. Move cursor to text field within 5 seconds
3. Test should pass

### Integration Testing

**Test Haptic Feedback:**

1. Run plugin in watch mode
2. Enable logging (see below)
3. Click a button on MX Master 4
4. Move cursor over link → should feel sharp vibration
5. Move cursor over text field → should feel gentle vibration
6. Move cursor back to normal → should feel soft return

**Test Monitoring Modes:**

Polling:
```bash
# Watch CPU usage in Task Manager
# Should see ~1-2% constant usage
```

Event-Driven:
```bash
# Watch CPU usage in Task Manager
# Should see <0.1% when cursor not moving
```

**Test Throttling:**

Low sensitivity (500ms throttle):
- Move cursor rapidly over multiple links
- Should only feel haptic every 500ms

### Debugging

**Enable Debug Logging:**

Modify `src/Pulsar.cs`:

```csharp
private void TriggerHapticEvent(string eventName)
{
    this.Log.Info($"Triggering haptic: {eventName}");
    this.PluginEvents.RaiseEvent(eventName);
}
```

**View Logs:**

Windows: `%LOCALAPPDATA%\Logi\LogiPluginService\Logs\`

**Attach Debugger:**

Visual Studio:
1. Debug → Attach to Process
2. Find `LogiPluginService.exe`
3. Set breakpoints in plugin code

## Project Structure

```
Pulsar/
├── src/
│   ├── Actions/              # Loupedeck UI actions
│   │   ├── EnableToggleCommand.cs
│   │   ├── MonitoringModeCommand.cs
│   │   └── PresetSelectorCommand.cs
│   ├── Device/              # Activity tracking
│   │   └── DeviceActivityTracker.cs
│   ├── Filtering/           # Throttle + type filters
│   │   ├── CursorTypeFilter.cs
│   │   └── ThrottleFilter.cs
│   ├── Haptics/             # Waveform mapping + controller
│   │   ├── HapticController.cs
│   │   ├── WaveformMapper.cs
│   │   └── WaveformType.cs
│   ├── Helpers/             # Utilities
│   │   ├── PluginLog.cs
│   │   └── PluginResources.cs
│   ├── Monitoring/          # Cursor change detection
│   │   ├── EventDrivenCursorMonitor.cs
│   │   ├── ICursorMonitor.cs
│   │   └── PollingCursorMonitor.cs
│   ├── Native/              # Windows API P/Invoke
│   │   ├── CursorType.cs
│   │   └── User32.cs
│   ├── Settings/            # Configuration
│   │   ├── HapticSettings.cs
│   │   ├── MonitoringMode.cs
│   │   └── SensitivityPreset.cs
│   ├── package/
│   │   ├── metadata/
│   │   │   ├── Icon256x256.png
│   │   │   └── LoupedeckPackage.yaml
│   │   └── events/
│   │       ├── DefaultEventSource.yaml
│   │       └── extra/
│   │           └── eventMapping.yaml
│   ├── PulsarApplication.cs
│   ├── Pulsar.cs
│   └── Pulsar.csproj
├── Pulsar.Tests/
│   ├── Device/
│   ├── Filtering/
│   ├── Haptics/
│   ├── Monitoring/
│   ├── Native/
│   └── Settings/
├── docs/
│   ├── ARCHITECTURE.md
│   └── DEVELOPMENT.md
└── README.md
```

## Making Changes

### Adding a New Cursor Type

1. **Add to CursorType enum**:
   ```csharp
   // src/Native/CursorType.cs
   public enum CursorType
   {
       // ...
       NewType,
   }
   ```

2. **Add to User32 cursor mapping**:
   ```csharp
   // src/Native/User32.cs
   private const int IDC_NEWTYPE = 32XXX;

   _systemCursors = new Dictionary<IntPtr, CursorType>
   {
       // ...
       { LoadCursor(IntPtr.Zero, IDC_NEWTYPE), CursorType.NewType },
   };
   ```

3. **Add waveform mapping**:
   ```csharp
   // src/Haptics/WaveformMapper.cs
   mapper.SetMapping(CursorType.Arrow, CursorType.NewType, WaveformType.SharpCollision);
   ```

4. **Update presets** if needed in `HapticSettings.CreatePreset()`

### Adding a New Waveform

1. **Add to WaveformType enum**:
   ```csharp
   // src/Haptics/WaveformType.cs
   public enum WaveformType
   {
       // ...
       NewWaveform,
   }
   ```

2. **Update ToEventName**:
   ```csharp
   // src/Haptics/WaveformMapper.cs
   public static string ToEventName(WaveformType waveform)
   {
       return waveform switch
       {
           // ...
           WaveformType.NewWaveform => "new_waveform",
       };
   }
   ```

3. **Register event in plugin**:
   ```csharp
   // src/Pulsar.cs
   this.PluginEvents.AddEvent("new_waveform", "New Waveform", "Description");
   ```

4. **Add to event mapping**:
   ```yaml
   # src/package/events/DefaultEventSource.yaml
   - name: new_waveform
     displayName: New Waveform
     description: Description
   ```

   ```yaml
   # src/package/events/extra/eventMapping.yaml
   haptics:
     new_waveform:
       DEFAULT: subtle_collision  # Fallback for non-haptic devices
       MX Master 4: new_waveform
   ```

### Changing Default Settings

Modify `HapticSettings.CreatePreset()`:

```csharp
case SensitivityPreset.Medium:
    settings.ThrottleMs = 200; // Was 250
    // Enable additional transitions
    settings.CursorFilter.EnableTransition(CursorType.Arrow, CursorType.Wait);
    break;
```

## Building for Release

```bash
cd src
dotnet build -c Release
```

Output: `bin/Release/net8.0/Pulsar.dll`

Package for distribution:
1. Copy `bin/Release/net8.0/` contents
2. Include `src/package/` directory
3. Zip as `Pulsar-v{version}.zip`

## Common Issues

**"Plugin failed to load"**:
- Check .NET 8 Runtime installed
- Check DLL not blocked (Right-click → Properties → Unblock)
- Check Windows Event Viewer for exceptions

**"No haptics triggered"**:
- Add logging to `TriggerHapticEvent()`
- Verify events registered in `Load()`
- Check `eventMapping.yaml` syntax

**"Cursor changes not detected"**:
- Test both monitoring modes
- Add logging to `OnCursorChanged()`
- Verify P/Invoke working: `User32.GetCurrentCursor()`

**"High CPU usage in Event-Driven mode"**:
- Check fallback polling interval (should be 200ms)
- Verify hook properly unhooked in `Stop()`

## Contributing

1. Fork repository
2. Create feature branch
3. Write tests for new functionality
4. Update documentation
5. Submit pull request

## References

- [Logitech Actions SDK Docs](https://logitech.github.io/actions-sdk-docs/)
- [Windows Cursor API](https://docs.microsoft.com/en-us/windows/win32/menurc/cursors)
- [SetWinEventHook Documentation](https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwineventhook)
