# Architecture

## Overview

```
Windows Cursor → Monitor → Throttle → Type Filter → Waveform Mapper → Haptic Event
                    ↑                                                       ↓
                    └──────────── Activity Tracker ←─── Device Input ──────┘
```

## Components

### 1. Native Layer (`src/Native/`)

**User32.cs**: P/Invoke declarations for Windows cursor APIs
- `GetCursorInfo()`: Get current cursor handle and position
- `SetWinEventHook()`: Register for system events (event-driven mode)
- System cursor handle caching

**CursorType.cs**: Enum of detectable cursor types

### 2. Monitoring Layer (`src/Monitoring/`)

**ICursorMonitor**: Common interface for both monitoring strategies

**PollingCursorMonitor**: Timer-based implementation
- Checks cursor every 50ms
- Simple, reliable
- ~1-2% CPU usage

**EventDrivenCursorMonitor**: Event hook implementation
- Hooks `EVENT_SYSTEM_FOREGROUND` + 200ms fallback polling
- Complex, efficient
- Near-zero CPU when idle

### 3. Filtering Layer (`src/Filtering/`)

**ThrottleFilter**: Debounce timer to prevent haptic spam
- Configurable window (100-500ms)
- Uses Stopwatch for precision timing

**CursorTypeFilter**: Whitelist of allowed transitions
- HashSet of (from, to) tuples
- Preset configurations

### 4. Haptics Layer (`src/Haptics/`)

**WaveformMapper**: Cursor transition → waveform mapping
- Semantic defaults (collision, state change, etc.)
- User-overridable mappings

**HapticController**: Coordinates entire pipeline
- Receives cursor change events
- Applies filters
- Checks device activity
- Triggers haptic events

**WaveformType**: Enum of SDK waveforms

### 5. Device Layer (`src/Device/`)

**DeviceActivityTracker**: Detects MX Master 4 usage
- Tracks last activity timestamp
- 5-second activity window (configurable)
- Prevents haptics when using trackpad/other mice

### 6. Settings Layer (`src/Settings/`)

**HapticSettings**: Configuration storage
- Preset levels (Low/Medium/High/Custom)
- Monitoring mode selection
- Filter and mapper instances

**SensitivityPreset**: Enum of preset levels

### 7. Plugin Integration

**MxHapticCursorPlugin**: Main plugin class
- Registers haptic events with SDK
- Instantiates components
- Handles settings persistence
- Wires up event handlers

## Data Flow

1. **Cursor Changes**: Windows cursor changes (user hovers over link)

2. **Detection**:
   - Polling: Timer tick → `GetCursorInfo()` → compare handle
   - Event-Driven: Windows event → `GetCursorInfo()`

3. **Event Raised**: `CursorChanged(from, to)` event fired

4. **Controller Receives**: `HapticController.OnCursorChanged()`

5. **Filtering Pipeline**:
   - Check enabled flag
   - Check device activity (MX Master 4 used recently?)
   - Check cursor type filter (is this transition allowed?)
   - Check throttle (too soon since last haptic?)

6. **Waveform Selection**: `WaveformMapper.GetWaveform(from, to)`

7. **Haptic Trigger**: `PluginEvents.RaiseEvent(eventName)`

8. **SDK → Hardware**: Logitech Options+ → MX Master 4 haptic motor

## Threading Model

- **Monitor timer/events**: Run on threadpool
- **Filter checks**: Synchronous, non-blocking
- **Haptic triggers**: Synchronous SDK call
- **No locking needed**: Single-threaded event flow

## Performance Characteristics

**Polling Mode:**
- CPU: ~1-2% constant
- Memory: ~5MB
- Latency: 50ms average, 100ms worst-case

**Event-Driven Mode:**
- CPU: <0.1% idle, ~0.5% active
- Memory: ~5MB
- Latency: 10-200ms (varies by Windows event delivery)

**Throttling Impact:**
- 500ms: Max 2 haptics/second
- 250ms: Max 4 haptics/second
- 100ms: Max 10 haptics/second

## Extension Points

**Custom Cursors**: Add to `User32._systemCursors` dictionary

**New Waveforms**: Add to `WaveformType` enum + `eventMapping.yaml`

**Advanced Filters**: Implement new filters in `Filtering/` namespace

**Settings UI**: Add Loupedeck actions in `Actions/` namespace
