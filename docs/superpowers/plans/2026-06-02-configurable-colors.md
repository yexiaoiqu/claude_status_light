# 可配置的颜色和状态映射实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 让用户可以自定义每个状态的颜色、灯模式（常亮/闪烁/关闭）以及使用哪些灯，并在设置界面提供颜色选择器和重置功能。

**Architecture:** 在 StatusModels.cs 中添加 StateDisplayConfig 数据模型，扩展 AppConfig 包含 stateDisplay 配置。MainWindow 根据配置动态创建画刷并应用到灯控件。SettingsWindow 添加颜色配置 UI，使用 Windows 系统颜色对话框选择颜色，提供重置/恢复默认功能。

**Tech Stack:** WPF (.NET 8), System.Windows.Forms (颜色对话框), System.Text.Json

---

## 文件结构

| 文件 | 变更类型 | 职责 |
|------|----------|------|
| `ClaudeStatusLight/StatusModels.cs` | 修改 | 添加 StateDisplayConfig 类，扩展 AppConfig，添加默认配置常量 |
| `ClaudeStatusLight/MainWindow.xaml.cs` | 修改 | 使用配置的颜色和模式，移除硬编码画刷 |
| `ClaudeStatusLight/MainWindow.xaml` | 修改 | 为灯控件添加 x:Name 以便代码访问 |
| `ClaudeStatusLight/SettingsWindow.xaml` | 修改 | 添加颜色配置 UI 区域 |
| `ClaudeStatusLight/SettingsWindow.xaml.cs` | 修改 | 添加颜色选择、加载、保存、重置逻辑 |
| `ClaudeStatusLight/ColorExtensions.cs` | 新建 | WPF 和 WinForms 颜色转换扩展方法 |
| `ClaudeStatusLight/ClaudeStatusLight.csproj` | 修改 | 添加 System.Windows.Forms 引用 |

---

### Task 1: 添加数据模型和默认配置

**Files:**
- Modify: `ClaudeStatusLight/StatusModels.cs`

- [ ] **Step 1: 添加 StateDisplayConfig 类**

在 StatusModels.cs 中添加：

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
```

- [ ] **Step 2: 扩展 AppConfig 类**

在 AppConfig 类中添加 StateDisplay 属性：

```csharp
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

- [ ] **Step 3: 添加默认配置常量**

在 StatusModels.cs 底部添加：

```csharp
public static class DefaultColors
{
    public static readonly Dictionary<string, StateDisplayConfig> StateDisplay = new()
    {
        ["standby"] = new() { Color = "#DC3232", Mode = "on", Lights = ["red"] },
        ["error"] = new() { Color = "#DC3232", Mode = "blink", Lights = ["red"] },
        ["need_input"] = new() { Color = "#F0C828", Mode = "on", Lights = ["yellow"] },
        ["thinking"] = new() { Color = "#F0C828", Mode = "blink", Lights = ["yellow"] },
        ["done"] = new() { Color = "#32C850", Mode = "on", Lights = ["green"] },
        ["just_done"] = new() { Color = "#32C850", Mode = "blink", Lights = ["green"] }
    };
}
```

- [ ] **Step 4: 验证编译通过**

Run: `dotnet build ClaudeStatusLight/ClaudeStatusLight.csproj`
Expected: BUILD SUCCEEDED

- [ ] **Step 5: Commit**

```bash
git add ClaudeStatusLight/StatusModels.cs
git commit -m "feat: add StateDisplayConfig model and default color config"
```

---

### Task 2: 添加颜色转换扩展方法

**Files:**
- Create: `ClaudeStatusLight/ColorExtensions.cs`

- [ ] **Step 1: 创建 ColorExtensions.cs**

```csharp
using System.Windows.Media;

namespace ClaudeStatusLight;

public static class ColorExtensions
{
    public static System.Drawing.Color ToWinFormsColor(this Color wpfColor)
        => System.Drawing.Color.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B);

    public static Color ToWpfColor(this System.Drawing.Color winFormsColor)
        => Color.FromArgb(winFormsColor.A, winFormsColor.R, winFormsColor.G, winFormsColor.B);
}
```

- [ ] **Step 2: 验证编译通过**

Run: `dotnet build ClaudeStatusLight/ClaudeStatusLight.csproj`
Expected: BUILD SUCCEEDED

- [ ] **Step 3: Commit**

