# Mdis 智能摆药系统

**Mdis — Medication Dispensing System**（原 EZ-Dose），面向养老院与康养机构。
系统负责计算和引导，护理人员负责确认和投药，机器负责实际摆药。

## 当前阶段

本仓库已进入 V2 Monorepo 初始化阶段。Unity / Flask V1 是行为参考基线；V2 尚未具备真实摆药能力。
Flutter 当前只有空白启动页，FastAPI 当前只有存活检查。租户模型、鉴权、同步、业务页面与真实硬件接入均待实现。

| 路径 | 职责 |
| --- | --- |
| `apps/client/` | Flutter V2，Windows / Android Pad |
| `apps/server/` | FastAPI V2，目标 PostgreSQL |
| `hardware/` | 持续维护的硬件工具 |
| `legacy/v1/client-unity/` | Unity V1 原始工程 |
| `legacy/v1/server-flask/` | Flask V1，完整原始 Git 历史 |
| `analysis/` | V1 脉冲分析工具 |
| `99_archive/` | 更早的实验与归档，不是当前 V1 基线 |
| `docs/` | 产品、架构、设计、协议与迁移共同上下文 |
| `scripts/`, `infra/` | 开发、迁移与部署骨架 |

## 新人阅读顺序

1. [产品与 roadmap](docs/product/PRODUCT.md)
2. [系统架构](docs/architecture/ARCHITECTURE.md)
3. [V1 基线](docs/migration/V1_BASELINES.md)与[迁移清单](docs/migration/V1_MIGRATION.md)
4. [Agent 工作约定](AGENTS.md)
5. 对应模块 README。

## 本地启动

Flutter：在 `apps/client` 中运行 `flutter pub get`，然后 `flutter run -d windows` 或连接 Android 设备运行。
FastAPI：参见 [服务端说明](apps/server/README.md)。
本地容器：复制 `.env.example` 为 `.env`，运行 `docker compose up --build`，访问 `http://127.0.0.1:8000/health/live`。
该端点仅证明进程存活，不代表数据库、租户隔离或摆药链路可用。

历史品牌与命名空间保留于 legacy；新代码和配置使用 `mdis`。GitHub 仓库名称与远端地址暂不变。
