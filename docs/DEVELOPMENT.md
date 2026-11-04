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

Salary remains offline. The local stock beta makes only user-enabled public Yahoo symbol GET requests; holdings, screenshots and account credentials are never uploaded. No telemetry or brokerage/order connection exists. See STOCK_BETA.md.

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

## Local stock beta

`Stocks.cs` implements bounded table/XLSX imports, local snapshot persistence, New York trading dates, prior-session close selection and quote freshness/coverage. `StockWindows.cs` owns import review and the separate motivation dashboard. `StockOcr.exe` is built from `scripts/OcrHelper.cs` with the existing Framework WinRT projection library; it uses local Windows OCR metadata and reconstructs rows from word geometry. No SDK or PowerShell execution-policy change is needed. Missing OCR languages are reported, never installed.

The user authorized publishing beta.4 on 2026-11-04. Publish audited source and synthetic samples on a separate beta branch/tag as a prerelease; preserve stable main and v1.4.0. Never publish actual holdings or market responses. Portfolio settings never use the stable directory. `StockService` requires online consent, limits refreshes to 5 min / manual 1 min, 3 concurrent requests and 12-second bounded HTTP reads, permits cancellation and refuses incomplete totals. The provider interface permits future approved sources without treating the current Yahoo endpoint as an official API.

Build runs 151 baseline plus 79 stock behavior checks. `-VisualCheck` adds per-language real modal stock confirm/cancel and invalidation checks, dashboard/private/import renders. Run `node tests/extension.test.js` for synthetic DOM/permission checks. `StockTests.exe --public-quote-probe` explicitly opts into a public AAPL-only connectivity probe; normal tests never network.

## Direct symbol watchlist (beta.2)

`StockPosition.WatchOnly` defaults false for backward-compatible holding JSON. Watch entries require zero quantity and no cost; they never enter owned PnL totals. `StockMath.Manual` edits a deep copy, normalizes one/batch symbols, preserves edit order and validates the combined 50-item limit. Manual mode controls explicit online consent and confirmation. Imports replace owned rows while retaining nonconflicting watches.

`PriceUsable` permits fresh price monitoring when a prior close is absent; `Usable` additionally requires a valid preceding close for percentage change/PnL. Watch coverage is independent of holding coverage. A pure watchlist displays counts instead of a fake zero-dollar profit. The manual editor, conversion, cancel isolation and retained-watch import are exercised by actual sample-only modal flows in every language.

## Default-hidden home module (beta.3)

`Settings.StocksEnabled` defaults false, including older JSON. The stock-tab checkbox is applied only after save. `StockService.FeatureEnabled` is an independent runtime gate; disabling it cancels in-flight requests without deleting positions or saved quote consent. Startup and periodic refreshes honor that gate.

Home content uses a down-only `Viewbox`, not a `ScrollViewer`. Stocks are the last body child and are collapsed in mini mode. `FitHomeWindow` measures the natural body, title and action rows, grows to that height and clamps to the work area; the body scales down on shorter screens. Title/actions retain their size. Six-language sample checks assert natural growth on a roomy screen and bottom-card bounds at a 600-DIP height.

## Dedicated Stocks tab and first-use route (beta.4)

`PrepareSettings` asks through `StockWelcomeWindow` only for an unconfigured first-use entry. An explicit nullable `Choice` distinguishes Not now from Cancel, because WPF closing ShowDialog otherwise reports false. Yes selects Stocks (item index5), No selects salary (index1), Cancel creates no settings editor. Existing expense tab indices are preserved.

`StockSettingsPanel` clones the current portfolio and provides manual, edit/remove and import actions. Child editors stage into that clone. `SettingsEditor.PortfolioResult` is produced only on Save with stocks enabled; cancel and disabled-stock saves do not commit drafts. Main persists settings and the optional portfolio before enabling quotes; if portfolio saving fails, it attempts to restore prior settings and reports the failure. The dedicated tab and prompt are rendered and the actual yes/no/cancel routes run in all six sample languages.

Portfolio-save readiness is checked before and after any opening-income modal confirmation. If quotes are still refreshing, settings stays open with its stock draft intact; retry after completion. The sample modal verification exercises that blocked-save/retry path in every language.


macOS port: see [MACOS.md](MACOS.md). The Mac project links shared domain files without changing the WPF application. Compatibility adapters provide legacy JSON/timer/resource interfaces; no WPF assemblies ship on Mac. Source packaging excludes nested bin/obj to avoid leaking machine paths. Native GitHub CI is the macOS verification fallback; .NET 8 locally is compatibility-only, released bundles use .NET 10.
