using Lumina.Client.Ui;
using Xunit;

namespace Lumina.Client.Tests;

public sealed class LuminaFeedbackTests
{
    [Fact]
    public void Show_Enqueues_A_Notification()
    {
        var feedback = new LuminaFeedback();
        feedback.Show(NotificationKind.Success, "Gespeichert", "Einstellungen gespeichert.");

        var item = Assert.Single(feedback.Notifications);
        Assert.Equal(NotificationKind.Success, item.Kind);
        Assert.Equal("Gespeichert", item.Title);
        Assert.Equal("Einstellungen gespeichert.", item.Message);
    }

    [Fact]
    public async Task ConfirmAsync_Completes_With_User_Result()
    {
        var feedback = new LuminaFeedback();
        var pending = feedback.ConfirmAsync("Löschen?", "Instanz entfernen", "Löschen", danger: true);

        Assert.NotNull(feedback.ActiveConfirmation);
        Assert.True(feedback.ActiveConfirmation!.Danger);
        feedback.ResolveConfirmation(true);

        Assert.True(await pending);
        Assert.Null(feedback.ActiveConfirmation);
    }

    [Fact]
    public void Dismiss_Removes_Notification()
    {
        var feedback = new LuminaFeedback();
        feedback.Show(NotificationKind.Info, "Hinweis", "Text");
        var item = Assert.Single(feedback.Notifications);

        feedback.Dismiss(item);

        Assert.Empty(feedback.Notifications);
    }
}
