# V1 → Mdis V2 迁移

## 本次仓库迁移

unity/ → legacy/v1/client-unity/；旧 README.md、README_EN.md、Agent.md → legacy/v1/。
Flask 原仓库历史导入 legacy/v1/server-flask/。hardware、images、docs 和 99_archive 保留。
旧 README 与 Agent 原文保留为历史记录，其中相对路径和 Android 主线描述可能过时。
当前运行 Unity 应打开 legacy/v1/client-unity；依赖需按旧文档单独准备。
运行 Flask 时工作目录必须为 legacy/v1/server-flask，避免其相对数据库路径落在主仓库根目录。
analysis 继续位于根目录，其日志路径调整到 legacy/v1/client-unity/Logs。
原始字节可通过两个基线 tag 恢复。新 README 和 AGENTS 为当前入口。

## V1 行为依据

客户端 PrescriptionManager 负责处方过滤、4×7 矩阵、日期推进；MainController 负责分药、跳过和恢复；
DispenserController 与 SerialProtocol 负责命令/反馈；UIManager 负责交互。
Flask main.py 包含 users、patients、pill_boxes、prescriptions、system_settings、dispense_logs、operation_logs。
患者主键为六位零填充 TEXT，不能转成整数后丢失条码语义。
API 含 /packer/patients、/packer/pill-boxes、/packer/prescriptions、上传、dispense、dispense_logs 与 calibration。
需以实际源码而非旧 README 字段表为准；保留校准字段空值/零值上传保护行为并建立兼容测试。

## 数据迁移计划（尚未执行）

| V1 | V2 目标 |
| --- | --- |
| users + 权限标记 | User + 默认养老院 Membership，明确权限映射 |
| patients | Patient，保留 patient_code 与原始 ID 映射 |
| pill_boxes | 租户内药盒绑定，保留 RFID 与有效状态 |
| prescriptions | Prescription，保留时段、日期、规格、校准参数和历史 ID |
| dispense_logs | 历史发药事件，不伪造不存在的 Session/Step 细节 |
| operation_logs | 历史审计来源记录 |
| system_settings | 默认养老院 Settings，检查设备参数归属 |

先备份 SQLite、上传资源和环境配置；在副本上盘点 schema、行数、孤儿引用、无效日期和剂量。
数据库模式冻结后实现目标导入：显式指定默认养老院、原始 ID 映射、事务、幂等重跑、失败报告和行数核对。
旧密码哈希需核对验证算法兼容性或制定重置流程，不能丢弃权限或伪造账号。
先演练，核对抽样患者/处方/历史，再安排停写窗口与最终增量导入；保留 V1 备份用于回退。
生产数据不写入 Git。本次 migrate_v1_sqlite.py 只做只读 schema/计数盘点，不连接 PostgreSQL。
