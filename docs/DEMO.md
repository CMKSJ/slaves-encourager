# README 动图 / README animation

`docs/demo.gif` 展示真实 Windows 界面的收入翻页和六种语言切换。画面由同一时刻的完整主窗口、放大的金额卡片和语言标识组成；只使用内置示例数据。浅蓝、米白和暖杏配色与应用一致。

帧直接来自应用的 WPF 控件。数字由原有收入计时器和 `FlipDisplay` 驱动；语言通过设置窗口的真实 `LanguageBox` 选择事件切换。金额和状态不会用图片工具重画。录制期间把翻页速度选为应用支持的慢速，方便看清上下半页的运动。

- 画布：1000 × 660；20 fps；360 张源帧；18 秒，循环播放。
- 已交付动图：401,621 字节；全部 360 帧解码通过，已抽查各语言和翻页中间帧。
- 顺序：简体中文、English、繁體中文、日本語、Español、한국어，每种 3 秒。
- 临时帧、编译器输出和日志均保存在 `.build/readme-demo/`，不提交到 GitHub。
- 独立演示进程不加载真实设置，不保存语言选择，也不关闭正在运行的应用。
- GIF 为 README 文档素材；应用运行、普通构建和 CI 不依赖 FFmpeg。

## 重建

使用已有 Windows .NET Framework 编译器及已有 FFmpeg，不安装主机软件。该可选 WPF 录制流程需要 Windows 桌面环境；常规应用构建仍优先使用仓库的 Windows 容器。

```powershell
./scripts/render-demo.ps1
```

也可指定已构建的应用和现有 FFmpeg 路径：

```powershell
./scripts/render-demo.ps1 -ApplicationPath "path/to/SlavesEncourager.exe" -FfmpegPath "path/to/ffmpeg.exe"
```

`DemoRenderer.cs` 在独立进程中读取项目程序集，创建示例控制器，通过设置事件切换语言，并导出真实控件帧。FFmpeg 使用共享调色板和差分区域编码 GIF。更新动图后运行 `python scripts/validate.py`，解码检查全部帧，并抽查翻页中间帧和全部语言。

## English

The looping README GIF uses real WPF controls and built-in sample data. The close-up and full window show the same live frame. The existing earnings timer drives digit animation, and the settings language selector's real event drives language changes. No real settings or salary data are loaded or saved.

The optional capture command above uses the existing Windows compiler and an existing FFmpeg executable. It exports 360 source frames at 1000 × 660 and encodes them at a fixed 20 fps into an 18-second GIF. Export may take longer than playback on slower machines. The delivered GIF is 401,621 bytes; all 360 frames decode successfully. Temporary files stay under `.build/readme-demo/`. FFmpeg is not an application or CI dependency. Published versioned release assets remain unchanged when only this documentation animation is updated.
