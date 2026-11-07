# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

CLICKO is an ultra-lightweight (~46 KB), hardware-aware mouse click tracking utility for Windows. Unlike generic click counters (e.g. `Jeanp78/click-counter`), CLICKO identifies the physical source of each click (USB Mouse vs. Built-in Touchpad) using the Windows Raw Input API and PnP device tree navigation. It features 0% idle CPU usage, <15 MB RAM consumption, persistent local JSON storage, Windows Startup integration, a system tray monitor with live tooltips, and a dark-mode dashboard.

## Build and Run Commands

CLICKO compiles using the built-in Windows C# compiler (`csc.exe` in .NET Framework 4.0/4.8), requiring no SDKs, Visual Studio installations, or external package managers.

### Build Executable
```cmd
build.bat
```
or via PowerShell:
```powershell
.\build.ps1
```
Direct compilation command:
```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /target:winexe /optimize+ /win32icon:icon.ico /r:System.dll,System.Drawing.dll,System.Windows.Forms.dll,System.Web.Extensions.dll /out:CLICKO.exe src\Win32.cs src\DeviceDetector.cs src\ClickStats.cs src\StartupManager.cs src\DashboardForm.cs src\TrayContext.cs src\Program.cs src\IconGenerator.cs
```

### Run Executable
```cmd
CLICKO.exe                 :: Launch dashboard and system tray
CLICKO.exe --minimized     :: Launch directly into system tray (for Windows Startup)
CLICKO.exe --reset         :: Reset counters to 0 on launch
```

### Regulate Icon Asset
```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:exe /r:System.Drawing.dll /out:icon_gen.exe src\IconGenerator.cs
.\icon_gen.exe icon.ico
```

## Architecture & Code Structure

```
CLICKO/
├── CLICKO.exe           # Pre-compiled standalone executable (~46 KB)
├── icon.ico             # Windows Icon asset
├── build.bat / .ps1     # 1-click build scripts
└── src/
    ├── Program.cs       # Entry point, single-instance mutex, CLI args (--minimized, --reset)
    ├── Win32.cs         # P/Invoke bindings (User32 Raw Input, CfgMgr32, Shell32, DPI awareness)
    ├── DeviceDetector.cs# Hardware classification engine (USB Mouse vs. Touchpad)
    ├── ClickStats.cs    # Atomic counters (Interlocked), auto-save timer, JSON persistence
    ├── StartupManager.cs# Windows registry run key manager (HKCU\Software\Microsoft\Windows\CurrentVersion\Run)
    ├── DashboardForm.cs # Dark theme dashboard, live click flashes, device comparison split bar, activity heatmap & streaks
    ├── TrayContext.cs   # System tray icon, dynamic tooltip, context menu, raw input message pump, injection hook
    └── IconGenerator.cs # Programmatic vector icon generator
```

### Critical Architecture Details

1. **Hardware-Level Click Attribution & Injection Discrimination (USB vs Touchpad vs AutoClicker)**:
   - High-level mouse hooks alone do not provide device handles, while Raw Input alone delivers NULL device handles for both Precision Touchpad taps and software-injected clicks.
   - CLICKO implements a coordinated dual-subsystem engine:
     - Win32 Raw Input sink (`RegisterRawInputDevices` with `RIDEV_INPUTSINK`, `usUsagePage = 1`, `usUsage = 2`) on an internal window handle.
     - Win32 Low-Level Mouse Hook (`WH_MOUSE_LL`) to capture kernel `LLMHF_INJECTED` and `LLMHF_LOWER_IL_INJECTED` flags on pointer messages.
   - For non-zero `hDevice`:
     - `cfgmgr32.dll` (`CM_Locate_DevNode`, `CM_Get_Parent`, `CM_Get_Device_ID`) traverses to the device's PnP parent node:
       - Parent starts with `USB\` or contains `\USB\` -> `DeviceType.UsbMouse`
       - Parent starts with `ACPI\`, `I2C\`, or contains `PNP0F`/`ELAN`/`SYN`/`ALPS` -> `DeviceType.Touchpad`
   - For `hDevice == IntPtr.Zero`:
     - If the low-level hook detected `LLMHF_INJECTED == 1` OR click interval indicates an inhuman burst (< 30ms / > 20 CPS) -> classified as `DeviceType.AutoClicker` and filtered into `AutoClicksFiltered`.
     - If `LLMHF_INJECTED == 0` -> genuine human touch/tap from Windows Precision Touchpad -> classified as `DeviceType.Touchpad` and attributed to physical touchpad counters.
   - Classifications for non-zero devices are cached in a thread-safe dictionary for O(1) microsecond lookup on every click.

2. **Day-by-Day Activity Heatmap & Streak Tracker**:
   - `ClickStats` maintains a date-keyed dictionary `DailyHistory` (`yyyy-MM-dd` -> `int`).
   - `CurrentStreak` and `LongestStreak` calculate active clicking streaks.
   - `DashboardForm` embeds a custom double-buffered `ActivityHeatmapPanel` rendering a GitHub/LeetCode-style 7-day grid across months with CLICKO's cyan/slate theme, hover tooltips, and month navigation.

2. **Memory Alignment on x64**:
   - `RAWMOUSE` requires `[StructLayout(LayoutKind.Explicit)]` because `union { ULONG ulButtons; struct { USHORT usButtonFlags; ... } }` is 4-byte aligned in Win32 C SDK. `usButtonFlags` is at `[FieldOffset(4)]` (offset 4, not 2).

3. **Persistence, Shutdown Safety & Multi-Tier Recovery**:
   - Counters use 64-bit `Interlocked.Increment` / `Interlocked.Read`.
   - **Fast Debounce**: Auto-saves 1000ms after the last click, or at least every 2500ms of continuous clicking.
   - **Atomic Disk Flushing**: Writes to `stats.json.tmp` with physical `fs.Flush(true)`, then atomically replaces `stats.json` and updates `stats.json.bak` via Win32 `ReplaceFile` / `File.Replace`.
   - **OS Shutdown Handling**:
     - `SetProcessShutdownParameters(0x3FF, 0)` gives CLICKO first-priority notification before other apps close.
     - `WM_QUERYENDSESSION` and `WM_ENDSESSION` are intercepted by the native message sink and dashboard form to synchronously flush stats to disk.
     - Calls `ShutdownBlockReasonCreate` during write to prevent the OS from terminating the process mid-save.
     - Fallback hooks on `SessionEnding`, `SessionEnded`, `ProcessExit`, and `ApplicationExit`.
   - **Self-Healing Multi-Tier Load**: If `stats.json` is ever missing, 0 bytes, or corrupted (e.g. hard power cut), CLICKO automatically recovers from `stats.json.bak` or `stats.json.tmp`.
   - **Single-Instance Wakeup**: Running a second instance broadcasts `CLICKO_SHOW_DASHBOARD_MSG` to wake the tray instance and open its dashboard.
