# Working agreements / 工作约定

- Packages must exclude nested bin/obj/__pycache__ and user configuration. Check architectures, execution permissions, signatures, six-language UI and behavior tests before publishing.

- Use UTF-8 without BOM. Keep source comments bilingual (Chinese/English).
- Application source: `src/SlavesEncourager`; localization: `locales/catalog.tsv`; icon: `assets/app.png` and generated multi-size `app.ico`.
- Keep all six translations complete; run `python scripts/validate.py` after editing strings. Never translate user-entered goal names or custom templates.
- Container-first build: `docker build -f Dockerfile.windows -t slaves-encourager-build .` (Windows container engine required).
- On the development host Docker is inaccessible; the authorized fallback uses its already-installed .NET Framework compiler: `./scripts/build.ps1 -Native -VisualCheck`. Do not install host packages.
- Build treats warnings as errors, runs behavior/localization checks, and optionally renders all six languages. Keep financial estimates documented when rules change.
- Publish only the clean source package. Exclude `.build`, `dist`, real settings, backups, machine paths and personal data. `scripts/package.py` uses an explicit allowlist.
- Preserve the settings schema. The runtime settings directory is named `SlavesEncouragerStockBeta` for historical reasons (the stock beta introduced it); keep the name so existing settings files are not orphaned. Never automatically read or modify the stable NiumaFlip directory.
- Update README, CHANGELOG and release notes for user-visible changes. Do not create or push a remote repository without a separate instruction.

- Home copy overrides are per language and main-window only. Preserve numeric placeholders, atomic save, cancel isolation and default restoration; update docs/HOME_TEXT.md and behavior checks if these rules change.
- README animation uses real WPF sample controls and the settings language event. Rebuild with scripts/render-demo.ps1 and an existing FFmpeg; see docs/DEMO.md. Never capture real salary/settings or replace published versioned assets for a documentation-only change.

- Pay-cycle, opening-income and expense-first rules are specified in docs/EXPENSES.md. Preserve default-off visibility, cancel/save isolation, month-end clamping, no duplicate allocation and zero-baseline compatibility; update all six READMEs and meaningful boundary checks when these rules change.
