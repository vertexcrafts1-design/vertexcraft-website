namespace Lumina.Core;

public sealed class InstanceProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Minecraft";
    public string Version { get; set; } = "1.21.1";
    public string Loader { get; set; } = "Vanilla";
    public string LoaderVersion { get; set; } = "";
    public string Preset { get; set; } = "Balanced";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastPlayedUtc { get; set; }
    public int Launches { get; set; }
    public string Accent { get; set; } = "#8B5CF6";
    public bool IsValid { get; set; } = true;
    public string InvalidReason { get; set; } = "";
    public bool AllowSnapshots { get; set; }

    public string Subtitle => $"Minecraft {Version}  •  {Loader}";
}

public sealed class AppSettings
{
    public int RamMb { get; set; } = 6144;
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;
    public string JavaPath { get; set; } = "";
    public string SelectedInstanceId { get; set; } = "";
    public bool SafeLaunch { get; set; } = true;
    public bool SmartMemory { get; set; } = true;
    public bool FocusMode { get; set; } = true;
    public bool CloseLauncherOnGameStart { get; set; }
    public string QuickServer { get; set; } = "";
    public string LibraryKind { get; set; } = "Mods";
}

public enum ContentKind
{
    Mod,
    ResourcePack,
    ShaderPack
}

public sealed class ContentItem
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public bool Enabled { get; set; }
    public ContentKind Kind { get; set; }
    public long SizeBytes { get; set; }
    public string Status => Enabled ? "Aktiv" : "Deaktiviert";
    public string SizeText => SizeBytes < 1024 * 1024 ? $"{SizeBytes / 1024d:0} KB" : $"{SizeBytes / 1024d / 1024d:0.0} MB";
}

public sealed class PerformancePreset
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public int RamMb { get; init; }
    public string[] JvmArgs { get; init; } = [];
}

public sealed class LaunchRecord
{
    public DateTime StartedUtc { get; set; }
    public DateTime? EndedUtc { get; set; }
    public string InstanceId { get; set; } = "";
    public string InstanceName { get; set; } = "";
    public int ExitCode { get; set; }
    public double Minutes => EndedUtc is null ? 0 : Math.Max(0, (EndedUtc.Value - StartedUtc).TotalMinutes);
}
