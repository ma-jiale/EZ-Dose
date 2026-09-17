# Mdis Agent 工作约定

- 首先阅读 `docs/product/PRODUCT.md`、`docs/architecture/ARCHITECTURE.md` 和 `docs/migration/V1_BASELINES.md`。
- 产品名 Mdis，英文全称 Medication Dispensing System，代码前缀 `mdis`。
- V2 开发位于 `apps/`；`legacy/v1/` 仅作行为与协议参考。未经明确要求不要修改 V1 业务逻辑。
- `hardware/` 保持活跃；`99_archive/` 不是现行协议/API 的事实来源。
- 机器控制只能在客户端本地执行。网络重试绝不能盲目重复机器命令。
- 所有租户数据必须由服务端验证 membership 后限定范围；不得信任客户端提供的养老院 ID。
- Session / Step 的业务状态与同步状态分离；完成必须有真实执行证据。
- 目前只有初始化骨架，不得把文档中的目标当作已实现能力。
- Flutter 面向 Windows 与 Android Pad；先定义设计系统，再实现患者工作台与摆药流程。
- Python V2 使用独立虚拟环境；V1 测试遵守 legacy 后端 `agent.md` 的 pill-dispenser 环境要求。
- 避免提交数据库、患者资料、密钥、SDK、缓存和构建产物。
- 变更后运行相关检查，说明已验证内容和未验证边界。先看 git status，保留用户已有修改。
