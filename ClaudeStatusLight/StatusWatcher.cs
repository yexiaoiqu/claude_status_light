using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

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
    private readonly string _statusDir;
    private readonly int _pollIntervalMs;
    private CancellationTokenSource? _cts;
    private FileSystemWatcher? _fileWatcher;
    private long _lastTimestamp;

    public event EventHandler<StatusChangedEventArgs>? StatusChanged;

    public StatusWatcher(string statusFilePath, int pollIntervalMs = 500)
    {
        _statusFilePath = statusFilePath;
        _statusDir = Path.GetDirectoryName(statusFilePath) ?? ".";
        _pollIntervalMs = pollIntervalMs;
    }

    public void Start()
    {
        // FileSystemWatcher for instant notification
        _fileWatcher = new FileSystemWatcher(_statusDir, "status.json")
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime
        };
        _fileWatcher.Changed += (s, e) => ReadAndNotify();
        _fileWatcher.Created += (s, e) => ReadAndNotify();
        _fileWatcher.EnableRaisingEvents = true;

        // Polling as fallback
        _cts = new CancellationTokenSource();
        Task.Run(() => PollLoop(_cts.Token));
    }

    public void Stop()
    {
        _fileWatcher?.Dispose();
        _cts?.Cancel();
    }

    private void ReadAndNotify()
    {
        try
        {
            if (!File.Exists(_statusFilePath)) return;

            var json = File.ReadAllText(_statusFilePath);
            var data = JsonSerializer.Deserialize<StatusData>(json);

            if (data != null && data.Timestamp > _lastTimestamp)
            {
                _lastTimestamp = data.Timestamp;
                StatusChanged?.Invoke(this, new StatusChangedEventArgs(data.GetClaudeState(), data.Message));
            }
        }
        catch
        {
            // ignore read errors
        }
    }

    private async Task PollLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            ReadAndNotify();
            await Task.Delay(_pollIntervalMs, ct);
        }
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
