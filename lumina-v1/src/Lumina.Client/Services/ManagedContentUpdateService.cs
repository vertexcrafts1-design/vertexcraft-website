using Lumina.Core;

namespace Lumina.Client.Services;

public static class ManagedContentUpdatePolicy
{
    public static bool IsUpdateAvailable(ManagedContentRecord installed, string? latestVersionId) =>
        !installed.IsLocalOnly &&
        !string.IsNullOrWhiteSpace(latestVersionId) &&
        !string.Equals(installed.VersionId, latestVersionId, StringComparison.OrdinalIgnoreCase);
}

public sealed class InstalledContentStatus
{
    public required ManagedContentRecord Record { get; init; }
    public GalleryProject? Project { get; init; }
    public ModrinthVersion? LatestVersion { get; init; }
    public bool HasUpdate { get; init; }
    public bool LocalOnly => Record.IsLocalOnly;
    public string Title => Project?.Title ?? Record.FileName;
    public string FileName => Record.FileName;
    public string ProjectId => Record.ProjectId ?? "";
    public string KindText => Record.Kind switch
    {
        ContentKind.ResourcePack => "Resource Pack",
        ContentKind.ShaderPack => "Shader",
        _ => "Mod"
    };
    public string VersionText => LocalOnly
        ? "lokale Datei"
        : HasUpdate
            ? $"{Record.VersionId ?? "?"} → {LatestVersion?.VersionNumber ?? LatestVersion?.Id ?? "neu"}"
            : LatestVersion?.VersionNumber ?? Record.VersionId ?? "unbekannt";
    public string StatusText => LocalOnly ? "Lokal · keine Auto-Updates" : HasUpdate ? "Update verfügbar" : "Aktuell";
    public string ActionText => HasUpdate ? "Aktualisieren" : "Aktuell";
}

public sealed class ManagedContentUpdateService
{
    private readonly AppPaths _paths;
    private readonly ModrinthService _modrinth;
    private readonly ManagedContentStore _store;
    private readonly ContentService _content;

    public ManagedContentUpdateService(AppPaths paths, ModrinthService modrinth)
    {
        _paths = paths;
        _modrinth = modrinth;
        _store = new ManagedContentStore(paths);
        _content = new ContentService(paths);
    }

    public async Task<IReadOnlyList<InstalledContentStatus>> GetAsync(
        InstanceProfile instance,
        CancellationToken cancellationToken = default)
    {
        var records = _store.Load(instance.Id).ToList();
        AddUnmanagedLocalFiles(instance.Id, records);
        var result = new List<InstalledContentStatus>();

        foreach (var record in records.OrderBy(x => x.Kind).ThenBy(x => x.FileName, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (record.IsLocalOnly)
            {
                result.Add(new InstalledContentStatus { Record = record, HasUpdate = false });
                continue;
            }

            try
            {
                var versions = await _modrinth.GetCompatibleVersionsAsync(record.ProjectId!, record.Kind, instance, cancellationToken);
                var compatible = versions.Where(v => CompatibilityService.Check(instance, record.Kind, v.Descriptor).IsCompatible).ToList();
                var latest = CompatibilityService.ChoosePreferred(compatible);
                var project = await _modrinth.GetProjectAsync(record.ProjectId!, cancellationToken);
                result.Add(new InstalledContentStatus
                {
                    Record = record,
                    Project = project,
                    LatestVersion = latest,
                    HasUpdate = ManagedContentUpdatePolicy.IsUpdateAvailable(record, latest?.Id)
                });
            }
            catch
            {
                result.Add(new InstalledContentStatus { Record = record, HasUpdate = false });
            }
        }

        return result;
    }

    public async Task<GalleryInstallResult> UpdateAsync(
        InstalledContentStatus item,
        InstanceProfile instance,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (item.LocalOnly || string.IsNullOrWhiteSpace(item.ProjectId))
            throw new InvalidOperationException("Lokale Dateien werden von LUMINA nicht automatisch aktualisiert.");
        if (!item.HasUpdate)
            return new GalleryInstallResult(0, []);

        var project = item.Project ?? await _modrinth.GetProjectAsync(item.ProjectId, cancellationToken)
                      ?? throw new InvalidOperationException("Das Modrinth-Projekt konnte nicht geladen werden.");
        var kind = item.Record.Kind switch
        {
            ContentKind.ResourcePack => "Resource Packs",
            ContentKind.ShaderPack => "Shaders",
            _ => "Mods"
        };

        var oldFile = item.Record.FileName;
        var result = await _modrinth.InstallAsync(project, kind, instance, _paths, progress, cancellationToken);
        var current = _store.Load(instance.Id).FirstOrDefault(x =>
            x.Kind == item.Record.Kind && string.Equals(x.ProjectId, item.ProjectId, StringComparison.OrdinalIgnoreCase));

        if (current is not null && !oldFile.Equals(current.FileName, StringComparison.OrdinalIgnoreCase))
        {
            var oldPath = Path.Combine(_content.Folder(instance.Id, item.Record.Kind), oldFile);
            try { if (File.Exists(oldPath)) File.Delete(oldPath); }
            catch { }
        }

        return result;
    }

    private void AddUnmanagedLocalFiles(string instanceId, List<ManagedContentRecord> records)
    {
        foreach (var kind in new[] { ContentKind.Mod, ContentKind.ResourcePack, ContentKind.ShaderPack })
        {
            foreach (var item in _content.Get(instanceId, kind))
            {
                var fileName = Path.GetFileName(item.Path);
                if (fileName.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase))
                    fileName = fileName[..^".disabled".Length];

                if (records.Any(x => x.Kind == kind && x.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
                    continue;
                records.Add(ManagedContentRecord.Local(fileName, kind));
            }
        }
    }
}
