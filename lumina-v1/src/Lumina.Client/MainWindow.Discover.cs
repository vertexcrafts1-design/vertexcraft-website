using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Lumina.Client.Services;
using Lumina.Core;

namespace Lumina.Client;

public partial class MainWindow
{
    private enum DiscoverMode { Modrinth, Collection, Installed }

    private DiscoverMode _discoverMode = DiscoverMode.Modrinth;
    private bool _enhancedDiscoverReady;
    private Border? _discoverFilterBar;
    private Grid? _discoverGalleryHost;
    private InstalledUpdatesView? _installedUpdatesView;
    private Button? _tabModrinth;
    private Button? _tabCollection;
    private Button? _tabInstalled;
    private TextBlock? _curatorHint;
    private LuminaCollectionService? _luminaCollectionService;
    private VerificationService? _verificationService;
    private ManagedContentUpdateService? _managedUpdateService;
    private bool? _curatorVerified;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        if (_enhancedDiscoverReady) return;
        InitializeEnhancedDiscover();
        Dispatcher.BeginInvoke(async () => await RefreshDiscoverModeAsync(), DispatcherPriority.Background);
    }

    private void InitializeEnhancedDiscover()
    {
        if (_enhancedDiscoverReady) return;
        _enhancedDiscoverReady = true;
        _luminaCollectionService = new LuminaCollectionService();
        _verificationService = new VerificationService();
        _managedUpdateService = new ManagedContentUpdateService(_paths, _modrinthService);

        _discoverFilterBar = DiscoverPage.Children.OfType<Border>().FirstOrDefault(x => Grid.GetRow(x) == 1);
        _discoverGalleryHost = DiscoverPage.Children.OfType<Grid>().FirstOrDefault(x => Grid.GetRow(x) == 2);

        foreach (UIElement child in DiscoverPage.Children.Cast<UIElement>().ToList())
        {
            var row = Grid.GetRow(child);
            if (row >= 1) Grid.SetRow(child, row + 1);
        }
        DiscoverPage.RowDefinitions.Insert(1, new RowDefinition { Height = GridLength.Auto });

        var tabs = new Grid { Margin = new Thickness(0, 0, 0, 14) };
        tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        tabs.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        tabs.ColumnDefinitions.Add(new ColumnDefinition());

        _tabModrinth = TabButton("Modrinth", DiscoverMode.Modrinth);
        _tabCollection = TabButton("✦  LUMINA Collection", DiscoverMode.Collection);
        _tabInstalled = TabButton("Installiert / Updates", DiscoverMode.Installed);
        Grid.SetColumn(_tabModrinth, 0);
        Grid.SetColumn(_tabCollection, 1);
        Grid.SetColumn(_tabInstalled, 2);
        _tabCollection.Margin = new Thickness(7, 0, 0, 0);
        _tabInstalled.Margin = new Thickness(7, 0, 0, 0);
        tabs.Children.Add(_tabModrinth);
        tabs.Children.Add(_tabCollection);
        tabs.Children.Add(_tabInstalled);

        _curatorHint = new TextBlock
        {
            Text = "",
            FontSize = 9,
            Foreground = Brush("#6F7B91"),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetColumn(_curatorHint, 3);
        tabs.Children.Add(_curatorHint);
        Grid.SetRow(tabs, 1);
        DiscoverPage.Children.Add(tabs);

        _installedUpdatesView = new InstalledUpdatesView { Visibility = Visibility.Collapsed };
        _installedUpdatesView.UpdateRequested += InstalledUpdate_Requested;
        _installedUpdatesView.RefreshRequested += async (_, _) => await LoadInstalledUpdatesAsync();
        Grid.SetRow(_installedUpdatesView, 3);
        DiscoverPage.Children.Add(_installedUpdatesView);

        DiscoverSearch.KeyDown -= DiscoverSearch_KeyDown;
        DiscoverSearch.KeyDown += EnhancedDiscoverSearch_KeyDown;
        DiscoverKind.SelectionChanged -= DiscoverFilter_Changed;
        DiscoverKind.SelectionChanged += EnhancedDiscoverFilter_Changed;
        DiscoverSort.SelectionChanged -= DiscoverFilter_Changed;
        DiscoverSort.SelectionChanged += EnhancedDiscoverFilter_Changed;

        var searchButton = FindButton(DiscoverPage, x => x.Equals("Suchen", StringComparison.OrdinalIgnoreCase));
        if (searchButton is not null)
        {
            searchButton.Click -= DiscoverSearch_Click;
            searchButton.Click += EnhancedDiscoverSearch_Click;
        }

        GalleryList.PreviewMouseRightButtonUp += GalleryList_RightClick;
        DiscoverPage.IsVisibleChanged += async (_, _) =>
        {
            if (DiscoverPage.Visibility == Visibility.Visible)
                await RefreshDiscoverModeAsync();
        };
        UpdateDiscoverTabs();
    }

    private Button TabButton(string text, DiscoverMode mode)
    {
        var button = new Button
        {
            Content = text,
            Style = (Style)FindResource("PillButton")
        };
        button.Click += async (_, _) =>
        {
            _discoverMode = mode;
            UpdateDiscoverTabs();
            await RefreshDiscoverModeAsync();
        };
        return button;
    }

    private async void EnhancedDiscoverSearch_Click(object sender, RoutedEventArgs e) => await RefreshDiscoverModeAsync();

    private async void EnhancedDiscoverSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await RefreshDiscoverModeAsync();
    }

    private async void EnhancedDiscoverFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || !_enhancedDiscoverReady) return;
        await RefreshDiscoverModeAsync();
    }

    private async Task RefreshDiscoverModeAsync()
    {
        if (!_enhancedDiscoverReady || SelectedInstance is null) return;
        UpdateDiscoverTabs();
        await RefreshCuratorStateAsync();

        switch (_discoverMode)
        {
            case DiscoverMode.Collection:
                if (_discoverFilterBar is not null) _discoverFilterBar.Visibility = Visibility.Visible;
                if (_discoverGalleryHost is not null) _discoverGalleryHost.Visibility = Visibility.Visible;
                if (_installedUpdatesView is not null) _installedUpdatesView.Visibility = Visibility.Collapsed;
                await LoadLuminaCollectionAsync();
                break;
            case DiscoverMode.Installed:
                if (_discoverFilterBar is not null) _discoverFilterBar.Visibility = Visibility.Collapsed;
                if (_discoverGalleryHost is not null) _discoverGalleryHost.Visibility = Visibility.Collapsed;
                if (_installedUpdatesView is not null) _installedUpdatesView.Visibility = Visibility.Visible;
                await LoadInstalledUpdatesAsync();
                break;
            default:
                if (_discoverFilterBar is not null) _discoverFilterBar.Visibility = Visibility.Visible;
                if (_discoverGalleryHost is not null) _discoverGalleryHost.Visibility = Visibility.Visible;
                if (_installedUpdatesView is not null) _installedUpdatesView.Visibility = Visibility.Collapsed;
                await SearchGalleryAsync();
                break;
        }
    }

    private async Task LoadLuminaCollectionAsync()
    {
        if (SelectedInstance is null || _luminaCollectionService is null) return;
        DiscoverEmpty.Visibility = Visibility.Collapsed;
        SetDownloadStatus("Lade LUMINA Collection …");
        try
        {
            var kindText = SelectedGalleryKind();
            var category = CategoryForKind(kindText);
            var contentKind = ModrinthService.KindToContentKind(kindText);
            var entries = (await _luminaCollectionService.GetAsync())
                .Where(x => x.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var compatibleIds = new List<string>();
            foreach (var entry in entries)
            {
                try
                {
                    var versions = await _modrinthService.GetCompatibleVersionsAsync(entry.ProjectId, contentKind, SelectedInstance);
                    var compatible = versions.Where(v => CompatibilityService.Check(SelectedInstance, contentKind, v.Descriptor).IsCompatible).ToList();
                    if (CompatibilityService.ChoosePreferred(compatible) is not null)
                        compatibleIds.Add(entry.ProjectId);
                }
                catch { }
            }

            var projects = await _modrinthService.GetProjectsAsync(compatibleIds);
            var query = DiscoverSearch.Text.Trim();
            if (!string.IsNullOrWhiteSpace(query))
                projects = projects.Where(x =>
                    x.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    x.Description.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

            _gallery.Clear();
            foreach (var project in projects) _gallery.Add(project);
            DiscoverEmptyText.Text = entries.Count == 0
                ? "Die LUMINA Collection ist leer oder der Collection-Dienst ist gerade nicht erreichbar. Modrinth funktioniert weiterhin normal."
                : "Für diese Instanz gibt es aktuell keinen kompatiblen Eintrag in der LUMINA Collection.";
            DiscoverEmpty.Visibility = _gallery.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            SetDownloadStatus("Bereit");
        }
        catch (Exception ex)
        {
            _gallery.Clear();
            DiscoverEmptyText.Text = "LUMINA Collection ist gerade nicht erreichbar. Nutze solange Modrinth.";
            DiscoverEmpty.Visibility = Visibility.Visible;
            AddLog("LUMINA Collection: " + ex.Message);
            SetDownloadStatus("Bereit");
        }
    }

    private async Task LoadInstalledUpdatesAsync()
    {
        if (SelectedInstance is null || _managedUpdateService is null || _installedUpdatesView is null) return;
        SetDownloadStatus("Prüfe installierten Content …");
        try
        {
            var items = await _managedUpdateService.GetAsync(SelectedInstance);
            _installedUpdatesView.SetItems(items);
            var updates = items.Count(x => x.HasUpdate);
            SetDownloadStatus(updates == 0 ? "Alles aktuell" : $"{updates} Update(s) verfügbar");
        }
        catch (Exception ex)
        {
            AddLog("Update-Prüfung: " + ex.Message);
            _installedUpdatesView.SetItems([]);
            SetDownloadStatus("Update-Prüfung fehlgeschlagen");
        }
    }

    private async void InstalledUpdate_Requested(object? sender, InstalledContentStatus item)
    {
        if (SelectedInstance is null || _managedUpdateService is null) return;
        try
        {
            var progress = new Progress<string>(message =>
            {
                SetDownloadStatus(message);
                AddLog(message);
            });
            var result = await _managedUpdateService.UpdateAsync(item, SelectedInstance, progress);
            AddLog($"Update installiert: {item.Title} · {result.FilesInstalled} Datei(en)");
            RefreshContent();
            await LoadInstalledUpdatesAsync();
        }
        catch (Exception ex)
        {
            AddLog("Update fehlgeschlagen: " + ex);
            MessageBox.Show(this, ex.Message, "LUMINA Update", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task RefreshCuratorStateAsync()
    {
        if (_curatorHint is null || _verificationService is null) return;
        if (!_accountService.IsSignedIn)
        {
            _curatorVerified = false;
            _curatorHint.Text = "";
            return;
        }

        if (_curatorVerified is null)
            _curatorVerified = await _verificationService.IsVerifiedAsync(_accountService.PlayerId);
        _curatorHint.Text = _curatorVerified == true
            ? "Verifizierter Kurator · Rechtsklick auf Projekt zum Hinzufügen"
            : "";
    }

    private async void GalleryList_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (_discoverMode != DiscoverMode.Modrinth || _verificationService is null || _luminaCollectionService is null) return;
        if (!_accountService.IsSignedIn) return;

        _curatorVerified = await _verificationService.IsVerifiedAsync(_accountService.PlayerId);
        if (_curatorVerified != true) return;
        if (FindDataContext<GalleryProject>(e.OriginalSource as DependencyObject) is not { } project) return;

        var menu = new ContextMenu();
        var add = new MenuItem { Header = "✦ Zur LUMINA Collection hinzufügen" };
        add.Click += async (_, _) =>
        {
            try
            {
                var session = _accountService.Session ?? throw new InvalidOperationException("Microsoft-Sitzung fehlt.");
                await _luminaCollectionService.AddAsync(project.ProjectId, CategoryForKind(SelectedGalleryKind()), "", session);
                AddLog($"LUMINA Collection: {project.Title} hinzugefügt");
                MessageBox.Show(this, $"{project.Title} wurde zur LUMINA Collection hinzugefügt.", "LUMINA Collection", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "LUMINA Collection", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };
        menu.Items.Add(add);
        menu.IsOpen = true;
        e.Handled = true;
    }

    private void UpdateDiscoverTabs()
    {
        SetTab(_tabModrinth, _discoverMode == DiscoverMode.Modrinth);
        SetTab(_tabCollection, _discoverMode == DiscoverMode.Collection);
        SetTab(_tabInstalled, _discoverMode == DiscoverMode.Installed);
        if (SelectedInstance is not null)
        {
            var suffix = _discoverMode switch
            {
                DiscoverMode.Collection => "LUMINA Collection",
                DiscoverMode.Installed => "Installiert & Updates",
                _ => "Modrinth"
            };
            DiscoverContext.Text = $"{SelectedInstance.Name} · Minecraft {SelectedInstance.Version} · {SelectedInstance.Loader} · {suffix}";
        }
    }

    private static void SetTab(Button? button, bool active)
    {
        if (button is null) return;
        button.Background = Brush(active ? "#6F55E8" : "#151B27");
        button.Foreground = Brush(active ? "#FFFFFF" : "#A8B1C5");
    }

    private static string CategoryForKind(string kind) => kind switch
    {
        "Resource Packs" => "resourcepack",
        "Shaders" => "shader",
        _ => "mod"
    };

    private static T? FindDataContext<T>(DependencyObject? start) where T : class
    {
        var current = start;
        while (current is not null)
        {
            if (current is FrameworkElement { DataContext: T value }) return value;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
