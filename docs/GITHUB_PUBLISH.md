# 发布流程 / Release publication

1.6.0 移除股票功能与 macOS 版本。发布前确认：编译、151 项行为检查、六语言界面流程和包校验全部通过。

- 先只读核对目标仓库、现有版本、提交和附件摘要，完成 dry-run 后再执行远程写入。
- 上传 Windows 便携包、干净源码包与 SHA256SUMS.txt；不覆盖已有附件或版本。
- 源码只使用 scripts/package.py 的白名单，排除构建日志、用户配置和个人数据；界面截图与示例均为合成数据。
- 保留历史 beta 标签与附件不删除；1.5.0-beta.4 及其 macOS 附件仅存档于原 Release。

The 1.6.0 release removes the stock feature and the macOS edition. Dry-run remote operations first, verify all uploaded asset hashes, and preserve existing releases and tags. No user data may be uploaded.
