using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Threading;

namespace ClaudeStatusLight;

public partial class MainWindow : Window
{
    private readonly StatusWatcher _watcher;
    private readonly DispatcherTimer _pollTimer;
    private readonly DispatcherTimer _blinkTimer;
    private readonly DispatcherTimer _staleCheckTimer;
    private readonly string _settingsPath;
    private readonly string _configPath;
    private TrayIconManager? _trayIcon;
    private bool _blinkOn = true;
    private ClaudeState _currentState = ClaudeState.Standby;
    private ToolType _activeTool = ToolType.Unknown;
    private DateTime _lastUpdateTime = DateTime.MinValue;

    private static readonly SolidColorBrush RedBrush = new SolidColorBrush(Color.FromRgb(220, 50, 50));
    private static readonly SolidColorBrush YellowBrush = new SolidColorBrush(Color.FromRgb(240, 200, 40));
    private static readonly SolidColorBrush GreenBrush = new SolidColorBrush(Color.FromRgb(50, 200, 80));
    private static readonly SolidColorBrush OffBrush = new SolidColorBrush(Color.FromRgb(50, 50, 50));

    public MainWindow()
    {
        InitializeComponent();

        var exePath = Environment.ProcessPath ?? "";
        var exeDir = Path.GetDirectoryName(exePath) ?? ".";
        var rootDir = FindProjectRoot(exeDir);

        _settingsPath = Path.Combine(rootDir, "window-settings.json");
        _configPath = Path.Combine(rootDir, "tool-config.json");

        LoadWindowPosition();

        // Load config or use default
        var config = LoadConfig(_configPath);
        var toolConfigs = config.Tools.Count > 0 ? config.Tools : GetDefaultToolConfigs(rootDir);

        // Resolve relative status file paths against project root
        foreach (var tc in toolConfigs)
        {
            if (!Path.IsPathRooted(tc.StatusFile))
            {
                tc.StatusFile = Path.Combine(rootDir, tc.StatusFile);
            }
        }

        _watcher = new StatusWatcher(toolConfigs, config.ActiveToolTimeoutSeconds);
        _watcher.StatusChanged += OnStatusChanged;

        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        _pollTimer.Tick += (s, e) => _watcher.Poll();

        _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _blinkTimer.Tick += BlinkTimer_Tick;

        _staleCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _staleCheckTimer.Tick += (s, e) => CheckStaleState();

        // Initialize tray icon
        _trayIcon = new TrayIconManager(_configPath, () => Close());

        // Load initial status from the first available tool
        LoadInitialStatus(toolConfigs);

        _pollTimer.Start();
        _staleCheckTimer.Start();

        Closed += (s, e) =>
        {
            _pollTimer.Stop();
            _watcher.Dispose();
            _blinkTimer.Stop();
            _staleCheckTimer.Stop();
            _trayIcon?.Dispose();
        };
    }

