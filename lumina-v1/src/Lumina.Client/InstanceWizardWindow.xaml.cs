using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Lumina.Client.Services;
using Lumina.Core;

namespace Lumina.Client;

public sealed record InstanceWizardResult(
    string Name,
    string MinecraftVersion,
    string Loader,
    string LoaderVersion,
    bool AllowSnapshots);

public partial class InstanceWizardWindow : Window
{
    private readonly MinecraftCatalogService _catalog;
    private readonly bool _signedIn;
    private readonly InstanceProfile? _source;
    private IReadOnlyList<MinecraftVersionChoice> _minecraftVersions = [];
    private IReadOnlyList<string> _loaderVersions = [];
    private int _step = 1;
    private bool _loading;
    private bool _initialized;

    public InstanceWizardResult? Result { get; private set; }

    public InstanceWizardWindow(
        MinecraftCatalogService catalog,
        bool signedIn,
        string accountName,
        InstanceProfile? source = null)
    {
        InitializeComponent();
        _catalog = catalog;
        _signedIn = signedIn;
        _source = source;

        WizardTitle.Text = source is null ? "Neue Instanz" : "Instanz reparieren";
        AccountLabel.Text = signedIn ? $"{accountName} · angemeldet" : "Microsoft nicht angemeldet";
        NameText.Text = source?.Name ?? "Meine Instanz";
        SnapshotsCheck.IsChecked = source?.AllowSnapshots == true;
        LoaderCombo.ItemsSource = MinecraftCatalogService.Loaders;
        LoaderCombo.SelectedItem = MinecraftCatalogService.Loaders.FirstOrDefault(x =>
            string.Equals(x, InstanceMigrationService.NormalizeLoader(source?.Loader ?? "Vanilla"), StringComparison.OrdinalIgnoreCase)) ?? "Vanilla";

        Loaded += async (_, _) =>
        {
            await LoadMinecraftVersionsAsync();
            _initialized = true;
            UpdatePage();
        };
    }

    private async Task LoadMinecraftVersionsAsync()
    {
        if (_loading) return;
        _loading = true;
        NextButton.IsEnabled = false;
        MinecraftStatus.Text = "Lade offiziellen Minecraft-Katalog …";
        FooterStatus.Text = "Katalog wird geladen …";
        try
        {
            var previous = (MinecraftCombo.SelectedItem as MinecraftVersionChoice)?.Name;
            _minecraftVersions = await _catalog.GetMinecraftVersionsAsync(SnapshotsCheck.IsChecked == true);
            MinecraftCombo.ItemsSource = _minecraftVersions;

            var wanted = previous ?? _source?.Version;
            var selected = _minecraftVersions.FirstOrDefault(x => string.Equals(x.Name, wanted, StringComparison.OrdinalIgnoreCase))
                           ?? _minecraftVersions.FirstOrDefault(x => string.Equals(x.Type, "release", StringComparison.OrdinalIgnoreCase))
                           ?? _minecraftVersions.FirstOrDefault();
            MinecraftCombo.SelectedItem = selected;

            if (_source is not null && !string.IsNullOrWhiteSpace(_source.Version) &&
                !_minecraftVersions.Any(x => string.Equals(x.Name, _source.Version, StringComparison.OrdinalIgnoreCase)))
            {
                CurrentVersionHint.Text = $"Die bisherige Version '{_source.Version}' ist nicht im aktuellen Katalog. Sie wird nicht still geändert – bestätige hier bewusst eine neue Version.";
                CurrentVersionHint.Foreground = Brush("#FF9AA8");
            }
            else
            {
                CurrentVersionHint.Text = "Wähle eine konkrete Version aus dem offiziellen Minecraft-Katalog.";
                CurrentVersionHint.Foreground = Brush("#7E899F");
            }

            MinecraftStatus.Text = _minecraftVersions.Count == 0
                ? "Keine Minecraft-Versionen gefunden."
                : $"{_minecraftVersions.Count} Versionen geladen.";
            await LoadLoaderVersionsAsync();
        }
        catch (Exception ex)
        {
            _minecraftVersions = [];
            MinecraftCombo.ItemsSource = null;
            MinecraftStatus.Text = "Minecraft-Katalog konnte nicht geladen werden.";
            FooterStatus.Text = ex.Message;
        }
        finally
        {
            _loading = false;
            RefreshNavigation();
        }
    }

