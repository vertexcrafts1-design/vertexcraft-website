using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Lumina.Client.Services;
using Lumina.Client.Ui;
using Lumina.Core;

namespace Lumina.Client;

public partial class MainWindow
{
    private bool _enhancedDiscoverReady;
    private DiscoverStorefrontView? _discoverStorefront;
    private InstalledUpdatesView? _installedUpdatesView;
    private LuminaCollectionService? _luminaCollectionService;
    private VerificationService? _verificationService;
    private ManagedContentUpdateService? _managedUpdateService;
    private bool? _curatorVerified;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        if (_enhancedDiscoverReady) return;
        InitializeEnhancedDiscover();
        Dispatcher.BeginInvoke(async () => await RefreshStorefrontAsync(), DispatcherPriority.Background);
    }

    private void InitializeEnhancedDiscover()
    {
        if (_enhancedDiscoverReady) return;
        _enhancedDiscoverReady = true;

        _luminaCollectionService = new LuminaCollectionService();
        _verificationService = new VerificationService();
        _managedUpdateService = new ManagedContentUpdateService(_paths, _modrinthService);
        _installedUpdatesView = new InstalledUpdatesView();
        _installedUpdatesView.UpdateRequested += InstalledUpdate_Requested;
        _installedUpdatesView.RefreshRequested += async (_, _) => await LoadInstalledUpdatesAsync();

        _discoverStorefront = new DiscoverStorefrontView();
        _discoverStorefront.SetProjects(_gallery);
        _discoverStorefront.SetInstalledView(_installedUpdatesView);
        _discoverStorefront.SetInstanceContext(SelectedInstance);
        _discoverStorefront.SearchRequested += Storefront_SearchRequested;
        _discoverStorefront.InstallRequested += Storefront_InstallRequested;
        _discoverStorefront.CurateRequested += Storefront_CurateRequested;

        DiscoverPage.Children.Clear();
        DiscoverPage.RowDefinitions.Clear();
        DiscoverPage.Children.Add(_discoverStorefront);
        DiscoverPage.IsVisibleChanged += async (_, _) =>
        {
            if (DiscoverPage.Visibility == Visibility.Visible)
            {
                _discoverStorefront.SetInstanceContext(SelectedInstance);
                await RefreshStorefrontAsync();
            }
        };
    }

    private async void Storefront_SearchRequested(object? sender, DiscoverRequest request) =>
        await RefreshStorefrontAsync(request);

    private async Task RefreshStorefrontAsync(DiscoverRequest? request = null)
    {
        if (!_enhancedDiscoverReady || _discoverStorefront is null || SelectedInstance is null) return;

        request ??= new DiscoverRequest(
            _discoverStorefront.Query,
            _discoverStorefront.Category,
            _discoverStorefront.SourceMode);
        _discoverStorefront.SetInstanceContext(SelectedInstance);

        if (request.Source == DiscoverSourceMode.Installed)
        {
            _discoverStorefront.ShowInstalled();
            await LoadInstalledUpdatesAsync();
            return;
        }

        _discoverStorefront.SetProjects(_gallery);
        if (request.Source == DiscoverSourceMode.Collection)
        {
            await LoadLuminaCollectionAsync(request.Category, request.Query);
            return;
        }

        await LoadModrinthStorefrontAsync(request);
    }

    private async Task LoadModrinthStorefrontAsync(DiscoverRequest request)
    {
        if (SelectedInstance is null || _discoverStorefront is null) return;
        var kind = StorefrontKind(request.Category);

        if (kind == "Mods" && SelectedInstance.Loader.Equals("Vanilla", StringComparison.OrdinalIgnoreCase))
        {
            _gallery.Clear();
            _discoverStorefront.SetEmpty(true,
                "Vanilla lädt keine Modloader-Mods. Nutze Fabric, Forge, NeoForge oder Quilt.");
            return;
        }

        SetDownloadStatus("Durchsuche Modrinth …");
        try
        {
            var sort = request.Category == "Featured" ? "downloads" : "relevance";
            var projects = await _modrinthService.SearchAsync(request.Query, kind, SelectedInstance, sort, 40);
            _gallery.Clear();
            foreach (var project in projects) _gallery.Add(project);
            _discoverStorefront.SetEmpty(_gallery.Count == 0, "Keine kompatiblen Projekte gefunden.");
            SetDownloadStatus("Bereit");
        }
        catch (Exception ex)
        {
            _gallery.Clear();
            _discoverStorefront.SetEmpty(true, "Modrinth konnte gerade nicht geladen werden.");
            AddLog("Discover / Modrinth: " + ex.Message);
            SetDownloadStatus("Discover-Fehler");
            ShowFeedback(NotificationKind.Error, "Discover", "Modrinth konnte nicht geladen werden. Minecraft-Start bleibt davon unberührt.");
        }
    }

    private async Task LoadLuminaCollectionAsync(string category, string query)
    {
        if (SelectedInstance is null || _luminaCollectionService is null || _discoverStorefront is null) return;
        var kindText = StorefrontKind(category);
        if (kindText == "Modpacks")
        {
            _gallery.Clear();
            _discoverStorefront.SetEmpty(true, "Die LUMINA Collection unterstützt aktuell Mods, Shader und Resource Packs.");
            return;
        }

        SetDownloadStatus("Lade LUMINA Collection …");
        try
        {
            var collectionCategory = CategoryForKind(kindText);
            var contentKind = ModrinthService.KindToContentKind(kindText);
            var entries = (await _luminaCollectionService.GetAsync())
                .Where(x => x.Category.Equals(collectionCategory, StringComparison.OrdinalIgnoreCase))
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
                catch
                {
                    // One bad source project must not break the collection.
                }
            }

            var projects = await _modrinthService.GetProjectsAsync(compatibleIds);
            if (!string.IsNullOrWhiteSpace(query))
                projects = projects.Where(x =>
                    x.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    x.Description.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

            _gallery.Clear();
            foreach (var project in projects) _gallery.Add(project);
            _discoverStorefront.SetEmpty(_gallery.Count == 0,
                entries.Count == 0
                    ? "Die LUMINA Collection ist leer oder gerade nicht erreichbar. Modrinth funktioniert weiterhin."
                    : "Für diese Instanz gibt es aktuell keinen kompatiblen Collection-Eintrag.");
            SetDownloadStatus("Bereit");
        }
        catch (Exception ex)
        {
            _gallery.Clear();
            _discoverStorefront.SetEmpty(true, "LUMINA Collection ist gerade nicht erreichbar. Nutze solange Modrinth.");
            AddLog("LUMINA Collection: " + ex.Message);
            SetDownloadStatus("Bereit");
        }
    }

    private async void Storefront_InstallRequested(object? sender, GalleryProject project)
    {
        if (SelectedInstance is null || _discoverStorefront is null) return;
        var kind = StorefrontKind(_discoverStorefront.Category);
        if (kind == "Modpacks")
        {
            ShowFeedback(NotificationKind.Info, "Modpack", "Modpacks können bereits durchsucht werden; ein eigener Instanz-Import folgt getrennt, damit keine .mrpack-Datei fälschlich als Mod installiert wird.");
            return;
        }

        try
        {
            var progress = new Progress<string>(message =>
            {
                SetDownloadStatus(message);
                AddLog(message);
            });
            var result = await _modrinthService.InstallAsync(project, kind, SelectedInstance, _paths, progress);
            AddLog($"Discover installiert: {project.Title} · {result.FilesInstalled} Datei(en)");
            RefreshContent();
            UpdateSelectedInstanceUi();
            SetDownloadStatus($"{project.Title} installiert");
            ShowFeedback(NotificationKind.Success, "Installiert", $"{project.Title} ist jetzt in {SelectedInstance.Name} installiert.");
        }
        catch (Exception ex)
        {
            AddLog($"Discover-Installation fehlgeschlagen: {ex}");
            ShowFeedback(NotificationKind.Error, "Installation fehlgeschlagen", ex.Message);
        }
    }

    private async void Storefront_CurateRequested(object? sender, GalleryProject project)
    {
        if (_verificationService is null || _luminaCollectionService is null || _discoverStorefront is null) return;
        if (!_accountService.IsSignedIn || _accountService.Session is null)
        {
            ShowFeedback(NotificationKind.Info, "Anmeldung erforderlich", "Melde dich an, um die LUMINA Collection zu verwalten.");
            return;
        }

        _curatorVerified = await _verificationService.IsVerifiedAsync(_accountService.PlayerId);
        if (_curatorVerified != true)
        {
            ShowFeedback(NotificationKind.Info, "LUMINA Collection", "Nur verifizierte LUMINA-Kuratoren können Projekte zur globalen Collection hinzufügen.");
            return;
        }

        var kind = StorefrontKind(_discoverStorefront.Category);
        if (kind == "Modpacks")
        {
            ShowFeedback(NotificationKind.Warning, "LUMINA Collection", "Modpacks sind für die Collection noch nicht freigeschaltet.");
            return;
        }

        try
        {
            await _luminaCollectionService.AddAsync(project.ProjectId, CategoryForKind(kind), "", _accountService.Session);
            AddLog($"LUMINA Collection: {project.Title} hinzugefügt");
            ShowFeedback(NotificationKind.Success, "LUMINA Collection", $"{project.Title} wurde zur Collection hinzugefügt.");
        }
        catch (Exception ex)
        {
            AddLog("Collection hinzufügen: " + ex.Message);
            ShowFeedback(NotificationKind.Warning, "LUMINA Collection", ex.Message);
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
            ShowFeedback(NotificationKind.Warning, "Updates", "Installierter Content konnte gerade nicht vollständig geprüft werden.");
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
            ShowFeedback(NotificationKind.Success, "Update installiert", $"{item.Title} wurde aktualisiert.");
        }
        catch (Exception ex)
        {
            AddLog("Update fehlgeschlagen: " + ex);
            ShowFeedback(NotificationKind.Error, "Update fehlgeschlagen", ex.Message);
        }
    }

    private static string StorefrontKind(string category) => category switch
    {
        "Shaders" => "Shaders",
        "Resource Packs" => "Resource Packs",
        "Modpacks" => "Modpacks",
        _ => "Mods"
    };

    private static string CategoryForKind(string kind) => kind switch
    {
        "Resource Packs" => "resourcepack",
        "Shaders" => "shader",
        "Modpacks" => "modpack",
        _ => "mod"
    };
}
