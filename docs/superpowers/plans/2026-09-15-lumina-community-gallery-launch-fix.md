# LUMINA Community Gallery + Launch Reliability Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix legacy-instance launch failures, add an account-gated version/loader instance wizard, expose the full Modrinth catalog with strict compatibility/update handling, and add a remotely verified LUMINA Collection without accepting arbitrary binaries.

**Architecture:** Keep the native WPF/.NET 8 client and split the new logic into testable services. Modrinth remains the source of binaries and project metadata; a small LUMINA API stores only verified curator UUIDs and Modrinth project references. Instance migration and validation run independently of the UI and are re-run immediately before launch/install.

**Tech Stack:** .NET 8, WPF, CmlLib.Core 4.0.6, CmlLib.Core.Auth.Microsoft 3.3.1, CmlLib Forge/NeoForge installers, HttpClient/System.Text.Json, xUnit, Cloudflare Worker-style JSON API or equivalent HTTPS deployment for LUMINA-specific state, GitHub Actions Windows runner.

**Spec:** `docs/superpowers/specs/2026-09-15-lumina-community-gallery-launch-fix-design.md`

## Global Constraints

- Windows x64 native/self-contained launcher remains the shipped client.
- Supported client loaders are exactly: Vanilla, Fabric, Quilt, Forge, NeoForge. Paper is excluded.
- Persist only concrete Mojang Minecraft version IDs; never persist or pass `latest-release`/`latest-snapshot` to CmlLib.
- Minecraft launch, instance creation, and LUMINA Collection mutation require a valid Microsoft/Minecraft session.
- Normal Discover is live Modrinth and must continue working when the LUMINA backend is unavailable.
- No arbitrary community JAR/ZIP/EXE upload endpoint.
- LUMINA Collection stores Modrinth project references only; binaries remain hosted/downloaded by Modrinth.
- Compatibility is checked at search/install/update time and again immediately before file activation.
- Required dependencies must match the same Minecraft version and loader.
- Downloads use temporary files and verified hashes when Modrinth supplies hashes.
- Manual local files remain supported but are never globally published.
- Existing Core tests, WPF build, self-contained publish, and Minecraft/Mojang-Java smoke test must remain green.

---

## File Structure

### Client core

- `lumina-v1/src/Lumina.Core/Models.cs` — extend persisted instance/content state with validity and managed-content metadata.
- `lumina-v1/src/Lumina.Core/InstanceMigrationService.cs` — migrate legacy aliases and normalize loader data.
- `lumina-v1/src/Lumina.Core/InstanceValidationService.cs` — validate concrete version/loader combinations without UI state.
- `lumina-v1/src/Lumina.Core/ManagedContentStore.cs` — persist Modrinth project/version/file sidecars per instance.

### Client services

- `lumina-v1/src/Lumina.Client/Services/MinecraftCatalogService.cs` — live Mojang + loader catalog and exact compatibility lookup.
- `lumina-v1/src/Lumina.Client/Services/MinecraftPreflightService.cs` — launch validation before CmlLib installation/process creation.
- `lumina-v1/src/Lumina.Client/Services/ModrinthService.cs` — search, metadata, dependencies, downloads, hashes and update candidates.
- `lumina-v1/src/Lumina.Client/Services/CompatibilityService.cs` — single compatibility decision point for Modrinth content.
- `lumina-v1/src/Lumina.Client/Services/LuminaCollectionService.cs` — remote curated collection reads/writes.
- `lumina-v1/src/Lumina.Client/Services/VerificationService.cs` — remote UUID verification.

### UI

- `lumina-v1/src/Lumina.Client/MainWindow.xaml` — instance wizard, invalid-instance state, Discover tabs, installed/update badges.
- `lumina-v1/src/Lumina.Client/MainWindow.xaml.cs` — orchestration only; no direct compatibility rules.

### LUMINA API

- `lumina-v1/backend/src/index.ts` — HTTPS API routes for verification and collection.
- `lumina-v1/backend/src/store.ts` — verified UUID + collection storage abstraction.
- `lumina-v1/backend/test/index.test.ts` — permission/duplicate/project-id contract tests.
- `lumina-v1/backend/wrangler.toml` — deployment configuration without secrets committed.

