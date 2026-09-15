using Xunit;

namespace Lumina.Client.Tests;

public sealed class AccountFeedbackTests
{
    private static string MainWindow => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "MainWindowUnderTest.xaml"));

    [Fact]
    public void Account_Ui_Is_Integrated_And_Does_Not_Collect_Passwords()
    {
        var xaml = MainWindow;
        Assert.Contains("x:Name=\"AccountPage\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"AccountState\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Mit Microsoft anmelden", xaml, StringComparison.Ordinal);
        Assert.Contains("LUMINA fragt niemals nach deinem Microsoft-Passwort", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("PasswordBox", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Feedback_System_Uses_Lumina_Host_Not_System_MessageBox()
    {
        var adapterPath = Path.Combine(AppContext.BaseDirectory, "MessageBoxAdapterUnderTest.cs");
        Assert.True(File.Exists(adapterPath));
        var source = File.ReadAllText(adapterPath);
        Assert.Contains("main.ShowFeedback", source, StringComparison.Ordinal);
        Assert.DoesNotContain("System.Windows.MessageBox.Show", source, StringComparison.Ordinal);
    }
}
