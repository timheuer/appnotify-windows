namespace AppNotify.Services;

public sealed class PollingService
{
    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private TimeSpan _interval;
    private readonly Func<Task> _action;

    public PollingService(TimeSpan interval, Func<Task> action)
    {
        _interval = interval;
        _action = action;
    }

    public void Start()
    {
        Stop();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _timer = new PeriodicTimer(_interval);

        // Run immediately, then on interval
        _ = Task.Run(async () =>
        {
            await _action();
            try
            {
                while (await _timer.WaitForNextTickAsync(token))
                {
                    await _action();
                }
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        _timer?.Dispose();
        _timer = null;
    }

    public void UpdateInterval(TimeSpan newInterval)
    {
        _interval = newInterval;
        if (_timer is not null)
        {
            Stop();
            Start();
        }
    }
}
