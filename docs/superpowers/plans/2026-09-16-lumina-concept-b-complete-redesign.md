# LUMINA Concept B Complete Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild the existing WPF launcher into the approved Concept B LUMINA UI while preserving Minecraft launch, Modrinth compatibility, instance migration, Microsoft authentication, managed updates, and current backend behavior.

**Architecture:** Keep existing service/business logic intact and replace the runtime-mutated presentation layer with declarative WPF XAML, shared resource dictionaries, and a single in-app feedback/confirmation service. `MainWindow` remains the application coordinator for this release, but UI-only responsibilities are split into focused XAML/resources and partial classes instead of `OnActivated` tree rewriting. The approved Concept B composition is the source of truth for shell, home, storefront, instance, settings, account, and feedback presentation.

**Tech Stack:** .NET 8, WPF, C#, CmlLib.Core 4.0.6, CmlLib Microsoft Auth 3.3.1, xUnit, GitHub Actions Windows runner.

**Spec:** `docs/superpowers/specs/2026-09-16-lumina-concept-b-complete-redesign-design.md`

## Global Constraints

- Branch stays `lumina-v1-client`; do not merge `main` as part of this work.
- Preserve Microsoft/Minecraft login, silent login, instance migration, launch preflight, Vanilla/Fabric/Quilt/Forge/NeoForge, Modrinth install/dependency/hash behavior, LUMINA Collection, local imports, Safe Launch, updates, and launch logging.
- Do not implement a fake Microsoft password form. Interactive auth continues through CmlLib/Microsoft; LUMINA owns surrounding status and errors only.
- Normal login/settings/install/launch feedback must not use native `MessageBox` dialogs after the notification task is complete.
- Destructive actions use the new LUMINA confirmation overlay rather than a stock Windows confirmation.
- Dark dropdown popup/items must remain explicitly styled and readable.
- Existing minimum client window remains usable without horizontal scrolling.
- Decorative artwork must have a gradient fallback and may never block launch.
- Do not rewrite launch/business services merely for visual reasons.

---

### Task 1: Lock the Concept B theme and remove runtime UI mutation

**Files:**
- Create: `lumina-v1/src/Lumina.Client/Themes/ConceptB.xaml`
- Modify: `lumina-v1/src/Lumina.Client/App.xaml`
- Delete after replacement: `lumina-v1/src/Lumina.Client/MainWindow.PremiumUi.cs`
- Modify: `lumina-v1/tests/Lumina.Client.Tests/Lumina.Client.Tests.csproj`
- Modify: `lumina-v1/tests/Lumina.Client.Tests/ThemeContrastTests.cs`
- Create: `lumina-v1/tests/Lumina.Client.Tests/ConceptBThemeTests.cs`

**Interfaces:**
- Produces resource keys used by all later tasks: `LuminaWindowBackground`, `LuminaSidebarBackground`, `LuminaSurface`, `LuminaSurfaceRaised`, `LuminaBorder`, `LuminaAccent`, `LuminaAccentBrush`, `LuminaHeroFallback`, `LuminaCard`, `LuminaNavButton`, `LuminaPrimaryButton`, `LuminaSecondaryButton`, `LuminaTabButton`, `LuminaToggle`, `LuminaComboBox`, `LuminaComboBoxItem`, `LuminaToastCard`, `LuminaModalCard`.
- Existing controls continue using normal WPF `Button`, `ComboBox`, `TextBox`, `CheckBox`, and `Border` types.

- [ ] **Step 1: Write failing theme tests**

Add tests that require the new resource dictionary and dark control states:

```csharp
[Fact]
public void ConceptB_Theme_Defines_Core_Resources()
{
    var xaml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ConceptBUnderTest.xaml"));
    foreach (var key in new[]
    {
        "LuminaWindowBackground", "LuminaSidebarBackground", "LuminaSurface",
        "LuminaAccentBrush", "LuminaNavButton", "LuminaPrimaryButton",
        "LuminaComboBox", "LuminaToastCard", "LuminaModalCard"
    })
        Assert.Contains($"x:Key=\"{key}\"", xaml, StringComparison.Ordinal);
}

[Fact]
public void ConceptB_ComboBox_Uses_Dark_Popup_And_Bright_Text()
{
    var xaml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "ConceptBUnderTest.xaml"));
    Assert.Contains("PART_Popup", xaml, StringComparison.Ordinal);
    Assert.Contains("ComboBoxItem", xaml, StringComparison.Ordinal);
    Assert.Contains("#F4F6FB", xaml, StringComparison.OrdinalIgnoreCase);
    Assert.DoesNotContain("SystemColors.WindowBrush", xaml, StringComparison.Ordinal);
}
```

