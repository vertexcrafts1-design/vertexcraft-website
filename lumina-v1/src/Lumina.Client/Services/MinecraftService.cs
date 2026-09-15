using System.Diagnostics;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.NeoForge;
using CmlLib.Core.Installer.NeoForge.Installers;
using CmlLib.Core.Installers;
using CmlLib.Core.ModLoaders.FabricMC;
using CmlLib.Core.ModLoaders.QuiltMC;
using CmlLib.Core.ProcessBuilder;
using Lumina.Core;

namespace Lumina.Client.Services;

public sealed class MinecraftService
{
    private readonly AppPaths _paths;
    private readonly HttpClient _http = new();

    public MinecraftService(AppPaths paths) => _paths = paths;

    public event Action<string>? StatusChanged;
    public event Action<double>? ProgressChanged;
    public event Action<string>? LogLine;

    public async Task<ProcessWrapper> LaunchAsync(
        InstanceProfile profile,
        AppSettings settings,
        MSession session,
        CancellationToken cancellationToken = default)
    {
        if (session is null || !session.CheckIsValid())
            throw new InvalidOperationException("Deine Microsoft-Sitzung ist nicht gültig. Bitte melde dich erneut an.");

        var gameDirectory = _paths.GameDirectory(profile.Id);
        Directory.CreateDirectory(gameDirectory);
        var minecraftPath = new MinecraftPath(gameDirectory);
        var launcher = new MinecraftLauncher(minecraftPath);

        var fileProgress = new Progress<InstallerProgressChangedEventArgs>(e =>
        {
            StatusChanged?.Invoke($"{PrettyEvent(e.EventType.ToString())}: {e.Name}");
        });
        var byteProgress = new Progress<ByteProgress>(e =>
        {
            if (e.TotalBytes > 0)
                ProgressChanged?.Invoke(Math.Clamp(e.ProgressedBytes * 100d / e.TotalBytes, 0, 100));
        });

        launcher.FileProgressChanged += (_, e) =>
            StatusChanged?.Invoke($"{PrettyEvent(e.EventType.ToString())}: {e.Name}");
        launcher.ByteProgressChanged += (_, e) =>
        {
            if (e.TotalBytes > 0)
                ProgressChanged?.Invoke(Math.Clamp(e.ProgressedBytes * 100d / e.TotalBytes, 0, 100));
        };

        StatusChanged?.Invoke($"Bereite Minecraft {profile.Version} vor …");
        LogLine?.Invoke($"Instance: {profile.Name} | Minecraft {profile.Version} | {profile.Loader} {profile.LoaderVersion}");

        var versionName = await EnsureLoaderAsync(
            profile,
            minecraftPath,
            launcher,
            fileProgress,
            byteProgress,
            cancellationToken);

        var preset = PresetService.Get(profile.Preset);
        var options = new MLaunchOption
        {
            Session = session,
            MaximumRamMb = Math.Max(2048, settings.RamMb),
            MinimumRamMb = Math.Min(2048, Math.Max(1024, settings.RamMb / 3)),
            ScreenWidth = Math.Max(640, settings.Width),
            ScreenHeight = Math.Max(480, settings.Height),
            GameLauncherName = "LUMINA",
            GameLauncherVersion = "1.1",
            ExtraJvmArguments = MLaunchOption.DefaultExtraJvmArguments
                .Concat(preset.JvmArgs.Select(arg => new MArgument(arg)))
                .ToArray()
        };

        if (!string.IsNullOrWhiteSpace(settings.JavaPath))
        {
            if (!File.Exists(settings.JavaPath))
                throw new FileNotFoundException("Der eingestellte Java-Pfad existiert nicht.", settings.JavaPath);
            options.JavaPath = settings.JavaPath;
        }

        ApplyQuickServer(settings.QuickServer, options);

        StatusChanged?.Invoke("Installiere und prüfe Minecraft-Dateien …");
        var process = await launcher.InstallAndBuildProcessAsync(
            versionName,
            options,
            fileProgress,
            byteProgress,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(process.StartInfo.FileName) || !File.Exists(process.StartInfo.FileName))
            throw new InvalidOperationException(
                $"Minecraft konnte keine gültige Java-Runtime finden. Aufgelöst wurde: '{process.StartInfo.FileName}'.");

        LogLine?.Invoke($"Java: {process.StartInfo.FileName}");
        LogLine?.Invoke($"Working directory: {process.StartInfo.WorkingDirectory}");
        ProgressChanged?.Invoke(100);
        StatusChanged?.Invoke("Minecraft wird gestartet …");

        var wrapper = new ProcessWrapper(process);
        wrapper.OutputReceived += (_, line) =>
        {
            if (!string.IsNullOrWhiteSpace(line)) LogLine?.Invoke(line);
        };
        wrapper.Exited += (_, _) =>
        {
            try { LogLine?.Invoke($"Minecraft beendet (Exit Code {wrapper.Process.ExitCode})."); }
            catch { LogLine?.Invoke("Minecraft beendet."); }
        };

        wrapper.StartWithEvents();
        if (wrapper.Process.HasExited)
            throw new InvalidOperationException($"Minecraft wurde sofort beendet. Exit Code: {wrapper.Process.ExitCode}. Sieh in Downloads → Launch Log nach.");

        StatusChanged?.Invoke("Minecraft läuft");
        return wrapper;
    }

