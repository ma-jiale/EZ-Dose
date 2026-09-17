# Mdis V2 产品与 Roadmap

## 为什么存在

养老院护理人员需要按照患者、处方、日期、早/午/晚等时段准备未来数天药物。
Mdis 将计算、确认投药、硬件执行分工明确，降低人工整理和核对负担。
V1 已建立 Unity Windows 串口客户端和 Flask/SQLite 后端，Android 蓝牙作为兼容实现存在。
V2 的任务是产品化，不重新发明已验证的摆药逻辑。

## 产品边界

面向多家养老院，Windows 与 Android Pad 为一等平台，iPad 预留扩展。
一级导航：工作台、患者、记录、设置。主流程围绕患者，不向护理人员暴露内部 Batch 概念。
工作台列出待摆药、进行中、待补药、已完成；患者卡片显示床号、日期范围、天数、药物数和明确操作。
Session 页显示患者、当前药物、规格、需投入片数、日期/时段分配、机器状态、进度和下一药物。

## 业务模型与状态（目标）

Patient → DispensingSession → MedicationStep[]。
Session 保存患者、操作人、设备、日期范围、处方快照；Step 保存药物、目标分配和实际执行证据。
Session：pending → in_progress → completed / pending_supplement / failed。
暂停是执行子状态，不等于完成。跳过的 Step 为 skipped_pending，补药确认后重新执行；全部必要 Step 完成才能完成 Session。
通信结果不明确时进入人工核对/恢复流程，不自动标记成功，也不自动重新下药。
`pending_sync` / `synced` 独立于上述业务状态。

## 九阶段计划

1. 保存 V1 源码基线与历史（本次）；稳定性仍需真实设备回归确认。
2. 建立产品、架构、设计、协议与迁移文档（本次第一版）。
3. FastAPI 基础设施及 NursingHome / User / Membership / Patient / Prescription / Device 模型。
4. SQLite 一次性导入 PostgreSQL，含默认养老院归属、校验、备份和重跑策略。
5. Flutter 设计系统，完成色彩、字体、间距和基础组件。
6. Mock API / Mock Machine 下完成患者工作台、Session、暂停、跳过与补药。
7. 接入 API 和真实硬件，先 Windows Serial，再 Bluetooth。
8. 本地事务、Outbox、幂等同步、恢复、审计、权限和租户隔离测试。
9. 真实养老院 Beta 测试、性能与交互优化、生产部署。

## Beta 验收

养老院 A 登录后只可访问 A 的患者与处方；正确计算日期与药物分配；真实 STM32 执行并反馈；
支持暂停、跳过与补药；断网仍能完成已在本地准备好的当前任务；本地记录可靠保存，恢复网络后同步；
PostgreSQL 保留完整 Session / Step / Audit Log；养老院 B 无法通过任意资源 ID、导出或同步接口访问 A 数据。
页面完成或健康检查通过不等于 Beta 完成。

## 待确认

睡前时段与备用矩阵行的映射、半片/非整数剂量策略、跨周任务拆分、药盒规格、离线授权期限、
远端处方变更与本地快照的冲突策略都需独立确认，不能从目标 UI 推断硬件支持。
