using System.Diagnostics;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ModLoaders.QuiltMC;
using CmlLib.Core.ProcessBuilder;
using Lumina.Core;

namespace Lumina.Client.Services;

public sealed class MinecraftService
{
    private readonly AppPaths _paths;

    public MinecraftService(AppPaths paths) => _paths = paths;

    public event Action<string>? StatusChanged;
    public event Action<double>? ProgressChanged;

    public async Task<Process> LaunchAsync(InstanceProfile profile, AppSettings settings, MSession session, CancellationToken cancellationToken = default)
    {
        var minecraftPath = new MinecraftPath(_paths.GameDirectory(profile.Id));
        var launcher = new MinecraftLauncher(minecraftPath);
        launcher.FileProgressChanged += (_, e) => StatusChanged?.Invoke($"{e.EventType}: {e.Name}");
        launcher.ByteProgressChanged += (_, e) =>
        {
            if (e.TotalBytes > 0)
                ProgressChanged?.Invoke(Math.Clamp(e.ProgressedBytes * 100d / e.TotalBytes, 0, 100));
        };

        StatusChanged?.Invoke($"Bereite Minecraft {profile.Version} vor …");
        var versionName = profile.Version;
        if (profile.Loader.Equals("Fabric", StringComparison.OrdinalIgnoreCase))
        {
            StatusChanged?.Invoke("Installiere/prüfe Fabric …");
            var installer = new FabricInstaller(new HttpClient());
            versionName = string.IsNullOrWhiteSpace(profile.LoaderVersion)
                ? await installer.Install(profile.Version, minecraftPath)
                : await installer.Install(profile.Version, profile.LoaderVersion, minecraftPath);
        }
        else if (profile.Loader.Equals("Quilt", StringComparison.OrdinalIgnoreCase))
        {
            StatusChanged?.Invoke("Installiere/prüfe Quilt …");
            var installer = new QuiltInstaller(new HttpClient());
            versionName = await installer.Install(profile.Version, minecraftPath);
        }

        var preset = PresetService.Get(profile.Preset);
        var options = new MLaunchOption
        {
            Session = session,
            MaximumRamMb = Math.Max(2048, settings.RamMb),
            MinimumRamMb = Math.Min(2048, Math.Max(1024, settings.RamMb / 3)),
            ScreenWidth = Math.Max(640, settings.Width),
            ScreenHeight = Math.Max(480, settings.Height),
            GameLauncherName = "LUMINA",
            GameLauncherVersion = "1.0",
            ExtraJvmArguments = MLaunchOption.DefaultExtraJvmArguments
                .Concat(preset.JvmArgs.Select(arg => new MArgument(arg)))
                .ToArray()
        };

        if (!string.IsNullOrWhiteSpace(settings.JavaPath)) options.JavaPath = settings.JavaPath;
        ApplyQuickServer(settings.QuickServer, options);

        StatusChanged?.Invoke("Minecraft wird installiert/überprüft …");
        var process = await launcher.InstallAndBuildProcessAsync(versionName, options, cancellationToken);
        ProgressChanged?.Invoke(100);
        StatusChanged?.Invoke("Minecraft startet …");
        process.EnableRaisingEvents = true;
        process.Start();
        return process;
    }

    private static void ApplyQuickServer(string quickServer, MLaunchOption options)
    {
        if (string.IsNullOrWhiteSpace(quickServer)) return;
        var value = quickServer.Trim();
        var index = value.LastIndexOf(':');
        if (index > 0 && int.TryParse(value[(index + 1)..], out var port))
        {
            options.ServerIp = value[..index];
            options.ServerPort = port;
        }
        else
        {
            options.ServerIp = value;
            options.ServerPort = 25565;
        }
    }
}
