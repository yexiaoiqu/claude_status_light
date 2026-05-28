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

# 使用临时文件避免写入冲突
$tempFile = "$statusFile.tmp"
$statusData | Out-File -FilePath $tempFile -Encoding UTF8 -NoNewline
Move-Item -Path $tempFile -Destination $statusFile -Force
