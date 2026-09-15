using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Lumina.Client.Ui;

public enum NotificationKind
{
    Success,
    Info,
    Warning,
    Error
}

public sealed class LuminaNotification
{
    public Guid Id { get; } = Guid.NewGuid();
    public required NotificationKind Kind { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
}

public sealed class LuminaConfirmation
{
    public required string Title { get; init; }
    public required string Message { get; init; }
    public required string ConfirmText { get; init; }
    public bool Danger { get; init; }
}

public sealed class LuminaFeedback : INotifyPropertyChanged
{
    private TaskCompletionSource<bool>? _confirmationSource;
    private LuminaConfirmation? _activeConfirmation;

    public ObservableCollection<LuminaNotification> Notifications { get; } = [];

    public LuminaConfirmation? ActiveConfirmation
    {
        get => _activeConfirmation;
        private set
        {
            if (ReferenceEquals(_activeConfirmation, value)) return;
            _activeConfirmation = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasConfirmation));
        }
    }

    public bool HasConfirmation => ActiveConfirmation is not null;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<LuminaNotification>? NotificationRaised;

    public void Show(NotificationKind kind, string title, string message)
    {
        var item = new LuminaNotification
        {
            Kind = kind,
            Title = title?.Trim() ?? string.Empty,
            Message = message?.Trim() ?? string.Empty
        };
        Notifications.Add(item);
        NotificationRaised?.Invoke(this, item);
    }

    public void Dismiss(LuminaNotification notification)
    {
        if (notification is null) return;
        Notifications.Remove(notification);
    }

    public Task<bool> ConfirmAsync(string title, string message, string confirmText, bool danger = false)
    {
        if (_confirmationSource is not null)
        {
            var previous = _confirmationSource;
            _confirmationSource = null;
            previous.TrySetResult(false);
        }

        _confirmationSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        ActiveConfirmation = new LuminaConfirmation
        {
            Title = title,
            Message = message,
            ConfirmText = confirmText,
            Danger = danger
        };
        return _confirmationSource.Task;
    }

    public void ResolveConfirmation(bool accepted)
    {
        var source = _confirmationSource;
        _confirmationSource = null;
        ActiveConfirmation = null;
        source?.TrySetResult(accepted);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
