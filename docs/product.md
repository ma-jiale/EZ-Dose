# Product Specification & Roadmap

## Why Mdis Exists

养老院护理人员需要按照患者、处方、日期、早/午/晚等时段准备未来数天药物。
Mdis（前身 EZ-Dose）将计算、确认投药、硬件执行分工明确，降低人工整理和核对负担。
V1 已建立 Unity Windows 串口客户端和 Flask/SQLite 后端，Android 蓝牙作为兼容实现存在。
V2 的核心任务是产品化与工程重构，不重新发明已验证的摆药逻辑。

## Product Boundary & Core Flows

- 面向多家养老院（多租户体系），Windows 与 Android 平板为一等支持平台，预留 iPad 扩展。
- 一级导航：工作台、患者、记录、设置。主流程围绕患者展开，不向护理人员暴露底层批处理（Batch）技术概念。
- 工作台列出待摆药、进行中、待补药、已完成；患者卡片展示床号、日期范围、天数、药物数和明确操作入口。
- Session 页面清晰展示：患者、当前药物、规格、需投入片数、日期/时段分配矩阵、分药机状态、进度和下一项药物。

## Business State Model

- 模型层级：`Patient` → `DispensingSession` → `MedicationStep[]`。
- Session 保存患者、操作人、设备、日期范围、处方快照；Step 保存药物、目标分配和实际执行证据。
- Session 状态流转：`pending` → `in_progress` → `completed` / `pending_supplement` / `failed`。
- 暂停（Paused）是执行子状态，不等于完成。跳过的 Step 标记为 `skipped_pending`，补药确认后重新执行；全部必要 Step 完成才能将 Session 标记为完成。
- 通信结果不明确或硬件反馈异常时，系统进入人工核对与恢复流程，不自动标记成功，也绝不盲目重放下药命令。
- `pending_sync` / `synced` 等同步状态完全独立于业务状态（例如 `completed + pending_sync` 为合法组合）。

## Nine-Stage Implementation Plan

1. **保存 V1 源码基线与历史**：已完成 Tag 冻结与 subtree 导入；稳定性需真实设备回归确认。
2. **规范化文档与仓库收口**：整理产品、架构、设计、协议与迁移文档，冻结 V2 monorepo 结构。
3. **FastAPI 服务端基础设施**：实现 NursingHome / User / Membership / Patient / Prescription / Device 模型与租户上下文。
4. **SQLite 历史数据迁移**：一次性导入 PostgreSQL，含默认养老院归属、数据校验、备份和重跑策略。
5. **Flutter 设计系统与基础组件**：完成色彩、字体、间距规范与基础 UI 组件库。
6. **客户端业务流程实现**：在 Mock API / Mock Machine 环境下完成患者工作台、Session、暂停、跳过与补药。
7. **客户端真实硬件与 API 接入**：优先接入 Windows Serial COM，次接入 Bluetooth HC-06。
8. **可靠性与合规加固**：本地事务、Outbox、幂等同步、断电断网恢复、审计日志与租户隔离测试。
9. **养老院试点与部署**：真实养老院 Beta 测试、性能与交互优化、生产部署。

## Beta Acceptance Criteria

- 养老院 A 登录后只可访问 A 的患者与处方；正确计算日期与药物分配。
- 真实 STM32 硬件精准执行并反馈，支持暂停、跳过与补药。
- 断网情况下仍能完成已在本地准备就绪的当前任务；本地记录可靠持久化，恢复网络后幂等同步。
- PostgreSQL 保留完整 Session / Step / Audit Log。
- 养老院 B 无法通过任意资源 ID、导出或同步接口访问养老院 A 数据。
- 单纯页面完成或健康检查通过不等于 Beta 完成。

## Open Items & Decisions

- 睡前时段与备用矩阵行的硬件映射规则。
- 半片/非整数剂量在物理分药中的处理策略。
- 跨周（大于 7 天）任务的自动拆分方案。
- 药盒物理规格检测与容错。
- 离线授权期限与凭据刷新机制。
- 远端处方变更与本地执行中快照的冲突合并策略。
