# Verification / 验证记录

Verified locally on **2026-11-04** for version **1.4.0**. Publication targets `v1.4.0`; consult the matching Release and Actions pages for remote publication/build status. Existing v1.3.1 Release assets remain unchanged.

| Check | Result |
| --- | --- |
| Native compilation | PASS, warnings treated as errors, zero warnings |
| Behavior and localization | 151 passed, 0 failed |
| Expense allocation | PASS: default off, partial/complete coverage, surplus-only rewards, no duplicate income, goal time includes pending costs |
| Expense persistence | PASS: order, empty list, disabled behavior, draft copy isolation and limits |
| Payroll boundaries | PASS: day 15, midnight rollover, day 31 clamping, leap February, year change, actual-day reconciliation and overnight attribution |
| Opening earnings | PASS: edited and zero baseline, future increments, restart recovery, next-cycle expiry, JSON validation and old-setting defaults |
| Real modal UI flows | PASS in all six languages: first-use opt-in, day selection, edited amount, confirm/save, cancel and original-settings isolation; sample drafts only |
| Language catalog | 243 complete keys × 6 languages; placeholder parity and localized default settings valid |
| WPF/XML and six-language rendering | PASS: themes, mini/privacy, all settings tabs, home editor, expense opt-in, partial/complete/empty/private expenses and income confirmation |
| Spanish default goal | PASS: shortened to fit the 28-character limit; no saved user names changed |
| Home text | Existing save/load, language isolation, reset, cancel and placeholder checks pass; expense button caption is editable |
| README animation | Existing 1000 × 660, 360-frame, 20-fps, 18-second looping sample GIF preserved; no real user data |
| Icons | PNG 1254 × 1254; ICO 16/24/32/48/64/128/256 |
| Static checks | UTF-8 without BOM, XML, translations, README links, GIF structure and MIT license PASS |
| Packages | Source/portable ZIP CRC and byte comparison PASS; SHA256SUMS.txt generated |
| Remote CI / Release | Local checks below passed; remote status is recorded on the matching commit in [Actions](https://github.com/thisemailrectrash-cpu/slaves-encourager/actions) and [v1.4.0 Release](https://github.com/thisemailrectrash-cpu/slaves-encourager/releases/tag/v1.4.0) |

All screenshots and modal tests use built-in sample settings. User settings were not modified. Visual review includes Chinese, English, Japanese, Korean and long Spanish labels. Scrollable expense lists deliberately retain all rows, including completed ones.

Windows containers were unavailable on this development host. The documented existing .NET Framework compiler fallback was used, with no host packages installed. Independent Windows 10, ARM, multi-monitor, DPI matrix and long-duration tray testing have not been performed. Automated modal checks do not replace broad manual interaction testing.

```powershell
./scripts/build.ps1 -Native -VisualCheck
python scripts/validate.py
python scripts/package.py
git diff --check
```

Build and test logs are generated in `.build/`; portable/source archives and checksums are generated in `dist/`. Rules and examples: [EXPENSES.md](EXPENSES.md).
