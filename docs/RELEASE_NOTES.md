# Slaves Encourager 1.6.0

**移除股票功能 / Stocks removed** · Windows · MIT

[简体中文](README.md) · [繁體中文](README.zh-Hant.md) · [English](README.en.md) · [日本語](README.ja.md) · [Español](README.es.md) · [한국어](README.ko.md)

## 中文

- 完整移除 1.5.0 beta 的股票监控与持仓功能（自选、持仓导入、截图 OCR、行情刷新和浏览器扩展），应用恢复完全离线。
- 同步移除 macOS 版本；git 历史保留全部源码。
- 新增网页版与安卓版（beta）：同口径计算、六语言、设置契约互通，可安装为 PWA 离线使用；本页提供安卓 APK。
- 修改月薪保存时会确认本期已累计收入，并按新月薪把本期此前收入重新估算。
- 网页版包含四个后续修复：数字翻页/滑动动效、目标与开支列表输入不再被刷新重置、月薪输入可删空重输、自定义金额模板可选可用。

下载 `SlavesEncourager-1.6.0-windows-portable.zip`，解压后运行 `SlavesEncourager.exe`。需要 Windows 10/11 和 .NET Framework 4.8。

## English

- Removes the entire 1.5.0 beta stock module (watchlists, holding imports, screenshot OCR, quote refresh and the browser extension); the app is fully offline again.
- The macOS edition is retired with this release; git history retains it.
- Adds the web and Android editions (beta): identical calculations, six languages, interoperable settings; installable as an offline PWA. An Android APK is attached.
- Saving after a salary edit now confirms this cycle's income so far, re-estimated on the new salary.
- The web edition ships four follow-up fixes: visible flip/slide digit animations, goal/expense list inputs that survive the live refresh, a salary field that can be cleared and retyped, and a selectable, usable custom money template.

Extract the Windows portable ZIP and run `SlavesEncourager.exe`. Requires Windows 10/11 and .NET Framework 4.8.

## 其他语言 / Other languages

- 繁體中文：移除股票監控與持倉功能，應用恢復完全離線；macOS 版本同步退役。
- 日本語：株式監視・保有機能を完全に削除し、アプリは完全オフラインに戻りました。macOS 版も終了します。
- Español: se eliminan todas las funciones de acciones; la aplicación vuelve a ser totalmente local. La edición macOS se retira.
- 한국어: 주식 모니터링과 보유 종목 기능을 완전히 제거해 앱이 완전 오프라인으로 돌아왔습니다. macOS 에디션도 종료됩니다.

## Verification

151 behavior checks passed across all six languages with clean-source allowlist, UTF-8, ZIP CRC and byte equality verified. Use `SHA256SUMS.txt` to verify downloads.
