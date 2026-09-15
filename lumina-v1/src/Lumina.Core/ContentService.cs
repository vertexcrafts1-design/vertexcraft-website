namespace Lumina.Core;

public sealed class ContentService
{
    private readonly AppPaths _paths;

    public ContentService(AppPaths paths) => _paths = paths;

    public string Folder(string instanceId, ContentKind kind) => kind switch
    {
        ContentKind.Mod => _paths.Mods(instanceId),
        ContentKind.ResourcePack => _paths.ResourcePacks(instanceId),
        ContentKind.ShaderPack => _paths.ShaderPacks(instanceId),
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public IReadOnlyList<ContentItem> Get(string instanceId, ContentKind kind)
    {
        var folder = Folder(instanceId, kind);
        Directory.CreateDirectory(folder);
        return Directory.GetFiles(folder)
            .Where(path => IsSupported(path, kind))
            .Select(path => new ContentItem
            {
                Name = FriendlyName(path),
                Path = path,
                Enabled = !path.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase),
                Kind = kind,
                SizeBytes = new FileInfo(path).Length
            })
            .OrderByDescending(x => x.Enabled)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<ContentItem> Import(string instanceId, ContentKind kind, IEnumerable<string> files)
    {
        var folder = Folder(instanceId, kind);
        Directory.CreateDirectory(folder);
        foreach (var source in files.Where(File.Exists))
        {
            if (!IsSourceSupported(source, kind)) continue;
            var target = Path.Combine(folder, Path.GetFileName(source));
            if (Path.GetFullPath(source).Equals(Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase)) continue;
            File.Copy(source, target, true);
        }
        return Get(instanceId, kind);
    }

    public string Toggle(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Content file not found", path);
        string target;
        if (path.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase))
            target = path[..^".disabled".Length];
        else
            target = path + ".disabled";
        if (File.Exists(target)) File.Delete(target);
        File.Move(path, target);
        return target;
    }

    public void Delete(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    private static bool IsSourceSupported(string path, ContentKind kind)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        return kind == ContentKind.Mod ? extension == ".jar" : extension == ".zip";
    }

    private static bool IsSupported(string path, ContentKind kind)
    {
        var candidate = path.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase) ? path[..^".disabled".Length] : path;
        return IsSourceSupported(candidate, kind);
    }

    private static string FriendlyName(string path)
    {
        var file = Path.GetFileName(path);
        if (file.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase)) file = file[..^".disabled".Length];
        return file;
    }
}
