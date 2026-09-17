# Monorepo 初始化校验

日期：2026-09-17。

- Flutter 3.47.1 / Dart 3.13.1：pub get、analyze、test 成功，1 个启动测试通过。
- FastAPI：临时隔离 Python 3.13 环境，1 个存活检查测试通过。依赖产生弃用警告，不影响本次结果。
- SQLite 盘点：临时数据库计数与缺表检测正确，文件 SHA-256 前后不变；不存在的路径不会创建文件。
- Python、YAML 与 Notebook 语法校验通过。
- Git tree 校验：迁移后的 Unity 与基线 unity 子树相同；Flask 与原始仓库基线根树相同。
- 旧 README.md、README_EN.md、Agent.md 的 blob 与原基线相同。
- Flutter lib/main.dart 与 Windows runner.exe.manifest 已纳入版本管理。
- 本地 OpenCV SDK、数据库、缓存未新增纳入提交。

未验证：Docker Compose 运行（本机未安装 Docker）、Windows/Android 发布构建、Unity V1 构建、真实硬件。
V1 Flask 回归未运行：当前没有其约定的 pill-dispenser 环境；此轮以源码树完全一致验证导入未改写业务。
CI 已配置但尚未在远端执行；integration-ci 是 Compose 启动检查，不是摆药业务端到端测试。
