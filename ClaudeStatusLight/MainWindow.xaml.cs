using System;
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
    private readonly DispatcherTimer _blinkTimer;
    private readonly DispatcherTimer _staleCheckTimer;
    private readonly string _settingsPath;
    private bool _blinkOn = true;
    private ClaudeState _currentState = ClaudeState.Standby;
    private DateTime _lastUpdateTime = DateTime.MinValue;

    private static readonly SolidColorBrush RedBrush = new SolidColorBrush(Color.FromRgb(220, 50, 50));
    private static readonly SolidColorBrush YellowBrush = new SolidColorBrush(Color.FromRgb(240, 200, 40));
    private static readonly SolidColorBrush GreenBrush = new SolidColorBrush(Color.FromRgb(50, 200, 80));
    private static readonly SolidColorBrush OffBrush = new SolidColorBrush(Color.FromRgb(50, 50, 50));

    public MainWindow()
    {
        InitializeComponent();

        var rootDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..");
        rootDir = Path.GetFullPath(rootDir);

        _settingsPath = Path.Combine(rootDir, "window-settings.json");
        var statusFile = Path.Combine(rootDir, "status.json");

        LoadWindowPosition();

        _watcher = new StatusWatcher(statusFile, 500);
        _watcher.StatusChanged += OnStatusChanged;

        _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _blinkTimer.Tick += BlinkTimer_Tick;

        _staleCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _staleCheckTimer.Tick += (s, e) => CheckStaleState();
        _staleCheckTimer.Start();

        LoadCurrentStatus(statusFile);
        _watcher.Start();

        Closed += (s, e) =>
        {
            _watcher.Dispose();
            _blinkTimer.Stop();
            _staleCheckTimer.Stop();
        };
    }

    private void LoadCurrentStatus(string statusFile)
    {
        try
        {
            if (!File.Exists(statusFile)) { UpdateDisplay(ClaudeState.Standby); return; }

            string json;
            using (var fs = new FileStream(statusFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            {
                json = reader.ReadToEnd();
            }

            var data = JsonSerializer.Deserialize<StatusData>(json);
            if (data == null) { UpdateDisplay(ClaudeState.Standby); return; }

            var state = data.GetClaudeState();
            var lastWrite = File.GetLastWriteTimeUtc(statusFile);
            var age = DateTime.UtcNow - lastWrite;

            if (IsStaleState(state) && age > TimeSpan.FromSeconds(60))
            {
                UpdateDisplay(ClaudeState.Standby);
            }
            else
            {
                _lastUpdateTime = DateTime.UtcNow;
                UpdateDisplay(state);
            }
        }
        catch
        {
            UpdateDisplay(ClaudeState.Standby);
        }
    }

    private static bool IsStaleState(ClaudeState state)
        => state == ClaudeState.Done || state == ClaudeState.JustDone || state == ClaudeState.Thinking;

    private void CheckStaleState()
    {
        if (!IsStaleState(_currentState)) return;

        var elapsed = DateTime.UtcNow - _lastUpdateTime;
        if (elapsed > TimeSpan.FromSeconds(60))
        {
            UpdateDisplay(ClaudeState.Standby);
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
        Dispatcher.Invoke(() =>
        {
            _lastUpdateTime = DateTime.UtcNow;
            UpdateDisplay(e.State);
        });
    }

    private void UpdateDisplay(ClaudeState state)
    {
        _currentState = state;
        _blinkOn = true;

        var redMode = StateDisplay.GetRedMode(state);
        var yellowMode = StateDisplay.GetYellowMode(state);
        var greenMode = StateDisplay.GetGreenMode(state);

        ApplyLight(RedLight, RedGlow, RedBrush, redMode, true);
        ApplyLight(YellowLight, YellowGlow, YellowBrush, yellowMode, true);
        ApplyLight(GreenLight, GreenGlow, GreenBrush, greenMode, true);

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