Update the test csproj with:

```xml
<None Include="../../src/Lumina.Client/Themes/ConceptB.xaml"
      Link="ConceptBUnderTest.xaml"
      CopyToOutputDirectory="PreserveNewest" />
```

- [ ] **Step 2: Run client tests and verify RED**

Run:

```powershell
dotnet test lumina-v1/tests/Lumina.Client.Tests/Lumina.Client.Tests.csproj -c Release
```

Expected: failures because `ConceptB.xaml` and its resource keys do not exist.

- [ ] **Step 3: Implement the Concept B resource dictionary**

Create a complete dictionary using the approved palette and declarative templates. Required palette floor:

```xml
<SolidColorBrush x:Key="LuminaWindowBackground" Color="#060812" />
<SolidColorBrush x:Key="LuminaSidebarBackground" Color="#090C17" />
<SolidColorBrush x:Key="LuminaSurface" Color="#0D1220" />
<SolidColorBrush x:Key="LuminaSurfaceRaised" Color="#12192A" />
<SolidColorBrush x:Key="LuminaBorder" Color="#252D45" />
<SolidColorBrush x:Key="LuminaAccentBrush" Color="#8B5CF6" />
<SolidColorBrush x:Key="LuminaText" Color="#F4F6FB" />
<SolidColorBrush x:Key="LuminaTextMuted" Color="#98A2B8" />
<LinearGradientBrush x:Key="LuminaHeroFallback" StartPoint="0,0" EndPoint="1,1">
  <GradientStop Color="#2B1654" Offset="0" />
  <GradientStop Color="#11162A" Offset="0.55" />
  <GradientStop Color="#080B14" Offset="1" />
</LinearGradientBrush>
```

Keep the existing proven dark popup logic but move it into keyed `LuminaComboBox` / `LuminaComboBoxItem` styles so it is reusable and independently testable.

- [ ] **Step 4: Merge ConceptB.xaml from App.xaml**

Reduce `App.xaml` to merged dictionaries and only application-wide defaults:

```xml
<Application.Resources>
  <ResourceDictionary>
    <ResourceDictionary.MergedDictionaries>
      <ResourceDictionary Source="Themes/ConceptB.xaml" />
    </ResourceDictionary.MergedDictionaries>
  </ResourceDictionary>
</Application.Resources>
```

- [ ] **Step 5: Remove `MainWindow.PremiumUi.cs`**

Delete the file only after every style/layout mutation it provided has a declarative replacement. The new UI must never depend on `OnActivated` to rewrite the visual tree.

- [ ] **Step 6: Run tests and WPF build**

```powershell
dotnet test lumina-v1/tests/Lumina.Client.Tests/Lumina.Client.Tests.csproj -c Release
dotnet build lumina-v1/src/Lumina.Client/Lumina.Client.csproj -c Release -r win-x64
```

Expected: PASS and build with zero errors.

- [ ] **Step 7: Commit**

```bash
git add lumina-v1/src/Lumina.Client/Themes lumina-v1/src/Lumina.Client/App.xaml lumina-v1/tests/Lumina.Client.Tests
git rm lumina-v1/src/Lumina.Client/MainWindow.PremiumUi.cs
git commit -m "feat: establish LUMINA Concept B theme"
```

---

### Task 2: Add LUMINA-owned notifications and confirmation overlays

**Files:**
- Create: `lumina-v1/src/Lumina.Client/Ui/LuminaFeedback.cs`
- Create: `lumina-v1/src/Lumina.Client/Ui/LuminaNotificationHost.xaml`
- Create: `lumina-v1/src/Lumina.Client/Ui/LuminaNotificationHost.xaml.cs`
- Create: `lumina-v1/tests/Lumina.Client.Tests/LuminaFeedbackTests.cs`
- Modify: `lumina-v1/src/Lumina.Client/MainWindow.xaml`
- Modify: `lumina-v1/src/Lumina.Client/MainWindow.xaml.cs`
- Modify: `lumina-v1/src/Lumina.Client/MainWindow.Discover.cs`

