using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace ClaudeStatusLight;

public partial class SettingsWindow : Window
{
    private readonly string _configPath;
    private readonly ObservableCollection<ToolConfigItem> _tools = new();
    private AppConfig _config;

    public SettingsWindow(string configPath)
    {
        InitializeComponent();
        _configPath = configPath;
        _config = LoadConfig();

        // Initialize UI
        AutoDetectCheckBox.IsChecked = _config.AutoDetect;
        TimeoutTextBox.Text = _config.ActiveToolTimeoutSeconds.ToString();

        // Load tools
        foreach (var tool in _config.Tools)
        {
            _tools.Add(new ToolConfigItem
            {
                Name = tool.Name,
                StatusFile = tool.StatusFile,
                ToolType = tool.ToolType
            });
        }

        ToolsList.ItemsSource = _tools;
    }

    private AppConfig LoadConfig()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch { }
        return new AppConfig();
    }

    private void ScanButton_Click(object sender, RoutedEventArgs e)
    {
        var foundCount = 0;
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // Common paths for AI coding tools
        var scanPaths = new List<(string name, string path, string toolType)>
        {
            // Claude
            ("Claude", Path.Combine(appData, "Claude"), "claude"),
            ("Claude", Path.Combine(localAppData, "Claude"), "claude"),

            // Trae
            ("Trae", Path.Combine(appData, "Trae"), "trae"),
            ("Trae", Path.Combine(localAppData, "Trae"), "trae"),
            ("Trae", Path.Combine(appData, "ByteDance", "Trae"), "trae"),

            // Codex
            ("Codex", Path.Combine(appData, "codex"), "other"),
            ("Codex", Path.Combine(localAppData, "codex"), "other"),
            ("Codex", Path.Combine(appData, "OpenAI", "codex"), "other"),
            ("Codex", Path.Combine(userProfile, ".codex"), "other"),

            // Cursor
            ("Cursor", Path.Combine(appData, "Cursor"), "other"),
            ("Cursor", Path.Combine(localAppData, "Cursor"), "other"),

            // Windsurf
            ("Windsurf", Path.Combine(appData, "Windsurf"), "other"),
            ("Windsurf", Path.Combine(localAppData, "Windsurf"), "other"),
        };

        // Also scan project directory
        var exePath = Environment.ProcessPath ?? "";
        var exeDir = Path.GetDirectoryName(exePath) ?? ".";
        var rootDir = FindProjectRoot(exeDir);
        scanPaths.Add(("Claude (项目)", rootDir, "claude"));

        foreach (var (name, basePath, toolType) in scanPaths)
        {
            if (!Directory.Exists(basePath)) continue;

            // Search for status.json files
            try
            {
                var statusFiles = Directory.GetFiles(basePath, "status.json", SearchOption.AllDirectories);
                foreach (var statusFile in statusFiles)
                {
                    // Check if already added
                    var isDuplicate = false;
                    foreach (var existing in _tools)
                    {
                        if (existing.StatusFile == statusFile)
                        {
                            isDuplicate = true;
                            break;
                        }
                    }

                    if (!isDuplicate)
                    {
                        _tools.Add(new ToolConfigItem
                        {
                            Name = name,
                            StatusFile = statusFile,
                            ToolType = toolType
                        });
                        foundCount++;
                    }
                }
            }
            catch { }
        }

        ScanResultText.Text = foundCount > 0
            ? $"找到 {foundCount} 个状态文件"
            : "未找到新的状态文件";
    }

    private static string FindProjectRoot(string startDir)
    {
        var dir = startDir;
        for (int i = 0; i < 6; i++)
        {
            if (File.Exists(Path.Combine(dir, "status.json")))
                return dir;
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        return startDir;
    }

    private void DeleteTool_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ToolConfigItem item)
        {
            _tools.Remove(item);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        // Validate
        foreach (var tool in _tools)
        {
            if (string.IsNullOrWhiteSpace(tool.Name))
            {
                MessageBox.Show("工具名称不能为空", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(tool.StatusFile))
            {
                MessageBox.Show($"工具 '{tool.Name}' 的状态文件路径不能为空", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }

        if (!int.TryParse(TimeoutTextBox.Text, out var timeout) || timeout < 10)
        {
            MessageBox.Show("超时时间必须是大于等于 10 的数字", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Build config
        var config = new AppConfig
        {
            AutoDetect = AutoDetectCheckBox.IsChecked ?? true,
            ActiveToolTimeoutSeconds = timeout,
            Tools = new List<ToolConfig>()
        };

        foreach (var tool in _tools)
        {
            config.Tools.Add(new ToolConfig
            {
                Name = tool.Name,
                StatusFile = tool.StatusFile,
                ToolType = tool.ToolType
            });
        }

        // Save
        try
        {
            var dir = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);

            DialogResult = true;
            Close();
        }
        catch (System.Exception ex)
        {
            MessageBox.Show($"保存失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

public class ToolConfigItem
{
    public string Name { get; set; } = "";
    public string StatusFile { get; set; } = "";
    public string ToolType { get; set; } = "claude";
}
