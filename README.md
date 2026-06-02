# Claude Status Light

Windows 桌面红绿灯，实时显示 AI 编程工具的工作状态。

支持工具：Claude Code、Trae、GitHub Copilot、Codex、Cursor、Windsurf

## 功能特性

- 浮动红绿灯窗口，实时显示状态
- 系统托盘图标，右键菜单快速操作
- 多工具支持，自动检测活跃工具
- 状态文件自动扫描配置
- 扫描时自动创建缺失的 status.json 和 hooks
- 拖拽移动，位置自动保存
- **可自定义颜色配置** - 为每个状态设置不同颜色
- **可配置灯模式** - 常亮、闪烁、关闭
- **可选择灯组合** - 灵活配置每个状态使用哪些灯
- **实时预览** - 应用按钮即时生效
- **颜色选择器** - 可视化选择颜色

## 状态对应（默认配置）

| 状态 | 灯光 | 含义 |
|------|------|------|
| `standby` | 红灯常亮 | 待机 |
| `error` | 红灯闪烁 | 出错 |
| `need_input` | 黄灯常亮 | 需要输入 |
| `thinking` | 黄灯闪烁 | 思考中 |
| `done` | 绿灯常亮 | 完成 |
| `just_done` | 绿灯闪烁 | 刚完成 |

> 所有颜色和灯光模式都可以在设置中自定义。

## 安装

### 方式一：下载可执行程序（推荐）