### Tests / CI

- `lumina-v1/tests/Lumina.Core.Tests/MigrationValidationTests.cs`
- `lumina-v1/tests/Lumina.Core.Tests/ManagedContentTests.cs`
- `lumina-v1/tests/Lumina.Client.Tests/ModrinthCompatibilityTests.cs`
- `lumina-v1/tests/Lumina.Client.Tests/PreflightTests.cs`
- `.github/workflows/lumina-v1-windows.yml`

---

### Task 1: Legacy instance migration and validity state

**Files:**
- Modify: `lumina-v1/src/Lumina.Core/Models.cs`
- Create: `lumina-v1/src/Lumina.Core/InstanceMigrationService.cs`
- Create: `lumina-v1/src/Lumina.Core/InstanceValidationService.cs`
- Create: `lumina-v1/tests/Lumina.Core.Tests/MigrationValidationTests.cs`

**Interfaces:**
- Consumes: `InstanceProfile`, a live `IReadOnlyCollection<string>` of concrete Minecraft versions, a loader-version resolver.
- Produces: `InstanceValidationResult Validate(InstanceProfile profile, IReadOnlyCollection<string> minecraftVersions, IReadOnlyCollection<string> loaderVersions)` and `Task<InstanceMigrationResult> MigrateAsync(...)`.

- [ ] **Step 1: Write failing migration tests**

```csharp
[Fact]
public async Task LatestRelease_Becomes_Concrete_Release()
{
    var profile = new InstanceProfile { Name = "Old", Version = "latest-release", Loader = "vanilla" };
    var sut = new InstanceMigrationService();

    var result = await sut.MigrateAsync(
        profile,
        latestRelease: "1.21.11",
        latestSnapshot: "26w37a",
        knownVersions: ["1.21.11", "1.21.10"],
        getLoaderVersions: (_, _) => Task.FromResult<IReadOnlyList<string>>(["Standard"]));

    Assert.Equal("1.21.11", profile.Version);
    Assert.Equal("Vanilla", profile.Loader);
    Assert.Equal("Standard", profile.LoaderVersion);
    Assert.True(result.Changed);
    Assert.True(profile.IsValid);
}

[Fact]
public async Task Unknown_Concrete_Version_Is_Invalid_Not_Rewritten()
{
    var profile = new InstanceProfile { Version = "1.99.99", Loader = "Fabric", LoaderVersion = "0.17.2" };
    var sut = new InstanceMigrationService();

    await sut.MigrateAsync(profile, "1.21.11", "26w37a", ["1.21.11"], (_, _) => Task.FromResult<IReadOnlyList<string>>([]));

    Assert.Equal("1.99.99", profile.Version);
    Assert.False(profile.IsValid);
    Assert.Contains("Minecraft-Version", profile.InvalidReason);
}
```

- [ ] **Step 2: Run tests and confirm RED**

Run:
```powershell
dotnet test lumina-v1/tests/Lumina.Core.Tests/Lumina.Core.Tests.csproj -c Release --filter MigrationValidationTests
```
Expected: compile/test failure because migration/validity APIs do not exist yet.

- [ ] **Step 3: Extend `InstanceProfile` with explicit validity state**

```csharp
public bool IsValid { get; set; } = true;
public string InvalidReason { get; set; } = "";
```

`Subtitle` remains display-only and must not encode validity decisions.

- [ ] **Step 4: Implement migration rules exactly**

```csharp
public sealed record InstanceMigrationResult(bool Changed, string Message);

public sealed class InstanceMigrationService
{
    public async Task<InstanceMigrationResult> MigrateAsync(
        InstanceProfile profile,
        string latestRelease,
        string latestSnapshot,
        IReadOnlyCollection<string> knownVersions,
        Func<string, string, Task<IReadOnlyList<string>>> getLoaderVersions)
    {
        var changed = false;
        if (profile.Version.Equals("latest-release", StringComparison.OrdinalIgnoreCase))
        {
            profile.Version = latestRelease;
            changed = true;
        }

        profile.Loader = NormalizeLoader(profile.Loader);
        if (profile.Loader == "Vanilla")
        {
            profile.LoaderVersion = "Standard";
            changed = true;
        }

        var validation = await ValidateAsync(profile, knownVersions, getLoaderVersions);
        profile.IsValid = validation.IsValid;
        profile.InvalidReason = validation.Error ?? "";
        return new InstanceMigrationResult(changed, profile.InvalidReason);
    }
}
```