```bash
git add ClaudeStatusLight/ColorExtensions.cs
git commit -m "feat: add ColorExtensions for WPF/WinForms color conversion"
```

---

### Task 3: 添加 WinForms 引用

**Files:**
- Modify: `ClaudeStatusLight/ClaudeStatusLight.csproj`

- [ ] **Step 1: 添加 WinForms 引用**

在 ClaudeStatusLight.csproj 的 `<PropertyGroup>` 中添加：

```xml
<UseWindowsForms>true</UseWindowsForms>
```

- [ ] **Step 2: 验证编译通过**

Run: `dotnet build ClaudeStatusLight/ClaudeStatusLight.csproj`
Expected: BUILD SUCCEEDED

- [ ] **Step 3: Commit**

```bash
git add ClaudeStatusLight/ClaudeStatusLight.csproj
git commit -m "feat: enable WinForms for color dialog support"
```

---

### Task 4: 更新 MainWindow 使用配置颜色

**Files:**
- Modify: `ClaudeStatusLight/MainWindow.xaml.cs`
- Modify: `ClaudeStatusLight/MainWindow.xaml`

- [ ] **Step 1: 更新 MainWindow.xaml 为灯控件添加 x:Name**

找到 RedLight、YellowLight、GreenLight 的 Border 控件，确保它们有 x:Name 属性（已存在则跳过）。

- [ ] **Step 2: 修改 MainWindow.xaml.cs 移除硬编码画刷**

删除：
```csharp
private static readonly SolidColorBrush RedBrush = new SolidColorBrush(Color.FromRgb(220, 50, 50));
private static readonly SolidColorBrush YellowBrush = new SolidColorBrush(Color.FromRgb(240, 200, 40));
private static readonly SolidColorBrush GreenBrush = new SolidColorBrush(Color.FromRgb(50, 200, 80));
private static readonly SolidColorBrush OffBrush = new SolidColorBrush(Color.FromRgb(50, 50, 50));
```

添加：
```csharp
private AppConfig _config;
private static readonly SolidColorBrush OffBrush = new SolidColorBrush(Color.FromRgb(50, 50, 50));
```

- [ ] **Step 3: 在构造函数中加载配置**

在构造函数中，在创建 watcher 之前加载配置：

```csharp
_config = LoadConfig(_configPath);
```

- [ ] **Step 4: 添加 GetDisplayConfig 辅助方法**

```csharp
private StateDisplayConfig GetDisplayConfig(ClaudeState state)
{
    var stateKey = state switch
    {
        ClaudeState.Standby => "standby",
        ClaudeState.Error => "error",
        ClaudeState.NeedInput => "need_input",
        ClaudeState.Thinking => "thinking",
        ClaudeState.Done => "done",
        ClaudeState.JustDone => "just_done",
        _ => "standby"
    };

    if (_config.StateDisplay.TryGetValue(stateKey, out var config))
        return config;

    return DefaultColors.StateDisplay.TryGetValue(stateKey, out var defaultConfig)
        ? defaultConfig
        : new StateDisplayConfig { Color = "#FFFFFF", Mode = "off", Lights = new List<string>() };
}
```

- [ ] **Step 5: 更新 UpdateDisplay 方法**

```csharp
private void UpdateDisplay(ClaudeState state, ToolType activeTool)
{
    _currentState = state;
    _activeTool = activeTool;
    _blinkOn = true;

    var displayConfig = GetDisplayConfig(state);
    var color = (Color)ColorConverter.ConvertFromString(displayConfig.Color);
    var brush = new SolidColorBrush(color);
    var mode = displayConfig.Mode switch
    {
        "on" => LightMode.On,
        "blink" => LightMode.Blink,
        _ => LightMode.Off
    };

    // Reset all lights to off
    ApplyLight(RedLight, RedGlow, OffBrush, LightMode.Off, true);
    ApplyLight(YellowLight, YellowGlow, OffBrush, LightMode.Off, true);
    ApplyLight(GreenLight, GreenGlow, OffBrush, LightMode.Off, true);

    // Apply configured lights
    foreach (var lightName in displayConfig.Lights)
    {
        switch (lightName.ToLower())
        {
            case "red":
                ApplyLight(RedLight, RedGlow, brush, mode, true);
                break;
            case "yellow":
                ApplyLight(YellowLight, YellowGlow, brush, mode, true);
                break;
            case "green":
                ApplyLight(GreenLight, GreenGlow, brush, mode, true);
                break;
        }
    }

    // Update tool name display
    if (activeTool != ToolType.Unknown)
    {
        ToolNameText.Text = StateDisplay.GetToolDisplayName(activeTool);
        ToolNameText.Visibility = Visibility.Visible;
    }
    else
    {
        ToolNameText.Visibility = Visibility.Collapsed;
    }

    StatusText.Text = StateDisplay.GetLabel(state);

    if (mode == LightMode.Blink)
        _blinkTimer.Start();
    else
        _blinkTimer.Stop();
}
```

