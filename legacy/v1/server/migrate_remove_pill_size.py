"""
数据库迁移脚本：移除 pill_size_area，新增 motor_speed / servo_angle
用法：python migrate_remove_pill_size.py [数据库路径]
默认路径：data/ezdose.db
"""
import sqlite3
import sys
import os
import shutil
from datetime import datetime

def migrate(db_path="data/ezdose.db"):
    if not os.path.exists(db_path):
        print(f"[提示] 数据库文件 {db_path} 不存在，跳过迁移。")
        return

    # 1. 备份
    backup = f"{db_path}.backup_{datetime.now().strftime('%Y%m%d_%H%M%S')}"
    shutil.copy2(db_path, backup)
    print(f"[备份] 已创建数据库备份: {backup}")

    conn = sqlite3.connect(db_path)
    conn.row_factory = sqlite3.Row
    cur = conn.cursor()

    # 2. 检查当前列
    cur.execute("PRAGMA table_info(prescriptions)")
    columns = {row[1] for row in cur.fetchall()}

    # 3. 新增 motor_speed / servo_angle（如果不存在）
    if 'motor_speed' not in columns:
        cur.execute("ALTER TABLE prescriptions ADD COLUMN motor_speed REAL")
        print("[新增列] motor_speed")
    if 'servo_angle' not in columns:
        cur.execute("ALTER TABLE prescriptions ADD COLUMN servo_angle REAL")
        print("[新增列] servo_angle")

    # 4. 移除 pill_size_area（通过重建表实现）
    if 'pill_size_area' in columns:
        cur.execute("PRAGMA table_info(prescriptions)")
        all_cols = [row[1] for row in cur.fetchall() if row[1] != 'pill_size_area']
        cols_str = ', '.join(all_cols)

        cur.execute(f"CREATE TABLE prescriptions_new AS SELECT {cols_str} FROM prescriptions")
        cur.execute("DROP TABLE prescriptions")
        cur.execute("ALTER TABLE prescriptions_new RENAME TO prescriptions")
        print("[移除列] pill_size_area")

    conn.commit()
    conn.close()
    print("[完成] 数据库迁移成功！")

if __name__ == "__main__":
    script_dir = os.path.dirname(os.path.abspath(__file__))
    default_db = os.path.join(script_dir, "data", "ezdose.db")
    path = sys.argv[1] if len(sys.argv) > 1 else default_db
    migrate(path)
