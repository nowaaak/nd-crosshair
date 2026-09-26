# ND Crosshair

**English** · [Deutsch](README.de.md)

A free, open source crosshair overlay for Windows. It draws a fully customizable crosshair on top of any game running in windowed or windowed fullscreen mode.

## Features

### Crosshair
- **Layers**: a preset consists of up to 12 stacked layers. Each layer can be moved, resized, blurred and hidden. The top of the list is in front.
- **Crosshair**: 2 to 8 arms, length, thickness, gap, rotation, center dot (square or round), ring. With 4 arms each line can be toggled, so T-shapes work too.
- **Shapes**: rectangle and triangle (filled or as a frame), chevron, arc with adjustable sweep and T shape, at any angle
- **Text**: names, tags or symbols with font, size, bold and rotation. Emojis are drawn in the layer color.
- **Image**: PNG, JPG, BMP or animated GIF (up to 8 MB and 2048 pixels). The image is copied into the app data, animated GIFs play in the overlay.
- Effects per layer: outline, soft shadow, opacity
- Color modes per layer:
  - **Fixed**: always uses the selected color
  - **Rainbow**: smooth color cycling with adjustable speed
- **Spread while shooting**: while the left mouse button is held, lines and ring move outward and glide back afterwards. Strength and recovery time are set per preset, clicks in the settings window do not count.
- Anti-aliasing through 4×4 supersampling. Straight edges stay pixel sharp.
- Live preview on four backgrounds (dark, light, sky, forest)
- A hint when dot size and line thickness have different parity, which puts the dot half a pixel off center

### Presets
- Unlimited presets with thumbnails, each with an optional hotkey
- Share codes to share and import presets: simple crosshairs keep a short `NDX3-` code, designs with layers, more arms or spread get an `NDX4-` code. Image layers are not included in codes. Older `NDX1-` and `NDX2-` codes are still supported.
- **Game code import**: CS2 codes (`CSGO-...`, the pixel format introduced with the September 2026 update) and Valorant codes (`0;P;...`). Anything that cannot be mapped exactly (for example outer lines or dynamic spread) is listed after the import. CS2 codes are scaled to the height of the selected monitor. Old CS2 codes in the unit format are rejected because CS2 itself no longer accepts them.

### Overlay
- Monitor selection and pixel offset
- **Games**: a list of process names (`cs2.exe`, exact match) or parts of window titles. Each game can be linked to a preset that becomes active when you switch to the game. "Capture window" picks up the process of the foreground window after three seconds. Older configurations with window titles are migrated automatically.
- **Show only over the game**: hides the overlay while no game from the list is in the foreground
- **Hide while aiming**: right or middle mouse button, mouse 4 or mouse 5, either hold or toggle
- **Streamer mode**: the crosshair does not appear in OBS, Discord or screenshots (`SetWindowDisplayAffinity`, Windows 10 2004 or later)

### Hotkeys
- Toggle overlay (default: F8), next preset, previous preset, one hotkey per preset
- Optional: move the crosshair with Alt + Shift + arrow keys, Alt + Shift + Home resets the position
- Key combinations assigned twice are detected and shown
- Optional notification when switching presets, shown on the overlay's monitor

### System
- Start with Windows, start in background, minimize to the notification area, keep running when closed
- Language: system default, German or English, switchable at runtime
- Export and restore backups, open the configuration folder, reset to defaults. Backups contain the settings but not the image files in the `images` folder.

### Interface
- Custom dark design with its own title bar, sidebar and categories
- Custom dialogs, color picker, notifications and tray menu
- Exception: export and import use the standard Windows file dialogs

## Download