**Interfaces:**
- Produces `LuminaFeedback.Show(NotificationKind kind, string title, string message)`.
- Produces `Task<bool> LuminaFeedback.ConfirmAsync(string title, string message, string confirmText, bool danger = false)`.
- Produces event-driven `LuminaNotificationHost` that is mounted once over the shell.

- [ ] **Step 1: Write failing queue/confirmation tests**

```csharp
[Fact]
public void Show_Enqueues_A_Notification()
{
    var feedback = new LuminaFeedback();
    feedback.Show(NotificationKind.Success, "Gespeichert", "Einstellungen gespeichert.");
    var item = Assert.Single(feedback.Notifications);
    Assert.Equal(NotificationKind.Success, item.Kind);
    Assert.Equal("Gespeichert", item.Title);
}

[Fact]
public async Task ConfirmAsync_Completes_With_User_Result()
{
    var feedback = new LuminaFeedback();
    var pending = feedback.ConfirmAsync("Löschen?", "Instanz entfernen", "Löschen", danger: true);
    Assert.NotNull(feedback.ActiveConfirmation);
    feedback.ResolveConfirmation(true);
    Assert.True(await pending);
}
```

- [ ] **Step 2: Run tests and verify RED**

Expected: missing `LuminaFeedback` types.

- [ ] **Step 3: Implement feedback model/service**

Use an observable collection for transient notifications and a `TaskCompletionSource<bool>` for one active confirmation. No WPF `MessageBox` dependency exists inside this service.

```csharp
public enum NotificationKind { Success, Info, Warning, Error }

public sealed class LuminaFeedback
{
    public ObservableCollection<LuminaNotification> Notifications { get; } = [];
    public LuminaConfirmation? ActiveConfirmation { get; private set; }
    // Show, ConfirmAsync, ResolveConfirmation, Dismiss
}
```

- [ ] **Step 4: Implement notification host XAML**

The host contains top-right stacked toast cards and a centered modal overlay. Error/warning/success colors use Concept B resources. Confirmation overlay traps pointer interaction behind a translucent backdrop.

- [ ] **Step 5: Mount the host in MainWindow**

Add a root overlay layer after the page content:

```xml
<ui:LuminaNotificationHost x:Name="FeedbackHost"
                           Grid.ColumnSpan="2"
                           Panel.ZIndex="100" />
```

Initialize once:

```csharp
_feedback = new LuminaFeedback();
FeedbackHost.Attach(_feedback);
```

- [ ] **Step 6: Replace normal `MessageBox` feedback**

Convert login failure, settings saved, launch failure, gallery install/update failure, duplicate-instance failure, collection add success/failure, and validation notices to `_feedback.Show(...)`. Convert instance delete confirmation to `await _feedback.ConfirmAsync(...)`.

Keep full exception text only in `AddLog`; user message should be concise, for example:

```csharp
AddLog("STARTFEHLER: " + ex);
_feedback.Show(NotificationKind.Error,
    "Minecraft konnte nicht starten",
    "Öffne Downloads → Launch Log für technische Details.");
```

- [ ] **Step 7: Run tests and grep for normal MessageBox paths**

```powershell
dotnet test lumina-v1/tests/Lumina.Client.Tests/Lumina.Client.Tests.csproj -c Release
Select-String -Path lumina-v1/src/Lumina.Client/*.cs,lumina-v1/src/Lumina.Client/**/*.cs -Pattern 'MessageBox.Show'
```

Expected: no normal login/settings/install/launch flows remain. If any stock dialog remains, it must be justified as a Windows file picker or a path outside the target feedback categories; `OpenFileDialog` is allowed.

- [ ] **Step 8: Commit**

```bash
git add lumina-v1/src/Lumina.Client/Ui lumina-v1/src/Lumina.Client/MainWindow* lumina-v1/tests/Lumina.Client.Tests
git commit -m "feat: add in-app LUMINA feedback system"
```

---

### Task 3: Rebuild the application shell and Concept B home screen

