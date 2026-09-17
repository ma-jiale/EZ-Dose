"""
安全数据库迁移脚本：移除 pill_size_area，新增 motor_speed / servo_angle
用法：python migrate_remove_pill_size_safe.py [数据库路径]
默认路径：ez_dose.db (或者你在服务器上的 database.db 路径)
"""
import sqlite3
import sys
import os
import shutil
from datetime import datetime

def migrate(db_path="ez_dose.db"):
    if not os.path.exists(db_path):
        print(f"[错误] 数据库文件 {db_path} 不存在！请检查路径。")
        return

    # 1. 创建备份
    backup = f"{db_path}.safe_backup_{datetime.now().strftime('%Y%m%d_%H%M%S')}"
    shutil.copy2(db_path, backup)
    print(f"[备份] 已创建数据库备份: {backup}")

    conn = sqlite3.connect(db_path)
    conn.row_factory = sqlite3.Row
    cur = conn.cursor()

    try:
        # 检查是否还需要迁移 (是否还有 pill_size_area)
        cur.execute("PRAGMA table_info(prescriptions)")
        old_columns = {row[1] for row in cur.fetchall()}
        
        if 'pill_size_area' not in old_columns:
            print("[提示] 数据库中已没有 pill_size_area 字段，似乎已经迁移过了。")
            if 'id' in old_columns:
                # 检查 id 是否有自增约束
                cur.execute("SELECT sql FROM sqlite_master WHERE type='table' AND name='prescriptions'")
                schema = cur.fetchone()[0]
                if 'AUTOINCREMENT' not in schema.upper():
                    print("⚠️ 警告：当前的 prescriptions 表丢失了 AUTOINCREMENT 约束！这是之前的错误脚本导致的。")
                    print("👉 建议：请先用你之前的【健康数据库备份】覆盖当前的数据库，然后再运行本脚本。")
            return

        print("[开始] 正在执行无损结构迁移...")

        # 2. 禁用外键检查，开始事务
        cur.execute("PRAGMA foreign_keys=OFF")
        conn.commit()  # 提交之前的任何操作
        
        # 3. 将旧表改名
        cur.execute("ALTER TABLE prescriptions RENAME TO prescriptions_old")
        
        # 4. 严格按照 main.py 中的原始定义创建新表，绝不丢失任何主键和外键约束！
        cur.execute('''
            CREATE TABLE prescriptions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                patient_id TEXT NOT NULL,
                medicine_name TEXT NOT NULL,
                morning_dosage REAL DEFAULT 0,
                noon_dosage REAL DEFAULT 0,
                evening_dosage REAL DEFAULT 0,
                meal_timing TEXT,
                start_date DATE NOT NULL,
                duration_days INTEGER NOT NULL,
                last_dispensed_expiry_date DATE,
                is_active INTEGER DEFAULT 1,
                motor_speed REAL,
                servo_angle REAL,
                image_resource_id TEXT,
                dosage_spec TEXT DEFAULT '',
                created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                FOREIGN KEY (patient_id) REFERENCES patients(id) ON DELETE CASCADE
            )
        ''')
        
        # 5. 从旧表将数据安全迁移到新表
        # 注意：故意漏掉 pill_size_area，并且不对 motor_speed, servo_angle 赋值 (让它们默认为 NULL)
        
        # 找出新旧表中**都有**的共同列
        cur.execute("PRAGMA table_info(prescriptions)")
        new_columns = {row[1] for row in cur.fetchall()}
        
        # 共同列 (排除新加的 motor_speed/servo_angle 和要废除的 pill_size_area)
        common_cols = list(old_columns.intersection(new_columns))
        cols_str = ', '.join(common_cols)
        
        cur.execute(f'''
            INSERT INTO prescriptions ({cols_str})
            SELECT {cols_str} FROM prescriptions_old
        ''')
        
        # 6. 删除旧表
        cur.execute("DROP TABLE prescriptions_old")
        
        # 7. 开启外键检查并提交
        conn.commit()
        cur.execute("PRAGMA foreign_keys=ON")
        
        print(f"[成功] 数据库迁移完美完成！移除了 pill_size_area，安全保留了所有的主键与自增约束！")

    except Exception as e:
        conn.rollback()
        print(f"[失败] 迁移过程中发生错误：{e}")
        print("事务已回滚，您的数据库未受任何破坏。")
    finally:
        conn.close()

if __name__ == "__main__":
    script_dir = os.path.dirname(os.path.abspath(__file__))
    default_db = os.path.join(script_dir, "ez_dose.db")
    
    # 如果用户在命令行传入了指定的数据库路径，则使用用户指定的；否则使用当前目录的 ez_dose.db
    path = sys.argv[1] if len(sys.argv) > 1 else default_db
    migrate(path)