- [ ] **Step 6: 更新 BlinkTimer_Tick 方法**

```csharp
private void BlinkTimer_Tick(object? sender, EventArgs e)
{
    _blinkOn = !_blinkOn;

    var displayConfig = GetDisplayConfig(_currentState);
    if (displayConfig.Mode != "blink") return;

    var color = (Color)ColorConverter.ConvertFromString(displayConfig.Color);
    var brush = new SolidColorBrush(color);

    foreach (var lightName in displayConfig.Lights)
    {
        switch (lightName.ToLower())
        {
            case "red":
                ApplyLight(RedLight, RedGlow, brush, LightMode.Blink, _blinkOn);
                break;
            case "yellow":
                ApplyLight(YellowLight, YellowGlow, brush, LightMode.Blink, _blinkOn);
                break;
            case "green":
                ApplyLight(GreenLight, GreenGlow, brush, LightMode.Blink, _blinkOn);
                break;
        }
    }
}
```

- [ ] **Step 7: 验证编译通过**

Run: `dotnet build ClaudeStatusLight/ClaudeStatusLight.csproj`
Expected: BUILD SUCCEEDED

- [ ] **Step 8: Commit**

```bash
git add ClaudeStatusLight/MainWindow.xaml.cs ClaudeStatusLight/MainWindow.xaml
git commit -m "feat: use configurable colors in MainWindow"
```

---

### Task 5: 添加 Settings UI 颜色配置区域

**Files:**
- Modify: `ClaudeStatusLight/SettingsWindow.xaml`

- [ ] **Step 1: 调整窗口高度**

将窗口高度从 520 改为 750：

```xml
Height="750"
```

- [ ] **Step 2: 在自动检测设置区域后添加颜色配置区域**

在 `<!-- 自动检测设置 -->` Border 之后，`<!-- 按钮 -->` StackPanel 之前添加：