    private async Task LoadLoaderVersionsAsync()
    {
        if (MinecraftCombo.SelectedItem is not MinecraftVersionChoice minecraft || LoaderCombo.SelectedItem is not string loader)
        {
            _loaderVersions = [];
            LoaderVersionCombo.ItemsSource = null;
            RefreshNavigation();
            return;
        }

        LoaderStatus.Text = $"Prüfe {loader} für Minecraft {minecraft.Name} …";
        LoaderVersionStatus.Text = "Lade kompatible Loader-Versionen …";
        NextButton.IsEnabled = false;
        try
        {
            _loaderVersions = await _catalog.GetLoaderVersionsAsync(minecraft.Name, loader);
            LoaderVersionCombo.ItemsSource = _loaderVersions;

            var wanted = _source is not null && string.Equals(_source.Loader, loader, StringComparison.OrdinalIgnoreCase)
                ? _source.LoaderVersion
                : null;
            LoaderVersionCombo.SelectedItem = _loaderVersions.FirstOrDefault(x => string.Equals(x, wanted, StringComparison.OrdinalIgnoreCase))
                                                   ?? _loaderVersions.FirstOrDefault();

            if (loader.Equals("Vanilla", StringComparison.OrdinalIgnoreCase))
            {
                LoaderStatus.Text = "Vanilla ist verfügbar.";
                LoaderVersionDescription.Text = "Vanilla benötigt keinen zusätzlichen Modloader und verwendet Standard.";
                LoaderVersionStatus.Text = "Standard";
            }
            else if (_loaderVersions.Count == 0)
            {
                LoaderStatus.Text = $"{loader} ist für Minecraft {minecraft.Name} nicht verfügbar.";
                LoaderVersionDescription.Text = $"Für {loader} muss eine konkrete, kompatible Loader-Version existieren.";
                LoaderVersionStatus.Text = "Keine kompatible Loader-Version gefunden.";
            }
            else
            {
                LoaderStatus.Text = $"{_loaderVersions.Count} kompatible {loader}-Versionen verfügbar.";
                LoaderVersionDescription.Text = $"Wähle die konkrete {loader}-Version für Minecraft {minecraft.Name}.";
                LoaderVersionStatus.Text = $"Empfohlen: {_loaderVersions[0]}";
            }
        }
        catch (Exception ex)
        {
            _loaderVersions = [];
            LoaderVersionCombo.ItemsSource = null;
            LoaderStatus.Text = "Loader-Abfrage fehlgeschlagen.";
            LoaderVersionStatus.Text = ex.Message;
        }
        finally
        {
            RefreshNavigation();
        }
    }

