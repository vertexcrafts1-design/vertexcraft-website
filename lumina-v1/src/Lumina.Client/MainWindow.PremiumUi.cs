using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace Lumina.Client;

public partial class MainWindow
{
    private bool _premiumUiApplied;

    protected override void OnActivated(EventArgs e)
    {
        base.OnActivated(e);
        if (_premiumUiApplied) return;
        _premiumUiApplied = true;
        ApplyPremiumUi();
    }

    private void ApplyPremiumUi()
    {
        ApplyPremiumSidebar();
        ApplyPremiumSettings();

        HomePlayButton.MinWidth = 210;
        HomePlayButton.Padding = new Thickness(34, 15, 34, 15);
        HomePlayButton.FontSize = 14;
        HeroInstanceName.FontSize = 41;
    }

    private void ApplyPremiumSidebar()
    {
        SidebarNav.HorizontalAlignment = HorizontalAlignment.Stretch;
        SidebarNav.Margin = new Thickness(8, 8, 8, 0);

        if (SidebarNav.Parent is not Grid sidebarGrid ||
            sidebarGrid.Parent is not Border sidebarBorder ||
            sidebarBorder.Parent is not Grid rootGrid ||
            rootGrid.ColumnDefinitions.Count == 0)
            return;

        rootGrid.ColumnDefinitions[0].Width = new GridLength(224);
        sidebarGrid.Margin = new Thickness(8, 12, 8, 14);

        var brand = sidebarGrid.Children.OfType<Border>().FirstOrDefault(x => Grid.GetRow(x) == 0);
        if (brand is not null)
        {
            brand.Width = double.NaN;
            brand.Height = 56;
            brand.Margin = new Thickness(8, 0, 8, 0);
            brand.HorizontalAlignment = HorizontalAlignment.Stretch;
            brand.Background = Brushes.Transparent;
            brand.Child = CreateBrandContent();
        }

        var labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Home"] = "Home",
            ["Discover"] = "Discover",
            ["Instances"] = "Instanzen",
            ["Library"] = "Bibliothek",
            ["Downloads"] = "Downloads",
            ["Settings"] = "Einstellungen"
        };

        foreach (var button in SidebarNav.Children.OfType<Button>())
        {
            if (button.Tag is not string tag || !labels.TryGetValue(tag, out var label)) continue;
            var icon = button.Content?.ToString() ?? "•";
            button.Content = CreateNavContent(icon, label);
            button.ToolTip = null;
        }

