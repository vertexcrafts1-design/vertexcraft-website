# LUMINA Concept B Complete Redesign

Date: 2026-09-16
Branch: `lumina-v1-client`
Status: Approved visual direction; implementation pending

## Goal

Rebuild the LUMINA desktop client UI/UX to match the approved Concept B direction: a premium, immersive, dark-purple Minecraft launcher that is highly structured, easy to use, and visually competitive with modern gaming launchers.

This is not a recolor. The existing launcher shell, page hierarchy, navigation density, account feedback, settings layout, cards, mod gallery presentation, and interaction feedback are reorganized around the approved design.

## Visual Direction

- Near-black base with deep indigo surfaces and restrained purple glow.
- Wide left sidebar with icon + text labels.
- Strong spacing and hierarchy; no dense random card wall.
- Large cinematic home hero with LUMINA identity and Minecraft-style world artwork.
- Rounded, consistent panels with subtle borders and glow only on important actions.
- Large purple Play button as the strongest action.
- Dark custom dropdowns everywhere, including popup content, selected state, hover, and disabled state.
- Typography must remain readable at normal Windows scaling.
- Avoid stock Windows visual controls wherever they break the design language.

## Shell and Navigation

Sidebar order:
1. Home
2. Discover
3. Instances
4. Library
5. Downloads
6. Settings
7. Account

The active page receives a clear purple selection state. Account status is integrated at the lower sidebar area with avatar, player name, and status.

Top-level shell uses a custom title bar with minimize, maximize/restore, and close actions. The old compact rail is removed.

## Home

The home page follows the approved Concept B composition:

- Large cinematic hero across the upper content area.
- LUMINA headline: `EXPLORE / CREATE / BELONG`.
- Supporting text describing one-place management for Minecraft, mods, shaders, packs, and instances.
- Block-style player/landscape art integrated into the hero.
- Below hero: one prominent active-instance bar.
- Active instance bar shows icon, instance name, Minecraft version, loader and loader version, active state, large Play button, and instance switch dropdown.
- No secondary statistics wall or unnecessary cards on the primary home screen.
- Content discovery preview appears below the active instance bar and follows the same card design as Discover.

## Discover

Discover is organized as a premium storefront/content browser.

Tabs:
- Featured
- Modpacks
- Mods
- Shaders
- Resource Packs
- Maps where supported

Filtering:
- Search field
- Minecraft version filter
- Loader filter
- Instance compatibility remains authoritative

Cards:
- Large visual thumbnail
- Project title
- Type/category
- Downloads
- Rating where available
- One clear Install button

Existing Modrinth compatibility, dependency resolution, hash verification, managed content records, LUMINA Collection and installed/update functionality remain intact. The redesign changes presentation and navigation, not safety rules.

## Instances

Instances becomes a focused management page rather than mixed scattered panels.

- Left or upper instance list/grid with clear active state.
- Primary details panel for the selected instance.
- Actions: Play, open folder, duplicate, repair, delete.
- New instance action opens the existing validated multi-step flow, visually redesigned to match Concept B.
- Instance creation continues to require a concrete Minecraft version, loader, and loader version.
- Invalid/migrated legacy instances display an in-app repair state rather than raw exceptions.

## Instance Wizard

The 5-step wizard remains functionally intact but is redesigned:

1. Name
2. Minecraft version
3. Loader
4. Loader version
5. Review / Create

Each step should show only the controls needed for that step. Loader selection should use visually distinct cards or highly readable controls. Vanilla, Fabric, Quilt, Forge and NeoForge must remain clearly readable in every state.

## Library / Installed Content

Library separates content by type and makes local/managed state obvious.

- Mods
- Resource Packs
- Shaders

Managed Modrinth content can show update state. Manual local files remain local and are never auto-updated. Drag-and-drop/import remains available but is visually integrated into the page rather than appearing as a generic Windows file-tool workflow.

## Downloads / Launch Activity

Downloads becomes a proper in-app activity center:

- Minecraft download/install progress
- Mod installation progress
- Launch preparation
- Launch log
- Errors and retry actions

Normal errors should not open native Windows `MessageBox` dialogs. Errors appear in LUMINA-owned banners, toasts, inline cards, or modal overlays.

## Settings

Settings is rebuilt into clearly separated sections:

### Performance
- RAM presets: 4 / 6 / 8 / 12 / 16 GB
- Optional advanced slider/value
- Smart Memory toggle

