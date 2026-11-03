# Working agreements / 工作约定

- Use UTF-8 without BOM. Keep source comments bilingual (Chinese/English).
- Application source: `src/SlavesEncourager`; localization: `locales/catalog.tsv`; icon: `assets/app.png` and generated multi-size `app.ico`.
- Keep all six translations complete; run `python scripts/validate.py` after editing strings. Never translate user-entered goal names or custom templates.
- Container-first build: `docker build -f Dockerfile.windows -t slaves-encourager-build .` (Windows container engine required).
- On the development host Docker is inaccessible; the authorized fallback uses its already-installed .NET Framework compiler: `./scripts/build.ps1 -Native -VisualCheck`. Do not install host packages.
- Build treats warnings as errors, runs behavior/localization checks, and optionally renders all six languages. Keep financial estimates documented when rules change.
- Publish only the clean source package. Exclude `.build`, `dist`, real settings, backups, machine paths and personal data. `scripts/package.py` uses an explicit allowlist.
- Maintain old settings compatibility. The historical `NiumaFlip` data directory name is retained internally only, to preserve existing users' settings.
- Update README, CHANGELOG, release notes and verification for user-visible changes. Do not create or push a remote repository without a separate instruction.

- Home copy overrides are per language and main-window only. Preserve numeric placeholders, atomic save, cancel isolation and default restoration; update docs/HOME_TEXT.md and behavior checks if these rules change.
