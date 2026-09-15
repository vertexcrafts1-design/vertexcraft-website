# LUMINA Community Gallery + Launch Reliability Design

**Date:** 2026-09-15
**Branch:** `lumina-v1-client`

## Goal

Turn LUMINA into a reliable Minecraft client/launcher where signed-in users create correctly versioned instances, browse a complete Modrinth-backed catalog of mods/resource packs/shaders, install only compatible content, and optionally use a curated LUMINA collection controlled by verified Minecraft accounts.

## Scope

This design covers five connected areas:

1. Fixing the current Minecraft launch failure caused by stale pseudo-version values such as `latest-release`.
2. Reworking instance creation so account login, Minecraft version, loader, and loader version are selected explicitly.
3. Making Modrinth the live catalog behind LUMINA Discover for mods, resource packs, and shaders.
4. Enforcing compatibility before install so incompatible content cannot be added to an instance through the gallery.
5. Adding a verified-account-only LUMINA Collection that references existing Modrinth projects without accepting arbitrary JAR uploads.

Paper is intentionally excluded from the client loader list because Paper is server software. Supported client choices are Vanilla, Fabric, Quilt, Forge, and NeoForge.

## Existing Failure and Root Cause

The current crash log shows an instance with Minecraft version `latest-release`. `MinecraftService` forwards the stored instance version directly to CmlLib. CmlLib expects a concrete version ID and fails with `KeyNotFoundException: Cannot find latest-release`.

The new design removes pseudo-versions from persisted instances. Existing instances are migrated before launch and before they are shown in the editor. `latest-release` resolves to the newest Mojang release from the live catalog. Other unknown stored values become invalid-state instances and cannot launch until the user picks a real version.

## Architecture

### Client services

LUMINA keeps the existing native WPF/.NET architecture but splits the new behavior into focused services:

- `MinecraftCatalogService`: Mojang version catalog and loader metadata.
- `InstanceMigrationService`: normalizes legacy instance data and resolves pseudo-versions.
- `InstanceValidationService`: validates version/loader combinations before save and launch.
- `ModrinthService`: search, project/version metadata, dependencies, download URLs, update metadata.
- `CompatibilityService`: determines whether a Modrinth project/version can be installed into a given instance.
- `LuminaCollectionService`: reads the curated LUMINA collection from a central JSON/API source.
- `VerificationService`: checks whether the signed-in Minecraft UUID is allowed to curate the LUMINA collection.

The launcher must never trust only UI state. Instance validation runs again immediately before install and immediately before launch.

## Account Model

Minecraft launch, instance creation, and LUMINA Collection curation require a valid Microsoft/Minecraft session.

The existing Microsoft authentication flow remains the login mechanism. The Minecraft UUID from the authenticated session is the stable identity used for verification.

### Verified curators

A small central allowlist contains verified Minecraft UUIDs. A verified curator can submit an existing Modrinth project to the LUMINA Collection. The client does not accept raw JAR/ZIP uploads for community distribution.

This means:

- normal users can browse/install the full Modrinth catalog;
- verified users can mark/select Modrinth projects for the curated LUMINA Collection;
- no curator can distribute a local executable/JAR through LUMINA;
- downloads still come from Modrinth-hosted files and metadata.

The allowlist is not stored as an editable local client file. It must come from a central remote source so modifying the local installation cannot grant curator rights.

## Modrinth-backed Discover

Discover is a live view over Modrinth rather than a local library with pre-bundled files.

The client supports these project types:

- Mods
- Resource packs
- Shaders

Search results display project icon, title, author, summary, downloads, supported game versions/loaders, and install/update status for the selected instance.

### Filtering

Every query is scoped to the selected instance.

For mods, results are filtered by:

- project type `mod`;
- exact Minecraft version;
- exact loader category: Fabric, Quilt, Forge, or NeoForge.

Vanilla instances do not show normal loader-based mods.

For resource packs and shaders, the exact Minecraft version is required. Loader filtering is only applied when Modrinth metadata for that content type requires it.

### Install selection

When the user presses Install, LUMINA requests compatible project versions from Modrinth and chooses the newest compatible release, falling back to beta only when no release exists. Alpha versions are not selected automatically.

A compatible file must match the selected instance's Minecraft version and, for mods, its loader. If no compatible file exists, install is blocked with an explanation.

Required dependencies are recursively resolved through Modrinth metadata and installed only when they are also compatible with the same instance.

All downloads go into the selected instance's isolated folders:

- mods -> `mods/`
- resource packs -> `resourcepacks/`
- shaders -> `shaderpacks/`

## LUMINA Collection

The LUMINA Collection is a curated overlay on top of Modrinth, not a separate binary repository.

Each collection entry stores only safe metadata needed to reference a Modrinth project:

- Modrinth project ID
- display category
- curator UUID
- added timestamp
- optional short LUMINA note

On display/install, current project/version/file metadata is fetched from Modrinth. If a referenced project is deleted, unavailable, or no longer compatible, LUMINA hides or disables its install action.

Verified curators can add a project by pasting a Modrinth URL or project ID, or from a visible Discover result. LUMINA validates that the project exists before submitting it to the central collection.

Duplicate project IDs are rejected.

## Instance Creation Flow

Creating an instance requires a valid signed-in Minecraft account.

The wizard flow is:

1. Instance name
2. Minecraft version from the live Mojang catalog
3. Loader: Vanilla, Fabric, Quilt, Forge, NeoForge
4. Loader version, populated only with versions compatible with the chosen Minecraft version
5. Summary and Create

The Create button remains disabled until the combination is valid.

Vanilla stores loader version as `Standard`. Modded loaders store an explicit loader version.