1. 从 [Releases](https://github.com/yexiaoiqu/claude_status_light/releases) 下载 `ClaudeStatusLight.exe`
2. 放到任意目录，双击运行
3. 无需安装 .NET 运行时

### 方式二：从源码构建

```bash
git clone https://github.com/yexiaoiqu/claude_status_light.git
cd claude_status_light
.\build.ps1
```

构建完成后，`publish\ClaudeStatusLight.exe` 即为单文件可执行程序。

## 使用方法

### 第一步：启动应用

双击 `ClaudeStatusLight.exe` 启动，会看到：
- 浮动红绿灯窗口（显示红灯 = 待机状态）
- 任务栏右侧出现系统托盘图标

> 窗口可以拖拽移动，位置会自动保存，下次启动时恢复。

### 第二步：配置工具

1. **右键点击** 任务栏右侧的系统托盘图标
2. 选择 **"设置"**
3. 点击 **"扫描"** 按钮

扫描会自动做两件事：

**自动检测**：扫描以下位置寻找 `status.json` 文件：
- `%APPDATA%\Claude`、`%LOCALAPPDATA%\Claude`
- `%USERPROFILE%\.claude`
- `%APPDATA%\Trae`、`%LOCALAPPDATA%\Trae`、`%USERPROFILE%\.trae`
- `%APPDATA%\GitHub Copilot`、`%LOCALAPPDATA%\GitHub Copilot`
- `%APPDATA%\codex`、`%USERPROFILE%\.codex`
- `%APPDATA%\Cursor`、`%APPDATA%\Windsurf`
- 项目目录及其父目录

**自动创建**：如果扫描到项目目录但缺少以下文件，会自动创建：
- `status.json` — 状态文件（初始状态 `standby`）
- `hooks/claude-hook.ps1` — Hook 入口脚本
- `hooks/update-status.ps1` — 状态更新脚本
- `.claude/settings.json` — Claude Code hooks 配置

4. 确认列表中的工具和路径正确
5. 点击 **"保存"**

### 第三步：配置 Claude Code Hook

如果自动创建的 hooks 已经到位（`.claude/settings.json` 中已配置），**不需要额外操作**。

如果需要手动配置，在你的项目目录下运行 Claude Code，输入 `/hooks`，添加以下四个 hook：

```
PreToolUse:    powershell -ExecutionPolicy Bypass -File "你的项目路径\hooks\claude-hook.ps1" PreToolUse
PostToolUse:   powershell -ExecutionPolicy Bypass -File "你的项目路径\hooks\claude-hook.ps1" PostToolUse
Notification:  powershell -ExecutionPolicy Bypass -File "你的项目路径\hooks\claude-hook.ps1" Notification
Stop:          powershell -ExecutionPolicy Bypass -File "你的项目路径\hooks\claude-hook.ps1" Stop
```

或者直接把 `.claude/settings.json` 放到项目根目录：

```json
{
  "hooks": {
    "PreToolUse": [
      {
        "matcher": "",
        "hooks": [{"type": "command", "command": "powershell -ExecutionPolicy Bypass -File \"项目路径\\hooks\\claude-hook.ps1\" PreToolUse"}]
      }
    ],
    "PostToolUse": [
      {
        "matcher": "",
        "hooks": [{"type": "command", "command": "powershell -ExecutionPolicy Bypass -File \"项目路径\\hooks\\claude-hook.ps1\" PostToolUse"}]
      }
    ],
    "Notification": [
      {
        "matcher": "",
        "hooks": [{"type": "command", "command": "powershell -ExecutionPolicy Bypass -File \"项目路径\\hooks\\claude-hook.ps1\" Notification"}]
      }
    ],
    "Stop": [
      {
        "matcher": "",
        "hooks": [{"type": "command", "command": "powershell -ExecutionPolicy Bypass -File \"项目路径\\hooks\\claude-hook.ps1\" Stop"}]
      }
    ]
  }
}
```

### 第四步：开始使用

现在当你在项目中使用 Claude Code 时：
- Claude **开始工作** → 黄灯闪烁（thinking）
- Claude **等待输入** → 黄灯常亮（need_input）
- Claude **完成任务** → 绿灯常亮（done）
- 出现 **错误** → 红灯闪烁（error）
- 空闲 **待机** → 红灯常亮（standby）

### 多工具切换

如果你同时使用多个 AI 工具（如 Claude + Trae），应用会根据状态文件的更新时间自动检测当前活跃的工具，并在红绿灯上方显示工具名称。

在设置中可以调整"自动检测活跃工具"和超时时间。

### 自定义颜色配置

1. 右键托盘图标 → 设置
2. 在"状态显示配置"区域，点击颜色预览打开调色盘
3. 选择灯光模式（常亮/闪烁/关闭）
4. 勾选要使用的灯（一/二/三）
5. 点击"应用"实时预览效果
6. 点击"保存"保存配置

配置保存在 `tool-config.json` 的 `stateDisplay` 字段。

### 开机自启动

创建 `ClaudeStatusLight.exe` 的快捷方式，放到 `shell:startup` 目录：

1. 右键 `ClaudeStatusLight.exe` → 创建快捷方式
2. 按 `Win+R`，输入 `shell:startup`，回车
3. 将快捷方式拖入打开的文件夹

## 状态文件格式

工具的状态文件需要是 JSON 格式：

```json
{
    "state": "thinking",
    "timestamp": 1780020554871,
    "message": ""
}
```

| 字段 | 类型 | 说明 |
|------|------|------|
| `state` | string | 状态值 |
| `timestamp` | number | Unix 毫秒时间戳 |
| `message` | string | 可选的消息 |

支持的 `state` 值：`standby` | `error` | `need_input` | `thinking` | `done` | `just_done`

## 文件结构

```
claude_status_light/
├── ClaudeStatusLight/           # WPF 应用源码
│   ├── App.xaml.cs              # 入口，mutex 单例
│   ├── MainWindow.xaml          # 浮动红绿灯 UI
│   ├── MainWindow.xaml.cs       # 主逻辑：轮询、状态显示
│   ├── StatusModels.cs          # 状态枚举、配置模型
│   ├── StatusWatcher.cs         # 多工具状态文件监控
│   ├── SettingsWindow.xaml      # 设置界面
│   ├── SettingsWindow.xaml.cs   # 扫描、自动创建、配置管理
│   ├── TrayIconManager.cs       # 系统托盘图标管理
│   └── IconGenerator.cs         # 动态图标生成
├── hooks/
│   ├── claude-hook.ps1          # Claude Code hook 入口
│   └── update-status.ps1        # 写入 status.json
├── build.ps1                    # 构建脚本
├── tool-config.json             # 工具配置文件
├── status.json                  # 当前状态（自动生成）
└── window-settings.json         # 窗口位置（自动生成）
```

## 常见问题

### Q: 启动后看不到窗口？

A: 窗口可能在屏幕边缘，检查任务栏是否有 Claude Status Light 图标。

### Q: 扫描找不到状态文件？

A: 扫描会检查常见工具的默认安装路径。如果工具安装在自定义路径，可以手动在设置中添加状态文件路径。

### Q: 扫描自动创建了什么？

A: 扫描到项目目录时，如果缺少以下文件会自动创建：
- `status.json` — 状态文件
- `hooks/claude-hook.ps1` — Hook 脚本
- `hooks/update-status.ps1` — 状态更新脚本
- `.claude/settings.json` — Claude Code hooks 配置

### Q: 如何开机自启？

A: 创建 `ClaudeStatusLight.exe` 的快捷方式，放到 `shell:startup` 目录。

### Q: hook 命令中的路径是什么？

A: 是你项目根目录的绝对路径，指向 `hooks/claude-hook.ps1`。自动创建时会使用正确的绝对路径。

## 开发

```bash
# 构建
dotnet build ClaudeStatusLight/ClaudeStatusLight.csproj

# 运行
dotnet run --project ClaudeStatusLight/ClaudeStatusLight.csproj

# 发布单文件
.\build.ps1
```

## License

MIT
