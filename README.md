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

## 安装

### 方式一：下载单文件可执行程序

从 GitHub Releases 下载 `ClaudeStatusLight.exe`，双击运行，无需安装 .NET。

### 方式二：从源码构建

```bash
git clone https://github.com/yexiaoiqu/claude_status_light.git
cd claude_status_light
dotnet build ClaudeStatusLight/ClaudeStatusLight.csproj -c Release
```

### 方式三：NuGet 包

```bash
dotnet nuget add source "https://nuget.pkg.github.com/yexiaoiqu/index.json" --name "GitHub" --username YOUR_USERNAME --password YOUR_GITHUB_TOKEN
dotnet add package ClaudeStatusLight --source "GitHub"
```

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
│   ├── MainWindow.xaml          # UI 布局（三个 Border 圆灯）
│   ├── MainWindow.xaml.cs       # 主逻辑：轮询、状态显示、闪烁
│   ├── StatusModels.cs          # 状态枚举、灯模式、显示映射
│   └── StatusWatcher.cs         # 状态文件读取器
├── hooks/
│   ├── claude-hook.ps1          # Claude Code hook 入口
│   └── update-status.ps1        # 写入 status.json
├── status.json                  # 当前状态（自动生成）
├── window-settings.json         # 窗口位置（自动生成）
├── publish/                     # 单文件发布输出
│   └── ClaudeStatusLight.exe    # 155MB 单文件可执行程序
├── nupkgs/                      # NuGet 包输出
│   └── ClaudeStatusLight.1.0.0.nupkg
├── nuget.config                 # NuGet 源配置
└── ClaudeStatusLight.sln        # 解决方案文件（如有）
```

## 部署指南（给 AI 看）

### 前置条件

- Windows 10/11
- .NET 8.0 SDK（构建时需要）或 .NET 8.0 Runtime（运行时需要）
- Claude Code CLI

### 完整部署流程

```bash
# 1. 克隆仓库
git clone https://github.com/yexiaoiqu/claude_status_light.git
cd claude_status_light

# 2. 构建
dotnet build ClaudeStatusLight/ClaudeStatusLight.csproj -c Release

# 3. 启动应用（会自动读取 status.json）
dotnet run --project ClaudeStatusLight/ClaudeStatusLight.csproj -c Release

# 或直接运行已构建的 exe
ClaudeStatusLight/bin/Release/net8.0-windows/ClaudeStatusLight.exe
```

### 打包为单文件可执行程序

```bash
dotnet publish ClaudeStatusLight/ClaudeStatusLight.csproj \
  -c Release \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -o publish
```

输出：`publish/ClaudeStatusLight.exe`（约 155MB，包含 .NET 运行时）

### Hook 脚本工作原理

1. Claude Code 触发事件（PreToolUse/PostToolUse/Notification/Stop）
2. `claude-hook.ps1` 接收事件，调用 `update-status.ps1` 写入 `status.json`
3. `update-status.ps1` 使用临时文件 + 原子移动避免写入冲突
4. WPF 应用每 100ms 轮询 `status.json`，检测到变化后更新灯的状态

### status.json 格式

```json
{
    "state": "thinking",
    "timestamp": 1780020554871,
    "message": ""
}
```

- `state`: `standby` | `error` | `need_input` | `thinking` | `done` | `just_done`
- `timestamp`: Unix 毫秒时间戳
- `message`: 可选消息

### 路径解析

应用使用 `Environment.ProcessPath` 获取 exe 所在目录，然后向上查找包含 `status.json` 的目录作为项目根目录。这确保了：
- 从 `bin/Release/` 运行时能找到项目根目录的 status.json
- 从 `publish/` 运行时也能找到上级目录的 status.json

### 已知限制

- 状态更新有约 5 秒延迟（Claude Code hook 机制限制，无"开始思考"事件）
- 不支持 macOS/Linux（WPF 仅限 Windows）
- 单文件 exe 体积较大（155MB，包含完整 .NET 运行时）

### GitHub Packages

NuGet 包发布在 GitHub Packages：

```bash
# 添加源
dotnet nuget add source "https://nuget.pkg.github.com/yexiaoiqu/index.json" \
  --name "GitHub" \
  --username YOUR_USERNAME \
  --password YOUR_GITHUB_TOKEN

# 安装
dotnet add package ClaudeStatusLight --source "GitHub"
```