    private async Task<string> EnsureLoaderAsync(
        InstanceProfile profile,
        MinecraftPath minecraftPath,
        MinecraftLauncher launcher,
        IProgress<InstallerProgressChangedEventArgs> fileProgress,
        IProgress<ByteProgress> byteProgress,
        CancellationToken cancellationToken)
    {
        switch (profile.Loader.Trim().ToLowerInvariant())
        {
            case "vanilla":
                return profile.Version;

            case "fabric":
            {
                StatusChanged?.Invoke("Installiere/prüfe Fabric …");
                var installer = new FabricInstaller(_http);
                return string.IsNullOrWhiteSpace(profile.LoaderVersion) || profile.LoaderVersion == "Standard"
                    ? await installer.Install(profile.Version, minecraftPath)
                    : await installer.Install(profile.Version, profile.LoaderVersion, minecraftPath);
            }

            case "quilt":
            {
                StatusChanged?.Invoke("Installiere/prüfe Quilt …");
                var installer = new QuiltInstaller(_http);
                return string.IsNullOrWhiteSpace(profile.LoaderVersion) || profile.LoaderVersion == "Standard"
                    ? await installer.Install(profile.Version, minecraftPath)
                    : await installer.Install(profile.Version, profile.LoaderVersion, minecraftPath);
            }

            case "forge":
            {
                StatusChanged?.Invoke("Installiere/prüfe Forge …");
                var installer = new ForgeInstaller(launcher, _http);
                var options = new ForgeInstallOptions
                {
                    FileProgress = fileProgress,
                    ByteProgress = byteProgress,
                    InstallerOutput = new Progress<string>(line => LogLine?.Invoke(line)),
                    CancellationToken = cancellationToken,
                    SkipIfAlreadyInstalled = true
                };
                var versionName = string.IsNullOrWhiteSpace(profile.LoaderVersion) || profile.LoaderVersion == "Standard"
                    ? await installer.Install(profile.Version, options)
                    : await installer.Install(profile.Version, profile.LoaderVersion, options);
                await launcher.InstallAsync(versionName, fileProgress, byteProgress, cancellationToken);
                return versionName;
            }

            case "neoforge":
            {
                StatusChanged?.Invoke("Installiere/prüfe NeoForge …");
                var installer = new NeoForgeInstaller(launcher);
                var options = new NeoForgeInstallOptions
                {
                    FileProgress = fileProgress,
                    ByteProgress = byteProgress,
                    InstallerOutput = new Progress<string>(line => LogLine?.Invoke(line)),
                    CancellationToken = cancellationToken,
                    SkipIfAlreadyInstalled = true
                };
                var versionName = string.IsNullOrWhiteSpace(profile.LoaderVersion) || profile.LoaderVersion == "Standard"
                    ? await installer.Install(profile.Version, options)
                    : await installer.Install(profile.Version, profile.LoaderVersion, options);
                await launcher.InstallAsync(versionName, fileProgress, byteProgress, cancellationToken);
                return versionName;
            }

            default:
                throw new NotSupportedException($"Der Loader '{profile.Loader}' wird von LUMINA nicht unterstützt.");
        }
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

    private static string PrettyEvent(string value) => value.Replace("Started", "Starte").Replace("Completed", "Fertig");
}
