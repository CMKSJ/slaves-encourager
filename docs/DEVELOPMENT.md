# Development / 开发说明

## Architecture

- `Engine.cs`: decimal money calculations, work intervals, calendar overrides, pay-period boundaries, confirmed opening balances, expense-first allocation, sequential goal allocations and compatible JSON settings.
- `Localization.cs`: embedded UTF-8 TSV catalog, locale resolution, formatted strings and WPF dynamic resources.
- `Program.cs`: application lifecycle, tray, live language preview, mode state and deterministic sample rendering.
- `EditorControls.cs`: clickable monthly calendar and ordered goal editor.
- `ExpenseWindow.cs`: opt-in read-only living-expense dashboard with shrinking red remaining bars.
- `IncomeConfirmation.cs`: modal, editable estimate confirmation before the settings draft is committed.
- `FlipDisplay.cs`: changed-digit animations and system animation preference.
- `Main.xaml` / `Settings.xaml`: resource-driven interface; user-entered data remains separate from translations.

Salary remains offline: no network requests, telemetry or account connections exist.

## Build environment

Target: .NET Framework 4.8, AnyCPU, WPF. Runtime compatibility is intended for Windows 10/11; current verification is on the development Windows machine. Separate Windows 10, ARM and multi-monitor runs have not been performed.

`Dockerfile.windows` uses the official .NET Framework SDK Windows image. It needs a compatible Windows container engine. The recorded development host cannot access Docker, so its existing compiler is used through `scripts/build.ps1 -Native`. The script installs no system packages.

`scripts/create-icon.ps1` generates seven PNG-compressed ICO frames from the checked-in PNG with Windows System.Drawing. `assets/app.ico` is also checked in so the project opens with an icon before building.

Python 3 is used for source validation and packaging, using the standard library only. The Windows app itself does not require Python. The optional [README animation](DEMO.md) uses real WPF controls through `scripts/render-demo.ps1` and an existing FFmpeg executable; FFmpeg is not a build dependency.

## Language behavior

Settings accept `auto`, `zh-Hans`, `zh-Hant`, `en`, `ja`, `es` and `ko`. Unsupported system languages fall back to English. Chinese regional variants resolve to the corresponding script. Selecting a language previews it in the main window and settings; cancelling restores the saved language. Saving persists it.

Date labels and formatted explanatory numbers follow the selected UI locale. The custom money formatter deliberately preserves its explicit grouping/precision/template choices and does not perform currency conversion. User goal names and templates are not translated.

## Data compatibility

This beta uses `%LOCALAPPDATA%\SlavesEncouragerStockBeta`, isolated from stable settings; schema compatibility does not trigger automatic migration. The public product and executable are named Slaves Encourager. Missing language fields are treated as system-default. Legacy single goals remain readable; new goals use an ordered list. The old `forest` theme identifier is retained for compatibility, but now selects lavender rather than green.

Settings writes use a temporary file and atomic replacement with `.bak`. Corrupt files are preserved while sample data is shown. Calendar and goal changes remain drafts until saved.

## Visual checks

```powershell
./scripts/build.ps1 -Native -VisualCheck
```

This renders all themes, main/mini/privacy modes and all settings tabs for each language into `.build/previews/<language>`. Screenshots use fixed sample values and are safe for public docs. Check long Spanish labels, Korean/Japanese glyphs, language switching, calendar actions and button behavior on a real window before release.


## Editable home copy

See [Home text](HOME_TEXT.md). `HomeCopy` resolves per-language overrides in `Settings.HomeText`. It validates the editable-key allowlist, 300-character limit and exact numeric placeholders. Overrides apply only to main-window resources and runtime captions; the translation catalog, settings dialogs and income calculations remain independent. `HomeTextEditor` edits a serialized copy; the controller applies it only after atomic persistence succeeds.

## Pay cycles and living expenses

See [rules and examples](EXPENSES.md). Missing `SettlementDay` means 1; missing `ExpensesEnabled` means false. `OpeningIncome` is nullable so zero is distinguishable from an absent baseline. Its paired `OpeningAt` uses local wall-clock ISO text with seven fractional-second digits, matching the engine's local schedule model. Only the current period uses an anchor; later periods ignore it. No timer state, transactions or payment confirmations are persisted.

`Snapshot.Month` and `MonthDays` retain their historical field names for callers, but now refer to the pay cycle. `Today` remains the current shift's earnings; correcting period income does not change it. `PeriodIncome` uses scheduled shift-start dates. At settlement midnight, a prior-cycle overnight shift is excluded from the new period even if the daily card still shows that shift. Changing schedule/salary recalculates future increments; reconfirm the baseline when a historical correction is needed.

Settings drafts own their expense rows. Confirming income modifies only the draft, and the main controller applies it after atomic save succeeds. Turning off expenses discards hidden unsaved row edits and retains the prior saved list. Stored names are never translated; starter names use the active locale before first save.

`-VisualCheck` also exercises actual nested modal save/edit/cancel flows on sample settings in each language, asserts expense opt-in visibility, and renders partial/complete/empty/private expense states and income confirmation.
