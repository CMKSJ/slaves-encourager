# Development / 开发说明

## Architecture

- `Engine.cs`: decimal money calculations, work intervals, calendar overrides, sequential goal allocations and compatible JSON settings.
- `Localization.cs`: embedded UTF-8 TSV catalog, locale resolution, formatted strings and WPF dynamic resources.
- `Program.cs`: application lifecycle, tray, live language preview, mode state and deterministic sample rendering.
- `EditorControls.cs`: clickable monthly calendar and ordered goal editor.
- `FlipDisplay.cs`: changed-digit animations and system animation preference.
- `Main.xaml` / `Settings.xaml`: resource-driven interface; user-entered data remains separate from translations.

No network or telemetry calls are used by the application. Developer CI and source downloads are separate from app runtime.

## Build environment

Target: .NET Framework 4.8, AnyCPU, WPF. Runtime compatibility is intended for Windows 10/11; current verification is on the development Windows machine. Separate Windows 10, ARM and multi-monitor runs have not been performed.

`Dockerfile.windows` uses the official .NET Framework SDK Windows image. It needs a compatible Windows container engine. The recorded development host cannot access Docker, so its existing compiler is used through `scripts/build.ps1 -Native`. The script installs no system packages.

`scripts/create-icon.ps1` generates seven PNG-compressed ICO frames from the checked-in PNG with Windows System.Drawing. `assets/app.ico` is also checked in so the project opens with an icon before building.

Python 3 is used for source validation and packaging, using the standard library only. The Windows app itself does not require Python. Visual previews can optionally be combined into a GIF with an existing image tool; that is not a build dependency.

## Language behavior

Settings accept `auto`, `zh-Hans`, `zh-Hant`, `en`, `ja`, `es` and `ko`. Unsupported system languages fall back to English. Chinese regional variants resolve to the corresponding script. Selecting a language previews it in the main window and settings; cancelling restores the saved language. Saving persists it.

Date labels and formatted explanatory numbers follow the selected UI locale. The custom money formatter deliberately preserves its explicit grouping/precision/template choices and does not perform currency conversion. User goal names and templates are not translated.

## Data compatibility

The internal settings directory remains `%LOCALAPPDATA%\NiumaFlip` to preserve prior users' data. The public product and executable are named Slaves Encourager. Missing language fields are treated as system-default. Legacy single goals remain readable; new goals use an ordered list. The old `forest` theme identifier is retained for compatibility, but now selects lavender rather than green.

Settings writes use a temporary file and atomic replacement with `.bak`. Corrupt files are preserved while sample data is shown. Calendar and goal changes remain drafts until saved.

## Visual checks

```powershell
./scripts/build.ps1 -Native -VisualCheck
```

This renders all themes, main/mini/privacy modes and all settings tabs for each language into `.build/previews/<language>`. Screenshots use fixed sample values and are safe for public docs. Check long Spanish labels, Korean/Japanese glyphs, language switching, calendar actions and button behavior on a real window before release.


## Editable home copy

See [Home text](HOME_TEXT.md). `HomeCopy` resolves per-language overrides in `Settings.HomeText`. It validates the editable-key allowlist, 300-character limit and exact numeric placeholders. Overrides apply only to main-window resources and runtime captions; the translation catalog, settings dialogs and income calculations remain independent. `HomeTextEditor` edits a serialized copy; the controller applies it only after atomic persistence succeeds.
