# Verification / 验证记录

Verified locally on **2026-11-04** for version **1.3.1**.

| Check | Result |
| --- | --- |
| Native application compilation | PASS, warnings treated as errors, zero warnings |
| Behavior and localization checks | 105 passed, 0 failed |
| Language catalog | 194 complete keys across 6 languages; placeholders preserved |
| WPF/XAML/XML | PASS |
| All six language renders | PASS: main, themes, mini/privacy and settings tabs |
| Real Windows display | New icon and blue palette; double-click opens the text editor; cancel restores the home screen |
| Interactive language selection/cancel/restart | Full click-through not completed: user activity interrupted automation |
| Custom home copy | PASS: save/load, language isolation, reset, cancel-copy isolation, placeholder validation and unchanged earnings |
| Copy editor renders | PASS across all six languages, including a customized home render |
| Language setting save/load | PASS in behavior tests for all six languages |
| Old settings without language | PASS; user-written names/templates preserved |
| Source icon | PNG, RGBA, 1254 × 1254, alpha range 0–255 |
| Windows icon | 16, 24, 32, 48, 64, 128 and 256 pixels |
| README links and screenshots | PASS |
| Source archive scope | PASS: no settings, private machine paths, binaries, backups or old packages |
| ZIP contents and CRC | PASS; packaged bytes matched source files |
| GitHub repository / remote CI / Release | Check the repository and Actions links in the publishing guide for remote status |

The visible preview uses an isolated developer configuration rather than the normal user settings file. It may remain open for the user to inspect. Developer logs and such configurations are excluded from both source and portable packages.

Windows container execution was unavailable on the development host. The documented existing-compiler fallback was used; no host packages were installed. Separate Windows 10, ARM, multi-monitor and long-duration tray testing have not been performed.

Reproduce local checks:

```powershell
./scripts/build.ps1 -Native -VisualCheck
python scripts/validate.py
python scripts/package.py
```

Build and test logs are generated in `.build/`; portable/source archives and checksums are generated in `dist/`.