```xml
<!-- 颜色配置 -->
<Border Grid.Row="3.5"
        Background="#2D2D2D"
        CornerRadius="8"
        Padding="12"
        Margin="0,12,0,0">
    <StackPanel>
        <TextBlock Text="状态显示配置"
                   Foreground="White"
                   FontSize="14"
                   FontWeight="Bold"
                   Margin="0,0,0,12"/>

        <!-- Standby -->
        <StackPanel Orientation="Horizontal" Margin="0,4">
            <TextBlock Text="待机:" Width="50" VerticalAlignment="Center" Foreground="#AAAAAA"/>
            <Border x:Name="StandbyColorPreview" Width="24" Height="24"
                    Background="#DC3232" BorderBrush="#555555" BorderThickness="1"
                    Margin="0,0,6,0" Cursor="Hand"
                    MouseLeftButtonDown="ColorPreview_MouseLeftButtonDown"/>
            <ComboBox x:Name="StandbyMode" Width="65" Margin="0,0,6,0"
                      SelectedIndex="0"
                      Background="#3D3D3D" Foreground="White" BorderBrush="#555555">
                <ComboBoxItem Content="常亮" Tag="on"/>
                <ComboBoxItem Content="闪烁" Tag="blink"/>
                <ComboBoxItem Content="关闭" Tag="off"/>
            </ComboBox>
            <CheckBox Content="红" x:Name="StandbyRed" VerticalAlignment="Center" Foreground="#AAAAAA" IsChecked="True"/>
            <CheckBox Content="黄" x:Name="StandbyYellow" VerticalAlignment="Center" Foreground="#AAAAAA" Margin="8,0,0,0"/>
            <CheckBox Content="绿" x:Name="StandbyGreen" VerticalAlignment="Center" Foreground="#AAAAAA" Margin="8,0,0,0"/>
        </StackPanel>

        <!-- Error -->
        <StackPanel Orientation="Horizontal" Margin="0,4">
            <TextBlock Text="错误:" Width="50" VerticalAlignment="Center" Foreground="#AAAAAA"/>
            <Border x:Name="ErrorColorPreview" Width="24" Height="24"
                    Background="#DC3232" BorderBrush="#555555" BorderThickness="1"
                    Margin="0,0,6,0" Cursor="Hand"
                    MouseLeftButtonDown="ColorPreview_MouseLeftButtonDown"/>
            <ComboBox x:Name="ErrorMode" Width="65" Margin="0,0,6,0"
                      SelectedIndex="1"
                      Background="#3D3D3D" Foreground="White" BorderBrush="#555555">
                <ComboBoxItem Content="常亮" Tag="on"/>
                <ComboBoxItem Content="闪烁" Tag="blink"/>
                <ComboBoxItem Content="关闭" Tag="off"/>
            </ComboBox>
            <CheckBox Content="红" x:Name="ErrorRed" VerticalAlignment="Center" Foreground="#AAAAAA" IsChecked="True"/>
            <CheckBox Content="黄" x:Name="ErrorYellow" VerticalAlignment="Center" Foreground="#AAAAAA" Margin="8,0,0,0"/>
            <CheckBox Content="绿" x:Name="ErrorGreen" VerticalAlignment="Center" Foreground="#AAAAAA" Margin="8,0,0,0"/>
        </StackPanel>

        <!-- NeedInput -->
        <StackPanel Orientation="Horizontal" Margin="0,4">
            <TextBlock Text="交互:" Width="50" VerticalAlignment="Center" Foreground="#AAAAAA"/>
            <Border x:Name="NeedInputColorPreview" Width="24" Height="24"
                    Background="#F0C828" BorderBrush="#555555" BorderThickness="1"
                    Margin="0,0,6,0" Cursor="Hand"
                    MouseLeftButtonDown="ColorPreview_MouseLeftButtonDown"/>
            <ComboBox x:Name="NeedInputMode" Width="65" Margin="0,0,6,0"
                      SelectedIndex="0"
                      Background="#3D3D3D" Foreground="White" BorderBrush="#555555">
                <ComboBoxItem Content="常亮" Tag="on"/>
                <ComboBoxItem Content="闪烁" Tag="blink"/>
                <ComboBoxItem Content="关闭" Tag="off"/>
            </ComboBox>
            <CheckBox Content="红" x:Name="NeedInputRed" VerticalAlignment="Center" Foreground="#AAAAAA"/>
            <CheckBox Content="黄" x:Name="NeedInputYellow" VerticalAlignment="Center" Foreground="#AAAAAA" Margin="8,0,0,0" IsChecked="True"/>
            <CheckBox Content="绿" x:Name="NeedInputGreen" VerticalAlignment="Center" Foreground="#AAAAAA" Margin="8,0,0,0"/>
        </StackPanel>

        <!-- Thinking -->
        <StackPanel Orientation="Horizontal" Margin="0,4">
            <TextBlock Text="思考:" Width="50" VerticalAlignment="Center" Foreground="#AAAAAA"/>
            <Border x:Name="ThinkingColorPreview" Width="24" Height="24"
                    Background="#F0C828" BorderBrush="#555555" BorderThickness="1"
                    Margin="0,0,6,0" Cursor="Hand"
                    MouseLeftButtonDown="ColorPreview_MouseLeftButtonDown"/>
            <ComboBox x:Name="ThinkingMode" Width="65" Margin="0,0,6,0"
                      SelectedIndex="1"
                      Background="#3D3D3D" Foreground="White" BorderBrush="#555555">
                <ComboBoxItem Content="常亮" Tag="on"/>
                <ComboBoxItem Content="闪烁" Tag="blink"/>
                <ComboBoxItem Content="关闭" Tag="off"/>
            </ComboBox>
            <CheckBox Content="红" x:Name="ThinkingRed" VerticalAlignment="Center" Foreground="#AAAAAA"/>
            <CheckBox Content="黄" x:Name="ThinkingYellow" VerticalAlignment="Center" Foreground="#AAAAAA" Margin="8,0,0,0" IsChecked="True"/>
            <CheckBox Content="绿" x:Name="ThinkingGreen" VerticalAlignment="Center" Foreground="#AAAAAA" Margin="8,0,0,0"/>
        </StackPanel>

        <!-- Done -->
        <StackPanel Orientation="Horizontal" Margin="0,4">
            <TextBlock Text="完成:" Width="50" VerticalAlignment="Center" Foreground="#AAAAAA"/>
            <Border x:Name="DoneColorPreview" Width="24" Height="24"
                    Background="#32C850" BorderBrush="#555555" BorderThickness="1"
                    Margin="0,0,6,0" Cursor="Hand"
                    MouseLeftButtonDown="ColorPreview_MouseLeftButtonDown"/>
            <ComboBox x:Name="DoneMode" Width="65" Margin="0,0,6,0"
                      SelectedIndex="0"
                      Background="#3D3D3D" Foreground="White" BorderBrush="#555555">
                <ComboBoxItem Content="常亮" Tag="on"/>
                <ComboBoxItem Content="闪烁" Tag="blink"/>
                <ComboBoxItem Content="关闭" Tag="off"/>
            </ComboBox>
            <CheckBox Content="红" x:Name="DoneRed" VerticalAlignment="Center" Foreground="#AAAAAA"/>
            <CheckBox Content="黄" x:Name="DoneYellow" VerticalAlignment="Center" Foreground="#AAAAAA" Margin="8,0,0,0"/>
            <CheckBox Content="绿" x:Name="DoneGreen" VerticalAlignment="Center" Foreground="#AAAAAA" Margin="8,0,0,0" IsChecked="True"/>
        </StackPanel>

        <!-- JustDone -->
        <StackPanel Orientation="Horizontal" Margin="0,4">
            <TextBlock Text="刚完成:" Width="50" VerticalAlignment="Center" Foreground="#AAAAAA"/>
            <Border x:Name="JustDoneColorPreview" Width="24" Height="24"
                    Background="#32C850" BorderBrush="#555555" BorderThickness="1"
                    Margin="0,0,6,0" Cursor="Hand"
                    MouseLeftButtonDown="ColorPreview_MouseLeftButtonDown"/>
            <ComboBox x:Name="JustDoneMode" Width="65" Margin="0,0,6,0"
                      SelectedIndex="1"
                      Background="#3D3D3D" Foreground="White" BorderBrush="#555555">
                <ComboBoxItem Content="常亮" Tag="on"/>
                <ComboBoxItem Content="闪烁" Tag="blink"/>
                <ComboBoxItem Content="关闭" Tag="off"/>
            </ComboBox>
            <CheckBox Content="红" x:Name="JustDoneRed" VerticalAlignment="Center" Foreground="#AAAAAA"/>
            <CheckBox Content="黄" x:Name="JustDoneYellow" VerticalAlignment="Center" Foreground="#AAAAAA" Margin="8,0,0,0"/>
            <CheckBox Content="绿" x:Name="JustDoneGreen" VerticalAlignment="Center" Foreground="#AAAAAA" Margin="8,0,0,0" IsChecked="True"/>
        </StackPanel>

        <!-- 恢复默认按钮 -->
        <StackPanel Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,12,0,0">
            <Button Content="恢复默认颜色"
                    Background="#555555"
                    Foreground="White"
                    BorderThickness="0"
                    Padding="12,6"
                    FontSize="12"
                    Cursor="Hand"
                    Click="ResetColors_Click"/>
        </StackPanel>
    </StackPanel>
</Border>
```

