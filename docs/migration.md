# Migration Guide

## V1 Baselines

- **基线冻结时间**：2026-09-17。
- **产品命名演进**：原产品名称 EZ-Dose，V2 起正式升级为 **Mdis**。历史代码与固件保留 EZ-Dose 命名。
- **基线 Tag 与代码版本**：
  | 组件 | 原仓库与分支 | 原始 Commit SHA | 基线 Tag | 当前归档路径 |
  | :--- | :--- | :--- | :--- | :--- |
  | **Unity 客户端** | `ma-jiale/EZ-Dose` (`main`) | `cf19c66d` | `mdis-v1-unity-baseline` | `legacy/v1/client/` |
  | **Flask 后端** | `ma-jiale/nursing-rx` (`feature/ux-improvement`) | `d37f2d8d` | `mdis-v1-flask-baseline` | `legacy/v1/server/` |

- **历史完整性保障**：
  - Flask 原仓库完整历史已通过 `git subtree add` 导入至 `legacy/v1/server/`（导入提交 `f574154`），保留了原始提交历史与 SHA。
  - 两个基线 Tag 均已推送到远端 Git 仓库。
- **如何通过 Tag 检出完整 V1 环境**（推荐使用 `git worktree`，避免污染当前工作区）：
  ```bash
  # 检出纯净的 V1 Unity 工程
  git worktree add ../mdis-unity-v1 mdis-v1-unity-baseline

  # 检出纯净的 V1 Flask 后端工程
  git worktree add ../mdis-flask-v1 mdis-v1-flask-baseline
  ```

## Repository Migration

本次仓库从多代异构原型演进为统一的标准 Monorepo，目录演进关系如下：

- **V0 阶段**：`99_archive/` 整体归档至 `legacy/v0/`，包含早期的 PyQt GUI（dispensing-gui、rx-manager-gui）、原生 Android nurse-app 与第一代测试服务端。
- **V1 阶段**：`legacy/v1/client-unity` 重命名为 `legacy/v1/client/`，`legacy/v1/server-flask` 重命名为 `legacy/v1/server/`。目录名不再承载技术栈名称，其技术选型（Unity 与 Flask + SQLite）记录在 `legacy/v1/README.md` 中。
- **硬件与实验**：`analysis/` 移入 `hardware/analysis/`，涵盖脉冲记录分析、Jupyter Notebook 与参数标定脚本。
- **基础设施收口**：`infra/docker/server.Dockerfile` 迁移至 `apps/server/Dockerfile`，删除占位空壳目录，并在 `docker-compose.yml` 中直接引用。
- **V2 现代化**：新业务开发仅集中于 `apps/client`（Flutter）与 `apps/server`（FastAPI + PostgreSQL）。

## V1 → V2 Data Migration

### 数据表映射设计（待实现）

| V1 (SQLite) 表名 | V2 (PostgreSQL) 目标模型 | 迁移注意事项 |
| :--- | :--- | :--- |
| `users` + 权限字段 | `User` + `Membership` | 绑定至默认养老院，将原本扁平的权限字段转化为 Membership 角色 |
| `patients` | `Patient` | 保留原始 `id` 映射，`patient_code` 严格保留 6 位补零文本格式（如 `000001`） |
| `pill_boxes` | `PillBox` | 关联所属机构，保留 RFID UID 与当前绑定患者状态 |
| `prescriptions` | `Prescription` | 保留用药时段、有效日期区间、药品规格及脉宽校准参数 |
| `dispense_logs` | `DispenseLog` | 历史归档发药记录，不回溯虚构不存在的 V2 Session / Step 明细 |
| `operation_logs` | `AuditLog` | 历史系统审计日志，统一导入为标准审计条目 |
| `system_settings` | `TenantSettings` | 归属默认机构设置，明确设备通用参数与机构参数边界 |

### 迁移原则与执行流程

1. **备份与盘点**：在离线环境下完整备份生产 SQLite 数据库，使用专用盘点工具核对数据表结构、行数、外键有效性与异常空值。
2. **脱敏与隔离**：生产患者敏感数据严禁直接提交至 Git 仓库。
3. **事务导入与幂等重跑**：
   - 编写确定性迁移脚本，提供 `--dry-run` 预览模式与详细核对报告。
   - 所有导入操作包裹在数据库事务中，遇到异常自动回滚。
   - 保留 V1 原始 ID 到 V2 UUID 的映射对照表，确保多次重跑具有幂等性。
4. **凭据处理**：密码哈希必须验证兼容性（PBKDF2/Argon2/bcrypt），对于无法平滑过渡的旧凭证，需提供安全的首次登录重置机制。

## Validation

Monorepo 初始化与结构收口经过以下最小基线验收：
1. **客户端测试**：
   ```bash
   cd apps/client
   flutter pub get
   flutter analyze
   flutter test
   ```
2. **服务端测试**：
   ```bash
   cd apps/server
   python -m pytest
   ```
3. **容器与环境检查**：
   ```bash
   docker compose build
   ```
4. **基线与路径核验**：
   - 确认 `mdis-v1-unity-baseline` 与 `mdis-v1-flask-baseline` Tag 完好可用。
   - 全仓库扫描无残留过时硬编码路径。
