# Claude Code Hook
param([string]$Event)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir
$statusScript = Join-Path $scriptDir "update-status.ps1"
$lightExe = "$rootDir\ClaudeStatusLight\bin\Release\net8.0-windows\ClaudeStatusLight.exe"

# Auto-start status light if not running (app has mutex to prevent duplicates)
if ($Event -eq "PreToolUse") {
    $proc = Get-Process ClaudeStatusLight -ErrorAction SilentlyContinue
    if (-not $proc -and (Test-Path $lightExe)) {
        Start-Process $lightExe -WindowStyle Hidden
    }
}

if ($Event -eq "PreToolUse" -or $Event -eq "PostToolUse") {
    & $statusScript "thinking"
} elseif ($Event -eq "Notification") {
    & $statusScript "need_input"
} elseif ($Event -eq "Stop") {
    & $statusScript "done"
}
