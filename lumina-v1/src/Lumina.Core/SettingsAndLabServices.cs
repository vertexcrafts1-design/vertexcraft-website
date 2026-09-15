using System.IO.Compression;

namespace Lumina.Core;

public sealed class SettingsService
{
    private readonly AppPaths _paths;
    public SettingsService(AppPaths paths) => _paths = paths;
    public AppSettings Load() => JsonStore.Load(_paths.SettingsFile, new AppSettings());
    public void Save(AppSettings settings) => JsonStore.Save(_paths.SettingsFile, settings);
}

public static class PresetService
{
    private static readonly IReadOnlyDictionary<string, PerformancePreset> Presets = new Dictionary<string, PerformancePreset>(StringComparer.OrdinalIgnoreCase)
    {
        ["Competitive"] = new() { Name = "Competitive", Description = "Maximale Reaktionsfreude und stabile FPS", RamMb = 6144, JvmArgs = ["-XX:+UseG1GC", "-XX:MaxGCPauseMillis=50", "-XX:+ParallelRefProcEnabled"] },
        ["Balanced"] = new() { Name = "Balanced", Description = "Der Standard für Alltag, Mods und SMP", RamMb = 6144, JvmArgs = ["-XX:+UseG1GC", "-XX:MaxGCPauseMillis=100"] },
        ["Quality"] = new() { Name = "Quality", Description = "Mehr Speicher für Shader und große Packs", RamMb = 8192, JvmArgs = ["-XX:+UseG1GC", "-XX:MaxGCPauseMillis=120"] },
        ["Low Spec"] = new() { Name = "Low Spec", Description = "Spart RAM und hält den Launcher leicht", RamMb = 4096, JvmArgs = ["-XX:+UseG1GC", "-XX:MaxGCPauseMillis=150"] }
    };

    public static IReadOnlyCollection<PerformancePreset> All => Presets.Values.ToList();
    public static PerformancePreset Get(string name) => Presets.TryGetValue(name, out var preset) ? preset : Presets["Balanced"];

    public static int SmartRamMb(ulong totalPhysicalBytes)
    {
        var gb = totalPhysicalBytes / 1024d / 1024d / 1024d;
        return gb switch
        {
            < 8 => 3072,
            < 12 => 4096,
            < 24 => 6144,
            < 40 => 8192,
            _ => 10240
        };
    }
}

public sealed class SafeLaunchService
{
    private readonly AppPaths _paths;
    public SafeLaunchService(AppPaths paths) => _paths = paths;

    public string CreateSnapshot(InstanceProfile profile)
    {
        var game = _paths.GameDirectory(profile.Id);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var targetDir = Path.Combine(_paths.Backups, profile.Id);
        Directory.CreateDirectory(targetDir);
        var zip = Path.Combine(targetDir, $"safe-{timestamp}.zip");
        var temp = Path.Combine(Path.GetTempPath(), "lumina-safe-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            CopyIfExists(Path.Combine(game, "options.txt"), Path.Combine(temp, "options.txt"));
            CopyIfExists(Path.Combine(game, "optionsof.txt"), Path.Combine(temp, "optionsof.txt"));
            CopyIfExists(Path.Combine(game, "servers.dat"), Path.Combine(temp, "servers.dat"));
            var config = Path.Combine(game, "config");
            if (Directory.Exists(config)) CopyDirectory(config, Path.Combine(temp, "config"));
            ZipFile.CreateFromDirectory(temp, zip, CompressionLevel.Fastest, false);
            return zip;
        }
        finally
        {
            if (Directory.Exists(temp)) Directory.Delete(temp, true);
        }
    }

    private static void CopyIfExists(string source, string target)
    {
        if (!File.Exists(source)) return;
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(source, target, true);
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
        foreach (var dir in Directory.GetDirectories(source)) CopyDirectory(dir, Path.Combine(target, Path.GetFileName(dir)));
    }
}

public sealed class StatsService
{
    private readonly AppPaths _paths;
    public StatsService(AppPaths paths) => _paths = paths;

    public IReadOnlyList<LaunchRecord> GetAll() => JsonStore.Load(_paths.StatsFile, new List<LaunchRecord>())
        .OrderByDescending(x => x.StartedUtc).Take(200).ToList();

    public void Add(LaunchRecord record)
    {
        var items = GetAll().ToList();
        items.Insert(0, record);
        JsonStore.Save(_paths.StatsFile, items.Take(200).ToList());
    }

    public double TotalMinutes => GetAll().Sum(x => x.Minutes);
    public int TotalLaunches => GetAll().Count;
}