Instance names are user-facing labels only. Filesystem IDs remain sanitized, unique IDs created by `InstanceService`.

## Legacy Instance Migration

Migration runs once during startup and is idempotent.

Rules:

- `latest-release` -> newest release returned by Mojang/CmlLib catalog
- `latest-snapshot` -> newest snapshot only if snapshot support is enabled for that instance; otherwise mark invalid
- concrete version that exists in catalog -> keep unchanged
- concrete version missing from catalog -> mark invalid and require reselection
- loader name casing is normalized to Vanilla/Fabric/Quilt/Forge/NeoForge
- Vanilla -> loader version `Standard`
- modded loader with missing/invalid loader version -> mark invalid and prompt for a compatible loader version

Migration never silently changes a concrete Minecraft version to a different historical version.

## Launch Reliability

Before launch, LUMINA performs a preflight:

1. account session is valid;
2. instance version exists in current catalog;
3. loader is supported;
4. loader version is valid for that Minecraft version;
5. required game/loader files can be installed/resolved;
6. Java is resolved by CmlLib/Mojang runtime unless the user explicitly configured a valid custom Java path.

If preflight fails, Minecraft is not started and the Downloads/Launch Log page shows a concise user-facing reason plus the technical exception.

Legacy aliases are never passed directly to CmlLib.

## Updates

For every installed Modrinth-backed content item, LUMINA stores a lightweight sidecar record containing project ID, version ID, file name, SHA-512/SHA-1 when available, and install time.

This allows the client to show:

- Installed
- Update available
- Incompatible after instance change
- Local/manual file (not managed by Modrinth)

Updating uses the same compatibility rules as first install. LUMINA never auto-upgrades a mod to a version incompatible with the current instance.

## Manual Content

Users may still manually add their own local mods/resource packs/shaders to their own instance folders.

Manual files are clearly labeled `Local` and are never uploaded, published, or made visible to other LUMINA users.

Only Modrinth-backed projects can appear in the global Discover catalog or LUMINA Collection.

## UI

### Discover

Discover has three top-level views:

- All / Modrinth
- LUMINA Collection
- Installed / Updates

The selected instance context is always visible at the top, including Minecraft version and loader.

Cards show compatibility state before the user clicks them. The primary action is one of Install, Update, Installed, or Incompatible.

### Instances

The New Instance action opens the account-gated wizard described above. Existing invalid legacy instances show a warning badge and an Edit/Fix action instead of a working Play button.

### Downloads / Launch Log

Downloads shows current Modrinth downloads and loader/game installation progress. Launch Log remains available for diagnostics and includes preflight failures and Minecraft process output.

## Security Constraints

- No arbitrary community JAR/EXE upload endpoint in this version.
- Curator status is determined remotely from authenticated Minecraft UUID, never by a local boolean/config edit.
- LUMINA Collection entries reference Modrinth project IDs only.
- Project files are downloaded from URLs returned by Modrinth metadata.
- Compatibility is revalidated at install time, not only search time.
- Duplicate filenames are handled explicitly instead of silently overwriting unrelated local files.
- Failed/partial downloads use a temporary file and are moved into place only after successful completion.
- Hashes from Modrinth metadata are verified when provided before the file is activated.

## Central Backend Boundary

The first version only needs a very small central service for LUMINA-specific state:

- `GET /v1/verification/{minecraftUuid}` -> verified curator status
- `GET /v1/collection` -> current curated project references
- `POST /v1/collection` -> verified curator adds a Modrinth project reference

The backend stores no mod binaries.

The client can still use full Modrinth Discover if the LUMINA backend is offline. Only verification/curation features become temporarily unavailable.

## Error Handling

Network errors must not make existing installed instances unusable.

- Mojang catalog unavailable: cached catalog may be used for already-known concrete versions, but new instance creation is disabled until versions can be verified.
- Modrinth unavailable: local library and Minecraft launch remain available; Discover shows offline state.
- LUMINA backend unavailable: Modrinth Discover works, but LUMINA Collection editing/verification is unavailable.
- Download failure: temporary file is removed; prior installed version remains untouched.
- Incompatible dependency: entire requested install fails before activating partial content.

## Testing

Required automated coverage:

- migration of `latest-release` to a concrete release;
- unknown legacy versions become invalid instead of being launched;
- valid/invalid loader-version combinations;
- Modrinth query facets for each supported loader;
- install selection rejects incompatible project versions;
- dependency resolution respects loader/game version;
- download hash validation and temporary-file behavior;
- verified/unverified curator permission behavior;
- duplicate LUMINA Collection entry rejection;
- manual files remain local-only;
- instance creation requires authentication;
- launch preflight never passes a pseudo-version to CmlLib.

Windows CI must continue to run the existing Core tests, WPF build, self-contained publish, and Minecraft/Mojang-Java smoke test.

## Definition of Done

The redesign is complete when:

1. an old `latest-release` instance can no longer trigger the current CmlLib exception;
2. a signed-in user can create a named instance with a real Minecraft version and compatible Vanilla/Fabric/Quilt/Forge/NeoForge setup;
3. Minecraft launch preflight validates the saved instance before CmlLib is called;
4. Discover exposes the live Modrinth catalog for mods, resource packs, and shaders;
5. install actions only offer versions compatible with the selected instance;
6. required compatible dependencies install automatically;
7. installed Modrinth content can show update state;
8. local manual files remain usable but never become globally published;
9. verified UUIDs can curate Modrinth projects into the LUMINA Collection while normal users cannot;
10. the full Windows CI pipeline, including the Minecraft/Java smoke test, passes.