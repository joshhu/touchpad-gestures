#Requires -Version 7
<#
.SYNOPSIS
  Sets up the touchpad gestures: builds CloseActive.exe, creates hotkey shortcuts,
  registers CloseActive at logon, and writes the Precision Touchpad gesture mappings.

.PARAMETER SkipGestures
  Don't touch the touchpad registry; map gestures by hand in Settings instead.
#>
param([switch]$SkipGestures)
$ErrorActionPreference = 'Stop'

$installDir = Join-Path $env:LOCALAPPDATA 'Programs\CloseActive'
$programs   = Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'
$startup    = Join-Path $programs 'Startup'
$shell      = New-Object -ComObject WScript.Shell

function New-Shortcut($path, $target, $arguments = '', $hotkey = '', $icon = '') {
    $s = $shell.CreateShortcut($path)
    $s.TargetPath = $target
    $s.Arguments = $arguments
    $s.WorkingDirectory = $env:USERPROFILE
    if ($hotkey) { $s.Hotkey = $hotkey }
    if ($icon) { $s.IconLocation = $icon }
    $s.Save()
    Write-Host "  shortcut: $path"
}

# 1. Build CloseActive.exe with the C# compiler that ships with Windows (.NET Framework 4.x).
$csc = @('FrameworkArm64', 'Framework64', 'Framework') |
    ForEach-Object { Join-Path $env:WINDIR "Microsoft.NET\$_\v4.0.30319\csc.exe" } |
    Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) { throw 'csc.exe (.NET Framework 4.x) not found.' }

Write-Host 'Building CloseActive.exe...'
New-Item -ItemType Directory -Force $installDir | Out-Null
Get-Process CloseActive -ErrorAction SilentlyContinue | Stop-Process -Force
& $csc -nologo -nowarn:0675 -target:winexe -out:"$installDir\CloseActive.exe" "$PSScriptRoot\src\CloseActive.cs"
if ($LASTEXITCODE) { throw 'Build failed.' }

# 2. Shortcuts. Ctrl+Alt+T / Ctrl+Alt+E are plain .lnk hotkeys (Explorer handles them).
#    Ctrl+Alt+W / Ctrl+Alt+C are owned by CloseActive.exe, which must run in the background.
Write-Host 'Creating shortcuts...'
New-Shortcut "$programs\PowerShell 7 (Terminal).lnk" "$env:LOCALAPPDATA\Microsoft\WindowsApps\wt.exe" `
    '-p "PowerShell" -d "%USERPROFILE%"' 'CTRL+ALT+T' "$env:ProgramFiles\PowerShell\7\pwsh.exe,0"
New-Shortcut "$programs\Explorer Home (Hotkey).lnk" "$env:WINDIR\explorer.exe" `
    $env:USERPROFILE 'CTRL+ALT+E' "$env:WINDIR\explorer.exe,0"
New-Shortcut "$startup\CloseActive.lnk" "$installDir\CloseActive.exe"

# 3. Gesture -> custom shortcut mappings (values captured from Settings > Touchpad >
#    Advanced gestures). KeyParams = (virtual key << 16) | 0x11 for Ctrl+Alt.
if (-not $SkipGestures) {
    Write-Host 'Writing touchpad gesture mappings...'
    $k = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\PrecisionTouchPad'
    $custom = 65535
    $params = { param($ch) ([int][char]$ch -shl 16) -bor 0x11 }
    $values = [ordered]@{
        ThreeFingerSlideEnabled      = $custom
        ThreeFingerRight             = $custom; ThreeFingerRightKeyParams    = & $params 'T'
        ThreeFingerUp                = $custom; ThreeFingerUpKeyParams       = & $params 'C'
        ThreeFingerLeft              = $custom; ThreeFingerLeftKeyParams     = & $params 'E'
        FourFingerTapEnabled         = $custom
        CustomFourFingerTap          = 7;       CustomFourFingerTapKeyParams = & $params 'W'
    }
    foreach ($name in $values.Keys) { Set-ItemProperty $k -Name $name -Value $values[$name] -Type DWord }
}

Start-Process "$installDir\CloseActive.exe"
Write-Host 'Done. If the gestures do nothing yet, sign out and back in.'
