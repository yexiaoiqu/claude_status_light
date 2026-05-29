using System;
using System.IO;
using System.Text.Json;

namespace ClaudeStatusLight;

public class StatusChangedEventArgs : EventArgs
{
    public ClaudeState State { get; }
    public string? Message { get; }

    public StatusChangedEventArgs(ClaudeState state, string? message)
    {
        State = state;
        Message = message;
    }
}

public class StatusWatcher : IDisposable
{
    private readonly string _statusFilePath;
    private long _lastTimestamp;
    private ClaudeState _lastState = ClaudeState.Standby;

    public event EventHandler<StatusChangedEventArgs>? StatusChanged;

    public StatusWatcher(string statusFilePath)
    {
        _statusFilePath = statusFilePath;
    }

    public void Poll()
    {
        try
        {
            if (!File.Exists(_statusFilePath)) return;

            string json;
            using (var fs = new FileStream(_statusFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(fs))
            {
                json = reader.ReadToEnd();
            }

            if (string.IsNullOrWhiteSpace(json)) return;

            var data = JsonSerializer.Deserialize<StatusData>(json);
            if (data == null) return;

            var newState = data.GetClaudeState();
            if (data.Timestamp > _lastTimestamp || newState != _lastState)
            {
                _lastTimestamp = data.Timestamp;
                _lastState = newState;
                StatusChanged?.Invoke(this, new StatusChangedEventArgs(newState, data.Message));
            }
        }
        catch
        {
            // ignore read/parse errors, retry next tick
        }
    }

    public void Dispose() { }
}
