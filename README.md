# Mdis

Medication Dispensing System, formerly EZ-Dose.

面向养老院与康养机构的智能摆药管理系统：系统负责计算与引导，护理人员负责确认与投药，分药机负责物理执行。

## Structure

```text
apps/       V2 software (Flutter client & FastAPI server)
hardware/   Current hardware tools, machine engineering, and analysis
docs/       Current specifications, architecture, protocol, and migration guides
legacy/     Historical product generations (V0 prototypes & validated V1)
```

## Current Generation

**V2**:
- **Client**: Flutter (Windows x64 / Android Pad)
- **Server**: FastAPI + PostgreSQL
- **Hardware Protocol**: Local Machine Control via Serial (115200 / 8N1) & Bluetooth

> 历史 V1（Unity 客户端 + Flask/SQLite 服务端）已完整保留并归档在 [`legacy/v1/`](legacy/v1/README.md) 作为行为参考基线，不可在 V2 开发中直接修改。

## Documentation

- [Product Specification & Roadmap](docs/product.md)
- [Architecture & Multi-Tenancy](docs/architecture.md)
- [Design Principles & System](docs/design.md)
- [STM32 Machine Protocol](docs/protocol.md)
- [Migration Guide & Baselines](docs/migration.md)

## Development

### Client

```bash
cd apps/client
flutter pub get
flutter run -d windows
```

### Server

```bash
cd apps/server
python -m pip install -e ".[dev]"
python -m pytest
python -m uvicorn app.main:app --reload --port 8000
```

### Docker

```bash
cp .env.example .env
docker compose up --build
```

存活检查端点：`http://127.0.0.1:8000/health/live`
