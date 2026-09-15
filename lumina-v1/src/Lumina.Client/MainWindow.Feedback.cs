using System.Windows;
using System.Windows.Controls;
using Lumina.Client.Ui;

namespace Lumina.Client;

public partial class MainWindow
{
    private readonly LuminaFeedback _feedback = new();
    private LuminaNotificationHost? _feedbackHost;

    internal LuminaFeedback Feedback => _feedback;

    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        AttachFeedbackHost();
    }

    private void AttachFeedbackHost()
    {
        if (_feedbackHost is not null || Content is not Grid root) return;

        _feedbackHost = new LuminaNotificationHost
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        Grid.SetColumn(_feedbackHost, 0);
        Grid.SetColumnSpan(_feedbackHost, Math.Max(1, root.ColumnDefinitions.Count));
        Grid.SetRow(_feedbackHost, 0);
        Grid.SetRowSpan(_feedbackHost, Math.Max(1, root.RowDefinitions.Count));
        Panel.SetZIndex(_feedbackHost, 1000);
        root.Children.Add(_feedbackHost);
        _feedbackHost.Attach(_feedback);
    }

    internal void ShowFeedback(NotificationKind kind, string title, string message) =>
        _feedback.Show(kind, title, message);
}
