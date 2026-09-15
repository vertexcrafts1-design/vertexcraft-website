using System.Text.RegularExpressions;

namespace Lumina.Core;

public sealed class InstanceService
{
    private readonly AppPaths _paths;

    public InstanceService(AppPaths paths) => _paths = paths;

    public IReadOnlyList<InstanceProfile> GetAll() => JsonStore.Load(_paths.InstancesFile, new List<InstanceProfile>())
        .OrderByDescending(x => x.LastPlayedUtc ?? x.CreatedUtc)
        .ToList();

    public static string SafeId(string name)
    {
        var value = Regex.Replace(name.Trim().ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrWhiteSpace(value) ? "instance" : value;
    }

    public InstanceProfile Create(string name, string version, string loader)
    {
        var items = GetAll().ToList();
        var baseId = SafeId(name);
        var id = baseId;
        var suffix = 2;
        while (items.Any(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase))) id = $"{baseId}-{suffix++}";

        var profile = new InstanceProfile
        {
            Id = id,
            Name = string.IsNullOrWhiteSpace(name) ? "Minecraft" : name.Trim(),
            Version = string.IsNullOrWhiteSpace(version) ? "1.21.1" : version.Trim(),
            Loader = string.IsNullOrWhiteSpace(loader) ? "Vanilla" : loader.Trim()
        };
        items.Add(profile);
        Save(items);
        EnsureFolders(profile.Id);
        return profile;
    }

    public InstanceProfile Duplicate(InstanceProfile source)
    {
        var clone = Create(source.Name + " Copy", source.Version, source.Loader);
        clone.LoaderVersion = source.LoaderVersion;
        clone.Preset = source.Preset;
        Update(clone);

        var sourceGame = _paths.GameDirectory(source.Id);
        var targetGame = _paths.GameDirectory(clone.Id);
        if (Directory.Exists(sourceGame)) CopyDirectory(sourceGame, targetGame);
        return clone;
    }

    public void Update(InstanceProfile profile)
    {
        var items = GetAll().ToList();
        var index = items.FindIndex(x => x.Id == profile.Id);
        if (index >= 0) items[index] = profile;
        else items.Add(profile);
        Save(items);
        EnsureFolders(profile.Id);
    }

    public bool Delete(string id)
    {
        var items = GetAll().ToList();
        var removed = items.RemoveAll(x => x.Id == id) > 0;
        if (!removed) return false;
        Save(items);
        var root = _paths.InstanceRoot(id);
        if (Directory.Exists(root)) Directory.Delete(root, true);
        return true;
    }

    public void EnsureFolders(string id)
    {
        Directory.CreateDirectory(_paths.GameDirectory(id));
        Directory.CreateDirectory(_paths.Mods(id));
        Directory.CreateDirectory(_paths.ResourcePacks(id));
        Directory.CreateDirectory(_paths.ShaderPacks(id));
        Directory.CreateDirectory(Path.Combine(_paths.GameDirectory(id), "config"));
    }

    private void Save(List<InstanceProfile> items) => JsonStore.Save(_paths.InstancesFile, items);

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
        foreach (var dir in Directory.GetDirectories(source)) CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
    }
}
