# V1 源码基线

冻结日期：2026-09-17。产品新名称 Mdis；历史实现保留 EZ-Dose 命名。

| 组件 | 原仓库 / 分支 | 原始 commit | 新基线 tag |
| --- | --- | --- | --- |
| Unity | ma-jiale/EZ-Dose / main | cf19c66d145bd569a18d3c5553da42c4155236f9 | mdis-v1-unity-baseline |
| Flask | ma-jiale/nursing-rx / feature/ux-improvement | d37f2d8d6f67bdae5952b337d8ac1c35e67167f3 | mdis-v1-flask-baseline |

实际本地后端目录为 `../EZ-Dose-server`，origin 仍配置为 ma-jiale/nursing-rx。
2026-09-17 推送标签时 GitHub 确认仓库已迁移到 ma-jiale/pillxa-dipenser；旧地址通过重定向推送成功。
冻结前已 fetch 两个 origin。后端功能分支包含本地 main；保留其分支，不把旧 main 误标为当前基线。
后端 14 个工作区差异经 git diff --ignore-space-at-eol 检查均为空，导入已提交内容；原工作区不变。
客户端既有 v0.0.1、v1.0.0、v2.0.0 标签均保留，不覆盖、不重新定义为 Mdis 版本。
这些是源码快照，不代表本次重新通过真实硬件资格测试。

## 历史导入

采用 git subtree add（不使用 --squash），完整保留 Flask 可达提交历史与 SHA。
导入提交：f574154；导入路径 legacy/v1/server-flask。
Flask 基线树：8aed4b5ba915aaa8f85b94479259298d67cab0b9。
Unity 基线 unity 子树：1adafdf2da3f42362c66a6fbfb00032580b578ee。
重构分支：chore/v2-monorepo-bootstrap。
2026-09-17：两个基线 tag 均已成功推送到主仓库；Flask tag 也已推送到原后端仓库。
V2 初始化分支仅在本地提交，未推送、未合并 main。

## 找回基线

使用独立 worktree 避免覆盖工作区：

```sh
git worktree add ../mdis-unity-v1 mdis-v1-unity-baseline
git worktree add ../mdis-flask-v1 mdis-v1-flask-baseline
```

Unity tag 下工程路径为 unity/；Flask tag 下 main.py 位于根目录，因为原始历史未重写。
在 V2 分支中二者位于 legacy/v1/ 下。git log --all 可见后端完整历史；导入前按原始路径查询。
数据库、未跟踪上传图片、环境配置和本地 SDK 不在 Git 快照范围，正式部署迁移需独立备份。
