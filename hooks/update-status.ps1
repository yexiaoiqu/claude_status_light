# Claude Code Hook - 状态更新脚本
# 用法: .\update-status.ps1 <state> [message]
# 状态: thinking, just_done, done, need_input, error, standby

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet("thinking", "just_done", "done", "need_input", "error", "standby")]
    [string]$State,

    [Parameter(Mandatory=$false)]
    [string]$Message = ""
)

$statusFile = Join-Path (Join-Path $PSScriptRoot "..") "status.json"
$statusFile = [System.IO.Path]::GetFullPath($statusFile)

$statusData = @{
    state     = $State
    timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
    message   = $Message
} | ConvertTo-Json

# 使用临时文件避免写入冲突，UTF8 无 BOM
$tempFile = "$statusFile.tmp"
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($tempFile, $statusData, $utf8NoBom)
Move-Item -Path $tempFile -Destination $statusFile -Force
