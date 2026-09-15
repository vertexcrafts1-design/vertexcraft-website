namespace Lumina.Core;

public sealed class AppPaths
{
    public AppPaths(string? root = null)
    {
        Root = root ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LUMINA");
        Instances = Path.Combine(Root, "instances");
        Backups = Path.Combine(Root, "backups");
        SettingsFile = Path.Combine(Root, "settings.json");
        InstancesFile = Path.Combine(Root, "instances.json");
        StatsFile = Path.Combine(Root, "stats.json");
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Instances);
        Directory.CreateDirectory(Backups);
    }

    public string Root { get; }
    public string Instances { get; }
    public string Backups { get; }
    public string SettingsFile { get; }
    public string InstancesFile { get; }
    public string StatsFile { get; }

    public string InstanceRoot(string id) => Path.Combine(Instances, id);
    public string GameDirectory(string id) => Path.Combine(InstanceRoot(id), "game");
    public string Mods(string id) => Path.Combine(GameDirectory(id), "mods");
    public string ResourcePacks(string id) => Path.Combine(GameDirectory(id), "resourcepacks");
    public string ShaderPacks(string id) => Path.Combine(GameDirectory(id), "shaderpacks");
}