**Files:**
- Modify: `lumina-v1/src/Lumina.Client/MainWindow.xaml`
- Create: `lumina-v1/src/Lumina.Client/MainWindow.ConceptB.cs`
- Modify: `lumina-v1/src/Lumina.Client/MainWindow.xaml.cs`
- Create binary resource during execution: `lumina-v1/src/Lumina.Client/Assets/hero-concept-b.png`
- Modify: `lumina-v1/src/Lumina.Client/Lumina.Client.csproj` only if explicit resource inclusion is required by the build
- Create: `lumina-v1/tests/Lumina.Client.Tests/ShellStructureTests.cs`

**Interfaces:**
- Existing x:Name hooks used by launch/business code remain: `SidebarNav`, `HomePage`, `DiscoverPage`, `InstancesPage`, `LibraryPage`, `DownloadsPage`, `SettingsPage`, `AccountPage`, `HomePlayButton`, `HomeStatus`, `InstancesList`, `GalleryList`, `ContentList`, `DownloadLog`.
- New home switcher: `ActiveInstanceCombo` updates the same selected instance/settings state as `InstancesList`.

- [ ] **Step 1: Write failing shell structure tests**

Link `MainWindow.xaml` into test output and assert the approved hierarchy:

```csharp
[Fact]
public void MainWindow_Contains_ConceptB_Shell()
{
    var xaml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "MainWindowUnderTest.xaml"));
    Assert.Contains("Width=\"236\"", xaml, StringComparison.Ordinal);
    Assert.Contains("EXPLORE", xaml, StringComparison.Ordinal);
    Assert.Contains("CREATE", xaml, StringComparison.Ordinal);
    Assert.Contains("BELONG", xaml, StringComparison.Ordinal);
    Assert.Contains("x:Name=\"ActiveInstanceCombo\"", xaml, StringComparison.Ordinal);
    Assert.DoesNotContain("Compact client rail", xaml, StringComparison.Ordinal);
}
```

- [ ] **Step 2: Run and verify RED**

Expected: current shell still has compact/old layout.

- [ ] **Step 3: Produce original hero artwork**

Use the approved Concept B composition as visual direction but create an original LUMINA-owned image with no baked-in UI text, logos from third parties, or copied launcher art. Target approximately 1920×720. Store it as `Assets/hero-concept-b.png`. The XAML must layer text separately and use `LuminaHeroFallback` behind the image.

- [ ] **Step 4: Replace the shell declaratively**

Main grid has exactly two columns: fixed 236 px sidebar + content. Sidebar contains brand, ordered nav buttons, spacer, Settings, Account, and bottom account card. Remove runtime-generated nav labels.

Use a page content margin of roughly `24,18,24,24`; keep custom titlebar/window controls small and separate from content.

- [ ] **Step 5: Replace Home with the approved composition**

Home order:
1. cinematic hero with `EXPLORE / CREATE / BELONG` text,
2. active instance bar with name/version/loader and large Play,
3. content tabs/search preview area,
4. a restrained promo/community row only if vertical space permits.

Remove the old statistics wall and feature cards from Home.

- [ ] **Step 6: Wire active-instance switcher without changing persistence rules**

```csharp
private void ActiveInstanceCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (_initializing || ActiveInstanceCombo.SelectedItem is not InstanceProfile profile) return;
    InstancesList.SelectedItem = profile;
}
```

Bind `ActiveInstanceCombo.ItemsSource = _instances` once in constructor and keep both controls synchronized through existing `InstancesList_SelectionChanged`.

- [ ] **Step 7: Build and run tests**

```powershell
dotnet test lumina-v1/tests/Lumina.Client.Tests/Lumina.Client.Tests.csproj -c Release
dotnet build lumina-v1/src/Lumina.Client/Lumina.Client.csproj -c Release -r win-x64
```

- [ ] **Step 8: Commit**

```bash
git add lumina-v1/src/Lumina.Client/MainWindow* lumina-v1/src/Lumina.Client/Assets lumina-v1/tests/Lumina.Client.Tests
git commit -m "feat: rebuild LUMINA shell and home"
```

---

### Task 4: Turn Discover into the Concept B storefront

**Files:**
- Modify: `lumina-v1/src/Lumina.Client/MainWindow.xaml`
- Modify: `lumina-v1/src/Lumina.Client/MainWindow.Discover.cs`
- Modify: `lumina-v1/src/Lumina.Client/InstalledUpdatesView.xaml`
- Create: `lumina-v1/tests/Lumina.Client.Tests/DiscoverPresentationTests.cs`

