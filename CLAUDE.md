# CLAUDE.md — Claude Status Light

## Project Overview

WPF (.NET 8) Windows desktop app that displays a floating traffic light showing AI coding tool status. Supports Claude Code, Trae, Codex, Cursor, Windsurf.

**How it works:** Claude Code hooks fire on events (PreToolUse/PostToolUse/Notification/Stop) → `hooks/claude-hook.ps1` dispatches to `hooks/update-status.ps1` → writes `status.json` → WPF app polls every 100ms via `StatusWatcher` → updates traffic light display.

## Architecture

- **Single-instance WPF app** (mutex in `App.xaml.cs`)
- **Floating window** with 3 lights (red/yellow/green), drag-to-move, position persistence
- **System tray** with right-click menu (settings, restart, quit)
- **Multi-tool support**: configure multiple status files, auto-detect active tool by file timestamp
- **Hook system**: PowerShell scripts in `hooks/` write `status.json`

## Key Files

| File | Purpose |
|------|---------|
| `ClaudeStatusLight/MainWindow.xaml(.cs)` | Traffic light UI, polling loop, state display |
| `ClaudeStatusLight/StatusWatcher.cs` | Multi-tool file polling, change detection, auto-detection |
| `ClaudeStatusLight/StatusModels.cs` | State enum, ToolType enum, config models, display logic |
| `ClaudeStatusLight/SettingsWindow.xaml(.cs)` | Settings UI, scan logic, auto-create status.json/hooks |
| `ClaudeStatusLight/TrayIconManager.cs` | System tray icon and context menu |
| `ClaudeStatusLight/IconGenerator.cs` | Dynamic traffic light icon for tray |
| `hooks/claude-hook.ps1` | Hook entry point, auto-starts exe on PreToolUse |
| `hooks/update-status.ps1` | Writes status.json atomically (temp file + move) |
| `tool-config.json` | Multi-tool configuration (paths, types) |
| `status.json` | Current state file (project root) |
| `window-settings.json` | Persisted window position |

## Build & Run

```bash
# Build debug
dotnet build ClaudeStatusLight/ClaudeStatusLight.csproj

# Build release single-file exe
.\build.ps1

# Run debug
dotnet run --project ClaudeStatusLight/ClaudeStatusLight.csproj
```

## Important: Process Lock During Build

The hook auto-restarts `ClaudeStatusLight.exe` on `PreToolUse`. If you kill the process to rebuild, the hook will restart it immediately, causing file lock errors.

**Always kill and build in one chained command:**
```bash
taskkill //F //IM ClaudeStatusLight.exe && dotnet build -c Release
```

## Scan & Auto-Create

`SettingsWindow.ScanButton_Click` scans known AI tool paths and project directories. When a project directory is found but missing `status.json` or hooks, `SetupProjectIfNeeded` auto-creates them with absolute paths in the hook commands.

`FindProjectRoot` walks up to 6 parent directories looking for `status.json`, `.claude/status.json`, or `.claude/settings.json`.

## Status File Format

```json
{"state": "thinking", "timestamp": 1780020554871, "message": ""}
```

States: `standby` | `error` | `need_input` | `thinking` | `done` | `just_done`

## Hook Events → States

| Hook Event | State |
|------------|-------|
| PreToolUse | thinking |
| PostToolUse | thinking |
| Notification | need_input |
| Stop | done |
