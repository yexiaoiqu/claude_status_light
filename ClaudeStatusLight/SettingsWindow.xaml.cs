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

            // Claude dotfile
            ("Claude", Path.Combine(userProfile, ".claude"), "claude"),

            // Trae dotfile
            ("Trae", Path.Combine(userProfile, ".trae"), "trae"),

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

        if (rootDir != null)
        {
            // Auto-create status.json and hooks if missing
            var setupMessages = SetupProjectIfNeeded(rootDir);
            if (setupMessages.Count > 0)
            {
                ScanResultText.Text = string.Join("\n", setupMessages);
            }
            scanPaths.Add(("Claude (项目)", rootDir, "claude"));
        }

        foreach (var (name, basePath, toolType) in scanPaths)
        {
            if (!Directory.Exists(basePath)) continue;

            // Search for status.json files
            try
            {
                var statusFiles = new List<string>();
                // Direct search
                statusFiles.AddRange(Directory.GetFiles(basePath, "status.json", SearchOption.TopDirectoryOnly));
                // Recursive search
                try { statusFiles.AddRange(Directory.GetFiles(basePath, "status.json", SearchOption.AllDirectories)); } catch { }
                // Also check .claude subdirectory
                var claudeSubDir = Path.Combine(basePath, ".claude");
                if (Directory.Exists(claudeSubDir))
                {
                    try { statusFiles.AddRange(Directory.GetFiles(claudeSubDir, "status.json", SearchOption.AllDirectories)); } catch { }
                }
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

        var resultLines = new List<string>();
        if (!string.IsNullOrEmpty(ScanResultText.Text))
            resultLines.Add(ScanResultText.Text);
        resultLines.Add(foundCount > 0
            ? $"找到 {foundCount} 个状态文件"
            : "未找到新的状态文件");
        ScanResultText.Text = string.Join("\n", resultLines);
    }

    private static string? FindProjectRoot(string startDir)
    {
        var dir = startDir;
        for (int i = 0; i < 6; i++)
        {
            if (File.Exists(Path.Combine(dir, "status.json")))
                return dir;
            if (File.Exists(Path.Combine(dir, ".claude", "status.json")))
                return dir;
            if (File.Exists(Path.Combine(dir, ".claude", "settings.json")))
                return dir;
            var parent = Directory.GetParent(dir);
            if (parent == null) break;
            dir = parent.FullName;
        }
        return null;
    }

    private static List<string> SetupProjectIfNeeded(string projectDir)
    {
        var messages = new List<string>();
        var statusFile = Path.Combine(projectDir, "status.json");
        if (!File.Exists(statusFile))
        {
            try
            {
                var defaultStatus = "{\n    \"message\": \"\",\n    " +
                    "\"timestamp\": " + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + ",\n    " +
                    "\"state\": \"standby\"\n}";
                File.WriteAllText(statusFile, defaultStatus);
                messages.Add("已创建 status.json");
            }
            catch (Exception ex)
            {
                messages.Add($"创建 status.json 失败: {ex.Message}");
            }
        }

        var hooksDir = Path.Combine(projectDir, "hooks");
        var claudeDir = Path.Combine(projectDir, ".claude");
        var settingsFile = Path.Combine(claudeDir, "settings.json");
        var hookScript = Path.Combine(hooksDir, "claude-hook.ps1");
        var updateScript = Path.Combine(hooksDir, "update-status.ps1");

        var hasHooks = File.Exists(hookScript) && File.Exists(updateScript);
        var hasSettings = false;
        if (File.Exists(settingsFile))
        {
            try
            {
                var content = File.ReadAllText(settingsFile);
                hasSettings = content.Contains("PreToolUse") && content.Contains("claude-hook");
            }
            catch { }
        }

        if (!hasHooks)
        {
            try
            {
                if (!Directory.Exists(hooksDir))
                    Directory.CreateDirectory(hooksDir);

                File.WriteAllText(hookScript,
                    "# Claude Code Hook\n" +
                    "param([string]$Event)\n\n" +
                    "$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path\n" +
                    "$rootDir = Split-Path -Parent $scriptDir\n" +
                    "$statusScript = Join-Path $scriptDir \"update-status.ps1\"\n" +
                    "$lightExe = \"$rootDir\\ClaudeStatusLight\\bin\\Release\\net8.0-windows\\ClaudeStatusLight.exe\"\n\n" +
                    "# Auto-start status light if not running\n" +
                    "if ($Event -eq \"PreToolUse\") {\n" +
                    "    $proc = Get-Process ClaudeStatusLight -ErrorAction SilentlyContinue\n" +
                    "    if (-not $proc -and (Test-Path $lightExe)) {\n" +
                    "        Start-Process $lightExe -WindowStyle Hidden\n" +
                    "    }\n" +
                    "}\n\n" +
                    "if ($Event -eq \"PreToolUse\" -or $Event -eq \"PostToolUse\") {\n" +
                    "    & $statusScript \"thinking\"\n" +
                    "} elseif ($Event -eq \"Notification\") {\n" +
                    "    & $statusScript \"need_input\"\n" +
                    "} elseif ($Event -eq \"Stop\") {\n" +
                    "    & $statusScript \"done\"\n" +
                    "}\n");

                File.WriteAllText(updateScript,
                    "# Claude Code Hook - 状态更新脚本\n" +
                    "# 用法: .\\update-status.ps1 <state> [message]\n" +
                    "# 状态: thinking, just_done, done, need_input, error, standby\n\n" +
                    "param(\n" +
                    "    [Parameter(Mandatory=$true)]\n" +
                    "    [ValidateSet(\"thinking\", \"just_done\", \"done\", \"need_input\", \"error\", \"standby\")]\n" +
                    "    [string]$State,\n\n" +
                    "    [Parameter(Mandatory=$false)]\n" +
                    "    [string]$Message = \"\"\n" +
                    ")\n\n" +
                    "$statusFile = Join-Path (Join-Path $PSScriptRoot \"..\") \"status.json\"\n" +
                    "$statusFile = [System.IO.Path]::GetFullPath($statusFile)\n\n" +
                    "$statusData = @{\n" +
                    "    state     = $State\n" +
                    "    timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()\n" +
                    "    message   = $Message\n" +
                    "} | ConvertTo-Json\n\n" +
                    "# 使用临时文件避免写入冲突，UTF8 无 BOM\n" +
                    "$tempFile = \"$statusFile.tmp\"\n" +
                    "$utf8NoBom = New-Object System.Text.UTF8Encoding $false\n" +
                    "[System.IO.File]::WriteAllText($tempFile, $statusData, $utf8NoBom)\n" +
                    "Move-Item -Path $tempFile -Destination $statusFile -Force\n");

                messages.Add("已创建 hooks 脚本");
            }
            catch (Exception ex)
            {
                messages.Add($"创建 hooks 脚本失败: {ex.Message}");
            }
        }

        if (!hasSettings)
        {
            try
            {
                if (!Directory.Exists(claudeDir))
                    Directory.CreateDirectory(claudeDir);

                var hookCmd = $"powershell -ExecutionPolicy Bypass -File \"{Path.Combine(hooksDir, "claude-hook.ps1")}\"";
                var settings = new
                {
                    hooks = new
                    {
                        PreToolUse = new[] { new { matcher = "", hooks = new[] { new { type = "command", command = hookCmd + " PreToolUse" } } } },
                        PostToolUse = new[] { new { matcher = "", hooks = new[] { new { type = "command", command = hookCmd + " PostToolUse" } } } },
                        Notification = new[] { new { matcher = "", hooks = new[] { new { type = "command", command = hookCmd + " Notification" } } } },
                        Stop = new[] { new { matcher = "", hooks = new[] { new { type = "command", command = hookCmd + " Stop" } } } }
                    }
                };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsFile, json);
                messages.Add("已创建 .claude/settings.json (hooks 配置)");
            }
            catch (Exception ex)
            {
                messages.Add($"创建 hooks 配置失败: {ex.Message}");
            }
        }

        return messages;
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

    private void ColorPreview_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Placeholder - will be implemented in Task 6
    }

    private void ResetColors_Click(object sender, RoutedEventArgs e)
    {
        // Placeholder - will be implemented in Task 6
    }
}

public class ToolConfigItem
{
    public string Name { get; set; } = "";
    public string StatusFile { get; set; } = "";
    public string ToolType { get; set; } = "claude";
}
