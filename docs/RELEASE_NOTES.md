# Slaves Encourager 1.5.0-beta.4

**公开预发布版 / Public prerelease** · Windows · MIT

[简体中文](https://github.com/thisemailrectrash-cpu/slaves-encourager/blob/v1.5.0-beta.4/README.md) · [繁體中文](https://github.com/thisemailrectrash-cpu/slaves-encourager/blob/v1.5.0-beta.4/README.zh-Hant.md) · [English](https://github.com/thisemailrectrash-cpu/slaves-encourager/blob/v1.5.0-beta.4/README.en.md) · [日本語](https://github.com/thisemailrectrash-cpu/slaves-encourager/blob/v1.5.0-beta.4/README.ja.md) · [Español](https://github.com/thisemailrectrash-cpu/slaves-encourager/blob/v1.5.0-beta.4/README.es.md) · [한국어](https://github.com/thisemailrectrash-cpu/slaves-encourager/blob/v1.5.0-beta.4/README.ko.md)

## 中文

- 新增独立“股票”设置页；第一次使用先询问是否开启，开启直达股票页，暂不开启进入工资页，取消不保存。
- 按代码批量添加美股自选，或输入股数建立固定持仓；支持编辑、移除及四种导入：CSV/Excel、复制表格、本机截图 OCR、浏览器扩展。导入均先预览确认，设置内修改点击保存后才应用。
- 股票默认隐藏，开启后放在主页最下方并自动拉长窗口；短屏幕自动缩放，无需滚动。可选 Yahoo 实验行情，显示价格、涨跌幅及固定股数估算日盈亏，与工资、开支、奖励目标独立。
- 保留翻页赚钱数字、六语言切换、可编辑主页文字、多目标、排班日历和工资结算周期。

下载 `SlavesEncourager-1.5.0-beta.4-windows-portable.zip`，完整解压后运行 `SlavesEncourager.exe`。请保留同目录的 `StockOcr.exe` 和两个配置文件。需要 Windows 10/11 和 .NET Framework 4.8；扩展须手动加载包内 `browser-extension` 文件夹，尚未上架商店。

这是实验功能，不连接券商、不下单。Yahoo 行情默认关闭，需另行同意联网；估算日盈亏不是券商实际日盈亏。配置与稳定版隔离，不自动迁移。真实券商页面适配与扩展实际交互仍待验证。稳定版 [1.4.0](https://github.com/thisemailrectrash-cpu/slaves-encourager/releases/tag/v1.4.0) 继续保留。

## English

- Dedicated Stocks tab and first-use prompt: Enable opens Stocks, Not now opens salary, and Cancel discards the setup choice.
- Batch-add US symbols as a watchlist or enter owned shares. Edit/remove and import through CSV/Excel, pasted tables, local Windows OCR, or the user-triggered browser extension. Preview imports and save settings to apply stock drafts.
- Stocks are hidden by default, appear at the bottom after opt-in, and extend the window with no-scroll short-screen fitting. Optional experimental Yahoo quotes show prices, changes and estimated fixed-share daily movement independently of salary and goals.
- Includes flip earnings, six UI languages, editable home text, multiple goals, schedules and payroll cycles.

Extract the entire Windows portable ZIP and run `SlavesEncourager.exe`. Keep `StockOcr.exe` and both config files together. Requires Windows 10/11 and .NET Framework 4.8. Load `browser-extension` manually if needed; it is not store-listed. No brokerage login or orders. Quotes require separate consent; estimates are not actual brokerage PnL. Real broker layouts and extension click-through remain unverified. Stable 1.4.0 stays available.

## 其他语言 / Other languages

- 繁體中文：獨立股票頁、首次開啟詢問、四種匯入與自選監控；實驗行情需另行同意，設定與穩定版隔離。
- 日本語：専用の株式タブ、初回確認、4つの取り込み方法と監視リスト。実験行情は任意、設定は安定版と分離します。
- Español: pestaña Valores, confirmación inicial, cuatro métodos de importación y lista de seguimiento. Precios experimentales opcionales y ajustes separados de la estable.
- 한국어: 전용 주식 탭, 최초 확인, 네 가지 가져오기와 관심 종목. 실험 시세는 별도 동의가 필요하며 안정 버전 설정과 분리됩니다.

## Verification

151 regression + 79 stock + 11 extension checks passed; all six languages passed modal routing and UI rendering checks. Clean-source allowlist, UTF-8, ZIP CRC and byte equality verified. All screenshots/examples are synthetic; no real portfolios or market-response data are included. Use `SHA256SUMS.txt` to verify downloads.
