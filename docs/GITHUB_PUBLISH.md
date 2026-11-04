# 预发布流程 / Prerelease publication

2026-11-04 用户明确要求“发布这个版本”，解除先前仅限本地的限制。1.5.0-beta.4 以公开预发布版发布，代码位于独立 beta 分支与 v1.5.0-beta.4 标签，稳定 main 与 1.4.0 保留。

- 先只读核对目标仓库、现有版本、提交和附件摘要，完成 dry-run 后再执行远程写入。
- 上传已审核的 Windows 便携包、干净源码包与 SHA256SUMS.txt；不覆盖已有附件或版本。
- 源码只使用 scripts/package.py 的白名单，排除构建日志、用户配置、真实持仓和实际行情响应；界面截图与示例均为合成数据。
- Release 设置 prerelease=true，保留稳定版为 Latest。编译、241 项检查、六语言界面流程和包校验须通过。
- 扩展仍由用户手动加载，不自动安装或上架。MIT 仅覆盖本项目代码，不授予第三方行情再分发权。真实券商页面和扩展实际交互仍待进一步验证。

The user authorized public beta.4 publication on 2026-11-04. Use a separate beta branch/tag, dry-run remote operations first, verify all three uploaded asset hashes, and preserve stable releases. No real holdings or market-response data may be uploaded.


macOS publication authorized: append Apple Silicon/Intel DMG and ZIP, macOS source ZIP and SHA256SUMS-macos.txt to the existing beta.4 prerelease only after native CI succeeds. Preserve old Windows asset IDs/digests, stable main/latest and the existing tag. Record exact macOS source commit and link it in the release body; API writes require a read-only dry-run first.
