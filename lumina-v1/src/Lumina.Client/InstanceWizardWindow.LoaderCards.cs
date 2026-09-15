using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Lumina.Client;

public partial class InstanceWizardWindow
{
    private async void LoaderCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string loader }) return;

        LoaderCombo.SelectedItem = loader;
        UpdateLoaderCardVisuals(loader);
        await LoadLoaderVersionsAsync();
        RefreshSummaryAndValidation();
    }

    private void UpdateLoaderCardVisuals(string selectedLoader)
    {
        foreach (var button in new[]
        {
            VanillaLoaderCard,
            FabricLoaderCard,
            QuiltLoaderCard,
            ForgeLoaderCard,
            NeoForgeLoaderCard
        })
        {
            var selected = string.Equals(button.Tag?.ToString(), selectedLoader, StringComparison.OrdinalIgnoreCase);
            button.Background = selected ? Brush("#2B1F52") : Brush("#12192A");
            button.BorderBrush = selected ? Brush("#8B5CF6") : Brush("#252D45");
            button.BorderThickness = new Thickness(selected ? 2 : 1);
        }
    }
}
