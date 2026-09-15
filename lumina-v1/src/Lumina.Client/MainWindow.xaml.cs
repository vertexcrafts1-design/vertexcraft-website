using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

    private readonly ObservableCollection<InstanceProfile> _instances = [];
    private readonly ObservableCollection<ContentItem> _content = [];
    private readonly ObservableCollection<string> _recent = [];
    private readonly ObservableCollection<string> _downloadLog = [];

    private AppSettings _settings;
    private ContentKind _libraryKind = ContentKind.Mod;
    private bool _initializing = true;
    private bool _launching;

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
        _settings = _settingsService.Load();

        InstancesList.ItemsSource = _instances;
        ContentList.ItemsSource = _content;
        RecentList.ItemsSource = _recent;
        DownloadLog.ItemsSource = _downloadLog;

        _minecraftService.StatusChanged += status => Dispatcher.Invoke(() => SetDownloadStatus(status));
        _minecraftService.ProgressChanged += progress => Dispatcher.Invoke(() => DownloadProgress.Value = progress);

        LoadSettingsIntoUi();
        RefreshInstances();
        RefreshStats();
        ShowPage("Home");
        _initializing = false;

        Loaded += async (_, _) =>
        {
            SetDownloadStatus("Prüfe Microsoft-Anmeldung …");
            await _accountService.TrySilentAsync();
            UpdateAccountUi();
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
    private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string page) ShowPage(page);
    }

    private void ShowPage(string page)
    {
        var pages = new UIElement[] { HomePage, InstancesPage, LibraryPage, LabPage, DownloadsPage, SettingsPage, AccountPage };
        foreach (var element in pages) element.Visibility = Visibility.Collapsed;

        switch (page)
        {
            case "Instances": InstancesPage.Visibility = Visibility.Visible; PageTitle.Text = "Instanzen"; break;
            case "Library": LibraryPage.Visibility = Visibility.Visible; PageTitle.Text = "Bibliothek"; RefreshContent(); break;
            case "Lab": LabPage.Visibility = Visibility.Visible; PageTitle.Text = "LUMINA Lab"; break;
            case "Downloads": DownloadsPage.Visibility = Visibility.Visible; PageTitle.Text = "Downloads"; break;
            case "Settings": SettingsPage.Visibility = Visibility.Visible; PageTitle.Text = "Einstellungen"; break;
            case "Account": AccountPage.Visibility = Visibility.Visible; PageTitle.Text = "Account"; UpdateAccountUi(); break;
            default: HomePage.Visibility = Visibility.Visible; PageTitle.Text = "Home"; RefreshStats(); break;
        }

        foreach (var button in SidebarNav.Children.OfType<Button>())
        {
            var active = string.Equals(button.Tag as string, page, StringComparison.OrdinalIgnoreCase) ||
                         (page == "Home" && string.Equals(button.Tag as string, "Home", StringComparison.OrdinalIgnoreCase));
            button.Background = active ? Brush("#1A2030") : Brushes.Transparent;
            button.Foreground = active ? Brushes.White : Brush("#A9B0C2");
        }
    }

    private void RefreshInstances(string? selectId = null)
    {
        var wanted = selectId ?? _settings.SelectedInstanceId;
        _instances.Clear();
        foreach (var profile in _instanceService.GetAll()) _instances.Add(profile);

        if (_instances.Count == 0)
        {
            var created = _instanceService.Create("LUMINA Vanilla", "1.21.1", "Vanilla");
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
    }

    private void UpdateSelectedInstanceUi()
    {
        var profile = SelectedInstance;
        if (profile is null) return;

        HeroInstanceName.Text = profile.Name;
        HeroInstanceMeta.Text = profile.Subtitle;
        HeroPreset.Text = profile.Preset;
        HeroSafeLaunch.Text = _settings.SafeLaunch ? "  AKTIV" : "  AUS";
        HeroSafeLaunch.Foreground = _settings.SafeLaunch ? Brush("#9AE6B4") : Brush("#F5A0AA");
        InstanceDetailName.Text = profile.Name;
        InstanceDetailMeta.Text = profile.Subtitle;
        InstanceDetailPreset.Text = profile.Preset;
        InstanceDetailLaunches.Text = profile.Launches.ToString();
        LibraryInstanceName.Text = $"Content für {profile.Name}";

        var contentCount = _contentService.Get(profile.Id, ContentKind.Mod).Count
                         + _contentService.Get(profile.Id, ContentKind.ResourcePack).Count
                         + _contentService.Get(profile.Id, ContentKind.ShaderPack).Count;
        HomeContentCount.Text = contentCount.ToString();
        HeroContentMini.Text = $"  {contentCount} Dateien";
        HomeLaunchCount.Text = profile.Launches.ToString();
    }

    private void NewInstance_Click(object sender, RoutedEventArgs e) => NewInstancePanel.Visibility = Visibility.Visible;
    private void CancelCreate_Click(object sender, RoutedEventArgs e) => NewInstancePanel.Visibility = Visibility.Collapsed;

    private void CreateInstance_Click(object sender, RoutedEventArgs e)
    {
        var loader = (NewInstanceLoader.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Fabric";
        var profile = _instanceService.Create(NewInstanceName.Text, NewInstanceVersion.Text, loader);
        NewInstancePanel.Visibility = Visibility.Collapsed;
        RefreshInstances(profile.Id);
        AddLog($"Instanz erstellt: {profile.Name} ({profile.Subtitle})");
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
        var answer = MessageBox.Show(this, $"'{deleting.Name}' inklusive eigener Mods, Packs und Spiel-Dateien wirklich löschen?", "Instanz löschen", MessageBoxButton.YesNo, MessageBoxImage.Warning);
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

    private void LibraryKind_Click(object sender, RoutedEventArgs e)
    {
        var tag = (sender as Button)?.Tag?.ToString();
        _libraryKind = tag switch
        {
            "ResourcePacks" => ContentKind.ResourcePack,
            "ShaderPacks" => ContentKind.ShaderPack,
            _ => ContentKind.Mod
        };
        _settings.LibraryKind = tag ?? "Mods";
        _settingsService.Save(_settings);
        RefreshContent();
    }

    private void RefreshContent()
    {
        if (SelectedInstance is null || ContentList is null) return;
        _content.Clear();
        foreach (var item in _contentService.Get(SelectedInstance.Id, _libraryKind)) _content.Add(item);
        LibraryDropText.Text = _libraryKind == ContentKind.Mod ? "JAR-Mods hier ablegen" : _libraryKind == ContentKind.ResourcePack ? "ZIP-Resourcepacks hier ablegen" : "ZIP-Shaderpacks hier ablegen";
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
        if (dialog.ShowDialog(this) != true) return;
        ImportContent(dialog.FileNames);
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
        var result = MessageBox.Show(this, $"{item.Name} aus dieser Instanz entfernen?", "Content entfernen", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;
        _contentService.Delete(item.Path);
        RefreshContent();
    }

    private void OpenContentFolder_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedInstance is null) return;
        OpenFolder(_contentService.Folder(SelectedInstance.Id, _libraryKind));
    }

    private void Preset_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedInstance is null || sender is not Button button || button.Tag is not string presetName) return;
        var profile = SelectedInstance;
        profile.Preset = presetName;
        _instanceService.Update(profile);
        var preset = PresetService.Get(presetName);
        if (!_settings.SmartMemory)
        {
            _settings.RamMb = preset.RamMb;
            RamSlider.Value = preset.RamMb;
        }
        _settingsService.Save(_settings);
        UpdateSelectedInstanceUi();
        AddLog($"Performance-Profil: {presetName}");
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

    private void SaveQuickServer_Click(object sender, RoutedEventArgs e)
    {
        _settings.QuickServer = QuickServerText.Text.Trim();
        _settingsService.Save(_settings);
        AddLog(string.IsNullOrWhiteSpace(_settings.QuickServer) ? "Quick Connect deaktiviert" : $"Quick Connect: {_settings.QuickServer}");
    }

    private void RamSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (RamValue is null) return;
        RamValue.Text = $"{(int)e.NewValue} MB  ({e.NewValue / 1024d:0.#} GB)";
        if (_initializing) return;
        _settings.RamMb = (int)e.NewValue;
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
        _settings.CloseLauncherOnGameStart = CloseOnStartCheck.IsChecked == true;
        _settingsService.Save(_settings);
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
        CloseOnStartCheck.IsChecked = _settings.CloseLauncherOnGameStart;
        SafeLaunchCheck.IsChecked = _settings.SafeLaunch;
        SmartMemoryCheck.IsChecked = _settings.SmartMemory;
        FocusModeCheck.IsChecked = _settings.FocusMode;
        QuickServerText.Text = _settings.QuickServer;
        _libraryKind = _settings.LibraryKind switch
        {
            "ResourcePacks" => ContentKind.ResourcePack,
            "ShaderPacks" => ContentKind.ShaderPack,
            _ => ContentKind.Mod
        };
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
            MessageBox.Show(this, $"Microsoft-Anmeldung fehlgeschlagen:\n\n{ex.Message}", "LUMINA Account", MessageBoxButton.OK, MessageBoxImage.Error);
            UpdateAccountUi();
            return false;
        }
        finally
        {
            LoginButton.IsEnabled = true;
        }
    }

    private async void Logout_Click(object sender, RoutedEventArgs e)
    {
        try { await _accountService.SignOutAsync(); }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "LUMINA", MessageBoxButton.OK, MessageBoxImage.Warning); }
        UpdateAccountUi();
    }

    private void UpdateAccountUi()
    {
        if (_accountService.IsSignedIn)
        {
            AccountState.Text = "ANGEMELDET";
            AccountState.Foreground = Brush("#9AE6B4");
            AccountPlayerName.Text = _accountService.PlayerName;
            AccountUuid.Text = _accountService.PlayerId;
            AccountMiniName.Text = _accountService.PlayerName;
            LoginButton.Visibility = Visibility.Collapsed;
            LogoutButton.Visibility = Visibility.Visible;
        }
        else
        {
            AccountState.Text = "NICHT ANGEMELDET";
            AccountState.Foreground = Brush("#8876CE");
            AccountPlayerName.Text = "Microsoft Account";
            AccountUuid.Text = "Melde dich an, um Minecraft Java zu starten.";
            AccountMiniName.Text = "Nicht angemeldet";
            LoginButton.Visibility = Visibility.Visible;
            LogoutButton.Visibility = Visibility.Collapsed;
        }
    }

    private async void Play_Click(object sender, RoutedEventArgs e)
    {
        if (_launching || SelectedInstance is null) return;
        var profile = SelectedInstance;

        if (!_accountService.IsSignedIn && !await LoginInteractiveAsync())
        {
            ShowPage("Account");
            return;
        }

        _launching = true;
        HomePlayButton.IsEnabled = false;
        DownloadProgress.Value = 0;
        ShowPage("Downloads");
        var started = DateTime.UtcNow;

        try
        {
            if (_settings.SmartMemory)
            {
                var totalMemory = GetTotalPhysicalMemory();
                if (totalMemory > 0)
                {
                    _settings.RamMb = PresetService.SmartRamMb(totalMemory);
                    RamSlider.Value = _settings.RamMb;
                }
            }

            if (_settings.SafeLaunch)
            {
                SetDownloadStatus("Safe Launch: sichere Config …");
                var backup = _safeLaunchService.CreateSnapshot(profile);
                AddLog($"Safe Launch Backup: {Path.GetFileName(backup)}");
            }

            var process = await _minecraftService.LaunchAsync(profile, _settings, _accountService.Session!);
            profile.Launches++;
            profile.LastPlayedUtc = started;
            _instanceService.Update(profile);
            UpdateSelectedInstanceUi();

            var record = new LaunchRecord { StartedUtc = started, InstanceId = profile.Id, InstanceName = profile.Name };
            process.Exited += (_, _) =>
            {
                record.EndedUtc = DateTime.UtcNow;
                try { record.ExitCode = process.ExitCode; } catch { }
                _statsService.Add(record);
                Dispatcher.BeginInvoke(() =>
                {
                    AddLog($"Minecraft beendet (Code {record.ExitCode})");
                    SetDownloadStatus("Bereit");
                    RefreshStats();
                    if (_settings.FocusMode)
                    {
                        WindowState = WindowState.Normal;
                        Activate();
                    }
                });
            };

            AddLog($"Gestartet: {profile.Name} mit {_settings.RamMb / 1024d:0.#} GB RAM");
            HomeStatus.Text = "Minecraft läuft";
            if (_settings.FocusMode) WindowState = WindowState.Minimized;
            if (_settings.CloseLauncherOnGameStart) Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            SetDownloadStatus("Start fehlgeschlagen");
            AddLog("FEHLER: " + ex.Message);
            MessageBox.Show(this, $"Minecraft konnte nicht gestartet werden.\n\n{ex.Message}", "LUMINA", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _launching = false;
            HomePlayButton.IsEnabled = true;
        }
    }

    private void RefreshStats()
    {
        if (HomePlaytime is null) return;
        HomePlaytime.Text = FormatMinutes(_statsService.TotalMinutes);
        _recent.Clear();
        foreach (var item in _statsService.GetAll().Take(6))
        {
            var duration = item.EndedUtc is null ? "läuft" : FormatMinutes(item.Minutes);
            _recent.Add($"{item.InstanceName}   •   {item.StartedUtc.ToLocalTime():dd.MM. HH:mm}   •   {duration}");
        }
        if (_recent.Count == 0) _recent.Add("Noch keine abgeschlossene Session – Zeit für den ersten Start.");
        UpdateSelectedInstanceUi();
    }

    private void SetDownloadStatus(string status)
    {
        DownloadStatus.Text = status;
        HomeStatus.Text = status;
        AddLog(status);
    }

    private void AddLog(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        if (_downloadLog.Count == 0 || _downloadLog[^1] != line) _downloadLog.Add(line);
        while (_downloadLog.Count > 150) _downloadLog.RemoveAt(0);
        if (DownloadLog.Items.Count > 0) DownloadLog.ScrollIntoView(DownloadLog.Items[^1]);
    }

    private static string FormatMinutes(double minutes)
    {
        if (minutes < 1) return "< 1 min";
        if (minutes < 60) return $"{minutes:0} min";
        return $"{minutes / 60d:0.0} h";
    }

    private static void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
    }

    private static SolidColorBrush Brush(string hex) => new((Color)ColorConverter.ConvertFromString(hex));

    private static ulong GetTotalPhysicalMemory()
    {
        var status = new MemoryStatusEx();
        return GlobalMemoryStatusEx(ref status) ? status.TotalPhysical : 0;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhysical;
        public ulong AvailablePhysical;
        public ulong TotalPageFile;
        public ulong AvailablePageFile;
        public ulong TotalVirtual;
        public ulong AvailableVirtual;
        public ulong AvailableExtendedVirtual;

        public MemoryStatusEx()
        {
            Length = (uint)Marshal.SizeOf<MemoryStatusEx>();
            MemoryLoad = 0;
            TotalPhysical = AvailablePhysical = TotalPageFile = AvailablePageFile = TotalVirtual = AvailableVirtual = AvailableExtendedVirtual = 0;
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);
}
