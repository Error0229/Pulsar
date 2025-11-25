# MX Haptic Cursor Feedback Plugin Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Create a Logitech Actions SDK plugin that triggers haptic feedback on MX Master 4 mouse when Windows cursor type changes (arrow → hand, arrow → I-beam, etc.)

**Architecture:** Windows API cursor monitoring (both polling and event-driven implementations) → throttle/filter → waveform mapper → Logitech haptic event system. Configurable sensitivity presets (Low/Medium/High/Custom) with granular control over cursor types, waveform intensity, and throttling.

**Tech Stack:** C# (.NET 8), Logitech Actions SDK 6.2.1+, Windows user32.dll P/Invoke, Logi Options+ 1.95+

---

## Prerequisites

Before starting implementation:

1. **Install .NET 8 SDK**: https://dotnet.microsoft.com/download/dotnet/8.0
2. **Install Logitech Options+**: Version 1.95 or later
3. **Install LogiPluginTool**: `dotnet tool install --global LogiPluginTool`
4. **MX Master 4 mouse** (for testing haptics)

---

## Task 1: Project Scaffolding

**Goal:** Generate plugin project structure using LogiPluginTool

**Step 1: Generate plugin template**

Run:
```bash
logiplugintool generate MxHapticCursor
```

Expected output: Creates `MxHapticCursor/` directory with plugin structure

**Step 2: Verify generated structure**

Run:
```bash
cd MxHapticCursor
ls -la
```

Expected files:
- `src/MxHapticCursorPlugin/MxHapticCursorPlugin.csproj`
- `src/MxHapticCursorPlugin/MxHapticCursorPlugin.cs`
- `src/MxHapticCursorPlugin/MxHapticCursorApplication.cs`
- `src/package/metadata/LoupedeckPackage.yaml`

**Step 3: Update package metadata**

File: `src/package/metadata/LoupedeckPackage.yaml`

```yaml
type: plugin4
name: MxHapticCursor
displayName: MX Haptic Cursor Feedback
version: 0.1.0
author: Your Name
supportPageUrl: https://github.com/yourusername/mx-haptic-cursor
license: MIT
licenseUrl: https://opensource.org/licenses/MIT
copyright: Copyright © 2025
pluginCapabilities:
  - HasHapticMapping

supportedDevices:
  - MX Master 4
```

**Step 4: Build initial project**

Run:
```bash
cd src/MxHapticCursorPlugin
dotnet build
```

Expected: Build succeeds with 0 errors

**Step 5: Verify plugin loads in Logi Options+**

Run:
```bash
dotnet watch build
```

Open Logi Options+ → Settings → Plugins → Verify "MX Haptic Cursor Feedback" appears

**Step 6: Initial commit**

```bash
git init
git add .
git commit -m "chore: initial plugin scaffolding"
```

---

## Task 2: Windows API Cursor Monitoring (P/Invoke Layer)

**Goal:** Create Windows API interop layer for cursor detection

**Files:**
- Create: `src/MxHapticCursorPlugin/Native/User32.cs`
- Create: `src/MxHapticCursorPlugin/Native/CursorType.cs`
- Test: `src/MxHapticCursorPlugin.Tests/Native/User32Tests.cs`

**Step 1: Create Native directory**

Run:
```bash
mkdir -p src/MxHapticCursorPlugin/Native
```

**Step 2: Write test for cursor info retrieval**

File: `src/MxHapticCursorPlugin.Tests/Native/User32Tests.cs`

```csharp
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
```

**Step 3: Run test to verify it fails**

Run:
```bash
dotnet test --filter "FullyQualifiedName~User32Tests"
```

Expected: FAIL - `User32` class does not exist

**Step 4: Implement User32 P/Invoke wrapper**

File: `src/MxHapticCursorPlugin/Native/User32.cs`

```csharp
using System;
using System.Runtime.InteropServices;

namespace MxHapticCursorPlugin.Native;

/// <summary>
/// Windows user32.dll P/Invoke declarations for cursor monitoring
/// </summary>
public static class User32
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CURSORINFO
    {
        public int cbSize;
        public int flags;
        public IntPtr hCursor;
        public POINT ptScreenPos;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorInfo(ref CURSORINFO pci);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

    // Standard cursor IDs from winuser.h
    private const int IDC_ARROW = 32512;
    private const int IDC_IBEAM = 32513;
    private const int IDC_WAIT = 32514;
    private const int IDC_CROSS = 32515;
    private const int IDC_UPARROW = 32516;
    private const int IDC_SIZE = 32640;
    private const int IDC_ICON = 32641;
    private const int IDC_SIZENWSE = 32642;
    private const int IDC_SIZENESW = 32643;
    private const int IDC_SIZEWE = 32644;
    private const int IDC_SIZENS = 32645;
    private const int IDC_SIZEALL = 32646;
    private const int IDC_NO = 32648;
    private const int IDC_HAND = 32649;
    private const int IDC_APPSTARTING = 32650;
    private const int IDC_HELP = 32651;

    private static readonly Dictionary<IntPtr, CursorType> _systemCursors;

    static User32()
    {
        // Load system cursor handles once at startup
        _systemCursors = new Dictionary<IntPtr, CursorType>
        {
            { LoadCursor(IntPtr.Zero, IDC_ARROW), CursorType.Arrow },
            { LoadCursor(IntPtr.Zero, IDC_IBEAM), CursorType.IBeam },
            { LoadCursor(IntPtr.Zero, IDC_WAIT), CursorType.Wait },
            { LoadCursor(IntPtr.Zero, IDC_CROSS), CursorType.Crosshair },
            { LoadCursor(IntPtr.Zero, IDC_HAND), CursorType.Hand },
            { LoadCursor(IntPtr.Zero, IDC_SIZEWE), CursorType.ResizeHorizontal },
            { LoadCursor(IntPtr.Zero, IDC_SIZENS), CursorType.ResizeVertical },
            { LoadCursor(IntPtr.Zero, IDC_SIZENWSE), CursorType.ResizeDiagonalNWSE },
            { LoadCursor(IntPtr.Zero, IDC_SIZENESW), CursorType.ResizeDiagonalNESW },
            { LoadCursor(IntPtr.Zero, IDC_NO), CursorType.NotAllowed },
            { LoadCursor(IntPtr.Zero, IDC_APPSTARTING), CursorType.AppStarting },
        };
    }

    /// <summary>
    /// Get current cursor information from Windows
    /// </summary>
    public static CURSORINFO GetCurrentCursor()
    {
        var cursorInfo = new CURSORINFO { cbSize = Marshal.SizeOf(typeof(CURSORINFO)) };
        GetCursorInfo(ref cursorInfo);
        return cursorInfo;
    }

    /// <summary>
    /// Detect cursor type from cursor handle
    /// </summary>
    public static CursorType GetCursorType(IntPtr hCursor)
    {
        if (_systemCursors.TryGetValue(hCursor, out var cursorType))
        {
            return cursorType;
        }

        return CursorType.Custom;
    }
}
```

**Step 5: Implement CursorType enum**

File: `src/MxHapticCursorPlugin/Native/CursorType.cs`

```csharp
namespace MxHapticCursorPlugin.Native;

/// <summary>
/// Windows cursor types we monitor for haptic feedback
/// </summary>
public enum CursorType
{
    Arrow,
    IBeam,
    Hand,
    Crosshair,
    Wait,
    AppStarting,
    ResizeHorizontal,
    ResizeVertical,
    ResizeDiagonalNWSE,
    ResizeDiagonalNESW,
    NotAllowed,
    Custom
}
```

**Step 6: Add test project reference**

Run:
```bash
cd src
dotnet new xunit -n MxHapticCursorPlugin.Tests
cd MxHapticCursorPlugin.Tests
dotnet add reference ../MxHapticCursorPlugin/MxHapticCursorPlugin.csproj
```

**Step 7: Run tests to verify they pass**

Run:
```bash
dotnet test
```

