# Contributing / 贡献指南

Bug reports, translations and focused improvements are welcome. Please use sample data in screenshots and reports.

欢迎问题反馈、翻译修正和小范围改进。截图与日志请使用示例数据。

## Build and check

Prefer a Windows container with `./scripts/build.ps1`. Where Docker is unavailable, use the already-installed .NET Framework compiler:

```powershell
./scripts/build.ps1 -Native -VisualCheck
python scripts/validate.py
python scripts/package.py
```

Warnings are errors. Behavior checks cover schedules, overnight shifts, settings compatibility, goals and localization. Visual checks render the real WPF UI in all six languages. Do not install system dependencies as part of a code change.

## Translation changes

`locales/catalog.tsv` is UTF-8 without BOM. The first column is a stable key; the next six columns are Simplified Chinese, Traditional Chinese, English, Japanese, Spanish and Korean. Keep all translations nonempty and preserve placeholders such as `{0:F1}`, `{amount}`, `{symbol}` and `{code}`. Use real tab separators, not spaces. Run the validator and review the target-language screenshots.

Never translate or overwrite user-written goal names, custom amount templates or saved dates.

## Pull requests

Explain the concrete behavior change and its validation. Preserve existing settings, keep the patch small, update relevant docs and add a changelog entry for user-facing changes. Use UTF-8 without BOM and bilingual Chinese/English comments where new comments help.

Contributions are submitted under this repository's [MIT license](LICENSE).