- [ ] **Step 3: 更新 Grid.RowDefinitions**

将 Grid 的行定义从 5 行改为 6 行：

```xml
<Grid.RowDefinitions>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="*"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
    <RowDefinition Height="Auto"/>
</Grid.RowDefinitions>
```

更新行号：
- 颜色配置区域的 Grid.Row 改为 "4"
- 按钮区域的 Grid.Row 改为 "5"

- [ ] **Step 4: 验证编译通过**

Run: `dotnet build ClaudeStatusLight/ClaudeStatusLight.csproj`
Expected: BUILD SUCCEEDED

- [ ] **Step 5: Commit**

```bash
git add ClaudeStatusLight/SettingsWindow.xaml
git commit -m "feat: add color configuration UI to SettingsWindow"
```

---

### Task 6: 添加 Settings 代码逻辑

**Files:**
- Modify: `ClaudeStatusLight/SettingsWindow.xaml.cs`

- [ ] **Step 1: 添加颜色预览点击事件处理**

```csharp
private void ColorPreview_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
{
    if (sender is not Border border) return;

    var dialog = new System.Windows.Forms.ColorDialog();
    var currentColor = ((SolidColorBrush)border.Background).Color;
    dialog.Color = currentColor.ToWinFormsColor();

    if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
    {
        var wpfColor = dialog.Color.ToWpfColor();
        border.Background = new SolidColorBrush(wpfColor);
    }
}
```