Implement `latest-snapshot` exactly as the spec requires and never rewrite an unknown concrete version.

- [ ] **Step 5: Implement reusable validation result**

```csharp
public sealed record InstanceValidationResult(bool IsValid, string? Error)
{
    public static InstanceValidationResult Ok() => new(true, null);
    public static InstanceValidationResult Fail(string message) => new(false, message);
}
```

Validation rejects pseudo-versions, missing concrete versions, unsupported loaders and absent loader versions.

- [ ] **Step 6: Run Core tests**

```powershell
dotnet test lumina-v1/tests/Lumina.Core.Tests/Lumina.Core.Tests.csproj -c Release
```
Expected: all old tests + new migration/validation tests PASS.

- [ ] **Step 7: Commit**

```bash
git add lumina-v1/src/Lumina.Core lumina-v1/tests/Lumina.Core.Tests
git commit -m "fix: migrate and validate legacy LUMINA instances"
```

---

### Task 2: Managed Modrinth content metadata and safe install pipeline

**Files:**
- Create: `lumina-v1/src/Lumina.Core/ManagedContentStore.cs`
- Modify: `lumina-v1/src/Lumina.Core/Models.cs`
- Modify: `lumina-v1/src/Lumina.Client/Services/ModrinthService.cs`
- Create: `lumina-v1/src/Lumina.Client/Services/CompatibilityService.cs`
- Create: `lumina-v1/tests/Lumina.Core.Tests/ManagedContentTests.cs`
- Create: `lumina-v1/tests/Lumina.Client.Tests/ModrinthCompatibilityTests.cs`

**Interfaces:**
- Produces: `ManagedContentRecord`, `CompatibilityResult`, `Task<ModrinthInstallPlan> BuildInstallPlanAsync(...)`, `Task ApplyInstallPlanAsync(...)`.
- Consumes: selected `InstanceProfile` and Modrinth project/version metadata.

- [ ] **Step 1: Add failing metadata-store tests**

```csharp
[Fact]
public void Manual_Jar_Is_Not_Treated_As_Global_Modrinth_Content()
{
    var record = ManagedContentRecord.Local("sodium-custom.jar", ContentKind.Mod);
    Assert.True(record.IsLocalOnly);
    Assert.Null(record.ProjectId);
}

[Fact]
public void Managed_Record_Retains_Project_And_Hash()
{
    var record = new ManagedContentRecord
    {
        ProjectId = "AANobbMI",
        VersionId = "version-123",
        FileName = "sodium.jar",
        Sha512 = "abc",
        Kind = ContentKind.Mod
    };
    Assert.False(record.IsLocalOnly);
    Assert.Equal("AANobbMI", record.ProjectId);
}
```

- [ ] **Step 2: Add failing compatibility tests**

```csharp
[Fact]
public void Fabric_Instance_Rejects_Forge_Only_Mod()
{
    var instance = new InstanceProfile { Version = "1.21.1", Loader = "Fabric", LoaderVersion = "0.16.14" };
    var candidate = new ModrinthVersionDescriptor(["1.21.1"], ["forge"], "release");

    var result = CompatibilityService.Check(instance, ContentKind.Mod, candidate);

    Assert.False(result.IsCompatible);
}
```

- [ ] **Step 3: Run tests and confirm RED**

```powershell
dotnet test lumina-v1/tests/Lumina.Core.Tests/Lumina.Core.Tests.csproj -c Release
dotnet test lumina-v1/tests/Lumina.Client.Tests/Lumina.Client.Tests.csproj -c Release
```

- [ ] **Step 4: Implement managed sidecar model/store**

```csharp
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

    public static ManagedContentRecord Local(string fileName, ContentKind kind) =>
        new() { FileName = fileName, Kind = kind };
}
```

