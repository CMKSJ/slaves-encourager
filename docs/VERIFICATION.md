# Local stock beta verification / 本地股票测试验证

Date: **2026-11-04**, Asia/Shanghai. Version: **1.5.0-beta.4**, public prerelease; experimental workflows remain under review. Stable [v1.4.0](https://github.com/thisemailrectrash-cpu/slaves-encourager/releases/tag/v1.4.0) was published separately; beta publication was authorized on 2026-11-04.

| Check | Result |
| --- | --- |
| Existing Framework compilation | PASS, warnings as errors, zero warnings; no system packages installed |
| Salary/expense/locale regression checks | 151 PASS |
| Stock import, quote math, watchlist, visibility, consent/cancellation | 79 PASS |
| Extension synthetic DOM, minimization and permissions | 11 PASS |
| CSV/TSV/HTML/extension JSON | PASS: explicit mapping, optional cost, invalid-row blocking, duplicate rejection and culture ambiguity |
| XLSX | PASS: inline/shared strings, numeric cells, formula rejection, unsafe relationship rejection |
| Real local OCR helper | PASS on synthetic screenshot: Symbol/Quantity/AverageCost/Currency plus AAPL 10/150 and MSFT 5/300 reconstructed into rows |
| OCR isolation | Uses native WinRT reflection, no image upload or PowerShell execution-policy changes; installed Windows OCR required |
| Public quote probe | PASS, AAPL only: quote trading day and preceding trading-day baseline verified; no actual quote response packaged |
| Correct prior close | PASS: multi-day chartPreviousClose is ignored in favor of the preceding daily bar |
| Quote quality | PASS: missing/future/stale data, active-session delay, NY DST, weekend dating, missing symbols and simulated/live mixing |
| Network consent and stop | PASS: no provider call while offline, confirmed opt-in, cooldown, persisted stop and active-request cancellation |
| Default-hidden stocks | PASS: old settings default off, save/cancel isolation, stock-tab checkbox, saved-online portfolio cannot fetch while module is hidden |
| No-scroll home layout | PASS: last content position, automatic height growth with available screen space, mini collapse, fully visible stock card above actions at 600-DIP height |
| Six-language real WPF renders | PASS: stock dashboard, private dashboard, import preview plus baseline themes, settings and expenses |
| Manual watchlist | PASS: batch normalization, no invented shares/PnL, conversion, edit order, duplicates/limits, JSON and older holdings compatibility |
| Import/watch coexistence | PASS in real modal flow: imports preserve other watch entries |
| First-use routing in six languages | PASS: Enable → Stocks tab; Not now → salary; Cancel → no settings; original preferences and portfolio unchanged |
| Stock-tab drafts | PASS: save during quote refresh keeps the dialog/draft open and succeeds on retry; local draft staging, save/cancel isolation, independent watch/holding editor and no quote requests before application |
| Six-language modal flows | PASS: manual batch add/cancel, watch-to-holding conversion, import confirm/cancel, retained watches, online consent and original-snapshot isolation; existing setup flows also pass |
| Desktop import UI visual review | PASS for Chinese/English and long Spanish labels; real table columns render at readable widths |
| User brokerage access | NOT RUN: no account login, real holdings, clipboard or brokerage page read |
| Extension actual install/click-through | NOT RUN: extension not installed or enabled automatically; provided locally for user review |
| Real brokerage-specific layout coverage | NOT VERIFIED; generic readable tables supported, not an all-broker compatibility claim |
| Privacy | Amounts, share counts and PnL masked; symbol/time remain visible by design |
| Stable isolation | Beta settings path differs; no stable settings migration/write, PnL does not affect salary/expenses/goals |
| Public beta CI / Release | Publication authorized on 2026-11-04; see GitHub tag/CI for remote status |

Developer checks:

```powershell
./scripts/build.ps1 -Native -VisualCheck
node tests/extension.test.js
python scripts/validate.py
python scripts/package.py
```

An explicit live provider probe uses `.build/StockTests.exe --public-quote-probe`; normal behavior tests are offline. Screenshots and OCR samples are synthetic. Logs and test data remain in ignored `.build/`; packages include only allowlisted project files and synthetic examples.

The native OCR helper was tested in the normal Windows user context, since the developer sandbox denies the WinRT storage broker. No policy was changed. Windows 10, ARM, multiple DPI/monitor setups, missing OCR packs, prolonged quote refresh and actual individual broker workflows remain review items. Source rules and detailed user steps: [STOCK_BETA.md](STOCK_BETA.md).
