<p align="center"><img src="assets/app.png" width="96" alt="Slaves Encourager"></p>

# 牛馬鼓勵器

<p align="center">
  <a href="https://github.com/thisemailrectrash-cpu/slaves-encourager/actions/workflows/windows.yml"><img src="https://github.com/thisemailrectrash-cpu/slaves-encourager/actions/workflows/windows.yml/badge.svg?branch=main" alt="CI"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/code-MIT-blue" alt="Code: MIT"></a>
  <a href="https://github.com/thisemailrectrash-cpu/slaves-encourager/releases/latest"><img src="https://img.shields.io/github/v/release/thisemailrectrash-cpu/slaves-encourager?label=release" alt="Latest release"></a>
</p>

<p align="center"><a href="README.md">简体中文</a> · <strong>繁體中文</strong> · <a href="README.en.md">English</a> · <a href="README.ja.md">日本語</a> · <a href="README.es.md">Español</a> · <a href="README.ko.md">한국어</a></p>

<p align="center"><a href="https://github.com/thisemailrectrash-cpu/slaves-encourager/releases/latest"><strong>下載 Windows 便攜版</strong></a></p>

主頁固定文字可直接修改：按兩下文字，或以右鍵點按鈕選擇「編輯主頁文字」（`Ctrl+E`）。支援儲存、取消、還原單項與目前語言，各語言分別保存。詳見[文字編輯說明](docs/HOME_TEXT.md)。

把工作時間變成看得見的小獎勵。支援翻頁數字、收入估算、下班倒數、排班日曆與多個目標的 Windows 桌面工具。


<img src="docs/screenshots/zh-Hant-main.png" width="380" alt="繁體中文介面">

## 使用

適用於 Windows 10/11，系統需有 .NET Framework 4.8。解壓縮後執行 `SlavesEncourager.exe`，不必安裝或登入。

首次顯示明確標記的範例，按「第一次使用設定」填寫月薪與排班。在「休息與補班」選擇操作後直接點選日期，再點一次可取消。可新增最多 20 個目標、調整順序及切換查看。設定右上角可切換六種語言，儲存後保留選擇；取消會恢復原語言。

主視窗的非按鈕區域都可拖曳。`Ctrl+M` 切換迷你模式，`Ctrl+Shift+P` 切換隱私模式；快捷鍵在視窗取得焦點時生效。「—」隱藏至系統匣，按兩下圖示恢復；「×」結束。

## 資料與計算

預設按當月排班日分攤月薪，或選固定 21.75 天。休息時段不累計，過去工作日預設全勤。未計算稅款、獎金、加班費及實際考勤；節假日須依公司排班自行點選。跨午夜班次歸開始日期，貨幣選擇只變更符號。

多個目標共用本月估算收入，按清單順序分配，不重複計入，並於每月重新計算；不是實際存款餘額。

資料僅儲存在本機 `%LOCALAPPDATA%\NiumaFlip\settings.json`，舊版目錄名保留以維持相容性。儲存時保留 `.bak`，設定未加密。

## 開發與授權

首選 Windows 容器 `./scripts/build.ps1`；容器無法使用時，可用既有編譯器執行 `./scripts/build.ps1 -Native -VisualCheck`，再執行 `python scripts/validate.py` 及 `python scripts/package.py`。

程式採 [MIT 授權](LICENSE)。[開發說明](docs/DEVELOPMENT.md) · [貢獻指南](CONTRIBUTING.md) · [版本記錄](CHANGELOG.md)