- [ ] **Step 2: 添加 LoadStateDisplayConfig 方法**

```csharp
private void LoadStateDisplayConfig()
{
    var stateDisplay = _config.StateDisplay.Count > 0
        ? _config.StateDisplay
        : DefaultColors.StateDisplay;

    if (stateDisplay.TryGetValue("standby", out var standby))
    {
        StandbyColorPreview.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(standby.Color));
        StandbyMode.SelectedIndex = standby.Mode == "on" ? 0 : standby.Mode == "blink" ? 1 : 2;
        StandbyRed.IsChecked = standby.Lights.Contains("red");
        StandbyYellow.IsChecked = standby.Lights.Contains("yellow");
        StandbyGreen.IsChecked = standby.Lights.Contains("green");
    }

    if (stateDisplay.TryGetValue("error", out var error))
    {
        ErrorColorPreview.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(error.Color));
        ErrorMode.SelectedIndex = error.Mode == "on" ? 0 : error.Mode == "blink" ? 1 : 2;
        ErrorRed.IsChecked = error.Lights.Contains("red");
        ErrorYellow.IsChecked = error.Lights.Contains("yellow");
        ErrorGreen.IsChecked = error.Lights.Contains("green");
    }

    if (stateDisplay.TryGetValue("need_input", out var needInput))
    {
        NeedInputColorPreview.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(needInput.Color));
        NeedInputMode.SelectedIndex = needInput.Mode == "on" ? 0 : needInput.Mode == "blink" ? 1 : 2;
        NeedInputRed.IsChecked = needInput.Lights.Contains("red");
        NeedInputYellow.IsChecked = needInput.Lights.Contains("yellow");
        NeedInputGreen.IsChecked = needInput.Lights.Contains("green");
    }

    if (stateDisplay.TryGetValue("thinking", out var thinking))
    {
        ThinkingColorPreview.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(thinking.Color));
        ThinkingMode.SelectedIndex = thinking.Mode == "on" ? 0 : thinking.Mode == "blink" ? 1 : 2;
        ThinkingRed.IsChecked = thinking.Lights.Contains("red");
        ThinkingYellow.IsChecked = thinking.Lights.Contains("yellow");
        ThinkingGreen.IsChecked = thinking.Lights.Contains("green");
    }

    if (stateDisplay.TryGetValue("done", out var done))
    {
        DoneColorPreview.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(done.Color));
        DoneMode.SelectedIndex = done.Mode == "on" ? 0 : done.Mode == "blink" ? 1 : 2;
        DoneRed.IsChecked = done.Lights.Contains("red");
        DoneYellow.IsChecked = done.Lights.Contains("yellow");
        DoneGreen.IsChecked = done.Lights.Contains("green");
    }

    if (stateDisplay.TryGetValue("just_done", out var justDone))
    {
        JustDoneColorPreview.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(justDone.Color));
        JustDoneMode.SelectedIndex = justDone.Mode == "on" ? 0 : justDone.Mode == "blink" ? 1 : 2;
        JustDoneRed.IsChecked = justDone.Lights.Contains("red");
        JustDoneYellow.IsChecked = justDone.Lights.Contains("yellow");
        JustDoneGreen.IsChecked = justDone.Lights.Contains("green");
    }
}
```

- [ ] **Step 3: 在构造函数中调用 LoadStateDisplayConfig**

在构造函数末尾添加：

```csharp
LoadStateDisplayConfig();
```

- [ ] **Step 4: 添加重置按钮事件处理**

```csharp
private void ResetColors_Click(object sender, RoutedEventArgs e)
{
    var result = MessageBox.Show(
        "确定要恢复默认颜色配置吗？",
        "确认",
        MessageBoxButton.YesNo,
        MessageBoxImage.Question);

    if (result == MessageBoxResult.Yes)
    {
        _config.StateDisplay = new Dictionary<string, StateDisplayConfig>(DefaultColors.StateDisplay);
        LoadStateDisplayConfig();
        MessageBox.Show("已恢复默认颜色配置", "提示");
    }
}
```

