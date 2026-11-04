# PUBLIC STOCK PRERELEASE / 股票公开预发布版

- On 2026-11-04 the user explicitly authorized publication of 1.5.0-beta.4, superseding the earlier local-only restriction. Publish the audited source and synthetic screenshots as a prerelease on its own beta branch/tag; preserve stable main and v1.4.0. Never upload real holdings, market responses or install an extension without a separate instruction.
- Keep beta settings isolated. No brokerage login/orders, no automatic clipboard/page access, no persistent host permissions. All imported rows require preview and user confirmation; quote access is opt-in.
- Follow docs/STOCK_BETA.md for fixed-share PnL, USD/US limits, source/freshness rules and pending review. Update it whenever these rules change. Actual public quote responses must stay outside source/public packages.

# Working agreements / 工作约定

- Use UTF-8 without BOM. Keep source comments bilingual (Chinese/English).
- Application source: `src/SlavesEncourager`; localization: `locales/catalog.tsv`; icon: `assets/app.png` and generated multi-size `app.ico`.
- Keep all six translations complete; run `python scripts/validate.py` after editing strings. Never translate user-entered goal names or custom templates.
- Container-first build: `docker build -f Dockerfile.windows -t slaves-encourager-build .` (Windows container engine required).
- On the development host Docker is inaccessible; the authorized fallback uses its already-installed .NET Framework compiler: `./scripts/build.ps1 -Native -VisualCheck`. Do not install host packages.
- Build treats warnings as errors, runs behavior/localization checks, and optionally renders all six languages. Keep financial estimates documented when rules change.
- Publish only the clean source package. Exclude `.build`, `dist`, real settings, backups, machine paths and personal data. `scripts/package.py` uses an explicit allowlist.
- Preserve the settings schema, but beta runtime uses SlavesEncouragerStockBeta. Never automatically read or modify the stable NiumaFlip directory.
- Update README, CHANGELOG, release notes and verification for user-visible changes. Do not create or push a remote repository without a separate instruction.

- Home copy overrides are per language and main-window only. Preserve numeric placeholders, atomic save, cancel isolation and default restoration; update docs/HOME_TEXT.md and behavior checks if these rules change.
- README animation uses real WPF sample controls and the settings language event. Rebuild with scripts/render-demo.ps1 and an existing FFmpeg; see docs/DEMO.md. Never capture real salary/settings or replace published versioned assets for a documentation-only change.

- Pay-cycle, opening-income and expense-first rules are specified in docs/EXPENSES.md. Preserve default-off visibility, cancel/save isolation, month-end clamping, no duplicate allocation and zero-baseline compatibility; update all six READMEs and meaningful boundary checks when these rules change.

- Symbol-only watchlists use WatchOnly=true with zero shares/no cost. Never invent one share or zero profit for a watched stock; holdings and watches have independent coverage. Preserve retained watches on imports and old JSON default holding semantics.

- Stocks default hidden via Settings.StocksEnabled. Keep the home card last, no home ScrollViewer, natural height growth and short-screen fitting. Runtime FeatureEnabled gates/cancels network independently of saved quote consent. Update docs and visibility/layout/consent checks together.

- First-use stock prompt routes explicit Yes to Stocks tab, No to salary, Cancel to no settings. Do not repeat for configured users. Keep portfolio edits in the stock-tab draft until settings save, and preserve cancellation isolation.
