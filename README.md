# ND Crosshair

**English** · [Deutsch](README.de.md)

A free, open source crosshair overlay for Windows. It draws a fully customizable crosshair on top of any game running in windowed or windowed fullscreen mode.

## Features

### Crosshair
- Shape: lines (each can be toggled, so T-shapes work too), length, thickness, gap, rotation (for example 45° for an X), center dot (square or round), ring
- Effects: outline, soft shadow
- Color modes:
  - **Fixed**: always uses the selected color
  - **Rainbow**: smooth color cycling with adjustable speed
  - **Contrast**: keeps your color while it stays visible and switches to a high-contrast color when it blends into the background (color distance in CIE Lab with hysteresis to prevent flicker)
- Anti-aliasing through 4×4 supersampling. Straight edges stay pixel sharp.
- Live preview on four backgrounds (dark, light, sky, forest)

### Presets
- Unlimited presets with thumbnails
- Share codes (`NDX3-...`) to share and import presets. Older `NDX1-` and `NDX2-` codes are still supported.

### Overlay
- Monitor selection and pixel offset
- **Show only over the game**: hides the overlay while another window is in the foreground. Only the window title is checked, the game process is never accessed. "Capture window" picks up the game's title after three seconds.
- **Streamer mode**: the crosshair does not appear in OBS, Discord or screenshots (`SetWindowDisplayAffinity`, Windows 10 2004 or later)

### Hotkeys
- Toggle overlay (default: F8), next preset, previous preset
- Optional notification when switching presets

### System
- Start with Windows, start in background, minimize to the notification area, keep running when closed
- Language: system default, German or English, switchable at runtime
- Export and restore backups, open the configuration folder, reset to defaults

### Interface
- Custom dark design with its own title bar, sidebar and categories
- Custom dialogs, color picker, notifications and tray menu
- Exception: export and import use the standard Windows file dialogs

## Download

Ready-to-use builds are available under [Releases](https://github.com/nowaaak/nd-crosshair/releases):

- `NdCrosshair-<version>-win-x64-portable.zip`: runs without installation and includes .NET (about 70 MB)
- `NdCrosshair-<version>-win-x64-requires-dotnet8.zip`: much smaller, requires the [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)

Extract the ZIP, move `NdCrosshair.exe` to a permanent location and run it. The executable is not code-signed, so Windows SmartScreen may show a warning on first launch ("More info", then "Run anyway").

## Requirements

- Windows 10 or 11
- .NET 8 SDK to build from source

## Building from source

```powershell
dotnet test
dotnet publish src/NdCrosshair.App -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
.\publish\NdCrosshair.exe
```

The settings open on first launch. After that the app starts in the background by default. Double-click the tray icon or start the executable again to open the settings. Exit through the tray menu.

"Start with Windows" registers the path of the currently running executable under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. Enable it from the executable you actually want to keep using.

The configuration is stored in `%APPDATA%\NdCrosshair\config.json`. A corrupted file is backed up as `config.json.corrupt-<timestamp>` and replaced with defaults.

## Releasing

Pushing a tag like `v1.0.0` runs the release workflow. It tests, builds both variants, creates the ZIP files with SHA-256 checksums and publishes a GitHub release. The workflow can also be started manually to produce test builds as workflow artifacts without creating a release.

## Games and anti-cheat

- In Fortnite, set the window mode to **Windowed Fullscreen**. In exclusive fullscreen, Windows cannot draw other windows on top of the game.
- The overlay never touches the game process. It does not read memory, inject code, install keyboard or mouse hooks, access the network or require admin rights.
- The only exception is the **Contrast** color mode: it reads a small frame of the screen around the crosshair about ten times per second (`BitBlt`). The game itself is not accessed, but there is no guarantee that anti-cheat systems will always tolerate this. The mode is off by default.
- Game publishers can change their rules at any time. No overlay tool can guarantee that you will never be banned. Use it at your own risk and check the rules of competitive events.
- Hotkeys are registered system-wide. The game no longer receives keys that are assigned here.

## Project structure

- `src/NdCrosshair.Core`: settings, renderer (rasterization and coloring are separate), color math, adaptive color selection, share codes, configuration with export and import, translations. No Windows dependencies, covered by tests.
- `src/NdCrosshair.App`: WPF interface (`Views`, `Controls`, `Theme.xaml`), the overlay as a native Win32 layered window, `OverlayController` for color modes and visibility, and services for screen sampling, foreground detection, autostart and notifications.
- `tests/NdCrosshair.Core.Tests`: xUnit tests, including a check that every translation key used in the app exists in both languages.

## Roadmap ideas

- Xbox Game Bar widget for exclusive fullscreen
- Hide while holding the right mouse button (ADS)
- Custom PNG images as crosshair

## Feedback

Found a bug or have an idea? Open an [issue](https://github.com/nowaaak/nd-crosshair/issues/new/choose). Pull requests are not accepted, see [CONTRIBUTING](CONTRIBUTING.md).

## License

[MIT](LICENSE)
