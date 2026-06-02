# 可配置的颜色和状态映射设计

## 概述

允许用户自定义红绿灯的颜色、模式（常亮/闪烁/关闭）以及每个状态使用哪些灯。

## 当前实现

**硬编码的颜色：**
- 红灯：`Color.FromRgb(220, 50, 50)` (#DC3232)
- 黄灯：`Color.FromRgb(240, 200, 40)` (#F0C828)
- 绿灯：`Color.FromRgb(50, 200, 80)` (#32C850)
- 关闭：`Color.FromRgb(50, 50, 50)` (#323232)

**硬编码的状态映射：**
- Standby → 红灯常亮
- Error → 红灯闪烁
- NeedInput → 黄灯常亮
- Thinking → 黄灯闪烁
- Done → 绿灯常亮
- JustDone → 绿灯闪烁

## 设计方案

### 配置数据结构

在 `tool-config.json` 中添加 `stateDisplay` 字段：

```json
{
  "tools": [...],
  "stateDisplay": {
    "standby": {
      "color": "#DC3232",
      "mode": "on",
      "lights": ["red"]
    },
    "error": {
      "color": "#DC3232",
      "mode": "blink",
      "lights": ["red"]
    },
    "need_input": {
      "color": "#F0C828",
      "mode": "on",
      "lights": ["yellow"]
    },
    "thinking": {
      "color": "#F0C828",
      "mode": "blink",
      "lights": ["yellow"]
    },
    "done": {
      "color": "#32C850",
      "mode": "on",
      "lights": ["green"]
    },
    "just_done": {
      "color": "#32C850",
      "mode": "blink",
      "lights": ["green"]
    }
  }
}
```

### 字段说明

| 字段 | 类型 | 说明 |
|------|------|------|
| `color` | string | 十六进制颜色值（如 `#FF0000`） |
| `mode` | string | 灯模式：`"on"`（常亮）、`"blink"`（闪烁）、`"off"`（关闭） |
| `lights` | string[] | 使用哪些灯：`["red"]`、`["yellow"]`、`["green"]` 或组合 |

### 数据模型变更

**StatusModels.cs 需要添加：**

```csharp
public class StateDisplayConfig
{
    [JsonPropertyName("color")]
    public string Color { get; set; } = "#FFFFFF";
    
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "off";
    
    [JsonPropertyName("lights")]
    public List<string> Lights { get; set; } = new();
}

public class AppConfig
{
    [JsonPropertyName("tools")]
    public List<ToolConfig> Tools { get; set; } = new();
    
    [JsonPropertyName("autoDetect")]
    public bool AutoDetect { get; set; } = true;
    
    [JsonPropertyName("activeToolTimeout")]
    public int ActiveToolTimeoutSeconds { get; set; } = 60;
    
    [JsonPropertyName("stateDisplay")]
    public Dictionary<string, StateDisplayConfig> StateDisplay { get; set; } = new();
}
```

### MainWindow 逻辑变更

**当前实现：**
- 使用硬编码的 `RedBrush`、`YellowBrush`、`GreenBrush`
- `StateDisplay` 类静态映射状态到灯模式

**新实现：**
- 根据配置动态创建 `SolidColorBrush`
- `UpdateDisplay` 方法根据配置决定哪个灯显示什么颜色和模式

```csharp
private void UpdateDisplay(ClaudeState state, ToolType activeTool)
{
    var stateKey = state.ToString().ToLower();
    if (_config.StateDisplay.TryGetValue(stateKey, out var displayConfig))
    {
        var color = (Color)ColorConverter.ConvertFromString(displayConfig.Color);
        var brush = new SolidColorBrush(color);
        
        foreach (var lightName in displayConfig.Lights)
        {
            var mode = displayConfig.Mode switch
            {
                "on" => LightMode.On,
                "blink" => LightMode.Blink,
                _ => LightMode.Off
            };
            
            // 应用到对应的灯
            ApplyLightByName(lightName, brush, mode);
        }
    }
}
```

### Settings UI 设计

在 `SettingsWindow.xaml` 中添加颜色配置区域：

```xml
<GroupBox Header="状态显示配置" Margin="0,10,0,0">
    <StackPanel>
        <!-- Standby 配置 -->
        <StackPanel Orientation="Horizontal" Margin="0,5">
            <TextBlock Text="待机状态:" Width="80" VerticalAlignment="Center"/>
            
            <!-- 颜色预览和选择按钮 -->
            <Border x:Name="StandbyColorPreview" Width="30" Height="30" 
                    Background="#DC3232" BorderBrush="Black" BorderThickness="1" 
                    Margin="0,0,5,0"/>
            <Button Content="选择颜色" Click="StandbyColorPicker_Click" 
                    Width="80" Margin="0,0,10,0"/>
            
            <!-- 模式选择 -->
            <ComboBox x:Name="StandbyMode" Width="80" Margin="0,0,10,0">
                <ComboBoxItem Content="常亮" Tag="on"/>
                <ComboBoxItem Content="闪烁" Tag="blink"/>
                <ComboBoxItem Content="关闭" Tag="off"/>
            </ComboBox>
            
            <!-- 灯选择 -->
            <CheckBox Content="红灯" x:Name="StandbyRed" VerticalAlignment="Center"/>
            <CheckBox Content="黄灯" x:Name="StandbyYellow" VerticalAlignment="Center"/>
            <CheckBox Content="绿灯" x:Name="StandbyGreen" VerticalAlignment="Center"/>
        </StackPanel>
        
        <!-- 其他状态类似... -->
    </StackPanel>
</GroupBox>
```

### 颜色选择实现

使用 Windows 系统颜色对话框：

```csharp
private void StandbyColorPicker_Click(object sender, RoutedEventArgs e)
{
    var dialog = new System.Windows.Forms.ColorDialog();
    dialog.Color = ((SolidColorBrush)StandbyColorPreview.Background).Color.ToWinFormsColor();
    
    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
    {
        var wpfColor = dialog.Color.ToWpfColor();
        StandbyColorPreview.Background = new SolidColorBrush(wpfColor);
    }
}
```

### 颜色转换扩展方法

```csharp
public static class ColorExtensions
{
    public static System.Drawing.Color ToWinFormsColor(this Color wpfColor)
        => System.Drawing.Color.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B);
    
    public static Color ToWpfColor(this System.Drawing.Color winFormsColor)
        => Color.FromArgb(winFormsColor.A, winFormsColor.R, winFormsColor.G, winFormsColor.B);
}
```

### 依赖项

需要在项目中添加 WinForms 引用：

```xml
<ItemGroup>
    <Reference Include="System.Windows.Forms" />
</ItemGroup>
```

## 向后兼容

- 如果 `stateDisplay` 配置缺失或为空，使用当前的默认映射（硬编码值）
- 保持现有行为不变

## 测试方案

1. 测试默认配置是否正常工作
2. 测试自定义颜色是否正确显示
3. 测试灯模式（常亮/闪烁/关闭）是否正常
4. 测试多灯组合是否正常
5. 测试配置文件缺失或格式错误时的降级处理
