# Claude Status Light

Windows 桌面红绿灯，实时显示 Claude Code 的工作状态。

## 状态对应

| 状态 | 灯光 | 含义 |
|------|------|------|
| `standby` | 红灯常亮 | 待机 |
| `error` | 红灯闪烁 | 出错 |
| `need_input` | 黄灯常亮 | 需要输入 |
| `thinking` | 黄灯闪烁 | 思考中 |
| `done` | 绿灯常亮 | 完成 |
| `just_done` | 绿灯闪烁 | 刚完成 |

非活动灯显示为灰色暗灯。

## 快速开始

### 1. 构建

```bash
dotnet build ClaudeStatusLight/ClaudeStatusLight.csproj -c Release
```

### 2. 配置 Claude Code Hook

在 Claude Code 中运行 `/hooks`，添加以下 hook：

```powershell
# PreToolUse / PostToolUse / Notification / Stop
powershell -ExecutionPolicy Bypass -File "D:\DEV\claude_status_light\hooks\claude-hook.ps1" -Event {event_type}
```

### 3. 使用

Hook 会自动启动红绿灯应用。关闭按钮在右上角。

## 文件说明

```
hooks/
  claude-hook.ps1      # Claude Code hook 脚本
  update-status.ps1    # 状态更新脚本
ClaudeStatusLight/     # WPF 应用源码
status.json            # 当前状态（自动生成）
window-settings.json   # 窗口位置（自动生成）
```

## 手动测试

```bash
# 写入状态（无 BOM）
printf '{"state":"thinking","timestamp":%s,"message":""}' $(date +%s000) > status.json
```

## 系统要求

- Windows 10/11
- .NET 8.0 Runtime
