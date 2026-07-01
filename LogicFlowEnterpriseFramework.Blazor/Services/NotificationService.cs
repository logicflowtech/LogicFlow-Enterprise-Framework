namespace LogicFlowEnterpriseFramework.Blazor.Services;

public sealed class NotificationService : IDisposable
{
    private readonly List<NotificationMessage> _messages = [];
    private readonly Dictionary<Guid, CancellationTokenSource> _timers = [];

    public event Action? Changed;

    public IReadOnlyList<NotificationMessage> Messages => _messages;

    public void Success(string message, string? title = null, int durationMs = 4000)
    {
        Push(NotificationLevel.Success, message, title ?? "Success", durationMs);
    }

    public void Error(string message, string? title = null, int durationMs = 6500)
    {
        Push(NotificationLevel.Error, message, title ?? "Action failed", durationMs);
    }

    public void Info(string message, string? title = null, int durationMs = 4500)
    {
        Push(NotificationLevel.Info, message, title ?? "Notice", durationMs);
    }

    public void Dismiss(Guid id)
    {
        if (_timers.Remove(id, out var timer))
        {
            timer.Cancel();
            timer.Dispose();
        }

        if (_messages.RemoveAll(message => message.Id == id) > 0)
        {
            Changed?.Invoke();
        }
    }

    private void Push(NotificationLevel level, string message, string title, int durationMs)
    {
        var notification = new NotificationMessage(Guid.NewGuid(), level, title, message);

        _messages.Add(notification);
        Changed?.Invoke();

        if (durationMs <= 0)
        {
            return;
        }

        var cancellationTokenSource = new CancellationTokenSource();
        _timers[notification.Id] = cancellationTokenSource;
        _ = DismissAfterDelayAsync(notification.Id, durationMs, cancellationTokenSource.Token);
    }

    private async Task DismissAfterDelayAsync(Guid id, int durationMs, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(durationMs, cancellationToken);
            Dismiss(id);
        }
        catch (TaskCanceledException)
        {
        }
    }

    public void Dispose()
    {
        foreach (var timer in _timers.Values)
        {
            timer.Cancel();
            timer.Dispose();
        }

        _timers.Clear();
        _messages.Clear();
    }
}

public sealed record NotificationMessage(Guid Id, NotificationLevel Level, string Title, string Message);

public enum NotificationLevel
{
    Success,
    Error,
    Info
}
