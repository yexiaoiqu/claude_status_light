# Claude Status Light 构建脚本
# 用法: .\build.ps1

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Claude Status Light 构建脚本" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 清理旧的发布文件
$publishDir = ".\publish"
if (Test-Path $publishDir) {
    Write-Host "清理旧的发布文件..." -ForegroundColor Yellow
    Remove-Item -Recurse -Force $publishDir
}

# 构建单文件 exe
Write-Host "正在构建单文件可执行程序..." -ForegroundColor Yellow
dotnet publish ClaudeStatusLight/ClaudeStatusLight.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Host "构建失败!" -ForegroundColor Red
    exit 1
}

# 检查输出
$exePath = Join-Path $publishDir "ClaudeStatusLight.exe"
if (Test-Path $exePath) {
    $fileSize = (Get-Item $exePath).Length / 1MB
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "  构建成功!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "输出文件: $exePath" -ForegroundColor White
    Write-Host "文件大小: $([math]::Round($fileSize, 2)) MB" -ForegroundColor White
    Write-Host ""
    Write-Host "使用方法:" -ForegroundColor Cyan
    Write-Host "  1. 将 ClaudeStatusLight.exe 复制到任意目录" -ForegroundColor White
    Write-Host "  2. 双击运行" -ForegroundColor White
    Write-Host "  3. 右键任务栏图标打开设置" -ForegroundColor White
    Write-Host ""
} else {
    Write-Host "构建失败: 未找到输出文件" -ForegroundColor Red
    exit 1
}
