using System.Windows;
using System.Windows.Threading;

namespace Lumina.Client;

/// <summary>
/// Compatibility bridge while legacy call sites are migrated. Because this type lives in
/// Lumina.Client it shadows System.Windows.MessageBox for launcher source files and keeps
/// all user-facing alerts inside the LUMINA window.
/// </summary>
internal static class MessageBox
{
    public static MessageBoxResult Show(
        Window owner,
        string messageBoxText,
        string caption,
        MessageBoxButton button,
        MessageBoxImage icon)
    {
        if (owner is not MainWindow main)
            main = Application.Current?.MainWindow as MainWindow
                   ?? throw new InvalidOperationException("LUMINA feedback host is not available.");

        if (button is MessageBoxButton.YesNo or MessageBoxButton.YesNoCancel or MessageBoxButton.OKCancel)
            return ShowConfirmation(main, messageBoxText, caption, button, icon);

        main.ShowFeedback(ToKind(icon), NormalizeTitle(caption), messageBoxText);
        return MessageBoxResult.OK;
    }

    public static MessageBoxResult Show(
        string messageBoxText,
        string caption,
        MessageBoxButton button,
        MessageBoxImage icon) =>
        Show(Application.Current?.MainWindow ?? throw new InvalidOperationException("LUMINA main window is not ready."),
            messageBoxText, caption, button, icon);

    private static MessageBoxResult ShowConfirmation(
        MainWindow main,
        string message,
        string caption,
        MessageBoxButton buttons,
        MessageBoxImage icon)
    {
        var confirmText = buttons is MessageBoxButton.YesNo or MessageBoxButton.YesNoCancel ? "Bestätigen" : "OK";
        var pending = main.Feedback.ConfirmAsync(
            NormalizeTitle(caption),
            message,
            confirmText,
            danger: icon == MessageBoxImage.Error || icon == MessageBoxImage.Warning);

        // WPF dialogs use a nested dispatcher loop as well. This keeps the LUMINA-owned
        // overlay responsive while legacy synchronous event handlers await a decision.
        var frame = new DispatcherFrame();
        _ = pending.ContinueWith(_ =>
            main.Dispatcher.BeginInvoke(DispatcherPriority.Send, new Action(() => frame.Continue = false)));
        Dispatcher.PushFrame(frame);

        var accepted = pending.GetAwaiter().GetResult();
        if (buttons is MessageBoxButton.YesNo or MessageBoxButton.YesNoCancel)
            return accepted ? MessageBoxResult.Yes : MessageBoxResult.No;
        return accepted ? MessageBoxResult.OK : MessageBoxResult.Cancel;
    }

    private static Ui.NotificationKind ToKind(MessageBoxImage image) => image switch
    {
        MessageBoxImage.Error => Ui.NotificationKind.Error,
        MessageBoxImage.Warning => Ui.NotificationKind.Warning,
        MessageBoxImage.Information => Ui.NotificationKind.Info,
        MessageBoxImage.Question => Ui.NotificationKind.Info,
        _ => Ui.NotificationKind.Info
    };

    private static string NormalizeTitle(string caption) =>
        string.IsNullOrWhiteSpace(caption) ? "LUMINA" : caption.Replace("LUMINA – ", string.Empty).Replace("LUMINA ", string.Empty).Trim();
}