Ready-to-use builds are available under [Releases](https://github.com/nowaaak/nd-crosshair/releases):

- **Recommended:** `NdCrosshair-win-Setup.exe` installs the app for your user, without admin rights, with Start menu and desktop shortcuts. Updates can be installed right in the app under **System**. Uninstall it through the Windows settings.
- `NdCrosshair-<version>-win-x64-portable.zip`: runs without installation and includes .NET (about 70 MB), no in-app updates
- `NdCrosshair-<version>-win-x64-requires-dotnet10.zip`: much smaller, requires the [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0), no in-app updates

The files are not code-signed, so Windows SmartScreen may show a warning on first launch ("More info", then "Run anyway").

The installed version checks GitHub for a new version at startup and every 12 hours. You can turn this off under **System**. An update is only installed after you confirm it.

## Requirements

- Windows 10 or 11
- .NET 10 SDK to build from source

## Building from source

```powershell
dotnet test
dotnet publish src/NdCrosshair.App -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o publish
.\publish\NdCrosshair.exe
```

The settings open on first launch. After that the app starts in the background by default. Double-click the tray icon or start the executable again to open the settings. Exit through the tray menu.

"Start with Windows" registers the path of the currently running executable under `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`. The installed version automatically points an existing entry to itself at startup.

The configuration is stored in `%APPDATA%\NdCrosshair\config.json`. A corrupted file is backed up as `config.json.corrupt-<timestamp>` and replaced with defaults. If the file is locked or unreadable, the app starts with defaults and does not save anything in that session, so the file stays untouched.

Debug builds are kept separate: they are called "ND Crosshair Dev", store their settings in `%APPDATA%\NdCrosshair.Dev\config.json`, use their own autostart entry and run side by side with the installed app.

## Releasing

Pushing a tag like `v1.0.0` runs the release workflow. It tests, builds both ZIP variants with SHA-256 checksums, builds the installer and update packages with [Velopack](https://github.com/velopack/velopack) (`vpk`, pinned in `.config/dotnet-tools.json`) and publishes everything as one GitHub release. Installed apps pick up the new version from there. The workflow can also be started manually to produce test builds as workflow artifacts without creating a release.

## Games and anti-cheat

- In Fortnite, set the window mode to **Windowed Fullscreen**. In exclusive fullscreen, Windows cannot draw other windows on top of the game.
- The overlay never touches the game process. It does not open a handle to the game, read memory, inject code, install keyboard or mouse hooks or require admin rights. The only network connection is the optional update check on GitHub in the installed version.
- To detect games, the app reads the foreground window's title and the process name from the Windows process list (`CreateToolhelp32Snapshot`), just like Task Manager. The list is only read when a different window comes to the foreground.
- "Hide while aiming" polls the state of the selected mouse button about a hundred times per second (`GetAsyncKeyState`). This is not a hook and does not change any input. Polling only runs while the feature is enabled and the overlay is visible.
- The app never reads pixels from the screen. The old **Contrast** color mode, which sampled the screen around the crosshair, has been removed. Presets and share codes that still use it fall back to **Fixed**.
- Game publishers can change their rules at any time. No overlay tool can guarantee that you will never be banned. Use it at your own risk and check the rules of competitive events.
- Hotkeys are registered system-wide. The game no longer receives keys that are assigned here.

## Project structure

- `src/NdCrosshair.Core`: settings, layer model (crosshair, shape, text, image), renderer with supersampling and layer blending, shape geometry, fire spread, color math, share codes (`NDX1` to `NDX4`), CS2 and Valorant code import, game rules, hotkey conflicts, configuration with export, import and migration, translations. No Windows dependencies, covered by tests. The app supplies text and images through the `ILayerContentProvider` interface.
- `src/NdCrosshair.App`: WPF interface (`Views`, `Controls`, `Theme.xaml`), the overlay as a native Win32 layered window, `OverlayController` for animation, spread and visibility, `WpfLayerContent` for text and images (including GIF animation), and services for the image store, foreground detection, mouse button state, updates, autostart and notifications.
- `tests/NdCrosshair.Core.Tests`: xUnit tests, including a check that every translation key used in the app exists in both languages.

## Roadmap ideas

- Multi-stage animations with their own curve per mouse button
- Previewing spread and GIF animation right in the editor
- Cleaning up image files that are no longer used

## Feedback

Found a bug or have an idea? Open an [issue](https://github.com/nowaaak/nd-crosshair/issues/new/choose). Pull requests are not accepted, see [CONTRIBUTING](CONTRIBUTING.md).

## License

[MIT](LICENSE)
