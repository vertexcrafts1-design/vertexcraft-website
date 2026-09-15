using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Lumina.Client.Ui;

public partial class LuminaNotificationHost : UserControl
{
    private LuminaFeedback? _feedback;

    public LuminaNotificationHost()
    {
        InitializeComponent();
    }

    public void Attach(LuminaFeedback feedback)
    {
        if (_feedback is not null)
        {
            _feedback.PropertyChanged -= Feedback_PropertyChanged;
            _feedback.NotificationRaised -= Feedback_NotificationRaised;
        }

        _feedback = feedback;
        DataContext = feedback;
        feedback.PropertyChanged += Feedback_PropertyChanged;
        feedback.NotificationRaised += Feedback_NotificationRaised;
        UpdateModal();
    }

    private void Feedback_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(LuminaFeedback.ActiveConfirmation) or nameof(LuminaFeedback.HasConfirmation))
            Dispatcher.Invoke(UpdateModal);
    }

    private async void Feedback_NotificationRaised(object? sender, LuminaNotification notification)
    {
        await Task.Delay(TimeSpan.FromSeconds(5.5));
        if (_feedback is null) return;
        await Dispatcher.InvokeAsync(() => _feedback.Dismiss(notification));
    }

    private void UpdateModal()
    {
        var confirmation = _feedback?.ActiveConfirmation;
        ModalLayer.Visibility = confirmation is null ? Visibility.Collapsed : Visibility.Visible;
        if (confirmation is null) return;

        ConfirmButton.Style = (Style)FindResource(confirmation.Danger ? "LuminaDangerButton" : "LuminaPrimaryButton");
    }

    private void Dismiss_Click(object sender, RoutedEventArgs e)
    {
        if (_feedback is not null && sender is Button { Tag: LuminaNotification item })
            _feedback.Dismiss(item);
    }

    private void CancelConfirmation_Click(object sender, RoutedEventArgs e) => _feedback?.ResolveConfirmation(false);
    private void Confirm_Click(object sender, RoutedEventArgs e) => _feedback?.ResolveConfirmation(true);
}
