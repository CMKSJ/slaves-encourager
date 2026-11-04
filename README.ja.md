# 1.5.0-beta.4 · 米国株の監視と保有管理 · 公開プレビュー

株式機能は既定で非表示です。初回設定で有効にするか確認し、はいなら専用の「株式」タブ、今は使わないなら給与設定へ進みます。株式タブで追加・編集・削除・取り込みを行い、保存するとホーム最下部に表示します。ウィンドウは縦に広がり、小さい画面では本文を縮小してスクロールを不要にします。無効化すると行情取得を一時停止し、保存データは保持します。

追加：`AAPL, MSFT, NVDA` のように**コードだけで監視**できます。価格・騰落率・時刻を表示し、株数を入力すると保有損益モードに切り替えられます。まとめて追加・編集・削除に対応。監視銘柄は保有損益に含めません。[説明](docs/STOCK_BETA.md)。

**[1.5.0-beta.4 プレビュー版をダウンロード](https://github.com/thisemailrectrash-cpu/slaves-encourager/releases/tag/v1.5.0-beta.4)**。株式機能は実験段階です。実際の証券ページと拡張機能の操作は追加検証が必要です。拡張はストア未掲載です。[安定版1.4.0](https://github.com/thisemailrectrash-cpu/slaves-encourager/releases/tag/v1.4.0)を維持し、以下の一般ダウンロードリンクは安定版を指します。

CSV/Excel、表の貼り付け、Windowsのローカル画像OCR、ブラウザー拡張の4方式です。固定株数を確認してからYahoo実験行情を任意で有効にします。直近取引日の推定損益を表示しますが、給与・生活費・目標には加算しません。証券口座への接続や注文はしません。拡張は `browser-extension` を手動で読み込みます。

[手順・計算規則・審査項目](docs/STOCK_BETA.md)。ローカルのベータEXE/ZIPを使い、架空のサンプルから試してください。

<p align="center"><img src="assets/app.png" width="96" alt="Slaves Encourager"></p>

# おしごと応援 / Slaves Encourager

<p align="center">
  <a href="https://github.com/thisemailrectrash-cpu/slaves-encourager/actions/workflows/windows.yml"><img src="https://github.com/thisemailrectrash-cpu/slaves-encourager/actions/workflows/windows.yml/badge.svg?branch=main" alt="CI"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/code-MIT-blue" alt="Code: MIT"></a>
  <a href="https://github.com/thisemailrectrash-cpu/slaves-encourager/releases/latest"><img src="https://img.shields.io/github/v/release/thisemailrectrash-cpu/slaves-encourager?label=release" alt="Latest release"></a>
</p>

<p align="center"><a href="README.md">简体中文</a> · <a href="README.zh-Hant.md">繁體中文</a> · <a href="README.en.md">English</a> · <strong>日本語</strong> · <a href="README.es.md">Español</a> · <a href="README.ko.md">한국어</a></p>

<p align="center"><a href="https://github.com/thisemailrectrash-cpu/slaves-encourager/releases/latest"><strong>Windows版をダウンロード</strong></a></p>

<p align="center"><img src="docs/demo.gif" width="900" alt="収入のフリップ表示と6言語切り替えのデモ"></p>
<p align="center"><sub>実際のアプリ画面 · サンプルデータ · 6言語切り替え</sub></p>

ホームの固定文字をダブルクリック、またはボタンを右クリックして編集できます（`Ctrl+E`）。保存・キャンセル・項目や現在の言語のリセットに対応。変更は言語ごとに保存されます。[説明](docs/HOME_TEXT.md)。

がんばる時間を、見えるごほうびに。Windows用の収入時計、退勤カウントダウン、勤務カレンダー、目標リストです。


<img src="docs/screenshots/ja-main.png" width="380" alt="日本語の画面">

## 給与周期と生活費（1.4.0）

開始日は1～31日（既定は1日）です。15日なら15日～翌月14日となり、日付がない月は月末を使います。開始日の午前0時に収入・生活費・目標をリセットします。

生活費は既定で非表示です。初回の給与設定で有効にすると、名前・金額・順番を最大20件まで編集できます。赤いバーは未充当の割合で、収入に応じて短くなります。生活費を先に充当し、残りをごほうびに回します。無効にしても保存済みのリストは残ります。

初回設定・途中での有効化・開始日変更時は、保存前に累計収入を確認します。金額を修正するか0から開始でき、確認後の収入のみ加算します。給与設定から再修正でき、次の周期にリセットします。実際の支払いではなく推定配分です。[詳細](docs/EXPENSES.md)。


## はじめに

Windows 10/11と.NET Framework 4.8が必要です。ZIPを展開し、`SlavesEncourager.exe`を実行します。給与はオフラインで利用でき、行情通信は確認後の任意機能です。

初回はデモ表示です。「初回設定」で月給と勤務時間を入力し、カレンダーで休日や追加出勤を指定して保存してください。目標は20個まで追加でき、順番の変更や切り替えができます。設定の右上で6言語を選択でき、保存した言語は次回も使われます。キャンセルすると元の言語に戻ります。

フリップ時計、スクロール、静止表示を選べます。小数桁数、桁区切り、通貨記号、書式も変更できます。メイン画面はボタン以外をドラッグして移動できます。

`Ctrl+M`でミニ表示、`Ctrl+Shift+P`で金額を非表示にします。ショートカットは画面が選択されているときに有効です。「—」で通知領域へ、「×」で終了します。

## 推定値について

月給を今期の勤務日数、または固定21.75日で按分します。休憩は除き、過去の勤務日は皆勤として計算します。税金、賞与、残業代、実際の勤怠は計算しません。祝日は自分の勤務予定に合わせて指定してください。日またぎ勤務は開始日に属します。通貨の変更は記号のみで、為替換算はしません。

目標には今期の推定収入を順番に割り当て、重複計上しません。給与周期の開始日にリセットされ、貯金残高を示すものではありません。

設定は安定版と分けるため`%LOCALAPPDATA%\SlavesEncouragerStockBeta\settings.json`に保存します。暗号化はされず、保存時に`.bak`を残します。

## 開発

Windowsコンテナーを優先します：`./scripts/build.ps1`。利用できない場合は既存のコンパイラーで`./scripts/build.ps1 -Native -VisualCheck`を実行し、`python scripts/validate.py`と`python scripts/package.py`で検証・作成できます。

[MITライセンス](LICENSE) · [開発ガイド](docs/DEVELOPMENT.md) · [貢献ガイド](CONTRIBUTING.md) · [変更履歴](CHANGELOG.md)
