# 多工具配置指南

## 配置方式

### 方式一：使用配置界面（推荐）

1. 点击红绿灯右上角的 **⚙** 设置按钮
2. 在配置界面中添加、编辑或删除工具
3. 点击"保存"按钮
4. 重启应用使配置生效

### 方式二：手动编辑配置文件

编辑 `tool-config.json` 文件来配置要监控的工具：

```json
{
  "tools": [
    {
      "name": "Claude",
      "statusFile": "status.json",
      "toolType": "claude"
    },
    {
      "name": "Trae",
      "statusFile": "trae-status.json",
      "toolType": "trae"
    }
  ],
  "autoDetect": true,
  "activeToolTimeout": 60
}
```

## 字段说明

| 字段 | 说明 |
|------|------|
| `name` | 工具显示名称，会显示在红绿灯上方 |
| `statusFile` | 状态文件路径（相对于项目根目录或绝对路径） |
| `toolType` | 工具类型：`claude` 或 `trae` |
| `autoDetect` | 是否自动检测活跃工具 |
| `activeToolTimeout` | 工具超时时间（秒），超过此时间无更新则视为不活跃 |

## Trae 状态文件

Trae 的状态文件格式应与 Claude 一致：

```json
{
  "state": "thinking",
  "timestamp": 1234567890,
  "message": "",
  "tool": "trae"
}
```

### 支持的状态值

| 状态 | 红绿灯显示 |
|------|-----------|
| `standby` | 红灯常亮 |
| `error` | 红灯闪烁 |
| `need_input` | 黄灯常亮 |
| `thinking` | 黄灯闪烁 |
| `done` | 绿灯常亮 |
| `just_done` | 绿灯闪烁 |

## 自动检测

当 `autoDetect` 为 `true` 时，系统会根据状态文件的最后更新时间自动判断哪个工具正在使用。最近更新的工具会被视为活跃工具，其名称会显示在红绿灯上方。

## 快速开始

1. 找到 Trae 的状态文件位置
2. 点击 ⚙ 设置按钮打开配置界面
3. 添加 Trae 工具并填入状态文件路径
4. 点击保存并重启应用
