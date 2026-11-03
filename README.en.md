# Slaves Encourager

Edit any fixed home caption: double-click text, or right-click a button and choose **Edit home text** (`Ctrl+E`). Save, cancel, reset one entry or reset the current language. Edits persist independently per language. See [Home text](docs/HOME_TEXT.md).

A local-first Windows earnings clock: see your time turn into small rewards.

[简体中文](README.md) · [繁體中文](README.zh-Hant.md) · **English** · [日本語](README.ja.md) · [Español](README.es.md) · [한국어](README.ko.md)

<img src="docs/screenshots/en-main.png" width="380" alt="Slaves Encourager in English">

## Features

- Flip-clock, smooth-slide and static numbers; 0–4 decimals, grouping, currency symbols and custom templates.
- A new ox-and-clock icon with a soft blue, ivory and apricot palette. Three themes and three number fonts.
- Simplified Chinese, Traditional Chinese, English, Japanese, Spanish and Korean, with system detection and saved language selection.
- Clickable days off and extra shifts, weekly schedules, breaks and overnight shifts.
- Up to 20 ordered goals. Monthly estimated income is allocated once, in list order.
- Pin, mini mode, privacy mode and system tray. Drag any non-button area of the main window.
- Offline operation, no account and no telemetry. [MIT licensed](LICENSE).

## Use

Windows 10/11 with .NET Framework 4.8. Extract the portable ZIP and run `SlavesEncourager.exe`.

First launch uses clearly labelled sample data. Open **First-time setup**, enter your salary and schedule, mark calendar exceptions, add goals and save. The language selector is at the top right of settings; cancelling restores the previous language. User-written goal names and amount templates are never translated automatically.

`Ctrl+M` toggles mini mode; `Ctrl+Shift+P` toggles privacy while the window has focus. “—” hides to the tray; double-click its icon to restore. “×” quits.

## Estimates and data

Salary is divided by this month's scheduled workdays, or an optional fixed 21.75-day basis. Breaks are excluded. Past scheduled days assume full attendance. No tax, bonus, overtime or actual attendance deductions are calculated. Overnight shifts belong to their starting date. Add holidays according to your own schedule. Currency selection changes symbols only.

Goal progress uses monthly estimated income, not savings. For goals of 1,000 and 3,000, income of 1,500 allocates 1,000 and 500. Progress resets each month.

Settings stay in `%LOCALAPPDATA%\NiumaFlip\settings.json`, a legacy internal path retained for compatibility. Saves keep a `.bak`; settings are not encrypted. Privacy mode hides amounts on screen.

## Build

Prefer the Windows container workflow: `./scripts/build.ps1`. If unavailable, use the existing Windows compiler:

```powershell
./scripts/build.ps1 -Native -VisualCheck
python scripts/validate.py
python scripts/package.py
```

The app and ZIPs are written to `dist/`; checks and six-language renders go to `.build/`. No third-party application packages are required. The supplied GitHub workflow has been prepared locally; its first remote run remains unverified.

[Development](docs/DEVELOPMENT.md) · [Contributing](CONTRIBUTING.md) · [Publishing](docs/GITHUB_PUBLISH.md) · [Changelog](CHANGELOG.md) · [Icon provenance](docs/ICON.md) · [MIT](LICENSE)
