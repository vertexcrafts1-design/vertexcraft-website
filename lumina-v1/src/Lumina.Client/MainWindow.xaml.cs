using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CmlLib.Core.ProcessBuilder;
using Lumina.Client.Services;
using Lumina.Core;
using Microsoft.Win32;

namespace Lumina.Client;

public partial class MainWindow : Window
{
    private readonly AppPaths _paths;
    private readonly SettingsService _settingsService;
    private readonly InstanceService _instanceService;
    private readonly ContentService _contentService;
    private readonly SafeLaunchService _safeLaunchService;
    private readonly StatsService _statsService;
    private readonly MicrosoftAccountService _accountService;
    private readonly MinecraftService _minecraftService;
    private readonly MinecraftCatalogService _catalogService;
    private readonly ModrinthService _modrinthService;

    private readonly ObservableCollection<InstanceProfile> _instances = [];
    private readonly ObservableCollection<ContentItem> _content = [];
    private readonly ObservableCollection<GalleryProject> _gallery = [];
    private readonly ObservableCollection<string> _downloadLog = [];

    private AppSettings _settings;
    private ContentKind _libraryKind = ContentKind.Mod;
    private bool _initializing = true;
    private bool _launching;
    private bool _catalogLoading;
    private bool _galleryLoading;

    private InstanceProfile? SelectedInstance => InstancesList.SelectedItem as InstanceProfile;

