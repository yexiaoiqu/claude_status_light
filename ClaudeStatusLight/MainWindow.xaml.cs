using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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

    private static readonly Color RedColor = Color.FromRgb(220, 50, 50);
    private static readonly Color YellowColor = Color.FromRgb(240, 200, 40);
    private static readonly Color GreenColor = Color.FromRgb(50, 200, 80);
    private static readonly Color OffGlow = Color.FromRgb(0, 0, 0);
    private static readonly Color OffFill = Color.FromRgb(40, 40, 40);

    public MainWindow()
    {
        InitializeComponent();

        _settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "window-settings.json");
        _settingsPath = Path.GetFullPath(_settingsPath);
        LoadWindowPosition();

        var statusFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "status.json");
        statusFile = Path.GetFullPath(statusFile);
        _watcher = new StatusWatcher(statusFile, 500);
        _watcher.StatusChanged += OnStatusChanged;

        _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _blinkTimer.Tick += BlinkTimer_Tick;

        // Timer to detect stale states (process crash/exit without Stop hook)
        _staleCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        _staleCheckTimer.Tick += (s, e) => CheckStaleState();
        _staleCheckTimer.Start();

        // Read current status from file on startup
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
            if (File.Exists(statusFile))
            {
                var json = File.ReadAllText(statusFile);
                var data = JsonSerializer.Deserialize<StatusData>(json);
                if (data != null)
                {
                    UpdateDisplay(data.GetClaudeState());
                }
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

        try
        {
            var statusFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "status.json");
            statusFile = Path.GetFullPath(statusFile);
            if (!File.Exists(statusFile)) return;

            var lastWrite = File.GetLastWriteTimeUtc(statusFile);
            var elapsed = DateTime.UtcNow - lastWrite;
            if (elapsed > TimeSpan.FromSeconds(60))
            {
                UpdateDisplay(ClaudeState.Standby);
            }
        }
        catch
        {
            // ignore errors
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
        catch
        {
            // use default position from XAML
        }
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
        Dispatcher.Invoke(() => UpdateDisplay(e.State));
    }

    private void UpdateDisplay(ClaudeState state)
    {
        _currentState = state;
        _blinkOn = true;

        var redMode = StateDisplay.GetRedMode(state);
        var yellowMode = StateDisplay.GetYellowMode(state);
        var greenMode = StateDisplay.GetGreenMode(state);

        ApplyLight(RedLight, RedGlow, RedColor, redMode, true);
        ApplyLight(YellowLight, YellowGlow, YellowColor, yellowMode, true);
        ApplyLight(GreenLight, GreenGlow, GreenColor, greenMode, true);

        StatusText.Text = StateDisplay.GetLabel(state);

        if (redMode == LightMode.Blink || yellowMode == LightMode.Blink || greenMode == LightMode.Blink)
            _blinkTimer.Start();
        else
            _blinkTimer.Stop();
    }

    private void ApplyLight(UIElement ellipse, DropShadowEffect glow, Color color, LightMode mode, bool isOn)
    {
        var fill = ellipse is System.Windows.Shapes.Shape shape ? shape : null;
        if (fill == null) return;

        switch (mode)
        {
            case LightMode.On:
                fill.Opacity = 1.0;
                fill.Fill = new SolidColorBrush(color);
                glow.Color = color;
                glow.Opacity = 0.8;
                break;
            case LightMode.Blink:
                fill.Opacity = 1.0;
                fill.Fill = isOn ? new SolidColorBrush(color) : new SolidColorBrush(OffFill);
                glow.Color = isOn ? color : OffGlow;
                glow.Opacity = isOn ? 0.8 : 0.0;
                break;
            case LightMode.Off:
                fill.Opacity = 1.0;
                fill.Fill = new SolidColorBrush(OffFill);
                glow.Color = OffGlow;
                glow.Opacity = 0.0;
                break;
        }
    }

    private void BlinkTimer_Tick(object? sender, EventArgs e)
    {
        _blinkOn = !_blinkOn;

        var redMode = StateDisplay.GetRedMode(_currentState);
        var yellowMode = StateDisplay.GetYellowMode(_currentState);
        var greenMode = StateDisplay.GetGreenMode(_currentState);

        if (redMode == LightMode.Blink)
            ApplyLight(RedLight, RedGlow, RedColor, LightMode.Blink, _blinkOn);
        if (yellowMode == LightMode.Blink)
            ApplyLight(YellowLight, YellowGlow, YellowColor, LightMode.Blink, _blinkOn);
        if (greenMode == LightMode.Blink)
            ApplyLight(GreenLight, GreenGlow, GreenColor, LightMode.Blink, _blinkOn);
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