        var accountButton = sidebarGrid.Children.OfType<Button>()
            .FirstOrDefault(x => string.Equals(x.Tag?.ToString(), "Account", StringComparison.OrdinalIgnoreCase));
        if (accountButton is not null)
        {
            accountButton.Content = CreateNavContent("◉", "Account");
            accountButton.ToolTip = null;
            accountButton.Margin = new Thickness(8, 4, 8, 0);
        }
    }

    private static UIElement CreateBrandContent()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        var logo = new Border
        {
            Width = 38,
            Height = 38,
            CornerRadius = new CornerRadius(12),
            Background = Brush("#8158F0"),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = "L",
                FontSize = 19,
                FontWeight = FontWeights.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White
            }
        };
        grid.Children.Add(logo);

        var text = new StackPanel { Margin = new Thickness(10, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(new TextBlock { Text = "LUMINA", FontSize = 15, FontWeight = FontWeights.Bold });
        text.Children.Add(new TextBlock { Text = "MINECRAFT CLIENT", FontSize = 8, FontWeight = FontWeights.SemiBold, Foreground = Brush("#69758B"), Margin = new Thickness(0, 2, 0, 0) });
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        return grid;
    }

    private static UIElement CreateNavContent(string icon, string label)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());

        var iconText = new TextBlock
        {
            Text = icon,
            FontSize = 17,
            FontWeight = FontWeights.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        BindToButtonForeground(iconText);
        grid.Children.Add(iconText);

        var labelText = new TextBlock
        {
            Text = label,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(9, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        BindToButtonForeground(labelText);
        Grid.SetColumn(labelText, 1);
        grid.Children.Add(labelText);
        return grid;
    }

    private static void BindToButtonForeground(TextBlock text) =>
        text.SetBinding(TextBlock.ForegroundProperty, new Binding("Foreground")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(Button), 1)
        });

    private void ApplyPremiumSettings()
    {
        if (SettingsPage.Content is not StackPanel root) return;
        root.MaxWidth = 1040;
        root.HorizontalAlignment = HorizontalAlignment.Stretch;

        var cards = root.Children.OfType<Border>().ToList();
        if (cards.Count < 2) return;

        RebuildPerformanceCard(cards[0]);
        RebuildMinecraftCard(cards[1]);

        var save = root.Children.OfType<Button>().FirstOrDefault();
        if (save is not null)
        {
            save.Content = "✓  Einstellungen speichern";
            save.Padding = new Thickness(24, 13, 24, 13);
            save.HorizontalAlignment = HorizontalAlignment.Right;
            save.Margin = new Thickness(0, 2, 0, 18);
        }
    }

    private void RebuildPerformanceCard(Border card)
    {
        card.Style = (Style)FindResource("PremiumCard");
        card.Padding = new Thickness(24);
        card.Margin = new Thickness(0, 0, 0, 14);

        if (card.Child is not StackPanel old) return;
        var oldToggles = old.Children.OfType<UniformGrid>().FirstOrDefault();
        if (oldToggles is not null) old.Children.Remove(oldToggles);
        old.Children.Remove(RamSlider);
        old.Children.Remove(RamValue);
        old.Children.Clear();

        var stack = old;
        stack.Children.Add(SectionHeader("Performance", "RAM und Startoptimierung ohne unnötige Technik-Menüs."));

        var ramHeader = new Grid { Margin = new Thickness(0, 22, 0, 10) };
        ramHeader.ColumnDefinitions.Add(new ColumnDefinition());
        ramHeader.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        ramHeader.Children.Add(new TextBlock { Text = "Arbeitsspeicher", FontSize = 13, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
        RamValue.FontSize = 12;
        RamValue.FontWeight = FontWeights.SemiBold;
        RamValue.Foreground = Brush("#CDBDFF");
        Grid.SetColumn(RamValue, 1);
        ramHeader.Children.Add(RamValue);
        stack.Children.Add(ramHeader);

        var presets = new UniformGrid { Columns = 5, Margin = new Thickness(-3, 0, -3, 10) };
        foreach (var gb in new[] { 4, 6, 8, 12, 16 })
        {
            var button = new Button
            {
                Content = $"{gb} GB",
                Tag = gb * 1024,
                Style = (Style)FindResource("RamPresetButton")
            };
            button.Click += RamPreset_Click;
            presets.Children.Add(button);
        }
        stack.Children.Add(presets);

        RamSlider.Margin = new Thickness(2, 0, 2, 0);
        stack.Children.Add(RamSlider);
        stack.Children.Add(new TextBlock
        {
            Text = "Tipp: 6–8 GB reichen für die meisten Modpacks. Zu viel RAM macht Minecraft nicht automatisch schneller.",
            Foreground = Brush("#748096"),
            FontSize = 10,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(2, 7, 0, 18)
        });

        var toggles = oldToggles ?? new UniformGrid { Columns = 3 };
        toggles.Columns = 3;
        toggles.Margin = new Thickness(-4, 0, -4, 0);
        StyleToggle(SmartMemoryCheck, "Smart Memory", "LUMINA wählt automatisch eine passende RAM-Menge.");
        StyleToggle(SafeLaunchCheck, "Safe Launch", "Sichert wichtige Dateien vor jedem Minecraft-Start.");
        StyleToggle(FocusModeCheck, "Focus Mode", "Reduziert Ablenkung während Minecraft läuft.");
        stack.Children.Add(toggles);
    }

    private void RebuildMinecraftCard(Border card)
    {
        card.Style = (Style)FindResource("PremiumCard");
        card.Padding = new Thickness(24);
        card.Margin = new Thickness(0, 0, 0, 14);
        if (card.Child is not StackPanel old) return;

        Detach(WidthText);
        Detach(HeightText);
        Detach(JavaPathText);
        Detach(QuickServerText);
        Detach(CloseOnStartCheck);
        old.Children.Clear();

        old.Children.Add(SectionHeader("Minecraft", "Anzeige, Java und Direktverbindung – klar voneinander getrennt."));

        var resolution = new Grid { Margin = new Thickness(0, 20, 0, 0) };
        resolution.ColumnDefinitions.Add(new ColumnDefinition());
        resolution.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
        resolution.ColumnDefinitions.Add(new ColumnDefinition());
        var width = Field("Breite", "Fensterbreite in Pixeln", WidthText);
        var height = Field("Höhe", "Fensterhöhe in Pixeln", HeightText);
        resolution.Children.Add(width);
        Grid.SetColumn(height, 2);
        resolution.Children.Add(height);
        old.Children.Add(SubCard("Auflösung", "Minecraft startet mit dieser Fenstergröße.", resolution));

        var javaGrid = new Grid();
        javaGrid.ColumnDefinitions.Add(new ColumnDefinition());
        javaGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        JavaPathText.VerticalAlignment = VerticalAlignment.Center;
        javaGrid.Children.Add(JavaPathText);
        var browse = new Button
        {
            Content = "Java wählen",
            Style = (Style)FindResource("SecondaryButton"),
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(15, 10, 15, 10)
        };
        browse.Click += BrowseJava_Click;
        Grid.SetColumn(browse, 1);
        javaGrid.Children.Add(browse);
        old.Children.Add(SubCard("Java Runtime", "Leer lassen = LUMINA wählt automatisch die passende Java-Version.", javaGrid));

        StyleToggle(CloseOnStartCheck, "Launcher nach Spielstart schließen", "Schließt LUMINA automatisch, sobald Minecraft erfolgreich gestartet wurde.");
        CloseOnStartCheck.Margin = new Thickness(0, 12, 0, 0);
        var connection = new StackPanel();
        connection.Children.Add(Field("Quick Connect Server", "Optional: Server-Adresse für deinen Schnellstart.", QuickServerText));
        connection.Children.Add(CloseOnStartCheck);
        old.Children.Add(SubCard("Start & Verbindung", "Optionale Komfortfunktionen für den Minecraft-Start.", connection));
    }

    private void RamPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int value }) RamSlider.Value = value;
    }

    private void StyleToggle(CheckBox checkBox, string title, string description)
    {
        checkBox.Style = (Style)FindResource("ToggleCheckBox");
        checkBox.Margin = new Thickness(4);
        var content = new StackPanel();
        content.Children.Add(new TextBlock { Text = title, FontSize = 11, FontWeight = FontWeights.SemiBold });
        content.Children.Add(new TextBlock
        {
            Text = description,
            FontSize = 9,
            Foreground = Brush("#738096"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 0)
        });
        checkBox.Content = content;
    }

    private static UIElement SectionHeader(string title, string description)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeights.SemiBold });
        stack.Children.Add(new TextBlock
        {
            Text = description,
            FontSize = 10,
            Foreground = Brush("#7B879D"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 4, 0, 0)
        });
        return stack;
    }

    private Border SubCard(string title, string description, UIElement content)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = title, FontSize = 13, FontWeight = FontWeights.SemiBold });
        stack.Children.Add(new TextBlock
        {
            Text = description,
            FontSize = 9,
            Foreground = Brush("#748096"),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 12)
        });
        stack.Children.Add(content);
        return new Border
        {
            Background = Brush("#090E16"),
            BorderBrush = Brush("#1D2738"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 14, 0, 0),
            Child = stack
        };
    }

    private static StackPanel Field(string title, string description, Control control)
    {
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = title.ToUpperInvariant(), FontSize = 9, FontWeight = FontWeights.SemiBold, Foreground = Brush("#7A869B") });
        stack.Children.Add(new TextBlock { Text = description, FontSize = 9, Foreground = Brush("#5F6B80"), Margin = new Thickness(0, 2, 0, 7) });
        stack.Children.Add(control);
        return stack;
    }

    private static void Detach(FrameworkElement element)
    {
        switch (element.Parent)
        {
            case Panel panel:
                panel.Children.Remove(element);
                break;
            case Decorator decorator when ReferenceEquals(decorator.Child, element):
                decorator.Child = null;
                break;
            case ContentControl content when ReferenceEquals(content.Content, element):
                content.Content = null;
                break;
        }
    }
}
