using System.Text.Json.Serialization;

namespace Lumina.Client.Services;

public sealed class ModrinthSearchResponse
{
    [JsonPropertyName("hits")]
    public List<GalleryProject> Hits { get; set; } = [];
}

public sealed class GalleryProject
{
    [JsonPropertyName("project_id")]
    public string ProjectId { get; set; } = "";
    [JsonPropertyName("slug")]
    public string Slug { get; set; } = "";
    [JsonPropertyName("title")]
    public string Title { get; set; } = "";
    [JsonPropertyName("description")]
    public string Description { get; set; } = "";
    [JsonPropertyName("author")]
    public string Author { get; set; } = "";
    [JsonPropertyName("downloads")]
    public long Downloads { get; set; }
    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }
    [JsonPropertyName("project_type")]
    public string ProjectType { get; set; } = "mod";
    [JsonPropertyName("categories")]
    public List<string> Categories { get; set; } = [];

    public string DownloadsText => Downloads switch
    {
        >= 1_000_000 => $"{Downloads / 1_000_000d:0.#}M Downloads",
        >= 1_000 => $"{Downloads / 1_000d:0.#}K Downloads",
        _ => $"{Downloads} Downloads"
    };
    public string CategoryText => Categories.Count == 0
        ? "Minecraft"
        : string.Join(" · ", Categories.Take(3).Select(x => x.Replace('-', ' ')));
}

public sealed class ModrinthVersion
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    [JsonPropertyName("project_id")]
    public string ProjectId { get; set; } = "";
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
    [JsonPropertyName("version_number")]
    public string VersionNumber { get; set; } = "";
    [JsonPropertyName("version_type")]
    public string VersionType { get; set; } = "release";
    [JsonPropertyName("loaders")]
    public List<string> Loaders { get; set; } = [];
    [JsonPropertyName("game_versions")]
    public List<string> GameVersions { get; set; } = [];
    [JsonPropertyName("files")]
    public List<ModrinthFile> Files { get; set; } = [];
    [JsonPropertyName("dependencies")]
    public List<ModrinthDependency> Dependencies { get; set; } = [];

    public ModrinthVersionDescriptor Descriptor => new(GameVersions, Loaders, VersionType);
}

public sealed class ModrinthFile
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";
    [JsonPropertyName("filename")]
    public string Filename { get; set; } = "";
    [JsonPropertyName("primary")]
    public bool Primary { get; set; }
    [JsonPropertyName("size")]
    public long Size { get; set; }
    [JsonPropertyName("hashes")]
    public Dictionary<string, string> Hashes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class ModrinthDependency
{
    [JsonPropertyName("version_id")]
    public string? VersionId { get; set; }
    [JsonPropertyName("project_id")]
    public string? ProjectId { get; set; }
    [JsonPropertyName("dependency_type")]
    public string DependencyType { get; set; } = "";
}

public sealed record ModrinthVersionDescriptor(
    IReadOnlyList<string> GameVersions,
    IReadOnlyList<string> Loaders,
    string VersionType);

public sealed record CompatibilityResult(bool IsCompatible, string Reason)
{
    public static CompatibilityResult Compatible() => new(true, "Kompatibel");
    public static CompatibilityResult Incompatible(string reason) => new(false, reason);
}

public sealed record ModrinthPlannedFile(
    string ProjectId,
    string VersionId,
    string FileName,
    string Url,
    string? Sha512,
    string? Sha1,
    Lumina.Core.ContentKind Kind);

public sealed record ModrinthInstallPlan(IReadOnlyList<ModrinthPlannedFile> Files);
public sealed record GalleryInstallResult(int FilesInstalled, IReadOnlyList<string> FileNames);
public sealed record MinecraftVersionChoice(string Name, string Type, DateTimeOffset ReleaseTime)
{
    public string DisplayName => Type == "release" ? Name : $"{Name}  ·  {Type}";
}
