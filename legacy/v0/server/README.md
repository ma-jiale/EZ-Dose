# EZ-Dose 服务器端

EZ-Dose养老院分药给药管理系统的后端服务器，基于Flask框架开发，为移动端护工应用、分药机控制软件和处方管理软件提供API服务，同时提供Web管理后台。

## 🏗️ 系统架构

```
EZ-Dose Server
├── 分药机API接口     # 设备端数据同步
├── 护工移动端API     # 手机App数据接口  
├── Web管理后台       # 浏览器管理界面
└── 自动排班系统     # 定时任务调度
```

## 📋 核心功能

### 🔧 分药机系统API
- `GET /packer/patients` - 获取所有患者信息
- `GET /packer/prescriptions` - 获取所有处方数据
- `POST /packer/patients/upload` - 批量上传患者信息
- `POST /packer/prescriptions/upload` - 批量上传处方数据

### 📱 护工移动端API
- `POST /login` - 护工/护士登录验证
- `GET /patients` - 获取患者列表（可按护工过滤）
- `GET /schedules` - 获取用药排班（可按护工过滤）
- `GET /tasks` - 获取每日用药任务
- `PUT /task` - 更新任务执行状态
- `GET /timeslots` - 获取用药时间段配置
- `GET /caregivers` - 获取护士列表
- `GET /aunties` - 获取护工列表

### 🌐 Web管理后台
- `/admin` - 管理后台首页
- `/admin/aunties` - 护工管理
- `/admin/caregivers` - 护士管理  
- `/admin/patients` - 患者管理
- `/admin/schedules` - 排班管理
- `/admin/timeslots` - 时间段管理
- `/admin/tasks` - 任务记录查看

### ⏰ 自动排班系统
- 每日定时生成用药排班
- 根据处方有效期自动筛选
- 按用药频次和用餐时机分配时间段
- 可配置的自动执行时间（默认凌晨04:00）

## 🛠️ 技术栈

- **Web框架**: Flask
- **文件处理**: Werkzeug (安全文件上传)
- **定时任务**: Schedule
- **数据存储**: CSV文件
- **编码格式**: UTF-8-SIG (兼容Excel)

## 📁 数据文件结构

```
data/
├── aunties.csv                    # 护工信息
├── caregivers.csv                 # 护士信息
├── patients.csv                   # 患者信息
├── local_prescriptions_data.csv   # 处方数据
├── schedules.csv                  # 排班数据
├── timeslots.csv                  # 时间段配置
├── tasks_YYYY-MM-DD.csv          # 每日任务记录
└── schedule_config.json          # 自动排班时间配置

static/
└── images_patients/               # 患者照片存储
```

## 🚀 快速开始

### 环境要求
- Python 3.7+
- Flask
- Werkzeug  
- Schedule

### 安装步骤

1. **安装依赖**
```bash
pip install flask werkzeug schedule
```

3. **配置部署环境**
编辑 `main_packer.py` 第13-15行：
```python
# 本地开发
URL_PREFIX = ''

# 远程部署时取消下面注释
# URL_PREFIX = '/flask'
```

4. **启动服务器**
```bash
python main_packer.py
```

服务器将在 `http://localhost:5050` 启动


## 🗃️ 数据格式说明

### 患者信息 (patients.csv)
```csv
auntieId,imageResourceId,patientBarcode,patientBedNumber,patientName,patientId
1,img001,BC001,101,张三,P001
```

### 处方信息 (local_prescriptions_data.csv)
```csv
patientId,medicationName,dosagePerDay,mealTiming,startDate,endDate
P001,阿司匹林,2,餐前,2025-09-01,2025-09-30
```

### 任务记录 (tasks_YYYY-MM-DD.csv)
```csv
date,patientId,patientName,timeSlotName,status,auntieId
2025-09-03,P001,张三,早餐前,待执行,1
```

### 排班信息 (schedules.csv)
```csv
patientId,timeSlotName,auntieId
P001,早餐前,1
P001,午餐后,2
P002,晚餐前,1
```

### 时间段配置 (timeslots.csv)
```csv
name,time
早餐前,07:30
早餐后,08:30
午餐前,11:30
午餐后,12:30
晚餐前,17:30
晚餐后,18:30
```

### 护工信息 (aunties.csv)
```csv
auntieId,auntieName,auntiePassword,caregiverId
1,李阿姨,123456,1
2,王阿姨,123456,1
```

### 护士信息 (caregivers.csv)
```csv
caregiverId,caregiverName,caregiverPassword
1,张护士,admin123
2,李护士,admin456
```

### 自动排班配置 (schedule_config.json)
```json
{
    "schedule_time": "04:00"
}
```

## 🔍 故障排除

### 常见问题
1. **CSV文件编码问题**: 确保使用UTF-8-SIG编码
2. **文件路径错误**: 检查相对路径和工作目录
3. **端口冲突**: 修改端口号（默认5050）
4. **权限问题**: 确保对data和static目录有写权限

### 日志查看
服务器运行时会在控制台输出详细日志，包括：
- API请求记录
- 文件操作状态
- 错误信息详情
- 定时任务执行情况

## 📞 技术支持

欢迎提issue给我！