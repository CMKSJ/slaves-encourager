# macOS 版 / macOS edition

1.5.0-beta.4 macOS 首次预发布。工资、结算周期、开支分配、目标、数字格式、主页文案和股票代码直接编译 Windows 共享源码；界面使用 Avalonia 11.3.22，包内置 .NET 10，无需服务器或自行安装运行时。

## 下载与安装

- Apple 芯片（M1/M2/M3/M4 等）：`SlavesEncourager-1.5.0-beta.4-macos-apple-silicon.dmg`。
- Intel：`SlavesEncourager-1.5.0-beta.4-macos-intel.dmg`。
- 需要 macOS 14+；打开 DMG，将 `Slaves Encourager.app` 拖入 Applications；也提供保留执行权限的 ZIP。
- 附件追加到 [beta.4 Release](https://github.com/thisemailrectrash-cpu/slaves-encourager/releases/tag/v1.5.0-beta.4)，不替换 Windows 文件；使用单独的 `SHA256SUMS-macos.txt` 核验。
- 仅有 **ad-hoc 签名，没有 Apple Developer ID 签名或 Apple 公证**。系统可能阻止打开；核对来源和 SHA-256 后，可按 [Apple 指引](https://support.apple.com/102445) 在“系统设置 → 隐私与安全”检查该应用的“仍要打开”。本软件不会修改系统安全策略。

## 功能对齐

| Windows beta.4 功能 | macOS 实现 |
| --- | --- |
| 实时工资、午休、跨午夜、实际/固定计薪、下班倒计时 | 共享 Engine.cs |
| 结算日 1–31、短月末日、首次/中途累计收入确认及校正 | 共享计算与确认金额流程 |
| 多目标、生活开支、顺序分配、随收入减少的红条 | 新增、编辑、排序、移除，开支默认隐藏 |
| 休息与补班日历 | 点击设置，同日互斥，再点取消 |
| 翻页、滑动、纯数字；主题、字体、货币、小数、分组、模板 | 同一格式核心，重绘上下半页；字体用系统替代 |
| 六语言与主页文案编辑 | 共享 catalog.tsv/HomeCopy；双击文字、右键按钮或 Cmd/Ctrl+E |
| 隐私、迷你、置顶、演示、非按钮区拖动、短屏无滚动 | 原生窗口；Cmd/Ctrl+Shift+P 隐私，Cmd/Ctrl+M 迷你 |
| 隐藏到托盘、恢复、迷你/完整、设置和退出 | macOS 菜单栏图标提供相同入口，隐藏后从菜单栏恢复 |
| 首次股票询问、独立股票页、草稿保存/取消 | 开启进股票，暂不开启进工资，取消不保存 |
| 自选代码、固定持仓、编辑/移除、最多 50 项 | 共享 StockMath/PortfolioState |
| 文件/Excel、粘贴表格、截图 OCR、扩展 JSON | 共享解析/映射/预览，截图改用本机 Apple Vision |
| 实验行情、自动/手动刷新、停止、独立联网同意、日盈亏 | 共享 StockService/YahooQuoteProvider；关闭功能即取消请求 |
| 隐私隐藏价格、股数、收益 | 主页与监控页隐藏金额；代码和时间仍可见 |

导入与行情范围、过期/缺失处理、固定股数盈亏口径、真实券商页面适配限制沿用 [股票说明](STOCK_BETA.md)。DMG 内含 `browser-extension`，需自行在 Chrome/Edge 开发者页手动加载；不自动安装、不登录券商、不后台读取页面、不下单。Apple Vision 只处理用户所选或主动粘贴的图像，不上传截图。

## 设置与迁移

文件位于 `~/Library/Application Support/SlavesEncouragerStockBeta/settings.json` 和同目录 `portfolio.json`，JSON 合同与 Windows 相同，原子保存并保留 `.bak`。不自动迁移或修改稳定版设置；退出两端程序并备份后，可手动复制这两个 JSON。隐私模式用于界面隐藏，配置不加密。字体、窗口按本机处理。

## 开发与验证

```sh
python3 scripts/build-macos.py --runtime osx-arm64
# Intel 使用 --runtime osx-x64；必须在原生 Mac 上执行。
```

`.github/workflows/macos.yml` 在 `macos-15`（Apple 芯片）与 `macos-15-intel` 构建。两端执行共享 151+79 项检查、Mac 草稿与路径检查、六语言实际弹窗/设置页/导入预览检查、Apple Vision 合成图 OCR、Mach-O ad-hoc 签名验证及打包。Windows CI 保留。开发主机只有 .NET 8 时可用 `-p:TargetFramework=net8.0` 兼容编译；发布包由 Mac runner 使用 .NET 10 构建。

本工作区 Docker 引擎不可用，Linux/Windows 容器也不能验证 AppKit/Vision；使用现有本机 SDK 和 GitHub 原生 Mac runner，不安装用户主机系统组件。源码包排除嵌套 bin/obj、日志、真实配置、持仓与行情响应。

CI 验证不代表所有型号、系统、屏幕或真实券商页面均已人工实测。本版无 App Store 发行或 Developer ID 公证；行情仍为实验功能，真实券商/扩展实际交互仍待进一步验证。原生 CI 和发布状态以关联提交与 Release 为准。

## English

Download the Apple Silicon or Intel DMG from the existing beta.4 Release. Requires macOS 14+; .NET 10 is bundled. Drag the app to Applications. This build is ad-hoc signed, **not Apple Developer ID signed or notarized**. Follow Apple's per-app approval flow if Gatekeeper blocks a trusted download. The app never changes system security settings.

The Mac UI compiles the exact Windows financial, formatting, localization and stock domain sources. It includes calendar overrides, payroll/opening-income confirmation, goals/expenses, three number animations/themes, editable copy, privacy/mini/pin/demo, first-use stock routing and four import paths. Apple Vision replaces Windows OCR locally. Settings use a separate Application Support directory. No automatic migration, brokerage login or orders. Quotes need separate consent; the included browser extension must be loaded manually. Real broker flows remain under review.

Official references checked 2026-11-04: [Avalonia deployment](https://docs.avaloniaui.net/docs/deployment/macos), [Avalonia 11.3.22](https://github.com/AvaloniaUI/Avalonia/releases/tag/11.3.22), [.NET macOS support](https://learn.microsoft.com/en-us/dotnet/core/install/macos), [Apple Vision](https://developer.apple.com/documentation/vision/recognizing-text-in-images), [GitHub runner images](https://github.com/actions/runner-images).