**Interfaces:**
- Preserve `RefreshDiscoverModeAsync`, `LoadLuminaCollectionAsync`, `LoadInstalledUpdatesAsync`, `GalleryInstall_Click`, `InstalledUpdate_Requested`.
- Preserve `_gallery`, `_modrinthService`, compatibility filtering, collection behavior, and managed updates.
- Presentation tabs map onto existing modes plus content kind: Featured/Mods/Shaders/Resource Packs; unsupported Maps does not pretend to install unsupported data.

- [ ] **Step 1: Write failing storefront structure tests**

Assert `DiscoverPage` contains tab strip, search field, Minecraft version filter, loader filter, and card template with image/title/install action. Also assert `InstalledUpdatesView` uses Concept B card/style keys.

- [ ] **Step 2: Verify RED**

Current Discover is still the older mixed layout.

- [ ] **Step 3: Rebuild Discover XAML**

Use one header row, one horizontal tab row, one filter row, and a responsive `WrapPanel`/items panel for cards. Cards target ~240–280 px width with 16:9 thumbnail, title, type, download count, compatibility badge, and one clear Install button.

- [ ] **Step 4: Map tabs to current safe search behavior**

Do not bypass compatibility. A tab switch sets the content kind and calls the existing search path. For Vanilla + Mods, keep the existing informative empty state.

- [ ] **Step 5: Integrate Collection and Installed/Updates cleanly**

Keep a secondary segmented control inside Discover for `Modrinth`, `LUMINA Collection`, and `Installiert & Updates`, or expose Collection/Installed as first-class tabs without removing existing behavior. Outage text remains inline; no modal blocks launch.

- [ ] **Step 6: Replace collection/update `MessageBox` calls with feedback service**

Success example:

```csharp
_feedback.Show(NotificationKind.Success, "Zur Collection hinzugefügt", project.Title);
```

- [ ] **Step 7: Test and build**

Run client tests and WPF build. Existing Modrinth compatibility/integrity tests must stay green.

- [ ] **Step 8: Commit**

```bash
git add lumina-v1/src/Lumina.Client/MainWindow.xaml lumina-v1/src/Lumina.Client/MainWindow.Discover.cs lumina-v1/src/Lumina.Client/InstalledUpdatesView.xaml lumina-v1/tests/Lumina.Client.Tests
git commit -m "feat: redesign Discover as LUMINA storefront"
```

---

### Task 5: Redesign Instances and the 5-step wizard

**Files:**
- Modify: `lumina-v1/src/Lumina.Client/MainWindow.xaml`
- Modify: `lumina-v1/src/Lumina.Client/MainWindow.Wizard.cs`
- Modify: `lumina-v1/src/Lumina.Client/InstanceWizardWindow.xaml`
- Modify only as needed: `lumina-v1/src/Lumina.Client/InstanceWizardWindow.xaml.cs`
- Create: `lumina-v1/tests/Lumina.Client.Tests/InstanceWizardPresentationTests.cs`

**Interfaces:**
- Preserve `InstanceWizardResult(Name, MinecraftVersion, Loader, LoaderVersion, AllowSnapshots)`.
- Preserve `InstanceWizardPolicy`, `InstanceValidationService`, migration behavior, `OpenRepairWizardAsync`, and launch preflight.

- [ ] **Step 1: Write failing presentation tests**

Assert five steps still exist and the loader UI explicitly includes Vanilla/Fabric/Quilt/Forge/NeoForge labels or binding source plus readable Concept B selection state. Assert there is no stock-light control template reference.

- [ ] **Step 2: Verify RED**

- [ ] **Step 3: Rebuild Instances page**

Use a clean instance list/grid with active/invalid badges and a single selected-instance details panel. Keep actions: Play/Repair, Folder, Duplicate, Delete, New Instance.

- [ ] **Step 4: Redesign wizard shell**

Keep five-step logic but use a dark left progress rail or top progress strip, larger content area, concise descriptions, and a fixed footer. Step 3 loader choice should be visual cards or a highly readable list; step 4 uses the dark ComboBox style.

- [ ] **Step 5: Convert repair/validation feedback to LUMINA feedback**

Invalid legacy instance never exposes raw exception text in the page. The full error remains in logs.

- [ ] **Step 6: Test all wizard/core policies**

