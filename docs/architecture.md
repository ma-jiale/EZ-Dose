# Architecture

## System Boundary

- **硬件边界**：Flutter 客户端通过本地 `MachineService` 与 STM32 分药机通信（Windows COM 串口为主线，Android HC-06 蓝牙为兼容传输）。硬件控制逻辑完全发生在客户端本地，禁止通过互联网中转机器实时控制指令。
- **云端边界**：Flutter 客户端通过 HTTPS 与 FastAPI 后端交互。云端统一管理养老院、用户、患者、处方、设备绑定、任务汇总、审计日志与数据同步。
- **网络容灾**：已在本地下载并确认就绪的 Dispensing Session 不受云端网络中断影响，断网期间仍能完整执行并本地落盘。

## Client Architecture

- **技术栈**：Flutter (Windows x64 / Android Pad)；目标引入 Riverpod、go_router、Dio、Freezed / json_serializable、Drift (SQLite) 与 Flutter Secure Storage。
- **分层规范**：
  - `MachineService`：负责与底层硬件交互，封装通信传输通道（Serial / Bluetooth）、`ProtocolCodec`（命令编解码与校验）与分药流程状态机。
  - `MockMachine`：与真实硬件传输实现共享相同接口契约，便于离线 UI 测试与自动化回归。
  - `Offline Store & Outbox`：本地 Drift SQLite 存储，承载离线缓存、当前执行中的 Session 以及发件箱队列。

## Server Architecture

- **技术栈**：FastAPI + SQLAlchemy 2.x + Alembic + PostgreSQL。
- **分层规范**（位于 `apps/server/app`）：
  - `core/`：系统配置、密钥管理、加密算法、通用安全工具。
  - `db/`：SQLAlchemy Session 工厂、基类模型与连接池配置。
  - `api/`：HTTP 路由边界、请求与响应 Schema、异常拦截与 TenantContext 依赖注入。
  - `modules/`：按业务领域划分（auth、nursing_homes、patients、prescriptions、dispensing、devices、audit 等），各模块内聚 schema、service 与 repository。
- **API-first**：服务端仅提供标准 RESTful JSON API，V1 的旧 Flask 模板渲染页面归入 legacy。

## Multi-tenancy

- **租户模型**：采用 `User` ↔ `Membership` ↔ `NursingHome` 关系。用户可属于多家养老院，具体角色与权限定义在 `Membership` 上，禁止在 `users` 表直接绑定单机构 ID。
- **TenantContext 派生**：
  - 客户端在请求头中携带目标养老院标识（例如 `X-Tenant-Id`），仅作为请求意图。
  - 服务端在认证后，必须根据当前登录用户的 Membership 关系鉴权派生出可信的 `TenantContext`。
  - 严禁服务端无条件信任客户端传入的机构 ID。
- **数据隔离与防御**：
  - 所有租户拥有的业务表（Patient, Prescription, Device, Session, Step, AuditLog, Settings）均包含 `nursing_home_id`。
  - 查询、插入、更新、软删除必须自动绑定 `TenantContext.nursing_home_id`。
  - 患者编号（`patient_code`）与设备序列号以机构为范围唯一，采用复合唯一约束 `(nursing_home_id, code)`。
  - 越权测试覆盖增删改查、批量导出、离线同步、附件访问及历史搜索。
  - PostgreSQL 行级安全策略（RLS）可作为底层深度防御，但不能替代 API 层授权拦截。

## Offline Operation & Sync

- **前置就绪**：在开始分药任务前，客户端必须完整下载并校验当前患者资料、处方版本快照、日期分配矩阵及本地授权凭证。
- **有限离线**：离线模式仅允许继续已准备好的本地 Session，不支持在离线状态下任意新建跨机构患者或开具新处方。
- **本地事务与发件箱（Outbox）**：
  - 物理动作执行前持久化意图，收到硬件反馈后持久化结果。
  - 执行状态记录、不可变事件日志与 Outbox 发件项在同一个本地事务内落盘。
  - 每个事件具备全局唯一 `event_id` (UUIDv7)、`tenant_id`、`device_id`、`session_id`、`step_id`、序列号与精确时间戳。
- **幂等同步**：
  - 服务端在 `(nursing_home_id, event_id)` 上建立唯一索引实现幂等插入。
  - 传输采用至少一次（At-least-once）语义，客户端仅在获得服务端确认响应后才将事件标记为 `synced`。
  - 网络重试支持指数退避与抖动；若因权限失效或凭证过期，需停止静默重试并提示重新鉴权。
- **物理执行不可重入原则**：
  - 断电可能发生在物理下药与结果落盘之间。
  - 恢复供电后若下药结果不明确，必须引导人工核对实体药盒与设备状态，严禁盲目自动重放下药指令。

## Data Authority

- **云端 PostgreSQL**：跨设备、跨机构的全局权威数据中心，保留不可篡改的历史 Session、执行 Step、参数变更与完整审计记录（Audit Log）。
- **客户端本地 SQLite**：充当读缓存、当前工作 Session 运行时状态树与离线 Outbox 队列。
- **处方快照不可变性**：分药任务初始化时生成不可变的处方版本快照。执行期间远端处方变更不得直接静默覆盖本地进行中的 Session。
- **审计要求**：审计日志必须包含操作主体（actor）、机构（tenant）、设备（device）、关联任务（session/step）、时间戳、动作类型、前置/后置状态及相关事件 ID。
