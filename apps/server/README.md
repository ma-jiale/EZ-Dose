# Mdis API

Python 3.11+；当前只有 GET /health/live，数据库、鉴权和业务模块尚未实现。

```sh
python -m venv .venv
# Linux: source .venv/bin/activate
# PowerShell: .venv\Scripts\Activate.ps1
python -m pip install -e ".[dev]"
python -m pytest
python -m uvicorn app.main:app --reload
```

从本目录运行。core/api/modules/db 划分职责，避免将业务堆入 main.py。
Alembic 目录是下一阶段入口；模型未确定前不生成空数据库迁移来冒充已实现的 schema。