```powershell
dotnet test lumina-v1/tests/Lumina.Core.Tests/Lumina.Core.Tests.csproj -c Release
dotnet test lumina-v1/tests/Lumina.Client.Tests/Lumina.Client.Tests.csproj -c Release
```

- [ ] **Step 7: Commit**

```bash
git add lumina-v1/src/Lumina.Client/MainWindow.xaml lumina-v1/src/Lumina.Client/MainWindow.Wizard.cs lumina-v1/src/Lumina.Client/InstanceWizardWindow* lumina-v1/tests/Lumina.Client.Tests
git commit -m "feat: redesign instances and instance wizard"
```

---

### Task 6: Redesign Library, Downloads, Settings, and Account

**Files:**
- Modify: `lumina-v1/src/Lumina.Client/MainWindow.xaml`
- Modify: `lumina-v1/src/Lumina.Client/MainWindow.xaml.cs`
- Create: `lumina-v1/src/Lumina.Client/MainWindow.Settings.cs`
- Create: `lumina-v1/tests/Lumina.Client.Tests/SettingsPresentationTests.cs`
- Create: `lumina-v1/tests/Lumina.Client.Tests/AccountFeedbackTests.cs`

**Interfaces:**
- Preserve existing settings properties and `SettingsService` persistence.
- Preserve local import/drag-drop/toggle/delete content behavior.
- Preserve `_downloadLog`, Minecraft status/progress events, and account service methods.

- [ ] **Step 1: Write failing Settings/Account tests**

Require RAM preset labels `4 GB`, `6 GB`, `8 GB`, `12 GB`, `16 GB`, explicit Concept B toggle style, account status area, and absence of Microsoft password fields.

```csharp
Assert.DoesNotContain("PasswordBox", accountXaml, StringComparison.Ordinal);
```

- [ ] **Step 2: Rebuild Library**

Tabs: Mods / Resource Packs / Shaders. Show import/drop zone as a bordered Concept B panel. Content rows/cards show enabled state, local/managed status, and actions without resembling a file manager.

- [ ] **Step 3: Rebuild Downloads/Activity**

Top: current operation card with progress and status. Bottom: scrollable launch/activity log. Launch error notification links the user mentally to this page; technical details stay here.

- [ ] **Step 4: Rebuild Settings declaratively**

Move settings reconstruction logic out of the deleted runtime UI file into XAML + `MainWindow.Settings.cs`. Required sections: Performance, Game Window, Java, Safety, Quick Connect. RAM presets set `RamSlider.Value`; save uses `_feedback.Show(Success, ...)` instead of MessageBox.

- [ ] **Step 5: Rebuild Account page**

States:
- signed out: integrated Microsoft sign-in call-to-action,
- signing in: spinner/progress text inside page,
- signed in: avatar/player name/UUID status, switch account, sign out,
- failed: inline error banner + Retry.

`LoginInteractiveAsync` keeps calling `_accountService.LoginInteractiveAsync()`; only surrounding UI changes.

- [ ] **Step 6: Replace remaining non-destructive native alerts**

Run `Select-String` for `MessageBox.Show` again and migrate all target paths.

- [ ] **Step 7: Test/build and commit**

```powershell
dotnet test lumina-v1/tests/Lumina.Client.Tests/Lumina.Client.Tests.csproj -c Release
dotnet build lumina-v1/src/Lumina.Client/Lumina.Client.csproj -c Release -r win-x64
git add lumina-v1/src/Lumina.Client lumina-v1/tests/Lumina.Client.Tests
git commit -m "feat: redesign LUMINA utility pages and account UX"
```

---

### Task 7: Responsive polish, fallback behavior, and visual regression guards

**Files:**
- Modify: `lumina-v1/src/Lumina.Client/MainWindow.xaml`
- Modify: `lumina-v1/src/Lumina.Client/Themes/ConceptB.xaml`
- Modify: `lumina-v1/src/Lumina.Client/InstanceWizardWindow.xaml`
- Create: `lumina-v1/tests/Lumina.Client.Tests/UiRegressionTests.cs`

**Interfaces:**
- No business interfaces change.

- [ ] **Step 1: Add regression tests for required UX markers**