Store one JSON sidecar per instance under `LUMINA/instances/<id>/.lumina/content.json`.

- [ ] **Step 5: Centralize compatibility rules**

```csharp
public sealed record CompatibilityResult(bool IsCompatible, string Reason);

public static CompatibilityResult Check(InstanceProfile instance, ContentKind kind, ModrinthVersionDescriptor version)
{
    if (!version.GameVersions.Contains(instance.Version, StringComparer.OrdinalIgnoreCase))
        return new(false, $"Nicht für Minecraft {instance.Version}.");

    if (kind == ContentKind.Mod)
    {
        var loader = instance.Loader.ToLowerInvariant();
        if (loader == "vanilla" || !version.Loaders.Contains(loader, StringComparer.OrdinalIgnoreCase))
            return new(false, $"Nicht für {instance.Loader}.");
    }

    return new(true, "Kompatibel");
}
```

- [ ] **Step 6: Build an install plan before writing files**

`BuildInstallPlanAsync` must recursively resolve required dependencies first. If any required dependency has no compatible version, throw before downloading/activating anything.

The selected version order is: newest compatible `release`, else newest compatible `beta`, never auto-select `alpha`.

- [ ] **Step 7: Download to `.part`, verify hash, then atomically move**

```csharp
var tempPath = finalPath + ".part";
await DownloadFile(file.Url, tempPath, cancellationToken);
await VerifyHashesAsync(tempPath, file.Hashes, cancellationToken);
File.Move(tempPath, finalPath, true);
```

On any exception, delete `.part` and leave the currently installed file untouched.

- [ ] **Step 8: Run all content/compatibility tests**

Expected: PASS including dependency incompatibility, hash mismatch and manual-file-local-only cases.

- [ ] **Step 9: Commit**

```bash
git add lumina-v1/src/Lumina.Core lumina-v1/src/Lumina.Client/Services lumina-v1/tests
git commit -m "feat: add safe Modrinth compatibility and managed content"
```

---

### Task 3: LUMINA verification + curated collection backend

**Files:**
- Create: `lumina-v1/backend/package.json`
- Create: `lumina-v1/backend/wrangler.toml`
- Create: `lumina-v1/backend/src/index.ts`
- Create: `lumina-v1/backend/src/store.ts`
- Create: `lumina-v1/backend/test/index.test.ts`
- Create: `lumina-v1/src/Lumina.Client/Services/VerificationService.cs`
- Create: `lumina-v1/src/Lumina.Client/Services/LuminaCollectionService.cs`

**Interfaces:**
- Backend:
  - `GET /v1/verification/:minecraftUuid`
  - `GET /v1/collection`
  - `POST /v1/collection`
- Client:
  - `Task<bool> IsVerifiedAsync(string minecraftUuid)`
  - `Task<IReadOnlyList<LuminaCollectionEntry>> GetAsync()`
  - `Task AddAsync(string projectId, string category, string note, MSession session)`

- [ ] **Step 1: Add backend tests for permissions and duplicates**

```ts
it("rejects an unverified curator", async () => {
  const response = await app.request("/v1/collection", {
    method: "POST",
    headers: { "content-type": "application/json", "x-minecraft-uuid": "not-verified" },
    body: JSON.stringify({ projectId: "AANobbMI", category: "mod", note: "Fast" })
  });
  expect(response.status).toBe(403);
});

it("rejects duplicate Modrinth project IDs", async () => {
  await seedCollection("AANobbMI");
  const response = await postAsVerified({ projectId: "AANobbMI", category: "mod", note: "Duplicate" });
  expect(response.status).toBe(409);
});
```

- [ ] **Step 2: Define backend storage contract**

```ts
export type CollectionEntry = {
  projectId: string;
  category: "mod" | "resourcepack" | "shader";
  curatorUuid: string;
  addedAt: string;
  note?: string;
};

export interface Store {
  isVerified(uuid: string): Promise<boolean>;
  listCollection(): Promise<CollectionEntry[]>;
  addCollection(entry: CollectionEntry): Promise<"created" | "duplicate">;
}
```

