using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Lumina.Core;

namespace Lumina.Client;

public partial class MainWindow
{
    private Button? _wizardNewInstanceButton;
    private Button? _instancePrimaryButton;
    private bool _instanceSafetyReady;

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        _wizardNewInstanceButton = FindButton(this, text => text.Contains("Neue Instanz", StringComparison.OrdinalIgnoreCase));
        if (_wizardNewInstanceButton is not null)
        {
            _wizardNewInstanceButton.Click -= NewInstance_Click;
            _wizardNewInstanceButton.Click += WizardNewInstance_Click;
        }

        HomePlayButton.Click -= Play_Click;
        HomePlayButton.Click += SafePlay_Click;

        _instancePrimaryButton = FindButton(this, text =>
            text.Contains("Spielen", StringComparison.OrdinalIgnoreCase), HomePlayButton);
        if (_instancePrimaryButton is not null)
        {
            _instancePrimaryButton.Click -= Play_Click;
            _instancePrimaryButton.Click += SafePlay_Click;
        }

        InstancesList.SelectionChanged += (_, _) =>
            Dispatcher.BeginInvoke(RefreshWizardSafetyUi, DispatcherPriority.Background);

        Loaded += (_, _) => Dispatcher.BeginInvoke(
            async () => await MigrateAndValidateInstancesAsync(),
            DispatcherPriority.ContextIdle);
    }

    private async void WizardNewInstance_Click(object sender, RoutedEventArgs e)
    {
        if (!_accountService.IsSignedIn && !await LoginInteractiveAsync()) return;
        if (!_accountService.IsSignedIn) return;

        var wizard = new InstanceWizardWindow(
            _catalogService,
            signedIn: true,
            accountName: _accountService.PlayerName)
        {
            Owner = this
        };

        if (wizard.ShowDialog() != true || wizard.Result is null) return;
        var result = wizard.Result;

        var profile = _instanceService.Create(result.Name, result.MinecraftVersion, result.Loader);
        profile.LoaderVersion = result.LoaderVersion;
        profile.AllowSnapshots = result.AllowSnapshots;
        profile.IsValid = true;
        profile.InvalidReason = "";
        _instanceService.Update(profile);
        NewInstancePanel.Visibility = Visibility.Collapsed;
        RefreshInstances(profile.Id);
        RefreshWizardSafetyUi();
        AddLog($"Instanz erstellt: {profile.Name} · {profile.Version} · {profile.Loader} {profile.LoaderVersion}");
    }

    private async void SafePlay_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedInstance is null) return;
        var profile = SelectedInstance;

        if (!profile.IsValid)
        {
            await OpenRepairWizardAsync(profile);
            return;
        }

        var validation = await ValidateForLaunchAsync(profile);
        if (!validation.IsValid)
        {
            profile.IsValid = false;
            profile.InvalidReason = validation.Error ?? "Ungültige Instanz-Konfiguration.";
            _instanceService.Update(profile);
            RefreshWizardSafetyUi();
            await OpenRepairWizardAsync(profile);
            return;
        }

        Play_Click(sender, e);
    }

    private async Task OpenRepairWizardAsync(InstanceProfile profile)
    {
        if (!_accountService.IsSignedIn && !await LoginInteractiveAsync()) return;
        if (!_accountService.IsSignedIn) return;

        var wizard = new InstanceWizardWindow(
            _catalogService,
            signedIn: true,
            accountName: _accountService.PlayerName,
            source: profile)
        {
            Owner = this
        };

        if (wizard.ShowDialog() != true || wizard.Result is null) return;
        var result = wizard.Result;

        profile.Name = result.Name;
        profile.Version = result.MinecraftVersion;
        profile.Loader = result.Loader;
        profile.LoaderVersion = result.LoaderVersion;
        profile.AllowSnapshots = result.AllowSnapshots;
        profile.IsValid = true;
        profile.InvalidReason = "";
        _instanceService.Update(profile);
        RefreshInstances(profile.Id);
        RefreshWizardSafetyUi();
        AddLog($"Instanz repariert: {profile.Name} · {profile.Version} · {profile.Loader} {profile.LoaderVersion}");
    }

    private async Task MigrateAndValidateInstancesAsync()
    {
        if (_instanceSafetyReady) return;
        try
        {
            SetDownloadStatus("Prüfe gespeicherte Instanzen …");
            var catalog = await _catalogService.GetMinecraftVersionsAsync(includeSnapshots: true);
            if (catalog.Count == 0) return;

            var release = catalog.FirstOrDefault(x => x.Type.Equals("release", StringComparison.OrdinalIgnoreCase));
            if (release is null) return;
            var snapshot = catalog.FirstOrDefault(x => x.Type.Equals("snapshot", StringComparison.OrdinalIgnoreCase)) ?? release;
            var known = catalog.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var migration = new InstanceMigrationService();

            foreach (var profile in _instanceService.GetAll())
            {
                var beforeVersion = profile.Version;
                var beforeLoader = profile.Loader;
                var beforeValid = profile.IsValid;
                var result = await migration.MigrateAsync(
                    profile,
                    release.Name,
                    snapshot.Name,
                    known,
                    (minecraftVersion, loader) => _catalogService.GetLoaderVersionsAsync(minecraftVersion, loader));

                _instanceService.Update(profile);
                if (result.Changed || beforeValid != profile.IsValid)
                {
                    AddLog($"Instanzprüfung: {profile.Name} · {beforeVersion}/{beforeLoader} → {profile.Version}/{profile.Loader} · {(profile.IsValid ? "gültig" : profile.InvalidReason)}");
                }
            }

            _instanceSafetyReady = true;
            RefreshInstances(_settings.SelectedInstanceId);
            RefreshWizardSafetyUi();
        }
        catch (Exception ex)
        {
            AddLog("Instanzprüfung konnte nicht abgeschlossen werden: " + ex.Message);
        }
        finally
        {
            SetDownloadStatus("Bereit");
        }
    }

    private async Task<InstanceValidationResult> ValidateForLaunchAsync(InstanceProfile profile)
    {
        try
        {
            var versions = await _catalogService.GetMinecraftVersionsAsync(includeSnapshots: true);
            var known = versions.Select(x => x.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var loaderVersions = profile.Loader.Equals("Vanilla", StringComparison.OrdinalIgnoreCase)
                ? new[] { "Standard" }
                : (await _catalogService.GetLoaderVersionsAsync(profile.Version, profile.Loader)).ToArray();
            return InstanceValidationService.Validate(profile, known, loaderVersions);
        }
        catch (Exception ex)
        {
            return InstanceValidationResult.Fail("Die Instanz konnte vor dem Start nicht geprüft werden: " + ex.Message);
        }
    }

    private void RefreshWizardSafetyUi()
    {
        if (SelectedInstance is not { } profile || HomePlayButton is null) return;
        var invalid = !profile.IsValid;

        HomePlayButton.IsEnabled = !invalid && !_launching;
        HomePlayButton.Content = invalid ? "⚠  INSTANZ REPARIEREN" : "▶   SPIELEN";

        if (_instancePrimaryButton is not null)
        {
            _instancePrimaryButton.IsEnabled = true;
            _instancePrimaryButton.Content = invalid ? "⚠  Instanz reparieren" : "▶  Spielen";
            _instancePrimaryButton.Background = invalid ? Brush("#6B2633") : Application.Current.Resources["AccentGradient"] as Brush;
        }

        if (invalid)
        {
            InstanceDetailMeta.Text = $"{profile.Subtitle} · ⚠ Instanz reparieren";
            InstanceDetailMeta.Foreground = Brush("#FF9AA8");
            HomeStatus.Text = string.IsNullOrWhiteSpace(profile.InvalidReason)
                ? "Diese Instanz muss repariert werden."
                : profile.InvalidReason;
        }
        else
        {
            InstanceDetailMeta.Text = profile.Subtitle + LoaderVersionSuffix(profile);
            InstanceDetailMeta.Foreground = Brush("#7F8AA0");
            if (!_launching) HomeStatus.Text = "Bereit";
        }
    }

    private static Button? FindButton(DependencyObject root, Func<string, bool> predicate, Button? exclude = null)
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is Button button && !ReferenceEquals(button, exclude))
            {
                var text = button.Content?.ToString() ?? "";
                if (predicate(text)) return button;
            }

            var nested = FindButton(child, predicate, exclude);
            if (nested is not null) return nested;
        }
        return null;
    }
}
