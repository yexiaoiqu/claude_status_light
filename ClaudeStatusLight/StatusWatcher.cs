using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ClaudeStatusLight;

public class StatusChangedEventArgs : EventArgs
{
    public ClaudeState State { get; }
    public string? Message { get; }
    public ToolType ActiveTool { get; }

    public StatusChangedEventArgs(ClaudeState state, string? message, ToolType activeTool)
    {
        State = state;
        Message = message;
        ActiveTool = activeTool;
    }
}

public class ToolStatusWatcher
{
    public string StatusFilePath { get; }
    public string ToolName { get; }
    public ToolType ToolType { get; }
    public long LastTimestamp { get; set; }
    public ClaudeState LastState { get; set; } = ClaudeState.Standby;
    public DateTime LastWriteTime { get; set; } = DateTime.MinValue;

    public ToolStatusWatcher(string statusFilePath, string toolName, ToolType toolType)
    {
        StatusFilePath = statusFilePath;
        ToolName = toolName;
        ToolType = toolType;
    }
}

public class StatusWatcher : IDisposable
{
    private readonly List<ToolStatusWatcher> _watchers = new();
    private readonly int _activeToolTimeoutSeconds;
    private ToolType _activeTool = ToolType.Unknown;
    private ClaudeState _currentState = ClaudeState.Standby;
    private string? _currentMessage;

    public event EventHandler<StatusChangedEventArgs>? StatusChanged;

    public StatusWatcher(List<ToolConfig> toolConfigs, int activeToolTimeoutSeconds = 60)
    {
        _activeToolTimeoutSeconds = activeToolTimeoutSeconds;

        foreach (var config in toolConfigs)
        {
            var toolType = config.ToolType?.ToLower() switch
            {
                "claude" => ToolType.Claude,
                "trae" => ToolType.Trae,
                _ => ToolType.Unknown
            };
            _watchers.Add(new ToolStatusWatcher(config.StatusFile, config.Name, toolType));
        }
    }

    // Legacy constructor for backward compatibility
    public StatusWatcher(string statusFilePath) : this(
        new List<ToolConfig>
        {
            new() { Name = "Claude", StatusFile = statusFilePath, ToolType = "claude" }
        }, 60)
    {
    }

    public void Poll()
    {
        ToolStatusWatcher? mostActive = null;
        var mostRecentWrite = DateTime.MinValue;

        foreach (var watcher in _watchers)
        {
            try
            {
                if (!File.Exists(watcher.StatusFilePath)) continue;

                string json;
                using (var fs = new FileStream(watcher.StatusFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs))
                {
                    json = reader.ReadToEnd();
                }

                if (string.IsNullOrWhiteSpace(json)) continue;

                var data = JsonSerializer.Deserialize<StatusData>(json);
                if (data == null) continue;

                var lastWrite = File.GetLastWriteTimeUtc(watcher.StatusFilePath);
                watcher.LastWriteTime = lastWrite;

                var newState = data.GetClaudeState();
                if (data.Timestamp > watcher.LastTimestamp || newState != watcher.LastState)
                {
                    watcher.LastTimestamp = data.Timestamp;
                    watcher.LastState = newState;

                    if (lastWrite > mostRecentWrite)
                    {
                        mostRecentWrite = lastWrite;
                        mostActive = watcher;
                    }
                }
                else if (lastWrite > mostRecentWrite)
                {
                    mostRecentWrite = lastWrite;
                    mostActive = watcher;
                }
            }
            catch
            {
                // ignore read/parse errors, retry next tick
            }
        }

        // Auto-detect active tool based on most recent file update
        if (mostActive != null && mostRecentWrite != DateTime.MinValue)
        {
            var age = DateTime.UtcNow - mostRecentWrite;
            if (age < TimeSpan.FromSeconds(_activeToolTimeoutSeconds))
            {
                var newTool = mostActive.ToolType;
                var newState = mostActive.LastState;
                var newMessage = _currentMessage;

                // Read the current message from the active tool
                try
                {
                    if (File.Exists(mostActive.StatusFilePath))
                    {
                        string json;
                        using (var fs = new FileStream(mostActive.StatusFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        using (var reader = new StreamReader(fs))
                        {
                            json = reader.ReadToEnd();
                        }
                        var data = JsonSerializer.Deserialize<StatusData>(json);
                        if (data != null)
                        {
                            newMessage = data.Message;
                        }
                    }
                }
                catch { }

                if (newTool != _activeTool || newState != _currentState)
                {
                    _activeTool = newTool;
                    _currentState = newState;
                    _currentMessage = newMessage;
                    StatusChanged?.Invoke(this, new StatusChangedEventArgs(newState, newMessage, newTool));
                }
            }
            else
            {
                // No active tool, go to standby
                if (_activeTool != ToolType.Unknown)
                {
                    _activeTool = ToolType.Unknown;
                    _currentState = ClaudeState.Standby;
                    _currentMessage = null;
                    StatusChanged?.Invoke(this, new StatusChangedEventArgs(ClaudeState.Standby, null, ToolType.Unknown));
                }
            }
        }
    }

    public void Dispose() { }
}
