# 生活开支与工资周期 / Expenses and pay cycles

## 使用规则

- 生活开支默认关闭，主页入口与设置标签默认隐藏。在「第一次使用设置 → 工资与排班」勾选「开启生活开支页面」。以后可在相同位置开关。
- 首次提供租金 3,000、生活费 2,000、必要开支 1,000 的可编辑示例。它们只是占位值，与工资使用同一币种。可添加、改名、改金额、删除、排序，最多 20 项；名称 1–28 字符，金额大于 0 且不超过 100,000,000。
- 同一清单每个周期重复使用。关闭功能保留已保存清单并停止扣除开支；已删除全部项目的空列表保持为空。关闭后隐藏的未保存编辑不会覆盖原清单。
- 用户填写的名称不随语言切换而翻译。默认示例在首次保存前使用当前语言，保存后作为用户数据保留。
- 红条表示「剩余待覆盖 / 项目总额」，从 100% 减到 0%。已覆盖项目仍留在清单并标记完成。主页隐私模式同时隐藏开支窗口的金额及进度；项目名称仍显示。

## 分配示例

本期开支按优先顺序为租金 3,000、生活费 2,000；奖励目标为旅行 1,000：

| 本期累计收入 | 租金待覆盖 | 生活费待覆盖 | 旅行已分配 |
| ---: | ---: | ---: | ---: |
| 0 | 3,000（100%） | 2,000（100%） | 0 |
| 3,500 | 0（0%） | 1,500（75%） | 0 |
| 5,500 | 0（0%） | 0（0%） | 500 |

每份收入只分配一次。奖励可用收入为 `max(0, 本期收入 − 已启用开支合计)`，再按目标顺序分配；达成目标所需工时也包括尚未覆盖的开支。关闭开支后，全部估算收入可进入目标。

这些是工资估算的分配进度，不是银行余额、已到账工资或实际付款记录。不连接账户，也不会支付账单。

## 结算日与重置

工资结算日是每期的开始日，可选 1–31，默认 1。选 15 日时，本期为本月 15 日零点至下月 15 日零点之前，即显示为 15 日至下月 14 日。收入、开支覆盖与奖励目标同时重置，清单和月薪不会删除。

短月份按月末，下一月恢复原来的日号。例如选择 31 日：2026-01-31 → 2026-02-28 → 2026-03-31；闰年 2 月使用 29 日。月底不会漂移为以后每月 28 日。

默认日薪为「月薪 / 本期排班天数」，也可使用固定 21.75 天。固定口径下，整期累计可能不等于月薪。休息与补班按日历生效，固定休息不累计。周期使用电脑本地日期时间，跨午夜班次按开班日归属；结算日零点，前一期的夜班收入不会进入新一期，今日卡片仍可显示该班次。

## 中途开启与确认收入

以下操作在保存前自动弹出确认：首次设置；从关闭切为开启开支；更改工资结算日。普通修改语言、数字样式或开支清单不会重复弹出。

弹窗显示本期范围及「此前已累计约 xxxx」。首次估算把此前排班日视为满勤；已有确认金额时优先沿用该基准推算。用户可以修改金额，或选择「从 0 开始」后确认。确认取消会返回设置，取消设置不保存任何修改。

以确认时刻为锚点：

`本期收入 = 确认金额 + max(0, 当前排班估算累计 − 确认时刻的排班估算累计)`

例如确认已有 400，之后按排班新增 25，则显示 425；确认 0 则显示 25。重开应用按时间补算，无需一直运行。确认只影响本期累计，不改变「今日」卡片的整班收入。下个周期忽略旧确认，重新按新期排班累计。

工资设置中的「校正本期已累计收入…」会在保存时重新确认。改变结算日会按新周期重新估算并确认；修改工资或排班会重算后续增量，需要修正历史累计时使用校正入口。设置成功写入后才应用确认结果；保存失败保留此前设置。

## Compatibility and implementation

Older JSON settings without these fields use day 1, expenses disabled and no opening anchor. Existing goals, names, calendars, formatting and language remain compatible. Settings stay local and retain atomic writes and backups.

`SettlementDay` is nullable; `CycleDay` supplies 1 when absent. `ExpensesEnabled` defaults to false. A null `Expenses` list supplies editable starter rows, while an empty list stays empty. `OpeningIncome` is nullable to distinguish confirmed zero from no confirmation and is paired with local wall-clock `OpeningAt`. Old anchors are ignored outside their cycle.

Income covers enabled expense rows in priority order. Red bars show the uncovered fraction. Only surplus funds goals; remaining work-hour estimates include outstanding expenses. No duplicate allocation, financial transactions or network access are introduced.

On first setup, enabling expenses or changing the reset day, the settings dialog requires an editable estimate confirmation before committing. Cancel and failed persistence leave the original intact. “Adjust earnings so far…” requests another confirmation. The engine derives future increments from local schedule time and expires the anchor next cycle.

For overnight work, period attribution follows the shift's start date, while the reset occurs at local midnight. A shift that started before the reset does not contribute to the new cycle. The daily card and period card can therefore refer to different cycles during that boundary interval.
