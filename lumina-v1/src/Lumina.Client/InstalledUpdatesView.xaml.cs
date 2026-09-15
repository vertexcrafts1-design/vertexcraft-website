using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Lumina.Client.Services;

namespace Lumina.Client;

public partial class InstalledUpdatesView : UserControl
{
    private readonly ObservableCollection<InstalledContentStatus> _items = [];

    public InstalledUpdatesView()
    {
        InitializeComponent();
        ItemsList.ItemsSource = _items;
    }

    public event EventHandler<InstalledContentStatus>? UpdateRequested;
    public event EventHandler? RefreshRequested;

    public void SetItems(IEnumerable<InstalledContentStatus> items)
    {
        _items.Clear();
        foreach (var item in items) _items.Add(item);
        EmptyState.Visibility = _items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Update_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: InstalledContentStatus item })
            UpdateRequested?.Invoke(this, item);
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => RefreshRequested?.Invoke(this, EventArgs.Empty);
}
