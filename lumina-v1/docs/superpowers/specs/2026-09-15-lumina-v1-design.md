# LUMINA V1 Client Design

## Goal
LUMINA V1 is a native Windows Minecraft Java client/launcher that feels like a polished consumer app rather than a Python prototype. It keeps the dark violet LUMINA identity while providing real instance isolation, Microsoft login, content management and differentiated client-side launcher features.

## Product shape
The app has seven primary surfaces: Home, Instances, Library, LUMINA Lab, Downloads, Settings and Account. The Home page is a large visual launch surface for the selected instance. Instances own their own game directory, mods, resource packs, shaders, config and launch settings. Library manages the selected instance's content. LUMINA Lab contains launcher-exclusive features such as Safe Launch, Smart Memory, performance presets, Focus Mode and Quick Connect.

## V1 features
- Native .NET 8 WPF Windows app with a single EXE publish target.
- Normal interactive Microsoft/Xbox/Minecraft sign-in and silent sign-in on later starts.
- Isolated Minecraft instances under `%APPDATA%/LUMINA/instances/<id>/game`.
- Vanilla, Fabric and Quilt instance types.
- Mods, resource packs and shader packs can be imported, enabled/disabled, deleted and opened in Explorer.
- Drag-and-drop import on the Library page.
- Per-instance performance preset: Competitive, Balanced, Quality or Low Spec.
- Global RAM, resolution, Java override and launcher behavior settings.
- Safe Launch snapshots important config before starting Minecraft.
- Smart Memory chooses a sane RAM amount from physical memory.
- Focus Mode minimizes the launcher while Minecraft is running.
- Quick Connect can launch directly into a saved multiplayer server.
- Session stats and launch history.
- Download/install progress shown in the app.

## Architecture
`Lumina.Core` contains persistence, instance/content management, presets, safe backups and stats. It does not depend on WPF or Minecraft libraries and is unit tested. `Lumina.Client` contains WPF UI plus Microsoft authentication and Minecraft install/launch integration through CmlLib.Core. Each instance is an independent game directory so a broken modpack does not contaminate another profile.

## Data and safety
Settings and metadata are JSON under `%APPDATA%/LUMINA`. Microsoft tokens are handled by the authentication library rather than written into LUMINA JSON. Deleting an instance requires explicit confirmation. Safe Launch backups never overwrite an older snapshot. Imported content is copied, never moved from the user's source file.

## Visual direction
Near-black background, elevated graphite cards, violet-to-indigo accents, large rounded hero panels, compact sidebar navigation, strong typography, subtle borders and no cyber/neon overload. The interface should look closer to a modern game client than a utilities form.