Use remote server-side storage for the verified UUID allowlist and collection; do not ship either as an editable local client file.

- [ ] **Step 3: Implement API permission behavior**

`POST /v1/collection` requires the authenticated/validated Minecraft UUID from the request boundary, rejects unverified UUIDs with 403, validates that the Modrinth project ID exists through Modrinth API, and rejects duplicates with 409.

- [ ] **Step 4: Add client fallback behavior**

```csharp
public async Task<bool> IsVerifiedAsync(string uuid, CancellationToken ct = default)
{
    try
    {
        return await _http.GetFromJsonAsync<VerificationResponse>($"v1/verification/{Uri.EscapeDataString(uuid)}", ct)
               is { Verified: true };
    }
    catch (HttpRequestException)
    {
        return false;
    }
}
```

Backend outage must not break Minecraft launch or normal Modrinth Discover.

- [ ] **Step 5: Run backend and client contract tests**

```bash
cd lumina-v1/backend
npm test
```

```powershell
dotnet test lumina-v1/tests/Lumina.Client.Tests/Lumina.Client.Tests.csproj -c Release --filter Collection
```

- [ ] **Step 6: Commit**

```bash
git add lumina-v1/backend lumina-v1/src/Lumina.Client/Services lumina-v1/tests
git commit -m "feat: add verified LUMINA collection service"
```

---

### Task 4: Account-gated instance creation/fix wizard

**Files:**
- Modify: `lumina-v1/src/Lumina.Client/MainWindow.xaml`
- Modify: `lumina-v1/src/Lumina.Client/MainWindow.xaml.cs`
- Modify: `lumina-v1/src/Lumina.Client/Services/MinecraftCatalogService.cs`

**Interfaces:**
- Consumes: Microsoft session, `MinecraftCatalogService`, `InstanceValidationService`.
- Produces: persisted concrete `InstanceProfile` only after valid summary step.

- [ ] **Step 1: Add testable wizard validation helper**

Create a pure helper in the Core project:

```csharp
public static bool CanCreateInstance(bool signedIn, string name, InstanceValidationResult validation) =>
    signedIn && !string.IsNullOrWhiteSpace(name) && validation.IsValid;
```

Add tests proving signed-out creation is rejected.

- [ ] **Step 2: Replace the one-panel form with five wizard states**

The WPF wizard pages are exactly:

```text
1. Name
2. Minecraft version
3. Loader
4. Loader version
5. Summary
```

`Create` is visible/enabled only on Summary and only when the account/session and validation are valid.

- [ ] **Step 3: Populate versions from live Mojang catalog**

Do not add synthetic `latest-release`/`latest-snapshot` options. Version selection stores the exact `MinecraftVersionChoice.Name`.

- [ ] **Step 4: Populate loader versions only after loader+Minecraft selection**

Vanilla stores `Standard`. Fabric/Quilt/Forge/NeoForge require a concrete loader version returned by `MinecraftCatalogService`.

- [ ] **Step 5: Add invalid legacy instance UI**

If `InstanceProfile.IsValid == false`:

```text
Play button -> disabled
Badge -> "Instanz reparieren"
Primary action -> opens wizard prefilled with current name and nearest valid selections
```

Do not silently change unknown concrete versions.

- [ ] **Step 6: Run WPF build + Core tests**

```powershell
dotnet test lumina-v1/tests/Lumina.Core.Tests/Lumina.Core.Tests.csproj -c Release
dotnet build lumina-v1/src/Lumina.Client/Lumina.Client.csproj -c Release -r win-x64
```

- [ ] **Step 7: Commit**

```bash
git add lumina-v1/src/Lumina.Client lumina-v1/src/Lumina.Core lumina-v1/tests
git commit -m "feat: add validated account-gated instance wizard"
```

---

### Task 5: Full Discover, LUMINA Collection, Installed/Updates UI

**Files:**
- Modify: `lumina-v1/src/Lumina.Client/MainWindow.xaml`
- Modify: `lumina-v1/src/Lumina.Client/MainWindow.xaml.cs`
- Modify: `lumina-v1/src/Lumina.Client/Services/ModrinthService.cs`
- Modify: `lumina-v1/src/Lumina.Client/Services/LuminaCollectionService.cs`

