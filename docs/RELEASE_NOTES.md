# Slaves Encourager 1.4.0

<p align="center">
  <a href="https://github.com/thisemailrectrash-cpu/slaves-encourager/actions/workflows/windows.yml"><img src="https://github.com/thisemailrectrash-cpu/slaves-encourager/actions/workflows/windows.yml/badge.svg?branch=main" alt="CI"></a>
  <a href="https://github.com/thisemailrectrash-cpu/slaves-encourager/blob/main/LICENSE"><img src="https://img.shields.io/badge/code-MIT-blue" alt="Code: MIT"></a>
</p>

<p align="center"><a href="#简体中文">简体中文</a> · <a href="#繁體中文">繁體中文</a> · <a href="#english">English</a> · <a href="#日本語">日本語</a> · <a href="#español">Español</a> · <a href="#한국어">한국어</a></p>

## 简体中文

- 新增默认隐藏的生活开支页面，可自定义最多 20 项租金、生活费等；红条随收入覆盖逐步缩短，余额再进入奖励目标。
- 工资结算日支持 1–31 日，作为收入、开支和目标的周期重置日，短月份自动使用月末。
- 首次设置、中途开启或更改周期时，自动确认此前已累计的估算收入；可修改、从 0 开始或日后校正，取消不保存。
- 保留翻页数字、自定义格式、六语言、隐私模式、日历和主页文案编辑。修复西班牙语默认目标名超长。

解压 Windows 便携包，运行 `SlavesEncourager.exe`。需要 Windows 10/11 与 .NET Framework 4.8。收入分配为估算，不代表实际到账或已付款。旧设置兼容，开支默认关闭。

## 繁體中文

- 生活開支預設隱藏，最多自訂 20 項；紅條隨收入縮短，餘額再進入獎勵。
- 結算日可選 1–31 日，重置收入、開支與目標，短月份按月底。
- 首次設定、中途開啟或更改週期時確認此前累計收入，可修改、從 0 開始或日後校正；取消不儲存。
- 保留翻頁、格式、六語言、隱私模式與舊設定相容性。收入是估算，並非實際入帳或付款。

## English

- Optional living expenses, hidden by default: customize up to 20 items; red uncovered bars shrink with income, and only surplus funds rewards.
- A reset day from 1–31 defines the shared income, expense and goal cycle; short months use their last day.
- Confirm or edit earnings so far during first setup, mid-cycle opt-in or cycle changes. Start from zero or adjust later; cancelling saves nothing.
- Preserve flip digits, formats, six languages, privacy, calendar and editable home captions. Fix the overlong Spanish default goal. Existing settings remain compatible.

Extract the Windows portable ZIP and run `SlavesEncourager.exe`. Requires Windows 10/11 and .NET Framework 4.8. Allocations are estimates, not payroll deposits or bill payments.

## 日本語

- 既定で非表示の生活費ページ。最大20件を編集でき、赤い未充当バーが収入に応じて短くなり、残額を目標へ回します。
- 給与周期の開始日は1～31日。日付がない月は月末を使い、収入・費用・目標をリセットします。
- 初回設定・途中の有効化・周期変更時に累計額を確認。修正、0から開始、後日の再修正が可能で、キャンセルでは保存しません。
- フリップ、書式、6言語、金額非表示、既存設定を維持します。金額は推定で、実際の入金や支払いではありません。

## Español

- Gastos opcionales, ocultos inicialmente: hasta 20 elementos; las barras rojas bajan con el ingreso y solo el excedente financia metas.
- El día de inicio del 1 al 31 reinicia ingresos, gastos y metas; los meses cortos usan su último día.
- Confirma o corrige lo acumulado al configurar, activar gastos o cambiar el ciclo. Puedes empezar desde 0 o corregir después; cancelar no guarda.
- Conserva animaciones, formatos, seis idiomas y ajustes anteriores. Corrige el nombre predeterminado de meta demasiado largo. Los importes son estimaciones, no cobros ni pagos.

## 한국어

- 기본 숨김인 생활비 페이지: 최대 20개를 편집하고 수입에 따라 빨간 미충당 막대가 줄어듭니다. 나머지만 보상에 배분합니다.
- 급여 주기 시작일은 1~31일이며 짧은 달은 말일입니다. 수입, 생활비, 목표를 함께 초기화합니다.
- 최초 설정, 주기 중 활성화, 주기 변경 시 누적 금액을 확인합니다. 수정, 0부터 시작, 나중에 재수정을 지원하며 취소하면 저장하지 않습니다.
- 플립 숫자, 형식, 6개 언어, 개인정보 모드, 기존 설정을 유지합니다. 금액은 추정치이며 실제 입금이나 결제가 아닙니다.

## Verification / 验证

- Local compilation: zero warnings; 151 behavior/localization checks passed.
- Six-language sample rendering includes expense states and income confirmation. Actual modal edit/save/cancel flows use isolated sample drafts.
- Source checks cover UTF-8, XML, translation placeholders, README links, icon and GIF. Packages include checksums and pass byte comparison/CRC.
- Remote Windows CI is linked from the release commit in Actions. Existing v1.3.1 assets remain unchanged. This release contains no stock-holdings beta.

## Assets / 附件

- `SlavesEncourager-1.4.0-windows-portable.zip` — Windows app / Windows 便携版
- `SlavesEncourager-1.4.0-source.zip` — Clean source / 干净源码
- `SHA256SUMS.txt` — SHA-256 checksums / 文件校验和
