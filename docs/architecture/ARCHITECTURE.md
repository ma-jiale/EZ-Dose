# Mdis V2 架构

## 边界

Flutter → 本地 MachineService → Serial / Bluetooth → STM32。
Flutter → HTTPS → FastAPI → PostgreSQL。
云端管理养老院、用户、患者、处方、设备、任务、历史、审计和同步；不通过互联网中转实时机器控制。
已经在本地准备并确认的 Session 不因服务器断网而停止。

## 仓库和分层

`apps/client`：Flutter；目标 Riverpod、go_router、Dio、Freezed/json_serializable、Drift/SQLite、Secure Storage。
本次仅添加 Flutter 工程，业务依赖随模块实现引入。
MachineService 隔离传输、ProtocolCodec 和流程协调；Mock Machine 与真实 transport 共用契约。

`apps/server/app`：main 仅组装应用；core 存放配置/安全；api 为 HTTP 边界；
modules 按 auth、patients、prescriptions、dispensing、devices 等领域组织 schema/service/repository；db 为数据库基础。
SQLAlchemy 2.x + Alembic + PostgreSQL 为目标持久层，本次不虚构业务模型或初始迁移。
API-first，V1 Flask 管理 UI 留在 legacy。

## 数据与可靠性

User ↔ Membership ↔ NursingHome；租户拥有 Patient、Prescription、Device、Session、Step、AuditLog 和 Settings。
同一 Session 使用明确的处方版本快照；目标计划与实际执行分开保存。
本地 SQLite 承担缓存、当前 Session 和 Outbox，云端 PostgreSQL 承担跨设备权威记录。
审计需要 actor、tenant、device、session/step、时间、动作、结果、关联事件 ID；不能只保留最终状态。
详见 [租户隔离](MULTI_TENANCY.md)和[离线同步](OFFLINE_SYNC.md)。

## 部署与当前实现

Docker Compose 仅用于本地 PostgreSQL + API 存活检查；生产 Nginx/HTTPS、备份、凭证管理和发布流水线待实现。
没有鉴权和业务 API 时不对外提供真实患者数据。
初始化的 CI 只验证骨架，不替代租户、断网、协议和真实机器验收。

参考：[FastAPI](https://fastapi.tiangolo.com/tutorial/first-steps/)、[Flutter CLI](https://docs.flutter.dev/reference/flutter-cli)。
