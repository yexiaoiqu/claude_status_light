# Claude Status Light

Windows 桌面红绿灯，实时显示 AI 编程工具的工作状态。

支持工具：Claude Code、Trae、Codex、Cursor、Windsurf

## 功能特性

- 浮动红绿灯窗口，实时显示状态
- 系统托盘图标，右键菜单快速操作
- 多工具支持，自动检测活跃工具
- 状态文件自动扫描配置
- 拖拽移动，位置自动保存

## 状态对应

| 状态 | 灯光 | 含义 |
|------|------|------|
| `standby` | 红灯常亮 | 待机 |
| `error` | 红灯闪烁 | 出错 |
| `need_input` | 黄灯常亮 | 需要输入 |
| `thinking` | 黄灯闪烁 | 思考中 |
| `done` | 绿灯常亮 | 完成 |
| `just_done` | 绿灯闪烁 | 刚完成 |

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

### 启动应用

双击 `ClaudeStatusLight.exe` 启动，会看到：
- 浮动红绿灯窗口（可拖拽移动）
- 任务栏右侧系统托盘图标

### 配置工具

1. 右键点击系统托盘图标
2. 选择"设置"
3. 点击"扫描"按钮自动检测已安装的 AI 工具
4. 点击"保存"

### 状态文件格式

工具的状态文件需要是 JSON 格式：

```json
{
    "state": "thinking",
    "timestamp": 1780020554871,
    "message": ""
}
```

支持的 state 值：`standby` | `error` | `need_input` | `thinking` | `done` | `just_done`

## 配置 Claude Code Hook

在 Claude Code 中运行 `/hooks`，添加以下 hook 事件：

```powershell
powershell -ExecutionPolicy Bypass -File "项目路径\hooks\claude-hook.ps1" -Event {event_type}
```

需要为以下事件配置：
- `PreToolUse`
- `PostToolUse`
- `Notification`
- `Stop`

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

### Q: 如何开机自启？

A: 创建 `ClaudeStatusLight.exe` 的快捷方式，放到 `shell:startup` 目录。

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
