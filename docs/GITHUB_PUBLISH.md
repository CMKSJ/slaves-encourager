# GitHub 发布 / Publishing

产品：**Slaves Encourager / 牛马鼓励器 1.3.1**，MIT 许可证。

- 仓库：[thisemailrectrash-cpu/slaves-encourager](https://github.com/thisemailrectrash-cpu/slaves-encourager)
- 下载：[Releases](https://github.com/thisemailrectrash-cpu/slaves-encourager/releases)
- 远程验证：[Actions](https://github.com/thisemailrectrash-cpu/slaves-encourager/actions)

远程可用性和构建状态以对应页面为准。每次发布前先完成本地构建和源码白名单核对；仅发布本项目源码、生成的图标与示例截图，不提交真实设置、工资数据、本机路径或开发日志。

## 重建与检查

```powershell
./scripts/build.ps1 -Native -VisualCheck
python scripts/validate.py
python scripts/package.py
git diff --check
git status --short
```

首选 Windows 容器，已有工具链的本机回退方式见 [开发说明](DEVELOPMENT.md)。不要在主机安装系统包。

`package.py` 显式列出允许发布的根文件和目录，排除开发日志、真实配置、备份、缓存和旧安装包。核对本地验证记录与 `dist/SHA256SUMS.txt`。

## 推送与 Release

先检查待提交文件和差异，再创建提交；远程写入前先预演：

```powershell
git push --dry-run origin main
git push -u origin main
```

在 GitHub 创建 `v1.3.1` Release，标题为 `Slaves Encourager 1.3.1`，正文使用 [发布说明](RELEASE_NOTES.md)。上传：

- `SlavesEncourager-1.3.1-windows-portable.zip`
- `SlavesEncourager-1.3.1-source.zip`
- `SHA256SUMS.txt`

检查公开仓库的 README、MIT 标识、源码结构、Actions 结果与 Release 附件，再通知用户。
