"""Read-only V1 inventory. PostgreSQL import is not implemented yet."""
import argparse
import json
import sqlite3
from pathlib import Path

TABLES = ("users", "patients", "pill_boxes", "prescriptions", "system_settings", "dispense_logs", "operation_logs")


def inventory(path: Path) -> dict:
    uri = path.resolve(strict=True).as_uri() + "?mode=ro"
    with sqlite3.connect(uri, uri=True) as connection:
        connection.execute("PRAGMA query_only = ON")
        present = {row[0] for row in connection.execute("SELECT name FROM sqlite_master WHERE type='table'")}
        return {
            "mode": "inventory_only",
            "tables": {
                table: {
                    "rows": connection.execute(f'SELECT COUNT(*) FROM "{table}"').fetchone()[0],
                    "columns": [row[1] for row in connection.execute(f'PRAGMA table_info("{table}")')],
                } if table in present else {"missing": True}
                for table in TABLES
            },
        }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path, help="Path to a backed-up V1 SQLite database")
    args = parser.parse_args()
    print(json.dumps(inventory(args.source), ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