    private AppConfig LoadConfig(string configPath)
    {
        try
        {
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch { }
        return new AppConfig();
    }

    private List<ToolConfig> GetDefaultToolConfigs(string rootDir)
    {
        return new List<ToolConfig>
        {
            new() { Name = "Claude", StatusFile = Path.Combine(rootDir, "status.json"), ToolType = "claude" }
        };
    }

    private void LoadInitialStatus(List<ToolConfig> toolConfigs)
    {
        foreach (var config in toolConfigs)
        {
            try
            {
                if (!File.Exists(config.StatusFile)) continue;

                string json;
                using (var fs = new FileStream(config.StatusFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs))
                {
                    json = reader.ReadToEnd();
                }

                var data = JsonSerializer.Deserialize<StatusData>(json);
                if (data == null) continue;

                var state = data.GetClaudeState();
                var lastWrite = File.GetLastWriteTimeUtc(config.StatusFile);
                var age = DateTime.UtcNow - lastWrite;

                if (IsStaleState(state) && age > TimeSpan.FromSeconds(60))
                {
                    UpdateDisplay(ClaudeState.Standby, ToolType.Unknown);
                    _trayIcon?.UpdateState(ClaudeState.Standby, ToolType.Unknown);
                }
                else
                {
                    _lastUpdateTime = DateTime.UtcNow;
                    var toolType = config.ToolType?.ToLower() switch
                    {
                        "claude" => ToolType.Claude,
                        "trae" => ToolType.Trae,
                        _ => ToolType.Unknown
                    };
                    UpdateDisplay(state, toolType);
                    _trayIcon?.UpdateState(state, toolType);
                }
                return; // Use first valid status
            }
            catch
            {
                continue;
            }
        }
        UpdateDisplay(ClaudeState.Standby, ToolType.Unknown);
        _trayIcon?.UpdateState(ClaudeState.Standby, ToolType.Unknown);
    }

    private static string FindProjectRoot(string startDir)
    {
        var dir = startDir;
        for (int i = 0; i < 6; i++)
        {
            if (File.Exists(Path.Combine(dir, "status.json")) ||
                File.Exists(Path.Combine(dir, "tool-config.json")))
                return dir;
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        return startDir;
    }

    private static bool IsStaleState(ClaudeState state)
        => state == ClaudeState.Done || state == ClaudeState.JustDone || state == ClaudeState.Thinking;

    private void CheckStaleState()
    {
        if (!IsStaleState(_currentState)) return;

        var elapsed = DateTime.UtcNow - _lastUpdateTime;
        if (elapsed > TimeSpan.FromSeconds(60))
        {
            UpdateDisplay(ClaudeState.Standby, ToolType.Unknown);
        }
    }

    private void LoadWindowPosition()
    {
        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = File.ReadAllText(_settingsPath);
                var pos = JsonSerializer.Deserialize<WindowPosition>(json);
                if (pos != null)
                {
                    Left = pos.Left;
                    Top = pos.Top;
                }
            }
        }
        catch { }
    }

    private void SaveWindowPosition()
    {
        try
        {
            var pos = new WindowPosition { Left = Left, Top = Top };
            var json = JsonSerializer.Serialize(pos, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch { }
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        SaveWindowPosition();
    }

    private void OnStatusChanged(object? sender, StatusChangedEventArgs e)
    {
        _lastUpdateTime = DateTime.UtcNow;
        UpdateDisplay(e.State, e.ActiveTool);
        _trayIcon?.UpdateState(e.State, e.ActiveTool);
    }

    private void UpdateDisplay(ClaudeState state, ToolType activeTool)
    {
        _currentState = state;
        _activeTool = activeTool;
        _blinkOn = true;

        var redMode = StateDisplay.GetRedMode(state);
        var yellowMode = StateDisplay.GetYellowMode(state);
        var greenMode = StateDisplay.GetGreenMode(state);

        ApplyLight(RedLight, RedGlow, RedBrush, redMode, true);
        ApplyLight(YellowLight, YellowGlow, YellowBrush, yellowMode, true);
        ApplyLight(GreenLight, GreenGlow, GreenBrush, greenMode, true);

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

        if (redMode == LightMode.Blink || yellowMode == LightMode.Blink || greenMode == LightMode.Blink)
            _blinkTimer.Start();
        else
            _blinkTimer.Stop();
    }

    private void ApplyLight(Border light, DropShadowEffect glow, SolidColorBrush colorBrush, LightMode mode, bool isOn)
    {
        switch (mode)
        {
            case LightMode.On:
                light.Background = colorBrush;
                glow.Color = colorBrush.Color;
                glow.Opacity = 0.8;
                break;
            case LightMode.Blink:
                light.Background = isOn ? colorBrush : OffBrush;
                glow.Color = isOn ? colorBrush.Color : Colors.Black;
                glow.Opacity = isOn ? 0.8 : 0.0;
                break;
            case LightMode.Off:
                light.Background = OffBrush;
                glow.Color = Colors.Black;
                glow.Opacity = 0.0;
                break;
        }
    }

    private void BlinkTimer_Tick(object? sender, EventArgs e)
    {
        _blinkOn = !_blinkOn;

        if (StateDisplay.GetRedMode(_currentState) == LightMode.Blink)
            ApplyLight(RedLight, RedGlow, RedBrush, LightMode.Blink, _blinkOn);
        if (StateDisplay.GetYellowMode(_currentState) == LightMode.Blink)
            ApplyLight(YellowLight, YellowGlow, YellowBrush, LightMode.Blink, _blinkOn);
        if (StateDisplay.GetGreenMode(_currentState) == LightMode.Blink)
            ApplyLight(GreenLight, GreenGlow, GreenBrush, LightMode.Blink, _blinkOn);
    }

    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
