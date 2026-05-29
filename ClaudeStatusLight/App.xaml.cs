using System;
using System.Threading;
using System.Windows;

namespace ClaudeStatusLight;

public partial class App : Application
{
    private static Mutex? _mutex;
    private static bool _ownsMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        const string mutexName = "Global\\ClaudeStatusLight_SingleInstance";
        _mutex = new Mutex(true, mutexName, out bool createdNew);

        if (!createdNew)
        {
            MessageBox.Show("Claude Status Light 已在运行中。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        _ownsMutex = true;
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_ownsMutex)
        {
            try
            {
                _mutex?.ReleaseMutex();
            }
            catch { }
        }
        _mutex?.Dispose();
        base.OnExit(e);
    }
}