Tests assert:
- sidebar labels all present,
- Home has one primary Play action,
- hero fallback brush exists,
- ComboBox popup/item contrast remains,
- settings labels remain visible,
- no `PasswordBox`,
- no runtime `ApplyPremiumUi` / `OnActivated` mutation file,
- no stock `MessageBox.Show` in normal target source files.

- [ ] **Step 2: Verify RED for any remaining gaps**

- [ ] **Step 3: Polish minimum-size behavior**

At 1180×720:
- sidebar remains 236 px,
- main content scrolls vertically where needed,
- card panels wrap instead of horizontal scrolling,
- hero may reduce height/text size via triggers but Play remains visible,
- settings uses two columns only where width allows; otherwise vertical stack.

- [ ] **Step 4: Add artwork fallback**

Hero container always has `LuminaHeroFallback`; the image sits above it with `Stretch=UniformToFill`. Missing resource must not break constructor or launch path.

- [ ] **Step 5: Full local/CI-equivalent test sequence**

```powershell
dotnet test lumina-v1/tests/Lumina.Core.Tests/Lumina.Core.Tests.csproj -c Release
dotnet test lumina-v1/tests/Lumina.Client.Tests/Lumina.Client.Tests.csproj -c Release
dotnet build lumina-v1/src/Lumina.Client/Lumina.Client.csproj -c Release -r win-x64
```

- [ ] **Step 6: Commit**

```bash
git add lumina-v1/src/Lumina.Client lumina-v1/tests/Lumina.Client.Tests
git commit -m "test: lock Concept B UX regressions"
```

---

### Task 8: Final Windows verification and release artifact

**Files:**
- Verify: `.github/workflows/lumina-v1-windows.yml`
- Modify only if necessary: workflow labels/artifact name
- Output from CI: `lumina-v1/publish/LUMINA.exe`, `lumina-v1/publish/SHA256.txt`

**Interfaces:**
- Release artifact remains a self-contained Windows x64 single EXE.

- [ ] **Step 1: Run complete workflow on latest branch HEAD**

Require success for:
1. core restore/tests,
2. client restore/tests,
3. backend tests/typecheck,
4. WPF restore/build,
5. Minecraft + Mojang Java smoke,
6. self-contained publish,
7. SHA-256 generation,
8. artifact upload.

- [ ] **Step 2: Inspect workflow logs rather than assuming success**

Verify exact test counts, WPF build exit code, smoke-test exit code, publish step, and artifact upload step from the same HEAD SHA.

- [ ] **Step 3: Inspect final artifact contents**

Artifact must contain only the expected release files at minimum:

```text
LUMINA.exe
SHA256.txt
```

Verify the calculated EXE SHA-256 matches `SHA256.txt`.

- [ ] **Step 4: Final source audit**

Search for:

```text
MessageBox.Show
ApplyPremiumUi
Compact client rail
PasswordBox
```

Expected:
- no old runtime premium UI mutation,
- no fake Microsoft password UI,
- no normal target feedback paths using MessageBox,
- no old compact shell marker.

- [ ] **Step 5: Manual visual inspection against approved Concept B structure**

Check the built UI for:
- wide icon+text sidebar,
- cinematic hero,
- active-instance bar + dominant purple Play button,
- structured storefront cards,
- readable dark dropdowns including `Vanilla`,
- clean Settings sections,
- integrated Account state,
- in-app LUMINA notifications/modals,
- no old statistics-card wall on Home.

- [ ] **Step 6: Deliver final EXE only after verification**

Report the workflow run ID, HEAD commit SHA, test/build status, EXE size, and SHA-256 alongside the downloadable artifact. Do not call the release finished if any required step is skipped or red.

---

## Plan Self-Review

**Spec coverage:** All approved sections are mapped: Concept B visual direction (Tasks 1, 3, 7), shell/navigation (3), Home (3), Discover (4), Instances/Wizard (5), Library/Downloads/Settings/Account (6), Microsoft UX + in-app notifications (2, 6), artwork/fallback/accessibility (3, 7), preservation of launcher behavior (global constraints + all tasks), final tests/publish (8).

**Placeholder scan:** No TBD/TODO/"implement later" steps. Decorative artwork has an explicit creation target and fallback requirement.

**Type consistency:** `LuminaFeedback`, `NotificationKind`, `ActiveInstanceCombo`, and existing `InstanceWizardResult` names are used consistently. Existing service APIs are consumed unchanged.
