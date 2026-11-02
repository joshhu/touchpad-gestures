# touchpad-gestures

Windows 11 Precision Touchpad gestures that launch apps and act differently depending on
the window in front, plus a tiny background helper (`CloseActive.exe`) that makes the
context-aware ones work.

| Gesture | Hotkey | Action |
|---|---|---|
| 3-finger swipe right | Ctrl+Alt+T | Open PowerShell 7 in Windows Terminal |
| 3-finger swipe up | Ctrl+Alt+C | In Chrome: new tab. Elsewhere: open Chrome |
| 3-finger swipe left | Ctrl+Alt+E | Open File Explorer at the home folder |
| 4-finger tap | Ctrl+Alt+W | In Chrome: close tab. Elsewhere: close window (ignores desktop/taskbar) |

These replace Windows' defaults for those gestures (app switching, Task View,
notification center). Use Alt+Tab, Win+Tab and Win+N instead.

## How it works

Windows can map a gesture only to a fixed key combo, so each gesture sends a hotkey:

- **Ctrl+Alt+T / Ctrl+Alt+E** are `.lnk` hotkeys on Start Menu shortcuts.
- **Ctrl+Alt+W / Ctrl+Alt+C** are registered by `CloseActive.exe` with `RegisterHotKey`.
  It checks the foreground window and sends Ctrl+W / Ctrl+T to Chrome, or closes the
  window / starts Chrome otherwise. A `.lnk` hotkey can't do this, because Explorer moves
  focus to the taskbar before launching the target, so the program would see the
  taskbar instead of the window you meant.

## Requirements

- Windows 11 with a Precision Touchpad
- PowerShell 7, Windows Terminal, Google Chrome at the default path
- .NET Framework 4.x (built into Windows; its `csc.exe` compiles the helper, native arm64 or x64)

## Install

```powershell
git clone https://github.com/joshhu/touchpad-gestures
cd touchpad-gestures
pwsh -File .\install.ps1
```

This builds `CloseActive.exe` into `%LOCALAPPDATA%\Programs\CloseActive`, creates the
shortcuts, adds the helper to the Startup folder and writes the gesture mappings to
`HKCU\Software\Microsoft\Windows\CurrentVersion\PrecisionTouchPad`. Sign out and back in
if the gestures don't respond yet.

To set the gestures by hand instead, run with `-SkipGestures`, then go to
**Settings > Bluetooth & devices > Touchpad > Advanced gestures**, choose
**Custom shortcut** for each gesture and record its hotkey.

## Uninstall

1. Delete `CloseActive.lnk` from the Startup folder and end `CloseActive.exe`.
2. Delete `PowerShell 7 (Terminal).lnk` and `Explorer Home (Hotkey).lnk` from
   `%APPDATA%\Microsoft\Windows\Start Menu\Programs`.
3. Delete `%LOCALAPPDATA%\Programs\CloseActive`.
4. Reset the gestures in **Settings > Touchpad > Advanced gestures**.

## License

MIT
