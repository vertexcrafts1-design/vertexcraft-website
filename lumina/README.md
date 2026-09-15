# LUMINA Minecraft Launcher

Dark Windows launcher with instance management.

## Features
- Vanilla, Fabric and Quilt instances
- Modrinth `.mrpack` import
- Microsoft/Minecraft login via `minecraft-launcher-lib`
- Minecraft installation and Mojang Java runtime handling
- Per-instance folders
- RAM and resolution settings
- Refresh token stored using Windows credential storage via `keyring`

## Microsoft login
Third-party launchers need their own Microsoft/Azure application Client ID and Minecraft API permission. Configure the Client ID in LUMINA Settings. Default redirect URI: `http://localhost:53682`.

## Data
`%APPDATA%\LUMINA`

## Windows builds
The workflow creates a portable `LUMINA.exe` folder and a single-file `LUMINA-Single.exe`. The portable build is recommended because unsigned one-file executables are more likely to trigger Windows reputation/security warnings.