    public MainWindow()
    {
        InitializeComponent();

        _paths = new AppPaths();
        _settingsService = new SettingsService(_paths);
        _instanceService = new InstanceService(_paths);
        _contentService = new ContentService(_paths);
        _safeLaunchService = new SafeLaunchService(_paths);
        _statsService = new StatsService(_paths);
        _accountService = new MicrosoftAccountService();
        _minecraftService = new MinecraftService(_paths);
        _catalogService = new MinecraftCatalogService(_paths);
        _modrinthService = new ModrinthService();
        _settings = _settingsService.Load();

        InstancesList.ItemsSource = _instances;
        ContentList.ItemsSource = _content;
        GalleryList.ItemsSource = _gallery;
        DownloadLog.ItemsSource = _downloadLog;
        NewInstanceLoaderCombo.ItemsSource = MinecraftCatalogService.Loaders;

        _minecraftService.StatusChanged += status => Dispatcher.Invoke(() => SetDownloadStatus(status));
        _minecraftService.ProgressChanged += progress => Dispatcher.Invoke(() => SetProgress(progress));
        _minecraftService.LogLine += line => Dispatcher.Invoke(() => AddLog(line));

        LoadSettingsIntoUi();
        RefreshInstances();
        ShowPage("Home");
        _initializing = false;

        Loaded += async (_, _) =>
        {
            SetDownloadStatus("Prüfe Microsoft-Anmeldung …");
            await _accountService.TrySilentAsync();
            UpdateAccountUi();
            SetDownloadStatus("Lade Minecraft-Katalog …");
            await LoadMinecraftVersionsAsync();
            SetDownloadStatus("Bereit");
        };
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            return;
        }
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void TopAccount_Click(object sender, RoutedEventArgs e) => ShowPage("Account");

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string page }) ShowPage(page);
    }

    private void ShowPage(string page)
    {
        var pages = new UIElement[]
        {
            HomePage, DiscoverPage, InstancesPage, LibraryPage, DownloadsPage, SettingsPage, AccountPage
        };
        foreach (var element in pages) element.Visibility = Visibility.Collapsed;

        switch (page)
        {
            case "Discover":
                DiscoverPage.Visibility = Visibility.Visible;
                PageTitle.Text = "Discover";
                UpdateDiscoverContext();
                _ = SearchGalleryAsync();
                break;
            case "Instances":
                InstancesPage.Visibility = Visibility.Visible;
                PageTitle.Text = "Instanzen";
                break;
            case "Library":
                LibraryPage.Visibility = Visibility.Visible;
                PageTitle.Text = "Bibliothek";
                RefreshContent();
                break;
            case "Downloads":
                DownloadsPage.Visibility = Visibility.Visible;
                PageTitle.Text = "Downloads";
                break;
            case "Settings":
                SettingsPage.Visibility = Visibility.Visible;
                PageTitle.Text = "Einstellungen";
                break;
            case "Account":
                AccountPage.Visibility = Visibility.Visible;
                PageTitle.Text = "Account";
                UpdateAccountUi();
                break;
            default:
                HomePage.Visibility = Visibility.Visible;
                PageTitle.Text = "Home";
                RefreshStats();
                break;
        }

        foreach (var button in SidebarNav.Children.OfType<Button>())
        {
            var active = string.Equals(button.Tag as string, page, StringComparison.OrdinalIgnoreCase);
            button.Background = active ? Brush("#1B2130") : Brushes.Transparent;
            button.Foreground = active ? Brushes.White : Brush("#707A91");
        }
    }

    private void GoDiscover_Click(object sender, RoutedEventArgs e) => ShowPage("Discover");

    private void RefreshInstances(string? selectId = null)
    {
        var wanted = selectId ?? _settings.SelectedInstanceId;
        _instances.Clear();
        foreach (var profile in _instanceService.GetAll()) _instances.Add(profile);

        if (_instances.Count == 0)
        {
            var created = _instanceService.Create("LUMINA Vanilla", "1.21.1", "Vanilla");
            created.LoaderVersion = "Standard";
            _instanceService.Update(created);
            _instances.Add(created);
            wanted = created.Id;
        }

        var selected = _instances.FirstOrDefault(x => x.Id == wanted) ?? _instances[0];
        InstancesList.SelectedItem = selected;
        _settings.SelectedInstanceId = selected.Id;
        _settingsService.Save(_settings);
        UpdateSelectedInstanceUi();
    }

    private void InstancesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SelectedInstance is null) return;
        _settings.SelectedInstanceId = SelectedInstance.Id;
        if (!_initializing) _settingsService.Save(_settings);
        UpdateSelectedInstanceUi();
        RefreshContent();
        UpdateDiscoverContext();
    }

    private void UpdateSelectedInstanceUi()
    {
        var profile = SelectedInstance;
        if (profile is null) return;

        HeroInstanceName.Text = profile.Name;
        HeroInstanceMeta.Text = profile.Subtitle + LoaderVersionSuffix(profile);
        HeroVersionChip.Text = profile.Version;
        HeroLoaderChip.Text = profile.Loader;
        HeroSafeLaunch.Text = _settings.SafeLaunch ? "AKTIV" : "AUS";
        HeroSafeLaunch.Foreground = _settings.SafeLaunch ? Brush("#54E1A5") : Brush("#FF7B8C");
        HeroRam.Text = $"{_settings.RamMb / 1024d:0.#} GB";
        HeroLastPlayed.Text = profile.LastPlayedUtc is null
            ? "Noch nie"
            : profile.LastPlayedUtc.Value.ToLocalTime().ToString("dd.MM. HH:mm");

        InstanceDetailName.Text = profile.Name;
        InstanceDetailMeta.Text = profile.Subtitle + LoaderVersionSuffix(profile);
        InstanceVersionText.Text = profile.Version;
        InstanceLoaderText.Text = string.IsNullOrWhiteSpace(profile.LoaderVersion) || profile.LoaderVersion == "Standard"
            ? profile.Loader
            : $"{profile.Loader} {profile.LoaderVersion}";
        InstanceDetailLaunches.Text = profile.Launches.ToString();
        LibraryInstanceName.Text = $"Content für {profile.Name}";

        var contentCount = ContentCount(profile.Id);
        HeroContentMini.Text = contentCount.ToString();
        HomeContentCount.Text = contentCount.ToString();
        HomeLaunchCount.Text = profile.Launches.ToString();
    }

    private static string LoaderVersionSuffix(InstanceProfile profile) =>
        string.IsNullOrWhiteSpace(profile.LoaderVersion) || profile.LoaderVersion == "Standard"
            ? ""
            : $" · {profile.LoaderVersion}";

    private int ContentCount(string id) =>
        _contentService.Get(id, ContentKind.Mod).Count +
        _contentService.Get(id, ContentKind.ResourcePack).Count +
        _contentService.Get(id, ContentKind.ShaderPack).Count;

    private async void NewInstance_Click(object sender, RoutedEventArgs e)
    {
        NewInstancePanel.Visibility = Visibility.Visible;
        if (NewInstanceVersionCombo.Items.Count == 0) await LoadMinecraftVersionsAsync();
        if (NewInstanceLoaderCombo.SelectedItem is null) NewInstanceLoaderCombo.SelectedItem = "Fabric";
        await LoadLoaderVersionsAsync();
    }

    private void CancelCreate_Click(object sender, RoutedEventArgs e) => NewInstancePanel.Visibility = Visibility.Collapsed;

    private async Task LoadMinecraftVersionsAsync()
    {
        if (_catalogLoading) return;
        _catalogLoading = true;
        try
        {
            var currentName = (NewInstanceVersionCombo.SelectedItem as MinecraftVersionChoice)?.Name;
            var versions = await _catalogService.GetMinecraftVersionsAsync(ShowSnapshotsCheck.IsChecked == true);
            NewInstanceVersionCombo.ItemsSource = versions;
            NewInstanceVersionCombo.SelectedItem = versions.FirstOrDefault(v => v.Name == currentName)
                                                 ?? versions.FirstOrDefault(v => v.Name == "1.21.1")
                                                 ?? versions.FirstOrDefault();
            if (NewInstanceLoaderCombo.SelectedItem is null)
                NewInstanceLoaderCombo.SelectedItem = "Fabric";
            await LoadLoaderVersionsAsync();
        }
        catch (Exception ex)
        {
            AddLog("Minecraft-Katalog konnte nicht geladen werden: " + ex.Message);
            LoaderAvailabilityText.Text = "Minecraft-Versionen konnten nicht geladen werden. Prüfe deine Internetverbindung.";
        }
        finally
        {
            _catalogLoading = false;
        }
    }

    private async Task LoadLoaderVersionsAsync()
    {
        if (_catalogLoading && NewInstanceVersionCombo.SelectedItem is null) return;
        if (NewInstanceVersionCombo.SelectedItem is not MinecraftVersionChoice version) return;
        if (NewInstanceLoaderCombo.SelectedItem is not string loader) return;

        LoaderAvailabilityText.Text = $"Prüfe {loader} für Minecraft {version.Name} …";
        NewInstanceLoaderVersionCombo.ItemsSource = null;
        try
        {
            var versions = await _catalogService.GetLoaderVersionsAsync(version.Name, loader);
            NewInstanceLoaderVersionCombo.ItemsSource = versions;
            NewInstanceLoaderVersionCombo.SelectedItem = versions.FirstOrDefault();
            LoaderAvailabilityText.Text = versions.Count == 0
                ? $"{loader} ist für Minecraft {version.Name} nicht verfügbar."
                : loader == "Vanilla"
                    ? "Vanilla benötigt keinen zusätzlichen Loader."
                    : $"{versions.Count} {loader}-Versionen verfügbar · empfohlen: {versions[0]}";
        }
        catch (Exception ex)
        {
            LoaderAvailabilityText.Text = $"Loader-Abfrage fehlgeschlagen: {ex.Message}";
        }
    }

    private async void NewInstanceVersion_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing) return;
        await LoadLoaderVersionsAsync();
    }

    private async void NewInstanceLoader_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing) return;
        await LoadLoaderVersionsAsync();
    }

    private async void ShowSnapshots_Changed(object sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        await LoadMinecraftVersionsAsync();
    }

    private void CreateInstance_Click(object sender, RoutedEventArgs e)
    {
        if (NewInstanceVersionCombo.SelectedItem is not MinecraftVersionChoice version ||
            NewInstanceLoaderCombo.SelectedItem is not string loader)
        {
            MessageBox.Show(this, "Wähle zuerst Minecraft-Version und Loader.", "LUMINA", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var loaderVersion = NewInstanceLoaderVersionCombo.SelectedItem?.ToString() ?? "";
        if (loader != "Vanilla" && string.IsNullOrWhiteSpace(loaderVersion))
        {
            MessageBox.Show(this, $"{loader} ist für Minecraft {version.Name} nicht verfügbar.", "LUMINA", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var profile = _instanceService.Create(NewInstanceName.Text, version.Name, loader);
        profile.LoaderVersion = loader == "Vanilla" ? "Standard" : loaderVersion;
        _instanceService.Update(profile);
        NewInstancePanel.Visibility = Visibility.Collapsed;
        RefreshInstances(profile.Id);
        AddLog($"Instanz erstellt: {profile.Name} · {profile.Version} · {profile.Loader} {profile.LoaderVersion}");
    }

    private void DuplicateInstance_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedInstance is null) return;
        try
        {
            var clone = _instanceService.Duplicate(SelectedInstance);
            RefreshInstances(clone.Id);
            AddLog($"Instanz dupliziert: {clone.Name}");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "LUMINA – Duplizieren", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteInstance_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedInstance is null) return;
        if (_instances.Count <= 1)
        {
            MessageBox.Show(this, "Mindestens eine Instanz muss bestehen bleiben.", "LUMINA", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var deleting = SelectedInstance;
        var answer = MessageBox.Show(this,
            $"'{deleting.Name}' inklusive Mods, Packs, Welten und Configs wirklich löschen?",
            "Instanz löschen", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (answer != MessageBoxResult.Yes) return;

        _instanceService.Delete(deleting.Id);
        RefreshInstances();
        AddLog($"Instanz gelöscht: {deleting.Name}");
    }

    private void OpenInstanceFolder_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedInstance is null) return;
        OpenFolder(_paths.GameDirectory(SelectedInstance.Id));
    }

    private void UpdateDiscoverContext()
    {
        if (SelectedInstance is null) return;
        DiscoverContext.Text = $"{SelectedInstance.Name}  ·  Minecraft {SelectedInstance.Version}  ·  {SelectedInstance.Loader}";
    }

    private async void DiscoverSearch_Click(object sender, RoutedEventArgs e) => await SearchGalleryAsync();

    private async void DiscoverSearch_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await SearchGalleryAsync();
    }

    private async void DiscoverFilter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || DiscoverKind is null || DiscoverSort is null) return;
        await SearchGalleryAsync();
    }

    private string SelectedGalleryKind() => (DiscoverKind.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Mods";
    private string SelectedGallerySort() => (DiscoverSort.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "downloads";

    private async Task SearchGalleryAsync()
    {
        if (_galleryLoading || SelectedInstance is null || DiscoverKind is null) return;
        _galleryLoading = true;
        DiscoverEmpty.Visibility = Visibility.Collapsed;
        try
        {
            var kind = SelectedGalleryKind();
            if (kind == "Mods" && SelectedInstance.Loader.Equals("Vanilla", StringComparison.OrdinalIgnoreCase))
            {
                _gallery.Clear();
                DiscoverEmptyText.Text = "Vanilla lädt keine normalen Modloader-Mods. Erstelle eine Fabric-, Forge-, NeoForge- oder Quilt-Instanz.";
                DiscoverEmpty.Visibility = Visibility.Visible;
                return;
            }

            SetDownloadStatus("Durchsuche Mod Gallery …");
            var projects = await _modrinthService.SearchAsync(
                DiscoverSearch.Text.Trim(), kind, SelectedInstance, SelectedGallerySort());
            _gallery.Clear();
            foreach (var project in projects) _gallery.Add(project);
            DiscoverEmptyText.Text = "Keine kompatiblen Projekte gefunden.";
            DiscoverEmpty.Visibility = _gallery.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            SetDownloadStatus("Bereit");
        }
        catch (Exception ex)
        {
            _gallery.Clear();
            DiscoverEmptyText.Text = "Gallery konnte nicht geladen werden.";
            DiscoverEmpty.Visibility = Visibility.Visible;
            AddLog("Mod Gallery: " + ex.Message);
            SetDownloadStatus("Gallery-Fehler");
        }
        finally
        {
            _galleryLoading = false;
        }
    }

    private async void GalleryInstall_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedInstance is null || sender is not Button { Tag: GalleryProject project } button) return;
        button.IsEnabled = false;
        var original = button.Content;
        button.Content = "Installiere …";
        try
        {
            var progress = new Progress<string>(message =>
            {
                SetDownloadStatus(message);
                AddLog(message);
            });
            var result = await _modrinthService.InstallAsync(project, SelectedGalleryKind(), SelectedInstance, _paths, progress);
            AddLog($"Gallery installiert: {project.Title} · {result.FilesInstalled} Datei(en)");
            RefreshContent();
            UpdateSelectedInstanceUi();
            SetDownloadStatus($"{project.Title} installiert");
            button.Content = "Installiert ✓";
        }
        catch (Exception ex)
        {
            AddLog($"Gallery-Installation fehlgeschlagen: {ex}");
            MessageBox.Show(this, ex.Message, "LUMINA Mod Gallery", MessageBoxButton.OK, MessageBoxImage.Error);
            button.Content = original;
        }
        finally
        {
            button.IsEnabled = true;
        }
    }

    private void LibraryKind_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (LibraryKindCombo?.SelectedItem is not ComboBoxItem item) return;
        _libraryKind = item.Content?.ToString() switch
        {
            "Resource Packs" => ContentKind.ResourcePack,
            "Shaders" => ContentKind.ShaderPack,
            _ => ContentKind.Mod
        };
        if (!_initializing)
        {
            _settings.LibraryKind = item.Content?.ToString() ?? "Mods";
            _settingsService.Save(_settings);
        }
        RefreshContent();
    }

    private void RefreshContent()
    {
        if (SelectedInstance is null || ContentList is null) return;
        _content.Clear();
        foreach (var item in _contentService.Get(SelectedInstance.Id, _libraryKind)) _content.Add(item);
        LibraryDropText.Text = _libraryKind switch
        {
            ContentKind.Mod => "JAR-Mods hier hineinziehen",
            ContentKind.ResourcePack => "ZIP-Resourcepacks hier hineinziehen",
            _ => "ZIP-Shaderpacks hier hineinziehen"
        };
        UpdateSelectedInstanceUi();
    }

    private void AddContent_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedInstance is null) return;
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Filter = _libraryKind == ContentKind.Mod ? "Minecraft Mods (*.jar)|*.jar" : "Minecraft Packs (*.zip)|*.zip"
        };
        if (dialog.ShowDialog(this) == true) ImportContent(dialog.FileNames);
    }

    private void Library_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Library_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files) ImportContent(files);
    }

    private void ImportContent(IEnumerable<string> files)
    {
        if (SelectedInstance is null) return;
        var before = _content.Count;
        _contentService.Import(SelectedInstance.Id, _libraryKind, files);
        RefreshContent();
        AddLog($"Bibliothek: {_content.Count - before:+#;-#;0} Datei(en) importiert");
    }

    private void ToggleContent_Click(object sender, RoutedEventArgs e)
    {
        if (ContentList.SelectedItem is not ContentItem item) return;
        try
        {
            _contentService.Toggle(item.Path);
            RefreshContent();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "LUMINA", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DeleteContent_Click(object sender, RoutedEventArgs e)
    {
        if (ContentList.SelectedItem is not ContentItem item) return;
        if (MessageBox.Show(this, $"{item.Name} entfernen?", "LUMINA", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _contentService.Delete(item.Path);
        RefreshContent();
    }

    private void OpenContentFolder_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedInstance is null) return;
        OpenFolder(_contentService.Folder(SelectedInstance.Id, _libraryKind));
    }

    private async void Play_Click(object sender, RoutedEventArgs e)
    {
        if (_launching || SelectedInstance is null) return;
        var profile = SelectedInstance;

        if (!_accountService.IsSignedIn && !await LoginInteractiveAsync()) return;
        if (_accountService.Session is null) return;

        _launching = true;
        HomePlayButton.IsEnabled = false;
        SetProgress(0);
        ShowPage("Downloads");
        AddLog("────────────────────────────────────────");
        AddLog($"LUMINA Start · {profile.Name}");
        var record = new LaunchRecord
        {
            StartedUtc = DateTime.UtcNow,
            InstanceId = profile.Id,
            InstanceName = profile.Name
        };

        try
        {
            if (_settings.SmartMemory)
            {
                var available = Math.Max(0, GC.GetGCMemoryInfo().TotalAvailableMemoryBytes);
                _settings.RamMb = PresetService.SmartRamMb((ulong)available);
                _settingsService.Save(_settings);
                Dispatcher.Invoke(() =>
                {
                    RamSlider.Value = Math.Clamp(_settings.RamMb, 2048, 16384);
                    HeroRam.Text = $"{_settings.RamMb / 1024d:0.#} GB";
                });
            }

            if (_settings.SafeLaunch)
            {
                SetDownloadStatus("Erstelle Safe-Launch-Sicherung …");
                try
                {
                    var snapshot = _safeLaunchService.CreateSnapshot(profile);
                    AddLog("Safe Launch: " + snapshot);
                }
                catch (Exception ex)
                {
                    AddLog("Safe Launch konnte kein Backup erstellen: " + ex.Message);
                }
            }

            var wrapper = await _minecraftService.LaunchAsync(profile, _settings, _accountService.Session);
            profile.Launches++;
            profile.LastPlayedUtc = DateTime.UtcNow;
            _instanceService.Update(profile);
            RefreshInstances(profile.Id);

            wrapper.Exited += (_, _) =>
            {
                record.EndedUtc = DateTime.UtcNow;
                try { record.ExitCode = wrapper.Process.ExitCode; } catch { record.ExitCode = -1; }
                _statsService.Add(record);
                Dispatcher.Invoke(() =>
                {
                    RefreshStats();
                    SetDownloadStatus(record.ExitCode == 0 ? "Minecraft beendet" : $"Minecraft beendet · Exit {record.ExitCode}");
                });
            };

            if (_settings.CloseLauncherOnGameStart) Hide();
        }
        catch (Exception ex)
        {
            AddLog("STARTFEHLER: " + ex);
            SetDownloadStatus("Minecraft konnte nicht gestartet werden");
            MessageBox.Show(this,
                $"Minecraft konnte nicht gestartet werden.\n\n{ex.Message}\n\nDen vollständigen Fehler findest du unter Downloads → Launch Log.",
                "LUMINA Startfehler", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _launching = false;
            HomePlayButton.IsEnabled = true;
        }
    }

    private void RamSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RamValue is null) return;
        RamValue.Text = $"{(int)e.NewValue} MB  ({e.NewValue / 1024d:0.#} GB)";
        if (_initializing) return;
        _settings.RamMb = (int)e.NewValue;
        HeroRam.Text = $"{e.NewValue / 1024d:0.#} GB";
    }

    private void LabToggle_Click(object sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        _settings.SafeLaunch = SafeLaunchCheck.IsChecked == true;
        _settings.SmartMemory = SmartMemoryCheck.IsChecked == true;
        _settings.FocusMode = FocusModeCheck.IsChecked == true;
        _settingsService.Save(_settings);
        UpdateSelectedInstanceUi();
    }

    private void BrowseJava_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Java Runtime|javaw.exe;java.exe|Executable (*.exe)|*.exe" };
        if (dialog.ShowDialog(this) == true) JavaPathText.Text = dialog.FileName;
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        _settings.RamMb = (int)RamSlider.Value;
        if (int.TryParse(WidthText.Text, out var width)) _settings.Width = Math.Max(640, width);
        if (int.TryParse(HeightText.Text, out var height)) _settings.Height = Math.Max(480, height);
        _settings.JavaPath = JavaPathText.Text.Trim();
        _settings.QuickServer = QuickServerText.Text.Trim();
        _settings.CloseLauncherOnGameStart = CloseOnStartCheck.IsChecked == true;
        _settings.SafeLaunch = SafeLaunchCheck.IsChecked == true;
        _settings.SmartMemory = SmartMemoryCheck.IsChecked == true;
        _settings.FocusMode = FocusModeCheck.IsChecked == true;
        _settingsService.Save(_settings);
        UpdateSelectedInstanceUi();
        AddLog("Einstellungen gespeichert");
        MessageBox.Show(this, "Einstellungen gespeichert.", "LUMINA", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void LoadSettingsIntoUi()
    {
        RamSlider.Value = Math.Clamp(_settings.RamMb, 2048, 16384);
        RamValue.Text = $"{_settings.RamMb} MB  ({_settings.RamMb / 1024d:0.#} GB)";
        WidthText.Text = _settings.Width.ToString();
        HeightText.Text = _settings.Height.ToString();
        JavaPathText.Text = _settings.JavaPath;
        QuickServerText.Text = _settings.QuickServer;
        CloseOnStartCheck.IsChecked = _settings.CloseLauncherOnGameStart;
        SafeLaunchCheck.IsChecked = _settings.SafeLaunch;
        SmartMemoryCheck.IsChecked = _settings.SmartMemory;
        FocusModeCheck.IsChecked = _settings.FocusMode;

        var libraryText = _settings.LibraryKind switch
        {
            "Resource Packs" => "Resource Packs",
            "Shaders" => "Shaders",
            _ => "Mods"
        };
        foreach (var item in LibraryKindCombo.Items.OfType<ComboBoxItem>())
            if (item.Content?.ToString() == libraryText) item.IsSelected = true;
    }

    private async void Login_Click(object sender, RoutedEventArgs e) => await LoginInteractiveAsync();

    private async Task<bool> LoginInteractiveAsync()
    {
        LoginButton.IsEnabled = false;
        AccountState.Text = "MICROSOFT WIRD GEÖFFNET …";
        try
        {
            await _accountService.LoginInteractiveAsync();
            UpdateAccountUi();
            AddLog($"Angemeldet als {_accountService.PlayerName}");
            return true;
        }
        catch (Exception ex)
        {
            AddLog("Microsoft Login: " + ex);
            MessageBox.Show(this, $"Microsoft-Anmeldung fehlgeschlagen:\n\n{ex.Message}", "LUMINA Account", MessageBoxButton.OK, MessageBoxImage.Error);
            UpdateAccountUi();
            return false;
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    private async void SignOut_Click(object sender, RoutedEventArgs e)
    {
        try { await _accountService.SignOutAsync(); }
        catch (Exception ex) { AddLog("Abmelden: " + ex.Message); }
        UpdateAccountUi();
    }

    private void UpdateAccountUi()
    {
        var signedIn = _accountService.IsSignedIn;
        AccountName.Text = signedIn ? _accountService.PlayerName : "Nicht angemeldet";
        AccountMiniName.Text = signedIn ? _accountService.PlayerName : "Nicht angemeldet";
        AccountState.Text = signedIn ? "MINECRAFT JAVA · ANGEMELDET" : "MICROSOFT ACCOUNT";
        AccountState.Foreground = signedIn ? Brush("#54E1A5") : Brush("#687389");
        AccountId.Text = signedIn ? _accountService.PlayerId : "";
        LoginButton.Content = signedIn ? "Konto wechseln" : "Mit Microsoft anmelden";
        SignOutButton.Visibility = signedIn ? Visibility.Visible : Visibility.Collapsed;
    }

    private void RefreshStats()
    {
        var all = _statsService.GetAll();
        var totalMinutes = all.Sum(x => x.Minutes);
        HomePlaytime.Text = totalMinutes < 60 ? $"{totalMinutes:0} min" : $"{totalMinutes / 60d:0.0} h";
        if (SelectedInstance is not null) HomeLaunchCount.Text = SelectedInstance.Launches.ToString();
    }

    private void SetDownloadStatus(string status)
    {
        DownloadStatus.Text = status;
        HomeStatus.Text = status;
    }

    private void SetProgress(double progress)
    {
        DownloadProgress.Value = Math.Clamp(progress, 0, 100);
        DownloadPercent.Text = $"{DownloadProgress.Value:0}%";
    }

    private void AddLog(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;
        _downloadLog.Add($"[{DateTime.Now:HH:mm:ss}] {line}");
        while (_downloadLog.Count > 500) _downloadLog.RemoveAt(0);
        if (DownloadLog.Items.Count > 0) DownloadLog.ScrollIntoView(DownloadLog.Items[^1]);
    }

    private static void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
    }

    private static Brush Brush(string hex) => (Brush)new BrushConverter().ConvertFromString(hex)!;
}