    private async void Minecraft_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized && _loading) return;
        await LoadLoaderVersionsAsync();
        RefreshSummaryAndValidation();
    }

    private async void Loader_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_initialized && _loading) return;
        await LoadLoaderVersionsAsync();
        RefreshSummaryAndValidation();
    }

    private async void Snapshots_Changed(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;
        await LoadMinecraftVersionsAsync();
    }

    private void WizardValue_Changed(object sender, RoutedEventArgs e)
    {
        if (!_initialized) return;
        RefreshNavigation();
        RefreshSummaryAndValidation();
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        if (!CanAdvance()) return;
        if (_step < 5) _step++;
        UpdatePage();
    }

    private void Back_Click(object sender, RoutedEventArgs e)
    {
        if (_step > 1) _step--;
        UpdatePage();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void Create_Click(object sender, RoutedEventArgs e)
    {
        var validation = ValidateCurrent();
        if (!InstanceWizardPolicy.CanCreateInstance(_signedIn, NameText.Text, validation))
        {
            ValidationText.Text = validation.Error ?? "Melde dich mit Microsoft an und vervollständige alle Schritte.";
            ValidationBox.Background = Brush("#2A1118");
            ValidationBox.BorderBrush = Brush("#6B2633");
            return;
        }

        var minecraft = (MinecraftCombo.SelectedItem as MinecraftVersionChoice)!.Name;
        var loader = (string)LoaderCombo.SelectedItem;
        var loaderVersion = loader.Equals("Vanilla", StringComparison.OrdinalIgnoreCase)
            ? "Standard"
            : LoaderVersionCombo.SelectedItem?.ToString() ?? "";

        Result = new InstanceWizardResult(
            NameText.Text.Trim(),
            minecraft,
            loader,
            loaderVersion,
            SnapshotsCheck.IsChecked == true);
        DialogResult = true;
        Close();
    }

    private void UpdatePage()
    {
        NameStep.Visibility = _step == 1 ? Visibility.Visible : Visibility.Collapsed;
        MinecraftStep.Visibility = _step == 2 ? Visibility.Visible : Visibility.Collapsed;
        LoaderStep.Visibility = _step == 3 ? Visibility.Visible : Visibility.Collapsed;
        LoaderVersionStep.Visibility = _step == 4 ? Visibility.Visible : Visibility.Collapsed;
        SummaryStep.Visibility = _step == 5 ? Visibility.Visible : Visibility.Collapsed;

        var names = new[] { "Name", "Minecraft-Version", "Loader", "Loader-Version", "Zusammenfassung" };
        WizardSubtitle.Text = $"Schritt {_step} von 5 · {names[_step - 1]}";
        BackButton.Visibility = _step > 1 ? Visibility.Visible : Visibility.Collapsed;
        NextButton.Visibility = _step < 5 ? Visibility.Visible : Visibility.Collapsed;
        CreateButton.Visibility = _step == 5 ? Visibility.Visible : Visibility.Collapsed;
        CreateButton.Content = _source is null ? "Instanz erstellen" : "Reparatur speichern";

        var badges = new[] { Step1Badge, Step2Badge, Step3Badge, Step4Badge, Step5Badge };
        for (var i = 0; i < badges.Length; i++)
            badges[i].Background = Brush(i + 1 == _step ? "#6F55E8" : i + 1 < _step ? "#183329" : "#111722");

        if (_step == 5) RefreshSummaryAndValidation();
        RefreshNavigation();
    }

    private bool CanAdvance()
    {
        if (_loading) return false;
        return _step switch
        {
            1 => !string.IsNullOrWhiteSpace(NameText.Text),
            2 => MinecraftCombo.SelectedItem is MinecraftVersionChoice,
            3 => LoaderCombo.SelectedItem is string,
            4 => LoaderCombo.SelectedItem is string loader &&
                 (loader.Equals("Vanilla", StringComparison.OrdinalIgnoreCase) || LoaderVersionCombo.SelectedItem is string),
            _ => true
        };
    }

    private InstanceValidationResult ValidateCurrent()
    {
        if (MinecraftCombo.SelectedItem is not MinecraftVersionChoice minecraft)
            return InstanceValidationResult.Fail("Wähle eine Minecraft-Version.");
        if (LoaderCombo.SelectedItem is not string loader)
            return InstanceValidationResult.Fail("Wähle einen Loader.");

        var profile = new InstanceProfile
        {
            Name = NameText.Text.Trim(),
            Version = minecraft.Name,
            Loader = loader,
            LoaderVersion = loader.Equals("Vanilla", StringComparison.OrdinalIgnoreCase)
                ? "Standard"
                : LoaderVersionCombo.SelectedItem?.ToString() ?? "",
            AllowSnapshots = SnapshotsCheck.IsChecked == true
        };

        return InstanceValidationService.Validate(
            profile,
            _minecraftVersions.Select(x => x.Name).ToArray(),
            _loaderVersions);
    }

    private void RefreshNavigation()
    {
        if (NextButton is null || CreateButton is null) return;
        NextButton.IsEnabled = CanAdvance();
        if (_step == 1) NameError.Text = string.IsNullOrWhiteSpace(NameText.Text) ? "Gib einen Namen ein." : "";

        var validation = ValidateCurrent();
        CreateButton.IsEnabled = _step == 5 && InstanceWizardPolicy.CanCreateInstance(_signedIn, NameText.Text, validation);
        FooterStatus.Text = !_signedIn
            ? "Für neue oder reparierte Instanzen ist eine Microsoft-Anmeldung erforderlich."
            : validation.IsValid ? "Konfiguration geprüft." : validation.Error ?? "";
    }

    private void RefreshSummaryAndValidation()
    {
        if (SummaryName is null) return;
        SummaryName.Text = string.IsNullOrWhiteSpace(NameText.Text) ? "—" : NameText.Text.Trim();
        SummaryMinecraft.Text = (MinecraftCombo.SelectedItem as MinecraftVersionChoice)?.Name ?? "—";
        SummaryLoader.Text = LoaderCombo.SelectedItem?.ToString() ?? "—";
        SummaryLoaderVersion.Text = LoaderCombo.SelectedItem is string loader && loader.Equals("Vanilla", StringComparison.OrdinalIgnoreCase)
            ? "Standard"
            : LoaderVersionCombo.SelectedItem?.ToString() ?? "—";

        var validation = ValidateCurrent();
        if (!_signedIn)
        {
            ValidationText.Text = "Microsoft-Anmeldung fehlt.";
            ValidationBox.Background = Brush("#2A1118");
            ValidationBox.BorderBrush = Brush("#6B2633");
            ValidationText.Foreground = Brush("#FF9AA8");
        }
        else if (validation.IsValid)
        {
            ValidationText.Text = "Konfiguration ist gültig und kann gespeichert werden.";
            ValidationBox.Background = Brush("#10241D");
            ValidationBox.BorderBrush = Brush("#235B48");
            ValidationText.Foreground = Brush("#72E0AE");
        }
        else
        {
            ValidationText.Text = validation.Error ?? "Ungültige Konfiguration.";
            ValidationBox.Background = Brush("#2A1118");
            ValidationBox.BorderBrush = Brush("#6B2633");
            ValidationText.Foreground = Brush("#FF9AA8");
        }
        RefreshNavigationWithoutValidationLoop();
    }

    private void RefreshNavigationWithoutValidationLoop()
    {
        NextButton.IsEnabled = CanAdvance();
        var validation = ValidateCurrent();
        CreateButton.IsEnabled = _step == 5 && InstanceWizardPolicy.CanCreateInstance(_signedIn, NameText.Text, validation);
    }

    private static Brush Brush(string hex) => (Brush)new BrushConverter().ConvertFromString(hex)!;
}