**Interfaces:**
- Consumes: selected valid instance, Modrinth search/install/update APIs, managed-content store, collection API.
- Produces: three Discover views: `Modrinth`, `LUMINA Collection`, `Installed / Updates`.

- [ ] **Step 1: Make Discover default to populated Modrinth results**

An empty search string must call Modrinth with the selected instance filters and `index=downloads`, not show an empty local library.

For mods send exact facets equivalent to:

```json
[
  ["project_type:mod"],
  ["versions:1.21.1"],
  ["categories:fabric"]
]
```

Resource packs/shaders use exact Minecraft-version filtering and their project type.

- [ ] **Step 2: Add card state derived from selected instance**

Each project card action must be one of:

```text
Install
Installed
Update
Incompatible
```

Never decide this only from the original search result; fetch compatible project versions before install/update.

- [ ] **Step 3: Add LUMINA Collection tab**

Load project references from the LUMINA backend, hydrate each project from Modrinth metadata, and hide/disable deleted or incompatible projects.

Verified accounts see `Zur LUMINA Collection hinzufügen`; normal users never see an enabled mutation action.

- [ ] **Step 4: Add Installed / Updates tab**

Compare every managed record with current compatible Modrinth versions. Label files not represented in the managed store as `Local`.

- [ ] **Step 5: Add safe one-click update**

Updating must build a complete compatible install plan first, download/verify replacement files, then remove superseded managed files only after the new set has activated successfully.

- [ ] **Step 6: Run UI build and Modrinth tests**

```powershell
dotnet test lumina-v1/tests/Lumina.Client.Tests/Lumina.Client.Tests.csproj -c Release
dotnet build lumina-v1/src/Lumina.Client/Lumina.Client.csproj -c Release -r win-x64
```

- [ ] **Step 7: Commit**

```bash
git add lumina-v1/src/Lumina.Client lumina-v1/tests
git commit -m "feat: add complete Modrinth and LUMINA Discover experience"
```

---

### Task 6: Launch preflight and startup migration

**Files:**
- Create: `lumina-v1/src/Lumina.Client/Services/MinecraftPreflightService.cs`
- Modify: `lumina-v1/src/Lumina.Client/Services/MinecraftService.cs`
- Modify: `lumina-v1/src/Lumina.Client/MainWindow.xaml.cs`
- Create: `lumina-v1/tests/Lumina.Client.Tests/PreflightTests.cs`

**Interfaces:**
- Produces: `Task<PreflightResult> CheckAsync(InstanceProfile profile, AppSettings settings, MSession session, CancellationToken ct)`.
- `MinecraftService.LaunchAsync` receives only a preflight-approved concrete profile.

- [ ] **Step 1: Add failing pseudo-version regression test**

```csharp
[Theory]
[InlineData("latest-release")]
[InlineData("latest-snapshot")]
public async Task Preflight_Rejects_Pseudo_Version(string version)
{
    var profile = new InstanceProfile { Version = version, Loader = "Vanilla", LoaderVersion = "Standard" };
    var result = await _sut.CheckAsync(profile, ValidSettings(), ValidSession(), CancellationToken.None);
    Assert.False(result.Success);
    Assert.Contains("konkrete Minecraft-Version", result.UserMessage);
}
```

- [ ] **Step 2: Run test and confirm RED**

```powershell
dotnet test lumina-v1/tests/Lumina.Client.Tests/Lumina.Client.Tests.csproj -c Release --filter Preflight
```

- [ ] **Step 3: Run migration on startup before selecting an instance**

Startup sequence becomes:

```text
silent Microsoft auth
load Mojang catalog
migrate all stored instances
persist changed instances
refresh UI
```

If catalog is unavailable, already-concrete known instances can remain visible; new instance creation is disabled until verification returns.

- [ ] **Step 4: Implement launch preflight**

Preflight checks account validity, concrete version existence, loader support, exact loader version, Java custom path if configured, and loader/game resolvability.

Return both concise `UserMessage` and technical `TechnicalMessage` for Downloads/Launch Log.

