# EZ-Dose 服务器端

EZ-Dose养老院分药给药管理系统的后端服务器，基于Flask框架开发，使用SQLite数据库存储数据，为移动端护工应用、分药机控制软件和处方管理软件提供API服务，同时提供Web管理后台。

## 🏗️ 系统架构

```
EZ-Dose Server
├── 分药机API接口     # 设备端数据同步
├── 护工移动端API     # 手机App数据接口  
├── Web管理后台       # 浏览器管理界面
└── SQLite数据库      # 数据持久化存储
```

## 📋 核心功能

### 🔧 分药机系统API
- `GET /packer/patients` - 获取所有患者信息
- `GET /packer/prescriptions` - 获取所有有效处方数据
- `POST /packer/patients/upload` - 批量上传患者信息
- `POST /packer/prescriptions/upload` - 批量上传处方数据
- `POST /packer/dispense` - 记录发药日志
- `GET /packer/dispense_logs` - 获取发药记录

### 🌐 Web管理后台
- `/admin` - 管理后台首页（含统计数据）
- `/admin/users` - 用户管理
- `/admin/patients` - 患者管理
- `/admin/prescriptions` - 处方管理
- `/admin/dispense_logs` - 发药记录查看

## 🛠️ 技术栈

- **Web框架**: Flask
- **数据库**: SQLite
- **密码安全**: Werkzeug (密码哈希)
- **文件处理**: Werkzeug (安全文件上传)

## 📁 项目结构

```
server/
├── main.py                    # 主程序入口
├── data/
│   └── ezdose.db             # SQLite数据库文件
├── static/
│   ├── styles.css            # 样式文件
│   └── images/               # 患者照片存储
└── templates/
    ├── base.html             # 基础模板
    ├── dashboard.html        # 首页仪表板
    ├── users.html            # 用户列表
    ├── user_form.html        # 用户表单
    ├── patients.html         # 患者列表
    ├── patient_form.html     # 患者表单
    ├── prescriptions.html    # 处方列表
    ├── prescription_form.html # 处方表单
    └── dispense_logs.html    # 发药记录
```

## 🗃️ 数据库设计

### users 表 - 系统用户
| 字段 | 类型 | 说明 |
|------|------|------|
| id | INTEGER | 主键，自增 |
| username | TEXT | 用户名，唯一 |
| password_hash | TEXT | 密码哈希值 |
| name | TEXT | 姓名 |
| can_edit_users | INTEGER | 用户管理权限 (0/1) |
| can_edit_patients | INTEGER | 患者管理权限 (0/1) |
| can_edit_prescriptions | INTEGER | 处方管理权限 (0/1) |
| created_at | DATETIME | 创建时间 |

### patients 表 - 患者信息
| 字段 | 类型 | 说明 |
|------|------|------|
| id | INTEGER | 主键，自增 |
| patient_name | TEXT | 患者姓名 |
| bed_number | TEXT | 床号 |
| profile_photo_resource_id | TEXT | 照片文件名 |
| created_at | DATETIME | 创建时间 |

### prescriptions 表 - 处方信息
| 字段 | 类型 | 说明 |
|------|------|------|
| id | INTEGER | 主键，自增 |
| patient_id | INTEGER | 患者ID (外键) |
| medicine_name | TEXT | 药品名称 |
| morning_dosage | REAL | 早餐剂量 |
| noon_dosage | REAL | 午餐剂量 |
| evening_dosage | REAL | 晚餐剂量 |
| meal_timing | TEXT | 用餐时机 (before_meal/after_meal/with_meal) |
| start_date | DATE | 开始日期 |
| duration_days | INTEGER | 持续天数 |
| last_dispensed_expiry_date | DATE | 最后发药有效期 |
| is_active | INTEGER | 是否有效 (0/1) |
| pill_size | TEXT | 药片大小 (S/M/L) |
| image_resource_id | TEXT | 药品图片 |
| created_at | DATETIME | 创建时间 |

### dispense_logs 表 - 发药记录
| 字段 | 类型 | 说明 |
|------|------|------|
| id | INTEGER | 主键，自增 |
| dispense_date | DATE | 发药日期 |
| patient_id | INTEGER | 患者ID (外键) |
| prescription_id | INTEGER | 处方ID (外键) |
| medicine_name | TEXT | 药品名称 |
| dosage | REAL | 发药剂量 |
| time_period | TEXT | 时段 (morning/noon/evening) |
| dispensed_by_user_id | INTEGER | 操作用户ID (外键) |
| created_at | DATETIME | 记录时间 |

## 🚀 快速开始

### 环境要求
- Python 3.7+
- Flask
- Werkzeug

### 安装步骤

1. **安装依赖**
```bash
pip install flask werkzeug
```

2. **配置部署环境**
编辑 `main.py` 第17-18行：
```python
# 本地开发
URL_PREFIX = ''

# 远程部署时取消下面注释
# URL_PREFIX = '/flask'
```

3. **启动服务器**
```bash
python main.py
```

服务器将在 `http://localhost:5050` 启动

数据库文件将自动创建在 `data/ezdose.db`

## 🔍 故障排除

### 常见问题
1. **数据库锁定**: 确保没有其他进程访问数据库文件
2. **端口冲突**: 修改端口号（默认5050）
3. **权限问题**: 确保对data和static目录有写权限

### 日志查看
服务器运行时会在控制台输出详细日志，包括：
- API请求记录
- 数据库操作状态
- 错误信息详情

## 📞 技术支持

欢迎提issue给我！