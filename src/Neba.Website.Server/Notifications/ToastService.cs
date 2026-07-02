namespace Neba.Website.Server.Notifications;

internal sealed class ToastService : IDisposable
{
    private CancellationTokenSource? _cts;

    public ToastNotification? Current { get; private set; }

    public event Action? OnChange;

    public void Show(string title, string message, NotifySeverity severity)
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        Current = new ToastNotification(title, message, severity);
        NotifyChange();

        _ = AutoDismissAsync(_cts.Token);
    }

    public void Dismiss()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
        Current = null;
        NotifyChange();
    }

    private async Task AutoDismissAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(4), ct);
            Current = null;
            NotifyChange();
        }
        catch (OperationCanceledException)
        {
            // A newer toast replaced this one; nothing to dismiss.
        }
    }

    private void NotifyChange() => OnChange?.Invoke();

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }
}