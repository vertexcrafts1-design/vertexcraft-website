using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Lumina.Client.Services;
using Lumina.Core;

namespace Lumina.Client.Ui;

public enum DiscoverSourceMode
{
    Modrinth,
    Collection,
    Installed
}

public sealed record DiscoverRequest(string Query, string Category, DiscoverSourceMode Source);

public partial class DiscoverStorefrontView : UserControl
{
    private string _category = "Featured";
    private DiscoverSourceMode _source = DiscoverSourceMode.Modrinth;

    public DiscoverStorefrontView()
    {
        InitializeComponent();
        RefreshTabVisuals();
    }

    public event EventHandler<DiscoverRequest>? SearchRequested;
    public event EventHandler<GalleryProject>? InstallRequested;
    public event EventHandler<GalleryProject>? CurateRequested;

    public string Category => _category;
    public DiscoverSourceMode SourceMode => _source;
    public string Query => SearchBox.Text.Trim();

    public void SetProjects(IEnumerable projects)
    {
        ProjectList.ItemsSource = projects;
        ProjectsScroll.Visibility = Visibility.Visible;
        InstalledHost.Visibility = Visibility.Collapsed;
    }

    public void SetInstanceContext(InstanceProfile? instance)
    {
        if (instance is null)
        {
            ContextText.Text = "Wähle eine Instanz, um kompatiblen Content zu sehen.";
            DiscoverVersionFilter.ItemsSource = null;
            DiscoverLoaderFilter.ItemsSource = null;
            return;
        }

        ContextText.Text = $"{instance.Name} · Minecraft {instance.Version} · {instance.Loader} · nur kompatible Versionen";
        DiscoverVersionFilter.ItemsSource = new[] { $"Minecraft {instance.Version}" };
        DiscoverVersionFilter.SelectedIndex = 0;
        DiscoverLoaderFilter.ItemsSource = new[] { instance.Loader };
        DiscoverLoaderFilter.SelectedIndex = 0;
    }

    public void SetInstalledView(UIElement view)
    {
        InstalledHost.Content = view;
    }

    public void ShowInstalled()
    {
        ProjectsScroll.Visibility = Visibility.Collapsed;
        EmptyState.Visibility = Visibility.Collapsed;
        InstalledHost.Visibility = Visibility.Visible;
    }

    public void SetEmpty(bool empty, string message)
    {
        EmptyText.Text = message;
        EmptyState.Visibility = empty ? Visibility.Visible : Visibility.Collapsed;
    }

    public void SetCategory(string category)
    {
        _category = category;
        RefreshTabVisuals();
    }

    public void SetSource(DiscoverSourceMode source)
    {
        _source = source;
        RefreshTabVisuals();
    }

    private void Category_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string category }) return;
        _category = category;
        RefreshTabVisuals();
        RaiseSearch();
    }

    private void Source_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string source }) return;
        _source = source switch
        {
            "Collection" => DiscoverSourceMode.Collection,
            "Installed" => DiscoverSourceMode.Installed,
            _ => DiscoverSourceMode.Modrinth
        };
        RefreshTabVisuals();
        RaiseSearch();
    }

    private void Search_Click(object sender, RoutedEventArgs e) => RaiseSearch();

    private void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) RaiseSearch();
    }

    private void Install_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: GalleryProject project })
            InstallRequested?.Invoke(this, project);
    }

    private void Curate_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: GalleryProject project })
            CurateRequested?.Invoke(this, project);
    }

    private void RaiseSearch() => SearchRequested?.Invoke(this, new DiscoverRequest(Query, _category, _source));

    private void RefreshTabVisuals()
    {
        foreach (var button in FindButtons(this))
        {
            if (button.Tag is not string tag) continue;
            var active = tag == _category ||
                         (_source == DiscoverSourceMode.Modrinth && tag == "Modrinth") ||
                         (_source == DiscoverSourceMode.Collection && tag == "Collection") ||
                         (_source == DiscoverSourceMode.Installed && tag == "Installed");
            if (!active) continue;
            button.Background = (Brush)FindResource("LuminaAccentBrush");
            button.Foreground = Brushes.White;
        }
    }

    private static IEnumerable<Button> FindButtons(DependencyObject root)
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is Button button) yield return button;
            foreach (var nested in FindButtons(child)) yield return nested;
        }
    }
}
