using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;

namespace ClaudeStatusLight;

public class TrayIconManager : IDisposable
{
    private TaskbarIcon? _trayIcon;
    private readonly string _configPath;
    private readonly Action _onClose;

    public TrayIconManager(string configPath, Action onClose)
    {
        _configPath = configPath;
        _onClose = onClose;
        CreateTrayIcon();
    }

    private void CreateTrayIcon()
    {
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Claude Status Light - 待机中",
            Visibility = Visibility.Visible
        };

        // Create context menu
        var contextMenu = new ContextMenu();

        var settingsItem = new MenuItem { Header = "设置" };
        settingsItem.Click += (s, e) => OpenSettings();

        var closeItem = new MenuItem { Header = "关闭" };
        closeItem.Click += (s, e) => _onClose();

        contextMenu.Items.Add(settingsItem);
        contextMenu.Items.Add(new Separator());
        contextMenu.Items.Add(closeItem);

        _trayIcon.ContextMenu = contextMenu;

        // Set icon
        SetDefaultIcon();

        // Double-click to open settings
        _trayIcon.TrayMouseDoubleClick += (s, e) => OpenSettings();
    }

    private void SetDefaultIcon()
    {
        try
        {
            var icon = IconGenerator.CreateTrafficLightIcon(ClaudeState.Standby);
            _trayIcon.Icon = icon;
        }
        catch
        {
            _trayIcon.Icon = SystemIcons.Application;
        }
    }

    public void UpdateState(ClaudeState state, ToolType tool)
    {
        if (_trayIcon == null) return;

        var icon = IconGenerator.CreateTrafficLightIcon(state);
        _trayIcon.Icon = icon;

        var stateText = StateDisplay.GetLabel(state);
        var toolText = tool != ToolType.Unknown ? $" [{StateDisplay.GetToolDisplayName(tool)}]" : "";
        _trayIcon.ToolTipText = $"Claude Status Light{toolText} - {stateText}";
    }

    private void OpenSettings()
    {
        try
        {
            var settingsWindow = new SettingsWindow(_configPath);
            settingsWindow.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"打开设置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void Dispose()
    {
        _trayIcon?.Dispose();
    }
}