- [ ] **Step 5: Gate `MinecraftService.LaunchAsync` behind preflight**

`MainWindow.Play_Click` must call preflight first. On failure it logs and returns before CmlLib gets any version string.

- [ ] **Step 6: Run regression tests**

The original `KeyNotFoundException: Cannot find latest-release` path must no longer be reachable.

- [ ] **Step 7: Commit**

```bash
git add lumina-v1/src/Lumina.Client lumina-v1/tests
git commit -m "fix: add launch preflight and legacy startup migration"
```

---

### Task 7: CI smoke coverage and final Windows artifact

**Files:**
- Modify: `.github/workflows/lumina-v1-windows.yml`
- Modify: `lumina-v1/tools/Lumina.LaunchSmoke/Program.cs`

**Interfaces:**
- CI must block publishing unless all Core/client tests, WPF compilation and real Minecraft/Mojang-Java smoke verification succeed.

- [ ] **Step 1: Expand smoke verification to reject aliases**

Before creating the Minecraft process, assert the requested version is a concrete ID:

```csharp
if (version.StartsWith("latest-", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("Smoke test received a pseudo-version.");
```

- [ ] **Step 2: Add Client tests to workflow**

```yaml
- name: Test client services
  run: dotnet test lumina-v1/tests/Lumina.Client.Tests/Lumina.Client.Tests.csproj -c Release --logger "console;verbosity=normal"
```

- [ ] **Step 3: Keep real Minecraft/Mojang-Java pipeline before publish**

The order must remain:

```text
Core tests
Client-service tests
WPF build
Minecraft + Mojang Java smoke test
Self-contained single-EXE publish
SHA-256
Artifact upload
```

- [ ] **Step 4: Run/observe Windows CI**

Expected: every gate succeeds. Do not provide a new EXE if any test/build/smoke step fails.

- [ ] **Step 5: Verify artifact locally after download**

Check:

```text
PE32+ executable (GUI) x86-64
SHA-256 equals runner-generated SHA256.txt
```

- [ ] **Step 6: Commit**

```bash
git add .github/workflows/lumina-v1-windows.yml lumina-v1/tools/Lumina.LaunchSmoke
git commit -m "ci: verify LUMINA gallery and launch pipeline"
```

---

## Final Verification Checklist

Run/confirm all of the following before completion:

```powershell
dotnet test lumina-v1/tests/Lumina.Core.Tests/Lumina.Core.Tests.csproj -c Release
dotnet test lumina-v1/tests/Lumina.Client.Tests/Lumina.Client.Tests.csproj -c Release
dotnet build lumina-v1/src/Lumina.Client/Lumina.Client.csproj -c Release -r win-x64
```

Backend:

```bash
cd lumina-v1/backend
npm test
```

Manual acceptance on Windows:

```text
1. Start with an existing instance containing latest-release -> it migrates to a concrete release and no CmlLib alias error occurs.
2. Sign out -> New Instance is account-gated.
3. Sign in -> create Fabric instance by selecting name, Minecraft version, loader and compatible loader version.
4. Discover with empty query -> populated compatible Modrinth catalog is visible.
5. Install a compatible mod -> required dependencies install and managed sidecar is written.
6. Switch to an incompatible instance -> that mod/version is no longer offered as compatible.
7. Add a manual jar -> it is shown as Local and never appears globally.
8. Verified UUID -> can add an existing Modrinth project reference to LUMINA Collection.
9. Unverified UUID -> POST/add action is denied.
10. Disable LUMINA backend -> normal Modrinth Discover and Minecraft launch still function.
11. Disable Modrinth network -> local library and Minecraft launch remain usable.
12. Windows CI including Minecraft/Mojang-Java smoke test passes before publishing EXE.
```

## Self-Review Result

- Spec coverage: all ten Definition-of-Done points are mapped to Tasks 1-7.
- Placeholder scan: no TBD/TODO/"implement later" instructions remain.
- Type consistency: migration, validation, compatibility, managed-content, collection and preflight interfaces are named consistently throughout the plan.
- Scope: backend stores only verification/collection references; binary hosting and arbitrary uploads remain explicitly out of scope.