using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.ProcessBuilder;

var root = Path.Combine(Path.GetTempPath(), "lumina-launch-smoke-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);

try
{
    Console.WriteLine($"Smoke root: {root}");
    var launcher = new MinecraftLauncher(new MinecraftPath(root));
    launcher.FileProgressChanged += (_, e) => Console.WriteLine($"{e.EventType}: {e.Name}");
    launcher.ByteProgressChanged += (_, e) =>
    {
        if (e.TotalBytes > 0)
            Console.WriteLine($"Bytes: {e.ProgressedBytes}/{e.TotalBytes}");
    };

    var process = await launcher.InstallAndBuildProcessAsync("1.21.1", new MLaunchOption
    {
        Session = MSession.CreateOfflineSession("LuminaSmoke"),
        MaximumRamMb = 2048,
        MinimumRamMb = 1024,
        GameLauncherName = "LUMINA-SMOKE",
        GameLauncherVersion = "1.1"
    });

    var javaw = process.StartInfo.FileName;
    if (string.IsNullOrWhiteSpace(javaw) || !File.Exists(javaw))
        throw new Exception($"Java runtime missing: {javaw}");
    if (string.IsNullOrWhiteSpace(process.StartInfo.WorkingDirectory) || !Directory.Exists(process.StartInfo.WorkingDirectory))
        throw new Exception($"Working directory missing: {process.StartInfo.WorkingDirectory}");
    if (process.StartInfo.ArgumentList.Count == 0 && string.IsNullOrWhiteSpace(process.StartInfo.Arguments))
        throw new Exception("Minecraft process has no arguments");

    var java = Path.Combine(Path.GetDirectoryName(javaw)!, "java.exe");
    if (!File.Exists(java)) throw new Exception($"java.exe missing next to javaw.exe: {java}");

    using var javaTest = Process.Start(new ProcessStartInfo
    {
        FileName = java,
        Arguments = "-version",
        UseShellExecute = false,
        RedirectStandardError = true,
        RedirectStandardOutput = true,
        CreateNoWindow = true
    }) ?? throw new Exception("Could not start downloaded Java runtime");
    await javaTest.WaitForExitAsync();
    var javaOutput = await javaTest.StandardError.ReadToEndAsync();
    if (javaTest.ExitCode != 0) throw new Exception($"Downloaded Java runtime failed: {javaTest.ExitCode}\n{javaOutput}");

    Console.WriteLine($"Java OK: {javaw}");
    Console.WriteLine($"Working directory OK: {process.StartInfo.WorkingDirectory}");
    Console.WriteLine("Minecraft launch pipeline smoke test passed.");
}
finally
{
    try { Directory.Delete(root, true); } catch { }
}