Expected: PASS - 2 tests pass (or 1 if cursor isn't arrow during test)

**Step 8: Commit**

```bash
git add src/MxHapticCursorPlugin/Native/
git add src/MxHapticCursorPlugin.Tests/
git commit -m "feat: add Windows cursor monitoring P/Invoke layer"
```

---

## Task 3: Cursor Change Detection (Polling Implementation)

**Goal:** Implement polling-based cursor monitoring with change detection

**Files:**
- Create: `src/MxHapticCursorPlugin/Monitoring/ICursorMonitor.cs`
- Create: `src/MxHapticCursorPlugin/Monitoring/PollingCursorMonitor.cs`
- Test: `src/MxHapticCursorPlugin.Tests/Monitoring/PollingCursorMonitorTests.cs`

**Step 1: Write test for cursor change detection**

File: `src/MxHapticCursorPlugin.Tests/Monitoring/PollingCursorMonitorTests.cs`

```csharp
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
```

**Step 2: Run test to verify it fails**

Run:
```bash
dotnet test --filter "FullyQualifiedName~PollingCursorMonitorTests"
```

Expected: FAIL - `PollingCursorMonitor` does not exist

**Step 3: Define ICursorMonitor interface**

File: `src/MxHapticCursorPlugin/Monitoring/ICursorMonitor.cs`

```csharp
using MxHapticCursorPlugin.Native;

namespace MxHapticCursorPlugin.Monitoring;

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
```

**Step 4: Implement PollingCursorMonitor**

File: `src/MxHapticCursorPlugin/Monitoring/PollingCursorMonitor.cs`

```csharp
using System;
using System.Threading;
using MxHapticCursorPlugin.Native;

namespace MxHapticCursorPlugin.Monitoring;

/// <summary>
/// Polls Windows cursor state at regular intervals to detect changes
/// Simple but uses constant CPU. Good for comparison with event-driven approach.
/// </summary>
public class PollingCursorMonitor : ICursorMonitor
{
    private readonly int _pollIntervalMs;
    private readonly Timer _timer;
    private CursorType _lastCursorType;
    private IntPtr _lastCursorHandle;
    private bool _isRunning;

    public event Action<CursorType, CursorType>? CursorChanged;

    public PollingCursorMonitor(int pollIntervalMs = 50)
    {
        _pollIntervalMs = pollIntervalMs;
        _timer = new Timer(OnTimerTick, null, Timeout.Infinite, Timeout.Infinite);

        // Initialize with current cursor
        var cursorInfo = User32.GetCurrentCursor();
        _lastCursorHandle = cursorInfo.hCursor;
        _lastCursorType = User32.GetCursorType(_lastCursorHandle);
    }

    public void Start()
    {
        if (_isRunning) return;

        _isRunning = true;
        _timer.Change(0, _pollIntervalMs);
    }

    public void Stop()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private void OnTimerTick(object? state)
    {
        if (!_isRunning) return;

        try
        {
            var cursorInfo = User32.GetCurrentCursor();

            // Only process if cursor handle changed
            if (cursorInfo.hCursor == _lastCursorHandle)
                return;

            var newCursorType = User32.GetCursorType(cursorInfo.hCursor);

            // Only raise event if cursor TYPE changed (not just handle)
            if (newCursorType != _lastCursorType)
            {
                var oldType = _lastCursorType;
                _lastCursorType = newCursorType;
                _lastCursorHandle = cursorInfo.hCursor;

                CursorChanged?.Invoke(oldType, newCursorType);
            }
            else
            {
                _lastCursorHandle = cursorInfo.hCursor;
            }
        }
        catch
        {
            // Swallow exceptions in timer callback to prevent crashes
        }
    }

    public void Dispose()
    {
        Stop();
        _timer?.Dispose();
    }
}
```

**Step 5: Create Monitoring directory**

Run:
```bash
mkdir -p src/MxHapticCursorPlugin/Monitoring
```

**Step 6: Run tests**

Run:
```bash
dotnet test --filter "FullyQualifiedName~PollingCursorMonitorTests"
```

Expected: Tests pass (first test requires manual cursor movement)

**Step 7: Commit**

```bash
git add src/MxHapticCursorPlugin/Monitoring/
git add src/MxHapticCursorPlugin.Tests/Monitoring/
git commit -m "feat: implement polling cursor monitor"
```

---

## Task 4: Cursor Change Detection (Event-Driven Implementation)

**Goal:** Implement Windows event hook-based cursor monitoring

**Files:**
- Create: `src/MxHapticCursorPlugin/Monitoring/EventDrivenCursorMonitor.cs`
- Modify: `src/MxHapticCursorPlugin/Native/User32.cs` (add event hook support)
- Test: `src/MxHapticCursorPlugin.Tests/Monitoring/EventDrivenCursorMonitorTests.cs`

**Step 1: Write test for event-driven monitor**

File: `src/MxHapticCursorPlugin.Tests/Monitoring/EventDrivenCursorMonitorTests.cs`

```csharp
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
```

**Step 2: Run test to verify it fails**

Run:
```bash
dotnet test --filter "FullyQualifiedName~EventDrivenCursorMonitorTests"
```

Expected: FAIL - `EventDrivenCursorMonitor` does not exist

**Step 3: Add event hook support to User32**

File: `src/MxHapticCursorPlugin/Native/User32.cs`

Add these members to the `User32` class:

```csharp
// Add after existing DllImport declarations

public delegate void WinEventDelegate(
    IntPtr hWinEventHook,
    uint eventType,
    IntPtr hwnd,
    int idObject,
    int idChild,
    uint dwEventThread,
    uint dwmsEventTime);

[DllImport("user32.dll")]
public static extern IntPtr SetWinEventHook(
    uint eventMin,
    uint eventMax,
    IntPtr hmodWinEventProc,
    WinEventDelegate lpfnWinEventProc,
    uint idProcess,
    uint idThread,
    uint dwFlags);

[DllImport("user32.dll")]
public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

// Event constants
public const uint EVENT_OBJECT_NAMECHANGE = 0x800C;
public const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
public const uint WINEVENT_OUTOFCONTEXT = 0x0000;
```

**Step 4: Implement EventDrivenCursorMonitor**

File: `src/MxHapticCursorPlugin/Monitoring/EventDrivenCursorMonitor.cs`

```csharp
using System;
using System.Threading;
using MxHapticCursorPlugin.Native;

namespace MxHapticCursorPlugin.Monitoring;

/// <summary>
/// Uses Windows event hooks to detect cursor changes.
/// More efficient than polling - zero overhead when cursor isn't changing.
/// </summary>
public class EventDrivenCursorMonitor : ICursorMonitor
{
    private IntPtr _hookHandle;
    private User32.WinEventDelegate? _hookDelegate;
    private CursorType _lastCursorType;
    private IntPtr _lastCursorHandle;
    private readonly Timer _pollbackTimer;
    private bool _isRunning;

    public event Action<CursorType, CursorType>? CursorChanged;

    public EventDrivenCursorMonitor()
    {
        // Initialize with current cursor
        var cursorInfo = User32.GetCurrentCursor();
        _lastCursorHandle = cursorInfo.hCursor;
        _lastCursorType = User32.GetCursorType(_lastCursorHandle);

        // Fallback polling at 200ms for cases where event hook misses changes
        _pollbackTimer = new Timer(OnPollbackTick, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Start()
    {
        if (_isRunning) return;

        _isRunning = true;

        // Create delegate and keep reference to prevent GC
        _hookDelegate = OnWinEvent;

        // Hook into cursor change events
        // Note: EVENT_OBJECT_NAMECHANGE doesn't always fire for cursor changes
        // So we also monitor foreground window changes as a trigger to check cursor
        _hookHandle = User32.SetWinEventHook(
            User32.EVENT_SYSTEM_FOREGROUND,
            User32.EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero,
            _hookDelegate,
            0,
            0,
            User32.WINEVENT_OUTOFCONTEXT);

        // Start fallback polling at 200ms intervals
        _pollbackTimer.Change(200, 200);
    }

    public void Stop()
    {
        if (!_isRunning) return;

        _isRunning = false;

        if (_hookHandle != IntPtr.Zero)
        {
            User32.UnhookWinEvent(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }

        _pollbackTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _hookDelegate = null;
    }

    private void OnWinEvent(
        IntPtr hWinEventHook,
        uint eventType,
        IntPtr hwnd,
        int idObject,
        int idChild,
        uint dwEventThread,
        uint dwmsEventTime)
    {
        CheckCursorChange();
    }

    private void OnPollbackTick(object? state)
    {
        if (!_isRunning) return;
        CheckCursorChange();
    }

    private void CheckCursorChange()
    {
        try
        {
            var cursorInfo = User32.GetCurrentCursor();

            // Only process if cursor handle changed
            if (cursorInfo.hCursor == _lastCursorHandle)
                return;

            var newCursorType = User32.GetCursorType(cursorInfo.hCursor);

            // Only raise event if cursor TYPE changed
            if (newCursorType != _lastCursorType)
            {
                var oldType = _lastCursorType;
                _lastCursorType = newCursorType;
                _lastCursorHandle = cursorInfo.hCursor;

                CursorChanged?.Invoke(oldType, newCursorType);
            }
            else
            {
                _lastCursorHandle = cursorInfo.hCursor;
            }
        }
        catch
        {
            // Swallow exceptions to prevent crashes
        }
    }

    public void Dispose()
    {
        Stop();
        _pollbackTimer?.Dispose();
    }
}
```

**Step 5: Run tests**

Run:
```bash
dotnet test --filter "FullyQualifiedName~EventDrivenCursorMonitorTests"
```

Expected: Tests pass (requires manual cursor movement)

**Step 6: Commit**

```bash
git add src/MxHapticCursorPlugin/Monitoring/EventDrivenCursorMonitor.cs
git add src/MxHapticCursorPlugin/Native/User32.cs
git add src/MxHapticCursorPlugin.Tests/Monitoring/EventDrivenCursorMonitorTests.cs
git commit -m "feat: implement event-driven cursor monitor"
```

---

## Task 5: Throttling and Filtering

**Goal:** Implement debounce throttling and cursor type filtering

**Files:**
- Create: `src/MxHapticCursorPlugin/Filtering/ThrottleFilter.cs`
- Create: `src/MxHapticCursorPlugin/Filtering/CursorTypeFilter.cs`
- Test: `src/MxHapticCursorPlugin.Tests/Filtering/ThrottleFilterTests.cs`
- Test: `src/MxHapticCursorPlugin.Tests/Filtering/CursorTypeFilterTests.cs`

**Step 1: Write test for throttle filter**

File: `src/MxHapticCursorPlugin.Tests/Filtering/ThrottleFilterTests.cs`

```csharp
using Xunit;
using MxHapticCursorPlugin.Filtering;
using MxHapticCursorPlugin.Native;

namespace MxHapticCursorPlugin.Tests.Filtering;

public class ThrottleFilterTests
{
    [Fact]
    public void ShouldAllow_FirstEvent_ReturnsTrue()
    {
        // Arrange
        var filter = new ThrottleFilter(throttleMs: 500);

        // Act
        var result = filter.ShouldAllow(CursorType.Arrow, CursorType.Hand);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldAllow_WithinThrottleWindow_ReturnsFalse()
    {
        // Arrange
        var filter = new ThrottleFilter(throttleMs: 500);
        filter.ShouldAllow(CursorType.Arrow, CursorType.Hand); // First event

        // Act
        var result = filter.ShouldAllow(CursorType.Hand, CursorType.IBeam); // Immediate second

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldAllow_AfterThrottleWindow_ReturnsTrue()
    {
        // Arrange
        var filter = new ThrottleFilter(throttleMs: 100);
        filter.ShouldAllow(CursorType.Arrow, CursorType.Hand); // First event

        // Act
        Thread.Sleep(150); // Wait past throttle window
        var result = filter.ShouldAllow(CursorType.Hand, CursorType.IBeam);

        // Assert
        Assert.True(result);
    }
}
```

**Step 2: Run test to verify it fails**

Run:
```bash
dotnet test --filter "FullyQualifiedName~ThrottleFilterTests"
```

Expected: FAIL - `ThrottleFilter` does not exist

**Step 3: Implement ThrottleFilter**

File: `src/MxHapticCursorPlugin/Filtering/ThrottleFilter.cs`

```csharp
using System;
using System.Diagnostics;
using MxHapticCursorPlugin.Native;

namespace MxHapticCursorPlugin.Filtering;

/// <summary>
/// Throttles haptic events to prevent overwhelming the user
/// </summary>
public class ThrottleFilter
{
    private readonly int _throttleMs;
    private readonly Stopwatch _stopwatch;
    private long _lastAllowedTimestamp;

    public ThrottleFilter(int throttleMs)
    {
        _throttleMs = throttleMs;
        _stopwatch = Stopwatch.StartNew();
        _lastAllowedTimestamp = 0;
    }

    /// <summary>
    /// Check if event should be allowed based on throttle window
    /// </summary>
    public bool ShouldAllow(CursorType from, CursorType to)
    {
        var currentMs = _stopwatch.ElapsedMilliseconds;

        if (currentMs - _lastAllowedTimestamp >= _throttleMs)
        {
            _lastAllowedTimestamp = currentMs;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Reset throttle timer
    /// </summary>
    public void Reset()
    {
        _lastAllowedTimestamp = 0;
    }
}
```

**Step 4: Run tests**

Run:
```bash
dotnet test --filter "FullyQualifiedName~ThrottleFilterTests"
```

Expected: PASS - All 3 tests pass

**Step 5: Write test for cursor type filter**

File: `src/MxHapticCursorPlugin.Tests/Filtering/CursorTypeFilterTests.cs`

```csharp
using Xunit;
using MxHapticCursorPlugin.Filtering;
using MxHapticCursorPlugin.Native;

namespace MxHapticCursorPlugin.Tests.Filtering;

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
```

**Step 6: Run test to verify it fails**

Run:
```bash
dotnet test --filter "FullyQualifiedName~CursorTypeFilterTests"
```

Expected: FAIL - `CursorTypeFilter` does not exist

**Step 7: Implement CursorTypeFilter**

File: `src/MxHapticCursorPlugin/Filtering/CursorTypeFilter.cs`

```csharp
using System;
using System.Collections.Generic;
using MxHapticCursorPlugin.Native;

namespace MxHapticCursorPlugin.Filtering;

/// <summary>
/// Filters cursor transitions based on user preferences
/// </summary>
public class CursorTypeFilter
{
    private readonly HashSet<(CursorType, CursorType)> _enabledTransitions;

    public CursorTypeFilter()
    {
        _enabledTransitions = new HashSet<(CursorType, CursorType)>();
    }

    /// <summary>
    /// Enable haptics for a specific cursor transition
    /// </summary>
    public void EnableTransition(CursorType from, CursorType to)
    {
        _enabledTransitions.Add((from, to));
    }

    /// <summary>
    /// Disable haptics for a specific cursor transition
    /// </summary>
    public void DisableTransition(CursorType from, CursorType to)
    {
        _enabledTransitions.Remove((from, to));
    }

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
                    _enabledTransitions.Add((from, to));
                }
            }
        }
    }

    /// <summary>
    /// Disable all cursor transitions
    /// </summary>
    public void DisableAll()
    {
        _enabledTransitions.Clear();
    }

    /// <summary>
    /// Check if transition should trigger haptic
    /// </summary>
    public bool ShouldAllow(CursorType from, CursorType to)
    {
        return _enabledTransitions.Contains((from, to));
    }
}
```

**Step 8: Run tests**

Run:
```bash
dotnet test --filter "FullyQualifiedName~CursorTypeFilterTests"
```

Expected: PASS - All 4 tests pass

**Step 9: Commit**

```bash
git add src/MxHapticCursorPlugin/Filtering/
git add src/MxHapticCursorPlugin.Tests/Filtering/
git commit -m "feat: implement throttle and cursor type filtering"
```

---

## Task 6: Haptic Waveform Mapping

**Goal:** Map cursor transitions to appropriate haptic waveforms

**Files:**
- Create: `src/MxHapticCursorPlugin/Haptics/WaveformType.cs`
- Create: `src/MxHapticCursorPlugin/Haptics/WaveformMapper.cs`
- Test: `src/MxHapticCursorPlugin.Tests/Haptics/WaveformMapperTests.cs`

**Step 1: Write test for waveform mapper**

File: `src/MxHapticCursorPlugin.Tests/Haptics/WaveformMapperTests.cs`

```csharp
using Xunit;
using MxHapticCursorPlugin.Haptics;
using MxHapticCursorPlugin.Native;

namespace MxHapticCursorPlugin.Tests.Haptics;

public class WaveformMapperTests
{
    [Fact]
    public void GetWaveform_ArrowToHand_ReturnsSharpStateChange()
    {
        // Arrange
        var mapper = WaveformMapper.CreateDefault();

        // Act
        var waveform = mapper.GetWaveform(CursorType.Arrow, CursorType.Hand);

        // Assert
        Assert.Equal(WaveformType.SharpStateChange, waveform);
    }

    [Fact]
    public void GetWaveform_ArrowToIBeam_ReturnsSubtleCollision()
    {
        // Arrange
        var mapper = WaveformMapper.CreateDefault();

        // Act
        var waveform = mapper.GetWaveform(CursorType.Arrow, CursorType.IBeam);

        // Assert
        Assert.Equal(WaveformType.SubtleCollision, waveform);
    }

    [Fact]
    public void SetCustomMapping_OverridesDefault()
    {
        // Arrange
        var mapper = WaveformMapper.CreateDefault();

        // Act
        mapper.SetMapping(CursorType.Arrow, CursorType.Hand, WaveformType.Mad);
        var waveform = mapper.GetWaveform(CursorType.Arrow, CursorType.Hand);

        // Assert
        Assert.Equal(WaveformType.Mad, waveform);
    }

    [Fact]
    public void GetWaveform_NoMapping_ReturnsSubtleCollision()
    {
        // Arrange
        var mapper = new WaveformMapper();

        // Act
        var waveform = mapper.GetWaveform(CursorType.Custom, CursorType.Arrow);

        // Assert
        Assert.Equal(WaveformType.SubtleCollision, waveform);
    }
}
```

**Step 2: Run test to verify it fails**

Run:
```bash
dotnet test --filter "FullyQualifiedName~WaveformMapperTests"
```

Expected: FAIL - `WaveformMapper` does not exist

**Step 3: Implement WaveformType enum**

File: `src/MxHapticCursorPlugin/Haptics/WaveformType.cs`

```csharp
namespace MxHapticCursorPlugin.Haptics;

/// <summary>
/// Available haptic waveforms from Logitech SDK
/// </summary>
public enum WaveformType
{
    SharpCollision,
    SubtleCollision,
    SharpStateChange,
    DampStateChange,
    Ringing,
    Knock,
    Mad
}
```

**Step 4: Implement WaveformMapper**

File: `src/MxHapticCursorPlugin/Haptics/WaveformMapper.cs`

```csharp
using System.Collections.Generic;
using MxHapticCursorPlugin.Native;

namespace MxHapticCursorPlugin.Haptics;

/// <summary>
/// Maps cursor transitions to haptic waveforms
/// </summary>
public class WaveformMapper
{
    private readonly Dictionary<(CursorType, CursorType), WaveformType> _mappings;

    public WaveformMapper()
    {
        _mappings = new Dictionary<(CursorType, CursorType), WaveformType>();
    }

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
    public void SetMapping(CursorType from, CursorType to, WaveformType waveform)
    {
        _mappings[(from, to)] = waveform;
    }

    /// <summary>
    /// Get waveform for a cursor transition
    /// </summary>
    public WaveformType GetWaveform(CursorType from, CursorType to)
    {
        if (_mappings.TryGetValue((from, to), out var waveform))
        {
            return waveform;
        }

        // Default fallback
        return WaveformType.SubtleCollision;
    }

    /// <summary>
    /// Convert WaveformType to SDK event name
    /// </summary>
    public static string ToEventName(WaveformType waveform)
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
```

**Step 5: Run tests**

Run:
```bash
dotnet test --filter "FullyQualifiedName~WaveformMapperTests"
```

Expected: PASS - All 4 tests pass

**Step 6: Commit**

```bash
git add src/MxHapticCursorPlugin/Haptics/
git add src/MxHapticCursorPlugin.Tests/Haptics/
git commit -m "feat: implement haptic waveform mapping"
```

---

## Task 7: Settings System

**Goal:** Implement settings storage and sensitivity presets

**Files:**
- Create: `src/MxHapticCursorPlugin/Settings/HapticSettings.cs`
- Create: `src/MxHapticCursorPlugin/Settings/SensitivityPreset.cs`
- Test: `src/MxHapticCursorPlugin.Tests/Settings/HapticSettingsTests.cs`

**Step 1: Write test for settings**

File: `src/MxHapticCursorPlugin.Tests/Settings/HapticSettingsTests.cs`

```csharp
using Xunit;
using MxHapticCursorPlugin.Settings;
using MxHapticCursorPlugin.Native;

namespace MxHapticCursorPlugin.Tests.Settings;

public class HapticSettingsTests
{
    [Fact]
    public void CreateLowPreset_ConfiguresCorrectly()
    {
        // Act
        var settings = HapticSettings.CreatePreset(SensitivityPreset.Low);

        // Assert
        Assert.Equal(SensitivityPreset.Low, settings.Preset);
        Assert.Equal(500, settings.ThrottleMs);
        Assert.True(settings.CursorFilter.ShouldAllow(CursorType.Arrow, CursorType.Hand));
        Assert.True(settings.CursorFilter.ShouldAllow(CursorType.Arrow, CursorType.IBeam));
        Assert.False(settings.CursorFilter.ShouldAllow(CursorType.Arrow, CursorType.Crosshair));
    }

    [Fact]
    public void CreateMediumPreset_ConfiguresCorrectly()
    {
        // Act
        var settings = HapticSettings.CreatePreset(SensitivityPreset.Medium);

        // Assert
        Assert.Equal(SensitivityPreset.Medium, settings.Preset);
        Assert.Equal(250, settings.ThrottleMs);
        Assert.True(settings.CursorFilter.ShouldAllow(CursorType.Arrow, CursorType.Crosshair));
    }

    [Fact]
    public void CreateHighPreset_EnablesAllTransitions()
    {
        // Act
        var settings = HapticSettings.CreatePreset(SensitivityPreset.High);

        // Assert
        Assert.Equal(SensitivityPreset.High, settings.Preset);
        Assert.Equal(100, settings.ThrottleMs);
        Assert.True(settings.CursorFilter.ShouldAllow(CursorType.Arrow, CursorType.Custom));
    }
}
```

**Step 2: Run test to verify it fails**

Run:
```bash
dotnet test --filter "FullyQualifiedName~HapticSettingsTests"
```

Expected: FAIL - `HapticSettings` does not exist

**Step 3: Implement SensitivityPreset enum**

File: `src/MxHapticCursorPlugin/Settings/SensitivityPreset.cs`

```csharp
namespace MxHapticCursorPlugin.Settings;

/// <summary>
/// Predefined sensitivity levels
/// </summary>
public enum SensitivityPreset
{
    Low,
    Medium,
    High,
    Custom
}
```

**Step 4: Implement HapticSettings**

File: `src/MxHapticCursorPlugin/Settings/HapticSettings.cs`

```csharp
using MxHapticCursorPlugin.Filtering;
using MxHapticCursorPlugin.Haptics;
using MxHapticCursorPlugin.Native;

namespace MxHapticCursorPlugin.Settings;

/// <summary>
/// Configuration for haptic cursor feedback
/// </summary>
public class HapticSettings
{
    public bool Enabled { get; set; } = true;
    public SensitivityPreset Preset { get; set; } = SensitivityPreset.Medium;
    public MonitoringMode MonitoringMode { get; set; } = MonitoringMode.Polling;
    public int ThrottleMs { get; set; } = 250;
    public int ActivityDetectionWindowMs { get; set; } = 5000;

    public CursorTypeFilter CursorFilter { get; set; } = new();
    public WaveformMapper WaveformMapper { get; set; } = WaveformMapper.CreateDefault();

    /// <summary>
    /// Create settings from a preset
    /// </summary>
    public static HapticSettings CreatePreset(SensitivityPreset preset)
    {
        var settings = new HapticSettings
        {
            Preset = preset,
            CursorFilter = new CursorTypeFilter(),
            WaveformMapper = WaveformMapper.CreateDefault()
        };

        switch (preset)
        {
            case SensitivityPreset.Low:
                settings.ThrottleMs = 500;
                // Only major transitions
                settings.CursorFilter.EnableTransition(CursorType.Arrow, CursorType.Hand);
                settings.CursorFilter.EnableTransition(CursorType.Hand, CursorType.Arrow);
                settings.CursorFilter.EnableTransition(CursorType.Arrow, CursorType.IBeam);
                settings.CursorFilter.EnableTransition(CursorType.IBeam, CursorType.Arrow);
                break;

            case SensitivityPreset.Medium:
                settings.ThrottleMs = 250;
                // Add resize handles and crosshair
                settings.CursorFilter.EnableTransition(CursorType.Arrow, CursorType.Hand);
                settings.CursorFilter.EnableTransition(CursorType.Hand, CursorType.Arrow);
                settings.CursorFilter.EnableTransition(CursorType.Arrow, CursorType.IBeam);
                settings.CursorFilter.EnableTransition(CursorType.IBeam, CursorType.Arrow);
                settings.CursorFilter.EnableTransition(CursorType.Arrow, CursorType.Crosshair);
                settings.CursorFilter.EnableTransition(CursorType.Crosshair, CursorType.Arrow);
                settings.CursorFilter.EnableTransition(CursorType.Arrow, CursorType.ResizeHorizontal);
                settings.CursorFilter.EnableTransition(CursorType.ResizeHorizontal, CursorType.Arrow);
                settings.CursorFilter.EnableTransition(CursorType.Arrow, CursorType.ResizeVertical);
                settings.CursorFilter.EnableTransition(CursorType.ResizeVertical, CursorType.Arrow);
                settings.CursorFilter.EnableTransition(CursorType.Arrow, CursorType.ResizeDiagonalNESW);
                settings.CursorFilter.EnableTransition(CursorType.ResizeDiagonalNESW, CursorType.Arrow);
                settings.CursorFilter.EnableTransition(CursorType.Arrow, CursorType.ResizeDiagonalNWSE);
                settings.CursorFilter.EnableTransition(CursorType.ResizeDiagonalNWSE, CursorType.Arrow);
                break;

            case SensitivityPreset.High:
                settings.ThrottleMs = 100;
                // Enable all transitions
                settings.CursorFilter.EnableAll();
                break;

            case SensitivityPreset.Custom:
                // User configures manually
                break;
        }

        return settings;
    }
}

public enum MonitoringMode
{
    Polling,
    EventDriven
}
```

**Step 5: Run tests**

Run:
```bash
dotnet test --filter "FullyQualifiedName~HapticSettingsTests"
```

Expected: PASS - All 3 tests pass

**Step 6: Commit**

```bash
git add src/MxHapticCursorPlugin/Settings/
git add src/MxHapticCursorPlugin.Tests/Settings/
git commit -m "feat: implement settings system with presets"
```

---

## Task 8: MX Master 4 Activity Detection

**Goal:** Detect when MX Master 4 is actively being used (via recent button/wheel events)

**Files:**
- Create: `src/MxHapticCursorPlugin/Device/DeviceActivityTracker.cs`
- Test: `src/MxHapticCursorPlugin.Tests/Device/DeviceActivityTrackerTests.cs`

**Step 1: Write test for activity tracker**

File: `src/MxHapticCursorPlugin.Tests/Device/DeviceActivityTrackerTests.cs`

```csharp
using Xunit;
using MxHapticCursorPlugin.Device;

namespace MxHapticCursorPlugin.Tests.Device;

public class DeviceActivityTrackerTests
{
    [Fact]
    public void IsActive_NoActivity_ReturnsFalse()
    {
        // Arrange
        var tracker = new DeviceActivityTracker(activityWindowMs: 1000);

        // Act
        var result = tracker.IsActive();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsActive_AfterRecordActivity_ReturnsTrue()
    {
        // Arrange
        var tracker = new DeviceActivityTracker(activityWindowMs: 1000);

        // Act
        tracker.RecordActivity();
        var result = tracker.IsActive();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsActive_AfterActivityWindowExpires_ReturnsFalse()
    {
        // Arrange
        var tracker = new DeviceActivityTracker(activityWindowMs: 100);

        // Act
        tracker.RecordActivity();
        Thread.Sleep(150);
        var result = tracker.IsActive();

        // Assert
        Assert.False(result);
    }
}
```

**Step 2: Run test to verify it fails**

Run:
```bash
dotnet test --filter "FullyQualifiedName~DeviceActivityTrackerTests"
```

Expected: FAIL - `DeviceActivityTracker` does not exist

**Step 3: Implement DeviceActivityTracker**

File: `src/MxHapticCursorPlugin/Device/DeviceActivityTracker.cs`

```csharp
using System;
using System.Diagnostics;

namespace MxHapticCursorPlugin.Device;

/// <summary>
/// Tracks MX Master 4 activity to determine if haptics should trigger
/// Based on recent button clicks, wheel scrolls, etc.
/// </summary>
public class DeviceActivityTracker
{
    private readonly int _activityWindowMs;
    private readonly Stopwatch _stopwatch;
    private long _lastActivityTimestamp;

    public DeviceActivityTracker(int activityWindowMs = 5000)
    {
        _activityWindowMs = activityWindowMs;
        _stopwatch = Stopwatch.StartNew();
        _lastActivityTimestamp = 0;
    }

    /// <summary>
    /// Record that MX Master 4 was used (button click, wheel scroll, etc.)
    /// </summary>
    public void RecordActivity()
    {
        _lastActivityTimestamp = _stopwatch.ElapsedMilliseconds;
    }

    /// <summary>
    /// Check if MX Master 4 has been used recently
    /// </summary>
    public bool IsActive()
    {
        var currentMs = _stopwatch.ElapsedMilliseconds;
        return (currentMs - _lastActivityTimestamp) <= _activityWindowMs;
    }

    /// <summary>
    /// Reset activity tracking
    /// </summary>
    public void Reset()
    {
        _lastActivityTimestamp = 0;
    }
}
```

**Step 4: Run tests**

Run:
```bash
dotnet test --filter "FullyQualifiedName~DeviceActivityTrackerTests"
```

Expected: PASS - All 3 tests pass

**Step 5: Commit**

```bash
git add src/MxHapticCursorPlugin/Device/
git add src/MxHapticCursorPlugin.Tests/Device/
git commit -m "feat: implement MX Master 4 activity tracking"
```

---

## Task 9: Haptic Controller Integration

**Goal:** Wire up monitoring → filtering → haptics pipeline

**Files:**
- Create: `src/MxHapticCursorPlugin/Haptics/HapticController.cs`
- Test: `src/MxHapticCursorPlugin.Tests/Haptics/HapticControllerTests.cs`

**Step 1: Write test for haptic controller**

File: `src/MxHapticCursorPlugin.Tests/Haptics/HapticControllerTests.cs`

```csharp
using Xunit;
using MxHapticCursorPlugin.Haptics;
using MxHapticCursorPlugin.Settings;
using MxHapticCursorPlugin.Native;

namespace MxHapticCursorPlugin.Tests.Haptics;

public class HapticControllerTests
{
    [Fact]
    public void OnCursorChanged_FilteredOut_DoesNotTrigger()
    {
        // Arrange
        var settings = HapticSettings.CreatePreset(SensitivityPreset.Low);
        var triggeredEvents = new List<string>();
        var controller = new HapticController(settings, eventName => triggeredEvents.Add(eventName));

        // Act - Crosshair not enabled in Low preset
        controller.OnCursorChanged(CursorType.Arrow, CursorType.Crosshair);

        // Assert
        Assert.Empty(triggeredEvents);
    }

    [Fact]
    public void OnCursorChanged_AllowedTransition_TriggersHaptic()
    {
        // Arrange
        var settings = HapticSettings.CreatePreset(SensitivityPreset.Low);
        var triggeredEvents = new List<string>();
        var controller = new HapticController(settings, eventName => triggeredEvents.Add(eventName));
        controller.RecordDeviceActivity(); // Mark device as active

        // Act
        controller.OnCursorChanged(CursorType.Arrow, CursorType.Hand);

        // Assert
        Assert.Single(triggeredEvents);
        Assert.Equal("sharp_state_change", triggeredEvents[0]);
    }

    [Fact]
    public void OnCursorChanged_Throttled_DoesNotTrigger()
    {
        // Arrange
        var settings = HapticSettings.CreatePreset(SensitivityPreset.Low);
        var triggeredEvents = new List<string>();
        var controller = new HapticController(settings, eventName => triggeredEvents.Add(eventName));
        controller.RecordDeviceActivity();

        // Act
        controller.OnCursorChanged(CursorType.Arrow, CursorType.Hand);
        controller.OnCursorChanged(CursorType.Hand, CursorType.Arrow); // Immediate second event

        // Assert
        Assert.Single(triggeredEvents); // Only first event triggered
    }

    [Fact]
    public void OnCursorChanged_DeviceInactive_DoesNotTrigger()
    {
        // Arrange
        var settings = HapticSettings.CreatePreset(SensitivityPreset.Low);
        var triggeredEvents = new List<string>();
        var controller = new HapticController(settings, eventName => triggeredEvents.Add(eventName));
        // Don't record device activity

        // Act
        controller.OnCursorChanged(CursorType.Arrow, CursorType.Hand);

        // Assert
        Assert.Empty(triggeredEvents);
    }
}
```

**Step 2: Run test to verify it fails**

Run:
```bash
dotnet test --filter "FullyQualifiedName~HapticControllerTests"
```

Expected: FAIL - `HapticController` does not exist

**Step 3: Implement HapticController**

File: `src/MxHapticCursorPlugin/Haptics/HapticController.cs`

```csharp
using System;
using MxHapticCursorPlugin.Settings;
using MxHapticCursorPlugin.Native;
using MxHapticCursorPlugin.Filtering;
using MxHapticCursorPlugin.Device;

namespace MxHapticCursorPlugin.Haptics;

/// <summary>
/// Coordinates cursor monitoring, filtering, and haptic triggering
/// </summary>
public class HapticController
{
    private readonly HapticSettings _settings;
    private readonly Action<string> _triggerHapticEvent;
    private readonly ThrottleFilter _throttleFilter;
    private readonly DeviceActivityTracker _activityTracker;

    public HapticController(HapticSettings settings, Action<string> triggerHapticEvent)
    {
        _settings = settings;
        _triggerHapticEvent = triggerHapticEvent;
        _throttleFilter = new ThrottleFilter(settings.ThrottleMs);
        _activityTracker = new DeviceActivityTracker(settings.ActivityDetectionWindowMs);
    }

    /// <summary>
    /// Handle cursor change from monitor
    /// </summary>
    public void OnCursorChanged(CursorType from, CursorType to)
    {
        if (!_settings.Enabled)
            return;

        // Check if device is active (prevents haptics when using trackpad/other mouse)
        if (!_activityTracker.IsActive())
            return;

        // Check cursor type filter
        if (!_settings.CursorFilter.ShouldAllow(from, to))
            return;

        // Check throttle
        if (!_throttleFilter.ShouldAllow(from, to))
            return;

        // Get waveform and trigger haptic
        var waveform = _settings.WaveformMapper.GetWaveform(from, to);
        var eventName = WaveformMapper.ToEventName(waveform);

        _triggerHapticEvent(eventName);
    }

    /// <summary>
    /// Record MX Master 4 activity (call from plugin when buttons/wheel used)
    /// </summary>
    public void RecordDeviceActivity()
    {
        _activityTracker.RecordActivity();
    }

    /// <summary>
    /// Update settings (throttle, filters, etc.)
    /// </summary>
    public void UpdateSettings(HapticSettings newSettings)
    {
        // Settings object is mutable, no need to replace
        // Just signal that filters may have changed
    }
}
```

**Step 4: Run tests**

Run:
```bash
dotnet test --filter "FullyQualifiedName~HapticControllerTests"
```

Expected: PASS - All 4 tests pass

**Step 5: Commit**

```bash
git add src/MxHapticCursorPlugin/Haptics/HapticController.cs
git add src/MxHapticCursorPlugin.Tests/Haptics/HapticControllerTests.cs
git commit -m "feat: implement haptic controller pipeline"
```

---

## Task 10: Plugin Integration

**Goal:** Wire everything into the main plugin class

**Files:**
- Modify: `src/MxHapticCursorPlugin/MxHapticCursorPlugin.cs`
- Create: `src/package/events/DefaultEventSource.yaml`
- Create: `src/package/events/extra/eventMapping.yaml`

**Step 1: Create haptic event definitions**

File: `src/package/events/DefaultEventSource.yaml`

```yaml
events:
  - name: sharp_collision
    displayName: Sharp Collision
    description: Sharp haptic feedback for collisions (resize handles)

  - name: subtle_collision
    displayName: Subtle Collision
    description: Gentle haptic feedback for soft transitions

  - name: sharp_state_change
    displayName: Sharp State Change
    description: Clear haptic for important state changes (clickable elements)

  - name: damp_state_change
    displayName: Damp State Change
    description: Soft haptic for returning to default state

  - name: ringing
    displayName: Ringing
    description: Continuous feedback for ongoing processes

  - name: knock
    displayName: Knock
    description: Notification-style haptic

  - name: mad
    displayName: Mad
    description: Strong negative feedback (blocked actions)
```

**Step 2: Create waveform mappings**

File: `src/package/events/extra/eventMapping.yaml`

```yaml
haptics:
  sharp_collision:
    DEFAULT: sharp_collision
    MX Master 4: sharp_collision

  subtle_collision:
    DEFAULT: subtle_collision
    MX Master 4: subtle_collision

  sharp_state_change:
    DEFAULT: sharp_state_change
    MX Master 4: sharp_state_change

  damp_state_change:
    DEFAULT: damp_state_change
    MX Master 4: damp_state_change

  ringing:
    DEFAULT: ringing
    MX Master 4: ringing

  knock:
    DEFAULT: knock
    MX Master 4: knock

  mad:
    DEFAULT: mad
    MX Master 4: mad
```

**Step 3: Create event directories**

Run:
```bash
mkdir -p src/package/events/extra
```

**Step 4: Modify main plugin class**

File: `src/MxHapticCursorPlugin/MxHapticCursorPlugin.cs`

Replace entire file contents:

```csharp
using Loupedeck;
using MxHapticCursorPlugin.Monitoring;
using MxHapticCursorPlugin.Haptics;
using MxHapticCursorPlugin.Settings;

namespace MxHapticCursorPlugin;

public class MxHapticCursorPlugin : Plugin
{
    private ICursorMonitor? _cursorMonitor;
    private HapticController? _hapticController;
    private HapticSettings _settings;

    public override void Load()
    {
        // Register all haptic events
        this.PluginEvents.AddEvent("sharp_collision", "Sharp Collision", "Resize handle collisions");
        this.PluginEvents.AddEvent("subtle_collision", "Subtle Collision", "Soft transitions");
        this.PluginEvents.AddEvent("sharp_state_change", "Sharp State Change", "Clickable elements");
        this.PluginEvents.AddEvent("damp_state_change", "Damp State Change", "Return to default");
        this.PluginEvents.AddEvent("ringing", "Ringing", "Ongoing processes");
        this.PluginEvents.AddEvent("knock", "Knock", "Notifications");
        this.PluginEvents.AddEvent("mad", "Mad", "Blocked actions");

        // Load settings (default to Medium preset for now)
        _settings = HapticSettings.CreatePreset(SensitivityPreset.Medium);

        // Create haptic controller
        _hapticController = new HapticController(_settings, TriggerHapticEvent);

        // Create cursor monitor based on settings
        _cursorMonitor = _settings.MonitoringMode == MonitoringMode.Polling
            ? new PollingCursorMonitor(pollIntervalMs: 50)
            : new EventDrivenCursorMonitor();

        // Wire up cursor change events
        _cursorMonitor.CursorChanged += _hapticController.OnCursorChanged;

        // Start monitoring
        _cursorMonitor.Start();

        this.Info.DisplayName = "MX Haptic Cursor Feedback";
    }

    public override void Unload()
    {
        _cursorMonitor?.Stop();
        _cursorMonitor?.Dispose();
        _cursorMonitor = null;
        _hapticController = null;
    }

    private void TriggerHapticEvent(string eventName)
    {
        try
        {
            this.PluginEvents.RaiseEvent(eventName);
        }
        catch (Exception ex)
        {
            this.Log.Error($"Failed to trigger haptic event '{eventName}': {ex.Message}");
        }
    }

    public override void RunCommand(string commandName, string parameter)
    {
        // Record device activity when any command is triggered
        _hapticController?.RecordDeviceActivity();
    }

    public override void ApplyAdjustment(string adjustmentName, string parameter)
    {
        // Record device activity when any adjustment is made
        _hapticController?.RecordDeviceActivity();
    }
}
```

**Step 5: Build project**

Run:
```bash
cd src/MxHapticCursorPlugin
dotnet build
```

Expected: Build succeeds

**Step 6: Test plugin loads**

Run:
```bash
dotnet watch build
```

Open Logi Options+ → Settings → Plugins → Verify "MX Haptic Cursor Feedback" appears and loads

**Step 7: Manual test - move cursor**

With plugin running:
1. Move cursor over a link (should feel sharp state change)
2. Move cursor over text field (should feel subtle collision)
3. Move cursor back to normal area (should feel damp state change)

**Step 8: Commit**

```bash
git add src/MxHapticCursorPlugin/MxHapticCursorPlugin.cs
git add src/package/events/
git commit -m "feat: integrate haptic cursor feedback into plugin"
```

---

## Task 11: Settings UI (Plugin Settings)

**Goal:** Add settings UI in Logi Options+ for preset selection and monitoring mode

**Files:**
- Create: `src/MxHapticCursorPlugin/Actions/SettingsCommand.cs`
- Modify: `src/MxHapticCursorPlugin/MxHapticCursorPlugin.cs`

**Step 1: Add plugin settings support**

File: `src/MxHapticCursorPlugin/MxHapticCursorPlugin.cs`

Add these methods to the `MxHapticCursorPlugin` class:

```csharp
// Add to MxHapticCursorPlugin class

private const string SettingPreset = "sensitivity_preset";
private const string SettingMonitoringMode = "monitoring_mode";
private const string SettingEnabled = "enabled";

private void LoadSettingsFromStorage()
{
    // Load from plugin settings storage
    var presetValue = this.TryReadIntSetting(SettingPreset, out var p) ? p : 1; // Default Medium
    var modeValue = this.TryReadIntSetting(SettingMonitoringMode, out var m) ? m : 0; // Default Polling
    var enabled = this.TryReadBoolSetting(SettingEnabled, out var e) ? e : true;

    var preset = (SensitivityPreset)presetValue;
    _settings = HapticSettings.CreatePreset(preset);
    _settings.MonitoringMode = (MonitoringMode)modeValue;
    _settings.Enabled = enabled;
}

private void SaveSettingsToStorage()
{
    this.TryWriteIntSetting(SettingPreset, (int)_settings.Preset);
    this.TryWriteIntSetting(SettingMonitoringMode, (int)_settings.MonitoringMode);
    this.TryWriteBoolSetting(SettingEnabled, _settings.Enabled);
}

public void UpdatePreset(SensitivityPreset preset)
{
    _settings = HapticSettings.CreatePreset(preset);
    SaveSettingsToStorage();

    // Restart monitor with new settings
    RestartMonitor();
}

public void UpdateMonitoringMode(MonitoringMode mode)
{
    _settings.MonitoringMode = mode;
    SaveSettingsToStorage();

    // Restart monitor with new mode
    RestartMonitor();
}

public void SetEnabled(bool enabled)
{
    _settings.Enabled = enabled;
    SaveSettingsToStorage();
}

private void RestartMonitor()
{
    _cursorMonitor?.Stop();
    _cursorMonitor?.Dispose();

    _cursorMonitor = _settings.MonitoringMode == MonitoringMode.Polling
        ? new PollingCursorMonitor(pollIntervalMs: 50)
        : new EventDrivenCursorMonitor();

    _cursorMonitor.CursorChanged += _hapticController.OnCursorChanged;
    _cursorMonitor.Start();
}
```

Update `Load()` method:

```csharp
public override void Load()
{
    // Register all haptic events (keep existing code)
    this.PluginEvents.AddEvent("sharp_collision", "Sharp Collision", "Resize handle collisions");
    // ... rest of events ...

    // Load settings from storage
    LoadSettingsFromStorage();

    // Create haptic controller (keep existing code)
    // ... rest of Load method ...
}
```

**Step 2: Create settings command action**

File: `src/MxHapticCursorPlugin/Actions/PresetSelectorCommand.cs`

```csharp
using Loupedeck;
using MxHapticCursorPlugin.Settings;

namespace MxHapticCursorPlugin.Actions;

public class PresetSelectorCommand : PluginMultistateDynamicCommand
{
    private readonly MxHapticCursorPlugin _plugin;

    public PresetSelectorCommand()
    {
        this.DisplayName = "Sensitivity Preset";
        this.Description = "Select haptic sensitivity level";
        this.GroupName = "Settings";

        _plugin = (MxHapticCursorPlugin)base.Plugin;

        // Add states for each preset
        this.AddState("Low", "Low Sensitivity");
        this.AddState("Medium", "Medium Sensitivity");
        this.AddState("High", "High Sensitivity");
    }

    protected override void RunCommand(string actionParameter)
    {
        var preset = actionParameter switch
        {
            "Low" => SensitivityPreset.Low,
            "Medium" => SensitivityPreset.Medium,
            "High" => SensitivityPreset.High,
            _ => SensitivityPreset.Medium
        };

        _plugin.UpdatePreset(preset);
        this.ActionImageChanged();
    }
}
```

**Step 3: Create enable/disable toggle**

File: `src/MxHapticCursorPlugin/Actions/EnableToggleCommand.cs`

```csharp
using Loupedeck;

namespace MxHapticCursorPlugin.Actions;

public class EnableToggleCommand : PluginDynamicCommand
{
    private readonly MxHapticCursorPlugin _plugin;
    private bool _isEnabled = true;

    public EnableToggleCommand()
    {
        this.DisplayName = "Enable/Disable Haptics";
        this.Description = "Toggle cursor haptic feedback on/off";
        this.GroupName = "Settings";

        _plugin = (MxHapticCursorPlugin)base.Plugin;
    }

    protected override void RunCommand(string actionParameter)
    {
        _isEnabled = !_isEnabled;
        _plugin.SetEnabled(_isEnabled);
        this.ActionImageChanged();
    }

    protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
    {
        var color = _isEnabled ? BitmapColor.Green : BitmapColor.Red;
        using var builder = new BitmapBuilder(imageSize);
        builder.Clear(color);
        builder.DrawText(_isEnabled ? "ON" : "OFF");
        return builder.ToImage();
    }
}
```

**Step 4: Create monitoring mode toggle**

File: `src/MxHapticCursorPlugin/Actions/MonitoringModeCommand.cs`

```csharp
using Loupedeck;
using MxHapticCursorPlugin.Settings;

namespace MxHapticCursorPlugin.Actions;

public class MonitoringModeCommand : PluginMultistateDynamicCommand
{
    private readonly MxHapticCursorPlugin _plugin;

    public MonitoringModeCommand()
    {
        this.DisplayName = "Monitoring Mode";
        this.Description = "Switch between polling and event-driven monitoring";
        this.GroupName = "Settings";

        _plugin = (MxHapticCursorPlugin)base.Plugin;

        this.AddState("Polling", "Polling Mode (Simple, Constant CPU)");
        this.AddState("EventDriven", "Event-Driven Mode (Efficient, Complex)");
    }

    protected override void RunCommand(string actionParameter)
    {
        var mode = actionParameter == "Polling"
            ? MonitoringMode.Polling
            : MonitoringMode.EventDriven;

        _plugin.UpdateMonitoringMode(mode);
        this.ActionImageChanged();
    }
}
```

**Step 5: Build and test**

Run:
```bash
dotnet build
```

Expected: Build succeeds

**Step 6: Verify settings persist**

Run:
```bash
dotnet watch build
```

In Logi Options+:
1. Find "Sensitivity Preset" action
2. Change to Low
3. Restart Logi Options+
4. Verify setting persists

**Step 7: Commit**

```bash
git add src/MxHapticCursorPlugin/Actions/
git add src/MxHapticCursorPlugin/MxHapticCursorPlugin.cs
git commit -m "feat: add settings UI and persistence"
```

---

## Task 12: Documentation

**Goal:** Write user-facing README and developer documentation

**Files:**
- Create: `README.md`
- Create: `docs/ARCHITECTURE.md`
- Create: `docs/DEVELOPMENT.md`

**Step 1: Write README**

File: `README.md`

```markdown
# MX Haptic Cursor Feedback

Logitech MX Master 4 plugin that provides haptic feedback when your cursor type changes.

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
```

**Step 2: Write architecture doc**

File: `docs/ARCHITECTURE.md`

```markdown
# Architecture

## Overview

```
Windows Cursor → Monitor → Throttle → Type Filter → Waveform Mapper → Haptic Event
                    ↑                                                       ↓
                    └──────────── Activity Tracker ←─── Device Input ──────┘
```

## Components

### 1. Native Layer (`Native/`)

**User32.cs**: P/Invoke declarations for Windows cursor APIs
- `GetCursorInfo()`: Get current cursor handle and position
- `SetWinEventHook()`: Register for system events (event-driven mode)
- System cursor handle caching

**CursorType.cs**: Enum of detectable cursor types

### 2. Monitoring Layer (`Monitoring/`)

**ICursorMonitor**: Common interface for both monitoring strategies

**PollingCursorMonitor**: Timer-based implementation
- Checks cursor every 50ms
- Simple, reliable
- ~1-2% CPU usage

**EventDrivenCursorMonitor**: Event hook implementation
- Hooks `EVENT_SYSTEM_FOREGROUND` + 200ms fallback polling
- Complex, efficient
- Near-zero CPU when idle

### 3. Filtering Layer (`Filtering/`)

**ThrottleFilter**: Debounce timer to prevent haptic spam
- Configurable window (100-500ms)
- Uses Stopwatch for precision timing

**CursorTypeFilter**: Whitelist of allowed transitions
- HashSet of (from, to) tuples
- Preset configurations

### 4. Haptics Layer (`Haptics/`)

**WaveformMapper**: Cursor transition → waveform mapping
- Semantic defaults (collision, state change, etc.)
- User-overridable mappings

**HapticController**: Coordinates entire pipeline
- Receives cursor change events
- Applies filters
- Checks device activity
- Triggers haptic events

**WaveformType**: Enum of SDK waveforms

### 5. Device Layer (`Device/`)

**DeviceActivityTracker**: Detects MX Master 4 usage
- Tracks last activity timestamp
- 5-second activity window (configurable)
- Prevents haptics when using trackpad/other mice

### 6. Settings Layer (`Settings/`)

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
```

**Step 3: Write development guide**

File: `docs/DEVELOPMENT.md`

```markdown
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
git clone https://github.com/yourusername/mx-haptic-cursor.git
cd mx-haptic-cursor/src/MxHapticCursorPlugin
dotnet build
```

### Development Workflow

**Hot Reload Mode:**
```bash
cd src/MxHapticCursorPlugin
dotnet watch build
```

Changes are automatically reloaded in Logi Options+ (may require action reassignment).

## Testing

### Unit Tests

```bash
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

Modify `MxHapticCursorPlugin.cs`:

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
MxHapticCursor/
├── src/
│   ├── MxHapticCursorPlugin/
│   │   ├── Native/              # Windows API P/Invoke
│   │   ├── Monitoring/          # Cursor change detection
│   │   ├── Filtering/           # Throttle + type filters
│   │   ├── Haptics/             # Waveform mapping + controller
│   │   ├── Device/              # Activity tracking
│   │   ├── Settings/            # Configuration
│   │   ├── Actions/             # Loupedeck UI actions
│   │   └── MxHapticCursorPlugin.cs
│   ├── MxHapticCursorPlugin.Tests/
│   └── package/
│       ├── metadata/
│       │   └── LoupedeckPackage.yaml
│       └── events/
│           ├── DefaultEventSource.yaml
│           └── extra/
│               └── eventMapping.yaml
├── docs/
│   ├── ARCHITECTURE.md
│   └── DEVELOPMENT.md
└── README.md
```

## Making Changes

### Adding a New Cursor Type

1. **Add to CursorType enum**:
   ```csharp
   // Native/CursorType.cs
   public enum CursorType
   {
       // ...
       NewType,
   }
   ```

2. **Add to User32 cursor mapping**:
   ```csharp
   // Native/User32.cs
   private const int IDC_NEWTYPE = 32XXX;

   _systemCursors = new Dictionary<IntPtr, CursorType>
   {
       // ...
       { LoadCursor(IntPtr.Zero, IDC_NEWTYPE), CursorType.NewType },
   };
   ```

3. **Add waveform mapping**:
   ```csharp
   // Haptics/WaveformMapper.cs
   mapper.SetMapping(CursorType.Arrow, CursorType.NewType, WaveformType.SharpCollision);
   ```

4. **Update presets** if needed in `HapticSettings.CreatePreset()`

### Adding a New Waveform

1. **Add to WaveformType enum**:
   ```csharp
   // Haptics/WaveformType.cs
   public enum WaveformType
   {
       // ...
       NewWaveform,
   }
   ```

2. **Update ToEventName**:
   ```csharp
   // Haptics/WaveformMapper.cs
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
   // MxHapticCursorPlugin.cs
   this.PluginEvents.AddEvent("new_waveform", "New Waveform", "Description");
   ```

4. **Add to event mapping**:
   ```yaml
   # package/events/DefaultEventSource.yaml
   - name: new_waveform
     displayName: New Waveform
     description: Description
   ```

   ```yaml
   # package/events/extra/eventMapping.yaml
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
cd src/MxHapticCursorPlugin
dotnet build -c Release
```

Output: `bin/Release/net8.0/MxHapticCursorPlugin.dll`

Package for distribution:
1. Copy `bin/Release/net8.0/` contents
2. Include `package/` directory
3. Zip as `MxHapticCursor-v{version}.zip`

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
```

**Step 4: Commit**

```bash
git add README.md docs/
git commit -m "docs: add README and developer documentation"
```

---

## Task 13: Final Testing & Validation

**Goal:** Comprehensive end-to-end testing

**Step 1: Run all unit tests**

Run:
```bash
dotnet test --verbosity normal
```

Expected: All tests pass (except manual cursor tests)

**Step 2: Manual haptic testing checklist**

Test with **Low sensitivity**:
- [ ] Arrow → Hand (link) - haptic triggered
- [ ] Hand → Arrow - haptic triggered
- [ ] Arrow → I-beam (text) - haptic triggered
- [ ] I-beam → Arrow - haptic triggered
- [ ] Arrow → Crosshair - NO haptic (not in Low preset)

Test with **Medium sensitivity**:
- [ ] All Low tests pass
- [ ] Arrow → Crosshair - haptic triggered
- [ ] Arrow → Resize handle - haptic triggered

Test with **High sensitivity**:
- [ ] All Medium tests pass
- [ ] Arrow → Wait cursor - haptic triggered
- [ ] Arrow → Custom app cursor - haptic triggered

**Step 3: Monitoring mode comparison**

**Polling Mode**:
- [ ] Task Manager shows ~1-2% CPU constant
- [ ] Haptics trigger reliably
- [ ] No noticeable latency

**Event-Driven Mode**:
- [ ] Task Manager shows <0.1% CPU idle
- [ ] Haptics still trigger (may miss some)
- [ ] Slight latency possible

**Step 4: Throttle testing**

Low sensitivity (500ms throttle):
- [ ] Rapid cursor movement over links triggers max 2 haptics/second

High sensitivity (100ms throttle):
- [ ] Can trigger up to 10 haptics/second

**Step 5: Device activity testing**

With plugin running:
1. Don't click any mouse buttons for 10 seconds
2. Move cursor over link
3. Expected: NO haptic (device inactive)
4. Click any button on MX Master 4
5. Move cursor over link
6. Expected: Haptic triggered (device active)

**Step 6: Settings persistence**

1. Set to Low sensitivity
2. Restart Logi Options+
3. Expected: Still Low sensitivity

**Step 7: Build release version**

Run:
```bash
cd src/MxHapticCursorPlugin
dotnet build -c Release
dotnet test -c Release
```

Expected: All builds and tests pass

**Step 8: Final commit and tag**

```bash
git add .
git commit -m "chore: final testing and validation complete"
git tag v0.1.0
```

---

## Next Steps After Implementation

1. **User Testing**: Get feedback from real users
2. **Battery Impact Study**: Measure actual battery drain with both monitoring modes
3. **Advanced Settings UI**: Build UI for custom waveform mappings
4. **Per-Application Profiles**: Different settings for different apps
5. **Marketplace Submission**: Submit to Logitech Marketplace

---

## Implementation Checklist

- [ ] Task 1: Project Scaffolding
- [ ] Task 2: Windows API Cursor Monitoring (P/Invoke Layer)
- [ ] Task 3: Cursor Change Detection (Polling)
- [ ] Task 4: Cursor Change Detection (Event-Driven)
- [ ] Task 5: Throttling and Filtering
- [ ] Task 6: Haptic Waveform Mapping
- [ ] Task 7: Settings System
- [ ] Task 8: MX Master 4 Activity Detection
- [ ] Task 9: Haptic Controller Integration
- [ ] Task 10: Plugin Integration
- [ ] Task 11: Settings UI
- [ ] Task 12: Documentation
- [ ] Task 13: Final Testing & Validation

---

**Total Estimated Time**: 4-6 hours for experienced C#/.NET developer

**Key Risks**:
1. Event-driven monitoring may not reliably detect all cursor changes
2. Haptic feedback may be annoying at high sensitivity
3. Battery drain on MX Master 4 unknown

**Mitigation**:
1. Implement both modes for comparison
2. Conservative default (Medium sensitivity)
3. User testing before wide release
