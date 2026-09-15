namespace Lumina.Core;

public sealed class ManagedContentRecord
{
    public string? ProjectId { get; set; }
    public string? VersionId { get; set; }
    public string FileName { get; set; } = "";
    public string? Sha512 { get; set; }
    public string? Sha1 { get; set; }
    public ContentKind Kind { get; set; }
    public DateTime InstalledUtc { get; set; } = DateTime.UtcNow;
    public bool IsLocalOnly => string.IsNullOrWhiteSpace(ProjectId);

    public static ManagedContentRecord Local(string fileName, ContentKind kind) => new()
    {
        FileName = fileName,
        Kind = kind
    };
}

public sealed class ManagedContentStore
{
    private readonly AppPaths _paths;

    public ManagedContentStore(AppPaths paths) => _paths = paths;

    public IReadOnlyList<ManagedContentRecord> Load(string instanceId) =>
        JsonStore.Load(ContentFile(instanceId), new List<ManagedContentRecord>());

    public void Save(string instanceId, IEnumerable<ManagedContentRecord> records) =>
        JsonStore.Save(ContentFile(instanceId), records.ToList());

    public void Upsert(string instanceId, ManagedContentRecord record)
    {
        var items = Load(instanceId).ToList();
        var index = items.FindIndex(x =>
            (!string.IsNullOrWhiteSpace(record.ProjectId) && x.ProjectId == record.ProjectId && x.Kind == record.Kind) ||
            (string.IsNullOrWhiteSpace(record.ProjectId) && x.FileName.Equals(record.FileName, StringComparison.OrdinalIgnoreCase) && x.Kind == record.Kind));
        if (index >= 0) items[index] = record;
        else items.Add(record);
        Save(instanceId, items);
    }

    public void Remove(string instanceId, string fileName, ContentKind kind)
    {
        var items = Load(instanceId).Where(x =>
            !(x.Kind == kind && x.FileName.Equals(fileName, StringComparison.OrdinalIgnoreCase))).ToList();
        Save(instanceId, items);
    }

    public string ContentFile(string instanceId) =>
        Path.Combine(_paths.InstanceRoot(instanceId), ".lumina", "content.json");
}
