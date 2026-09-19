# Keeps the Unity editor in the foreground while a build runs (the
# editor suspends its loop when it loses focus on this machine), and
# exits as soon as the Windows player has been freshly built AND the
# per-user install has been refreshed.
$unity = Get-Process Unity -ErrorAction SilentlyContinue |
    Where-Object { $_.MainWindowHandle -ne 0 } | Select-Object -First 1
if (-not $unity) { Write-Output "NO_UNITY_WINDOW"; exit 1 }

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;
public class W {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
}
"@

$exe = "C:\Users\Admin\.zcode\workspace\default\GemRush3D\Builds\GemRush3D.exe"
$installed = "$env:LOCALAPPDATA\Programs\GemRush3D\GemRush3D.exe"
$start = Get-Date

for ($i = 0; $i -lt 90; $i++) {
    [W]::SetForegroundWindow($unity.MainWindowHandle) | Out-Null
    Start-Sleep -Seconds 10

    $exeFresh = (Get-Item $exe -ErrorAction SilentlyContinue).LastWriteTime -gt $start
    $instFresh = (Get-Item $installed -ErrorAction SilentlyContinue).LastWriteTime -gt $start
    if ($exeFresh -and $instFresh) {
        Write-Output "DONE: exe + install refreshed"
        exit 0
    }
    if ($exeFresh -and ($i % 6 -eq 5)) { Write-Output "exe fresh, waiting for deploy..." }
}
Write-Output "TIMEOUT: no fresh exe+install after 15 min"
exit 2