### Game Window
- Width / height
- Close launcher on game start
- Focus mode where retained

### Java
- Automatic/default runtime state
- Custom Java path with Browse action
- Clear validation status

### Safety
- Safe Launch toggle
- Explanation of what it protects

### Quick Connect
- Server address
- Clear save/apply state

Settings must use in-app save feedback rather than a Windows message box.

## Account and Microsoft UX

All user-facing LUMINA account state lives inside the application.

Remove native Windows `MessageBox` alerts for Microsoft login success/failure and account flow status. Replace them with LUMINA-owned UI:

- Account page status panel
- In-app progress state
- In-app success/error banner or modal
- Retry action
- Sign-out action

The actual Microsoft authentication step must remain secure. LUMINA must never request, capture, store, or imitate a Microsoft password form. If Microsoft authentication requires an external secure Microsoft browser/window, that authentication surface is allowed; all surrounding status, errors, and guidance are rendered in LUMINA.

Silent login should not spam the user with alerts.

## Notification System

Introduce one shared in-app notification layer for normal application feedback.

Types:
- success
- info
- warning
- error

Uses:
- settings saved
- account login status
- failed login
- content installation success/failure
- launch error
- instance validation/repair message

Native `MessageBox` may remain only for destructive confirmation if no custom confirmation modal is implemented yet; target state is to use a LUMINA-owned modal for those too.

## Design Assets

The approved Concept B mockup is the visual target. Production artwork may use original LUMINA-owned/generated assets stored with the client. No third-party copyrighted launcher artwork should be copied.

The hero and promotional artwork are decorative and must not block core launcher operation if missing or failed to load.

## Interaction and Accessibility

- Keyboard focus remains visible.
- Minimum text contrast should remain high on all dark surfaces.
- Dropdown popup items use the same dark theme as the closed control.
- Hover/pressed/focus states are visually distinct.
- Main launcher remains usable at the existing minimum supported window size.
- Scrolling must be predictable; no page should require horizontal scrolling at normal size.

## Architecture

The redesign should separate UI shell and common visual components from business logic.

Recommended WPF structure:
- shared theme/resources in `App.xaml` and/or focused resource dictionaries
- reusable controls/styles for navigation, cards, buttons, dropdowns, tabs, badges, toggles and notifications
- page-oriented UI sections instead of continually growing `MainWindow.xaml`
- existing services (`MinecraftService`, `MinecraftCatalogService`, `ModrinthService`, account service, instance services) remain the data/business layer
- existing launch and compatibility behavior should not be rewritten solely for visual reasons

Where practical, split very large UI files into focused page/user-control files while preserving existing behavior.

## Error Handling

- No raw stack traces shown to users.
- Detailed technical logs remain under Downloads / Launch Log.
- User-facing errors are concise and actionable.
- Gallery/Collection service outages must not prevent Minecraft launch.
- Missing decorative images fall back to dark gradient surfaces.

## Regression / Functional Requirements

The redesign must preserve:

- Microsoft/Minecraft account login
- silent login
- instance migration from legacy symbolic versions such as `latest-release`
- launch preflight validation
- Vanilla/Fabric/Quilt/Forge/NeoForge support
- version and loader catalogs
- per-instance game directories
- Modrinth search and compatible install
- dependency resolution
- hash verification
- managed update metadata
- LUMINA Collection behavior
- local file import
- Safe Launch
- launch/download logging

## Testing and Completion Criteria

Before a release artifact is called finished:

1. Core tests pass.
2. Client/service tests pass.
3. UI regression tests cover dark ComboBox popup/item styling and notification/message behavior where feasible.
4. WPF project builds without errors.
5. Windows self-contained publish succeeds.
6. Minecraft/Mojang-Java smoke test passes.
7. New EXE artifact is produced and SHA-256 recorded.
8. No normal login/settings/install/launch failure path uses a stock Windows `MessageBox` where a LUMINA notification replacement was implemented.
9. Manual visual inspection confirms the final layout follows the approved Concept B structure, not the previous card-heavy layout.

## Out of Scope

- Replacing Microsoft authentication with a fake or password-collecting in-app login form.
- Rewriting Minecraft launch logic that already passes smoke tests without a functional reason.
- Mirroring the entire Modrinth binary catalog.
- Adding unrelated launcher features during the redesign.
