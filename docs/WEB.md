# Web edition / 网页版说明

`web/` 是 Windows 桌面版的浏览器移植（beta）。同口径计算、同六语言、同设置契约，数据仅保存在浏览器 localStorage。

## 运行 / Run

```bash
cd web
npm install        # 需要一次 Node.js LTS / requires Node.js LTS once
npm run dev        # 开发服务器 / dev server
npm test           # vitest（47 项检查，含 EngineTests.cs 黄金用例）
npm run build      # 产物在 web/dist，可直接静态托管
npm run preview    # 本地预览构建产物
npm run icons      # 从 assets/app.png 重新生成 PWA 图标（产物已入库）
```

`web/dist` 是纯静态文件：GitHub Pages、任意对象存储或本地 `python -m http.server` 都能托管。已生成 PWA manifest 与 service worker，可安装到桌面/手机主屏并离线运行。

## 架构 / Architecture

| 目录 | 职责 |
|---|---|
| `src/engine/` | 计算核心，逐行翻译自 `src/SlavesEncourager/Engine.cs`：`engine.ts`（工资/周期/目标/开支）、`settings.ts`（契约与校验）、`shift.ts`（班次时间）、`calendar.ts`（排班覆盖）、`format.ts`（金额格式化） |
| `src/i18n/` | `L.ts`（六语言解析）、`homeCopy.ts`（主页文案覆盖）；`catalog.tsv` 由 vite 虚拟模块在构建时从 `../locales/catalog.tsv` 读取——**单一来源，绝不复制** |
| `src/state/` | signals 全局状态与可注入时钟（demo 模式固定 2026-11-04 14:32:18） |
| `src/ui/` | Preact 组件：主面板、翻页数字、设置五页签、排班日历、开支/收入/文案对话框 |

## 与桌面版的刻意差异 / Deliberate deviations

- **精度**：C# `decimal` → JS `number` + 显式 AwayFromZero 舍入。EngineTests 的容差断言（1e-5）全部通过；若有黄金断言失败，先改整数缩放，最后才允许 decimal.js 且仅限 `engine.ts`（不得静默分叉财务计算）。
- **Snapshot / 校验错误**：只携带键名（`StatusKey`、`ValidationError.key`），翻译交给 UI 层，保证 engine 纯函数可测。
- **设置持久化**：localStorage 键 `slaves-encourager.settings`，字段保留 PascalCase 与桌面 JSON 契约一致（可互相导入）；写入前备份 `.bak`，主键损坏回退备份再回退默认；未知字段（如历史 `StocksEnabled`）被忽略。
- **日期**：一律本地时间分量构造；结算日手动钳制到月末（JS Date 构造器会向次月溢出）。
- **无对应功能**：置顶、托盘、拖动、无边框悬窗（浏览器限制）；`Ctrl/Cmd+M` 迷你、`Ctrl/Cmd+Shift+P` 隐私、`Ctrl/Cmd+E` 文案编辑保留。
- **计时器**：每秒全量重算（与桌面同构，休眠/重开不丢）；标签页隐藏时暂停，恢复即按当前时间重算。

## 安卓 APK / Android APK

网页版由 Capacitor 打包为安卓应用（实验性，`android/` 工程已入库）：

```bash
cd web
npm run build              # 更新 web/dist
npx cap sync android       # 拷贝产物进 android 工程
# 然后在装有 JDK 21 与 Android SDK 的环境里（见下）：
cd android && ./gradlew assembleDebug
# 产物 android/app/build/outputs/apk/debug/app-debug.apk（debug 签名，约 6.5MB）
```

构建环境实测（WSL Ubuntu，无需 Windows 侧安装）：

- **JDK 21 必需**（Capacitor 8 的 `capacitor-android` 模块要求 Java 21 源级；JDK 17 会在 `invalid source release: 21` 处失败）。可从 Adoptium 镜像解压到用户目录，无需 root：
  `wget https://mirrors.tuna.tsinghua.edu.cn/Adoptium/21/jdk/x64/linux/OpenJDK21U-jdk_x64_linux_hotspot_21.0.13_9.tar.gz`
- **Android SDK**：`cmdline-tools` + `platform-tools` + `platforms;android-36` + `build-tools;36.0.0`。dl.google.com 直连慢时，`commandlinetools`、`platform-36_r02.zip`、`platform-tools-latest-linux.zip` 均可从 `mirrors.cloud.tencent.com/AndroidSDK/` 下载后解压到 `~/Android/Sdk/` 标准布局；`android/local.properties` 写 `sdk.dir=<SDK 路径>`（不入库）。
- **gradle 发行版**：`android/gradle/wrapper/gradle-wrapper.properties` 已指向腾讯镜像；Maven 依赖可经 `~/.gradle/init.gradle` 配置阿里云镜像（主机本地配置，不入库）。
- 图标由 `npx @capacitor/assets generate --android --assetPath <含 logo.png 的目录>` 生成；应用清单不含任何权限（完全离线）。
- appId 为 `io.github.thisemailrectrashcpu.slavesencourager`（Android 包名段不允许连字符）。

release 签名流程见 Capacitor 文档（`keytool` 生成 keystore 库外保存 + `signingConfig`）；keystore 不入库。
