# 版本记录 / Changelog

## 1.6.0 — Stocks and macOS removed

- Remove the entire 1.5.0 beta stock module (watchlists, holdings, imports, OCR, quote refresh and the browser extension) from code, tests, localization and docs; the app is fully offline again.
- Retire the macOS/Avalonia port; git history keeps it. The Windows edition is unchanged apart from the stock removal.
- Settings remain compatible: old files with stock fields still load (unknown members are ignored) and the settings directory name is unchanged.

## 1.5.0-beta.4 macOS edition

- Apple Silicon and Intel app bundles with self-contained .NET 10 and Avalonia 11.3.22; compile shared Windows financial, localization and stock domain sources.
- Earnings animations/formats, schedules/calendar, payroll/opening-income confirmation, multiple goals, optional expenses, privacy/mini/pin/demo and editable home text.
- Dedicated Stocks tab, first-use prompt, isolated drafts and four import paths; Apple Vision replaces Windows OCR locally.
- Native two-architecture CI validates shared behavior, six-language UI and OCR, then verifies ad-hoc signatures and produces DMG/ZIP attachments for the existing beta.4 prerelease. No Developer ID notarization.

## 1.5.0-beta.4 — Public prerelease

- Public prerelease authorized on 2026-11-04; stable 1.4.0 remains available.
- Dedicated Stocks settings tab with manual entry, editing/removal and holding import; stock edits stay drafts until settings save.
- First-use prompt: Enable opens Stocks, Not now opens salary setup, Cancel exits; configured users are not prompted repeatedly.
- Retain default-hidden bottom home card, no-scroll height fitting and independent quote consent.


## 1.5.0-beta.3 — Local only, review pending

- Hide stocks by default; opt in from first-use salary settings and keep the module last on home.
- Grow home naturally when enabled; fit shorter screens without home scrolling or clipped bottom actions.
- Pause/cancel quote requests when the feature is hidden while retaining holdings and prior consent.


## 1.5.0-beta.2 — Local only, review pending

- Direct symbol entry and batch watchlists without shares, plus edit/remove and watch-to-holding conversion.
- Price/change monitoring, watch coverage and no fabricated holding PnL for unowned stocks.
- Preserve watch entries on holding imports, backward-compatible saved positions and six-language manual editor.


## 1.5.0-beta.1 — Local only, review pending

- Four reviewed import routes: CSV/XLSX, clipboard text/HTML, local geometric OCR and user-triggered MV3 holdings export.
- US/USD fixed-snapshot estimated trading-day PnL with opt-in Yahoo experimental quotes, bounded refresh/cancellation, timestamp and coverage checks.
- Separate motivation card, privacy masking, isolated beta settings, six locales and detailed stock guide.
- No brokerage credentials/orders, source upload, beta Release or automatic browser extension installation.


## 1.4.0 — 2026-11-04

- Add optional living expenses, hidden by default; customize up to 20 recurring items, shrink red uncovered bars with income, and fund rewards from surplus only.
- Add a configurable 1–31 pay-cycle reset day, month-end clamping and shared income/expense/goal rollover.
- Confirm or edit prior earnings on first setup, mid-cycle opt-in and reset-day changes; support starting from zero, later correction and an opening balance that expires next cycle.
- Expand all six locales, documentation and sample rendering; fix the overlong Spanish default goal.
- Add an 18-second README GIF showing real flip-digit animation and all six languages, with a close-up of the earnings card.
- Add reproducible sample-only WPF capture and GIF encoding scripts; existing v1.3.1 release assets remain unchanged.

## 1.3.1 — 2026-11-04

- Centered live CI, MIT and release badges; six-language README navigation and localized download links.

- Directly edit fixed home captions and button text; per-language persistence, cancel, reset and numeric-placeholder protection.
- Rename the English product, executable, project and release packages to Slaves Encourager.
- Preserve all six languages, the blue icon and existing settings compatibility.

## 1.3.0 — 2026-11-04

- New brand: Slaves Encourager / 牛马鼓励器, without the previous suffix.
- New ox-and-clock icon; misty blue, ivory and apricot replace the dark-green default.
- Full Simplified Chinese, Traditional Chinese, English, Japanese, Spanish and Korean localization, with live preview and saved selection.
- Preserve old salary, schedule, goals and custom templates; user-authored text is not translated.
- Clean `src`, `locales`, `assets`, `scripts`, `tests` and `docs` layout; six READMEs, contribution/development/publishing guides and MIT license.
- Windows workflow prepared; remote execution remains pending publication.

## 1.2 — 2026-11-04

- 按钮改为“隐私模式”“迷你模式”，开启时高亮，再次点击退出，名称保持不变。
- 主窗口所有非按钮区域均支持左键拖动，包括数字、卡片和进度条，完整/迷你模式都可用。
- 按钮及内部文字排除拖动，设置窗口保留输入与日历操作。
- 输出至 `dist-v1.2` 和 v1.2 压缩包，旧版交付与用户设置保留。

## 1.1 — 2026-11-04

- 首次入口改为“第一次使用设置”，并优先打开工资与排班。
- 以可点击月份日历替代日期文本框，支持休息、补班、恢复排班、再次点击取消和跨月导航。
- 增加最多 20 个有序目标，支持新增、编辑、移除和调整顺序；主窗口可切换查看目标。
- 目标按清单顺序分配同一份月收入，避免重复累计；默认展示第一个未完成的目标。
- 保留旧版工资、日历数据和单目标，空目标列表不会恢复已移除目标。
- 58 项行为检查通过，新增多目标分配/兼容和日历互斥/持久化验证。
- 新版输出 `dist-v1.1` 和独立 v1.1 压缩包，保留 v1.0 交付文件。

## 1.0 — 2026-11-04

- 首次交付 Windows 原生 WPF 桌面应用，独立重实现。
- 增加上下半页翻转、滑动、静态数字及三档速度。
- 支持金额小数位、千分位、币种符号和自定义格式模板。
- 增加三种主题和三种数字字体、迷你悬窗、置顶、托盘及隐私模式。
- 增加工时估算、午休、跨午夜、工作日覆盖、月累计与购买目标。
- 修复设置模板花括号的 XAML 转义，实际加载验证覆盖所有设置页。
- 修复数字下半页裁切、底栏超界和深色设置页对比度。
- 41 项独立行为检查通过，真实 Windows 界面完成格式切换、保存重启与快捷键验证。
- 使用现有本机工具链构建，无系统包安装；Windows 容器构建环境未可用。
