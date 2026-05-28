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
    private readonly int _pollIntervalMs;
    private CancellationTokenSource? _cts;
    private ClaudeState _lastState = ClaudeState.Standby;
    private long _lastTimestamp;

    public event EventHandler<StatusChangedEventArgs>? StatusChanged;

    public StatusWatcher(string statusFilePath, int pollIntervalMs = 500)
    {
        _statusFilePath = statusFilePath;
        _pollIntervalMs = pollIntervalMs;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        Task.Run(() => PollLoop(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
    }

    private async Task PollLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                if (File.Exists(_statusFilePath))
                {
                    var json = await File.ReadAllTextAsync(_statusFilePath, ct);
                    var data = JsonSerializer.Deserialize<StatusData>(json);

                    if (data != null && data.Timestamp > _lastTimestamp)
                    {
                        _lastTimestamp = data.Timestamp;
                        var newState = data.GetClaudeState();

                        if (newState != _lastState)
                        {
                            _lastState = newState;
                            StatusChanged?.Invoke(this, new StatusChangedEventArgs(newState, data.Message));
                        }
                    }
                }
            }
            catch
            {
                // 忽略读取错误，继续轮询
            }

            await Task.Delay(_pollIntervalMs, ct);
        }
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
