<p align="center"><img src="assets/app.png" width="96" alt="Slaves Encourager"></p>

# 牛马鼓励器 / Slaves Encourager

<p align="center">
  <a href="https://github.com/thisemailrectrash-cpu/slaves-encourager/actions/workflows/windows.yml"><img src="https://github.com/thisemailrectrash-cpu/slaves-encourager/actions/workflows/windows.yml/badge.svg?branch=main" alt="CI"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/code-MIT-blue" alt="Code: MIT"></a>
  <a href="https://github.com/thisemailrectrash-cpu/slaves-encourager/releases/latest"><img src="https://img.shields.io/github/v/release/thisemailrectrash-cpu/slaves-encourager?label=release" alt="Latest release"></a>
</p>

<p align="center"><strong>简体中文</strong> · <a href="README.zh-Hant.md">繁體中文</a> · <a href="README.en.md">English</a> · <a href="README.ja.md">日本語</a> · <a href="README.es.md">Español</a> · <a href="README.ko.md">한국어</a></p>

<p align="center"><a href="https://github.com/thisemailrectrash-cpu/slaves-encourager/releases/latest"><strong>下载 Windows 便携版</strong></a></p>

主页固定文字支持直接修改：双击文字，或右键按钮选择「编辑主页文字」（`Ctrl+E`）。支持保存、取消、恢复单项和恢复当前语言，修改按语言独立保存。详见[文字编辑说明](docs/HOME_TEXT.md)。

把工作时间变成看得见的小奖励。一个支持翻页数字、收入估算、下班倒计时、排班日历和多个目标的 Windows 桌面悬窗。


<img src="docs/screenshots/zh-Hans-main.png" width="370" alt="雾蓝暖杏主题的主窗口"> <img src="docs/screenshots/en-main.png" width="370" alt="English interface">

## 特色

- 翻页时钟、滑动、静态三种数字样式；0–4 位小数、千分位、币种符号和自定义模板。
- 全新「牛角＋时钟」图标；雾蓝暖杏、暮色薰衣草、奶油纸张三套配色。
- 简体中文、繁体中文、英语、日语、西班牙语、韩语；支持跟随系统、即时预览和保存语言选择。
- 工作日、固定休息、跨午夜班次；直接点击日历设置休息和补班。
- 最多 20 个目标，可编辑、删除、排序和切换查看；本月收入按清单顺序分配。
- 置顶、迷你模式、隐私模式、托盘；主窗口任意非按钮区域都能拖动。
- 本地运行，无需账户或联网，无遥测；源码采用 [MIT 许可证](LICENSE)。

## 开始使用

支持 Windows 10/11，需要系统已有 **.NET Framework 4.8**。解压便携包后双击 `SlavesEncourager.exe`。

1. 首次显示明确标记的示例数据，点击「第一次使用设置」。
2. 在「工资与排班」填写月薪、工作日、上下班时间和固定休息。
3. 在「休息与补班」选择操作，再点日期；同一模式再点一次可取消。
4. 在「目标与使用」新增奖励、填写金额，并调整先后顺序。
5. 点「保存并应用」。语言可在设置右上角切换，取消会恢复原语言。

`Ctrl+M` 切换迷你模式，`Ctrl+Shift+P` 切换隐私模式，快捷键在窗口获得焦点时生效。「—」隐藏至托盘，双击托盘图标恢复；「×」退出。

设置保存在 `%LOCALAPPDATA%\NiumaFlip\settings.json`。这是为兼容旧版本保留的内部目录名，产品界面已移除旧品牌后缀。旧版工资、日期、目标和格式会保留；保存时保留 `.bak`。设置未加密，隐私模式用于隐藏界面金额。

## 计算口径

默认以月薪除以当月排班天数，也可选固定 21.75 天折算。有效时长扣除固定休息，休息和下班后停止累计；每次按当前时间重算，因此休眠或重开不会丢失定时器间隔。

过去排班日默认满勤，不计算个税、奖金、加班费或实际考勤扣款。跨午夜班次归属开始日期。未内置全国节假日，请按自己的公司安排点选日历。币种只改变显示符号，不做汇率换算；自定义金额格式保持用户选择的分隔规则。

目标使用同一份本月估算收入，按列表顺序推进。例如先 1,000、后 3,000，收入为 1,500 时，两个目标分配 1,000 和 500，不重复计数。目标进度并非实际储蓄，每月重新计算。

## 从源码构建

首选 Windows 容器（需 Windows Docker engine）：

```powershell
./scripts/build.ps1
```

开发机 Docker 不可访问时，可使用已安装的 Windows 编译器，不安装系统包：

```powershell
./scripts/build.ps1 -Native -VisualCheck
python scripts/validate.py
python scripts/package.py
```

输出在 `dist/`，验证记录在 `.build/`。视觉检查会加载真实 WPF 界面并逐语言渲染；GitHub Actions 配置执行编译、测试、资源校验和打包，远程运行结果见仓库 Actions 页面。

## 项目结构

```text
src/SlavesEncourager/  桌面界面、计算、日历、目标和本地化
locales/catalog.tsv   194 个键 × 6 种语言
assets/              PNG 原图和七档尺寸 ICO
scripts/             构建、校验和白名单打包
tests/               计算、兼容性和语言行为测试
docs/                开发、发布、图标说明和示例截图
.github/workflows/   Windows CI
```

[贡献指南](CONTRIBUTING.md) · [开发说明](docs/DEVELOPMENT.md) · [GitHub 发布准备](docs/GITHUB_PUBLISH.md) · [验证记录](docs/VERIFICATION.md) · [版本记录](CHANGELOG.md) · [图标来源](docs/ICON.md) · [第三方说明](THIRD_PARTY_NOTICES.md)

本项目根据一个公开的 macOS 功能演示独立实现，不含原作者源码、图标或安装包；功能灵感来源见 [公开作品](https://www.douyin.com/note/7703818875581798641)。