- [ ] **Step 5: 添加 GetStateDisplayConfigFromUI 辅助方法**

```csharp
private StateDisplayConfig GetStateDisplayConfigFromUI(
    Border colorPreview, ComboBox modeCombo,
    CheckBox redCheck, CheckBox yellowCheck, CheckBox greenCheck)
{
    var color = ((SolidColorBrush)colorPreview.Background).Color;
    var mode = ((ComboBoxItem)modeCombo.SelectedItem).Tag.ToString() ?? "off";
    var lights = new List<string>();
    if (redCheck.IsChecked == true) lights.Add("red");
    if (yellowCheck.IsChecked == true) lights.Add("yellow");
    if (greenCheck.IsChecked == true) lights.Add("green");

    return new StateDisplayConfig
    {
        Color = $"#{color.R:X2}{color.G:X2}{color.B:X2}",
        Mode = mode,
        Lights = lights
    };
}
```

- [ ] **Step 6: 更新 Save_Click 方法**

在 Save_Click 方法中，在保存之前添加：

```csharp
config.StateDisplay = new Dictionary<string, StateDisplayConfig>
{
    ["standby"] = GetStateDisplayConfigFromUI(StandbyColorPreview, StandbyMode, StandbyRed, StandbyYellow, StandbyGreen),
    ["error"] = GetStateDisplayConfigFromUI(ErrorColorPreview, ErrorMode, ErrorRed, ErrorYellow, ErrorGreen),
    ["need_input"] = GetStateDisplayConfigFromUI(NeedInputColorPreview, NeedInputMode, NeedInputRed, NeedInputYellow, NeedInputGreen),
    ["thinking"] = GetStateDisplayConfigFromUI(ThinkingColorPreview, ThinkingMode, ThinkingRed, ThinkingYellow, ThinkingGreen),
    ["done"] = GetStateDisplayConfigFromUI(DoneColorPreview, DoneMode, DoneRed, DoneYellow, DoneGreen),
    ["just_done"] = GetStateDisplayConfigFromUI(JustDoneColorPreview, JustDoneMode, JustDoneRed, JustDoneYellow, JustDoneGreen)
};
```

- [ ] **Step 7: 验证编译通过**

Run: `dotnet build ClaudeStatusLight/ClaudeStatusLight.csproj`
Expected: BUILD SUCCEEDED

- [ ] **Step 8: Commit**

```bash
git add ClaudeStatusLight/SettingsWindow.xaml.cs
git commit -m "feat: add color picker and reset functionality to SettingsWindow"
```

---

### Task 7: 集成测试

- [ ] **Step 1: 构建并运行应用**

Run: `dotnet build -c Release ClaudeStatusLight/ClaudeStatusLight.csproj`
Expected: BUILD SUCCEEDED

- [ ] **Step 2: 测试默认配置**

1. 运行应用
2. 触发不同状态（通过修改 status.json）
3. 验证灯的颜色和模式与默认配置一致

- [ ] **Step 3: 测试自定义配置**

1. 打开设置界面
2. 修改某个状态的颜色（点击颜色预览，选择新颜色）
3. 修改灯模式和灯选择
4. 保存配置
5. 触发对应状态
6. 验证灯显示与自定义配置一致

- [ ] **Step 4: 测试重置功能**

1. 打开设置界面
2. 点击"恢复默认颜色"按钮
3. 确认对话框
4. 验证所有配置恢复为默认值
5. 保存并验证灯显示恢复默认

- [ ] **Step 5: 测试向后兼容**

1. 删除 tool-config.json 中的 stateDisplay 配置
2. 重启应用
3. 验证应用使用默认配置正常工作

- [ ] **Step 6: Final Commit**

```bash
git add -A
git commit -m "feat: complete configurable color-state mapping feature"
```

---

## 验收标准

1. 用户可以在设置界面为每个状态选择自定义颜色
2. 用户可以为每个状态选择灯模式（常亮/闪烁/关闭）
3. 用户可以为每个状态选择使用哪些灯（红/黄/绿，可多选）
4. 用户可以点击"恢复默认颜色"按钮重置所有配置
5. 配置保存到 tool-config.json 的 stateDisplay 字段
6. 缺失配置时使用默认值，保证向后兼容
7. 灯的颜色和模式正确反映配置
