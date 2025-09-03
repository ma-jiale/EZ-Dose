#######################################################################################
# ！！！注意：现在和auntie相关的代码都是对应护工，和caregiver相关的代码都是对应着护士！！！！#
#######################################################################################

from flask import Flask, jsonify, request, render_template, redirect, url_for
import time
import csv # 导入 Python 内置的 CSV 处理模块
import os
from werkzeug.utils import secure_filename # 导入安全文件名工具
from datetime import datetime, timedelta
import schedule
import threading


# --- 1. 创建 Flask 应用实例 ---
app = Flask(__name__)

# --- URL前缀配置 ---
# 本地开发时设置为空字符串，远程部署时设置为'/flask'
URL_PREFIX = ''  # 本地开发
# URL_PREFIX = '/flask'  # 远程部署时取消注释这行，注释上一行

# --- 护工给药APP的配置文件 ---
UPLOAD_FOLDER = 'static/images_patients'
app.config['UPLOAD_FOLDER'] = UPLOAD_FOLDER

# --- 分药机系统的配置文件 ---#
PRESCRIPTIONS_FILE = 'data/local_prescriptions_data.csv'
PATIENTS_FILE = 'data/patients.csv'

# 确保上传目录存在
os.makedirs(UPLOAD_FOLDER, exist_ok=True)

# 允许的文件扩展名
ALLOWED_EXTENSIONS = {'png', 'jpg', 'jpeg', 'gif', 'bmp', 'webp'}

# ============= 分药机系统需要的函数 ==============#

def read_csv_safe(filename):
    """Safely read CSV file and return data or empty list if file doesn't exist"""
    try:
        if os.path.exists(filename):
            data = []
            # ⭐ 修改：使用 utf-8-sig 自动处理BOM字符
            with open(filename, 'r', encoding='utf-8-sig') as file:
                csv_reader = csv.DictReader(file)
                for row in csv_reader:
                    # ⭐ 清理字段名，去除可能的特殊字符
                    clean_row = {}
                    for key, value in row.items():
                        clean_key = key.strip().replace('\ufeff', '')  # 去除BOM字符
                        clean_row[clean_key] = value
                    data.append(clean_row)
            return data
        else:
            return []
    except Exception as e:
        print(f"Error reading {filename}: {e}")
        return []

def write_csv_safe(filename, data, fieldnames):
    """Safely write data to CSV file"""
    try:
        # ⭐ 使用 utf-8-sig 确保兼容性，同时避免BOM问题
        with open(filename, 'w', newline='', encoding='utf-8-sig') as file:
            writer = csv.DictWriter(file, fieldnames=fieldnames)
            writer.writeheader()
            writer.writerows(data)
        return True
    except Exception as e:
        print(f"Error writing {filename}: {e}")
        return False

def ensure_patient_fields(patient_data):
    """Ensure patient data has all required fields with default values"""
    required_fields = {
        'auntieId': '',
        'imageResourceId': '',
        'patientBarcode': '',
        'patientBedNumber': '',
        'patientName': '',
        'patientId': ''
    }
    
    # Create a new dict with all required fields
    complete_patient = {}
    for field, default_value in required_fields.items():
        complete_patient[field] = patient_data.get(field, default_value)
    
    return complete_patient

# ============= 护工给药APP需要的函数 ==============#

def allowed_file(filename):
    """检查文件扩展名是否被允许"""
    return '.' in filename and \
           filename.rsplit('.', 1)[1].lower() in ALLOWED_EXTENSIONS

# --- 2. 增加读写CSV的辅助函数 ---
# (read_users_from_csv 函数保持不变)

def write_csv_file(filename, data, fieldnames):
    """将字典列表写入指定的CSV文件，会覆盖旧文件。"""
    try:
        with open(filename, mode='w', encoding='utf-8-sig', newline='') as csvfile:
            writer = csv.DictWriter(csvfile, fieldnames=fieldnames)
            writer.writeheader()
            writer.writerows(data)
    except Exception as e:
        print(f"!!! 严重错误: 写入 {filename} 时发生错误: {e}")

def read_csv_file(filename):
    """从指定的CSV文件中读取所有数据，并返回一个字典列表。"""
    data = []
    try:
        with open(filename, mode='r', encoding='utf-8-sig', newline='') as csvfile:
            reader = csv.DictReader(csvfile)
            for row in reader:
                data.append(row)
    except FileNotFoundError:
        print(f"!!! 严重错误: 找不到文件 {filename}！")
    except Exception as e:
        print(f"!!! 严重错误: 读取 {filename} 时发生错误: {e}")
    return data

# 在路由定义之前添加
@app.context_processor
def inject_url_prefix():
    """将 URL_PREFIX 注入到所有模板中"""
    return {'URL_PREFIX': URL_PREFIX}

#############
# 根路径路由 #
#############
@app.route('/', methods=['GET'])
def index():
    return jsonify({
        "message": "Flask服务器运行正常！",
        "timestamp": time.time(),
        "available_endpoints": [
            "GET / - 服务器状态",
            "POST /login - 用户登录"
        ]
    })

######################
# 分药机系统使用的API #
######################

@app.route('/packer/patients', methods=['GET'])
def get_patients_for_dispensing():
    """Get all patients"""
    patients = read_csv_safe(PATIENTS_FILE)
    return jsonify({
        "success": True,
        "data": patients,
        "count": len(patients)
    })

@app.route('/packer/prescriptions', methods=['GET'])
def get_prescriptions_for_dispensing():
    """Get all prescriptions"""
    prescriptions = read_csv_safe(PRESCRIPTIONS_FILE)
    return jsonify({
        "success": True,
        "data": prescriptions,
        "count": len(prescriptions)
    })

@app.route('/packer/patients/upload', methods=['POST'])
def upload_patients_for_dispensing():
    """Upload multiple patients (replaces existing data)"""
    try:
        data = request.get_json()
        
        if not data or 'patients' not in data or not isinstance(data['patients'], list):
            return jsonify({
                "success": False,
                "message": "Invalid data format. Expected: {'patients': [...]}"
            }), 400
        
        patients = data['patients']

        # Validate each patient and ensure required fields
        validated_patients = []
        for i, patient in enumerate(patients):
            if not isinstance(patient, dict):
                return jsonify({
                    "success": False,
                    "message": f"Invalid patient data at index {i}. Expected dictionary."
                }), 400
            
            # Check for required fields (patientName and patientId are mandatory)
            if 'patientName' not in patient or 'patientId' not in patient:
                return jsonify({
                    "success": False,
                    "message": f"Invalid patient data at index {i}. Required: patientName, patientId"
                }), 400
            
            # Ensure all fields are present with default values
            complete_patient = ensure_patient_fields(patient)
            validated_patients.append(complete_patient)
        
        # Write to CSV with all 6 fields
        fieldnames = ['auntieId', 'imageResourceId', 'patientBarcode', 'patientBedNumber', 'patientName', 'patientId']
        if write_csv_safe(PATIENTS_FILE, validated_patients, fieldnames):
            return jsonify({
                "success": True,
                "message": f"Successfully uploaded {len(validated_patients)} patients",
                "count": len(validated_patients)
            })
        else:
            return jsonify({
                "success": False,
                "message": "Failed to upload patients"
            }), 500
            
    except Exception as e:
        return jsonify({
            "success": False,
            "message": f"Error uploading patients: {str(e)}"
        }), 500

@app.route('/packer/prescriptions/upload', methods=['POST'])
def upload_prescriptions_for_dispensing():
    """Upload multiple prescriptions (replaces existing data)"""
    try:
        data = request.get_json()
        
        if not data or 'prescriptions' not in data or not isinstance(data['prescriptions'], list):
            return jsonify({
                "success": False,
                "message": "Invalid data format. Expected: {'prescriptions': [...]}"
            }), 400
        
        prescriptions = data['prescriptions']
        
        # Validate each prescription
        required_fields = ['patient_name', 'patientId', 'medicine_name', 'morning_dosage', 
                          'noon_dosage', 'evening_dosage', 'meal_timing', 'start_date', 
                          'duration_days', 'pill_size']
        
        for i, prescription in enumerate(prescriptions):
            if not isinstance(prescription, dict):
                return jsonify({
                    "success": False,
                    "message": f"Invalid prescription data at index {i}"
                }), 400
            
            missing_fields = [field for field in required_fields if field not in prescription]
            if missing_fields:
                return jsonify({
                    "success": False,
                    "message": f"Missing fields in prescription {i}: {', '.join(missing_fields)}"
                }), 400
        
        # Add default values for optional fields
        for prescription in prescriptions:
            prescription.setdefault('rfid', '')
            prescription.setdefault('last_dispensed_expiry_date', '')
            prescription.setdefault('is_active', 1)
        
        # Write to CSV (replaces existing file)
        fieldnames = ['patient_name', 'patientId', 'rfid', 'medicine_name', 'morning_dosage',
                     'noon_dosage', 'evening_dosage', 'meal_timing', 'start_date',
                     'duration_days', 'last_dispensed_expiry_date', 'is_active', 'pill_size']
        
        if write_csv_safe(PRESCRIPTIONS_FILE, prescriptions, fieldnames):
            return jsonify({
                "success": True,
                "message": f"Successfully uploaded {len(prescriptions)} prescriptions",
                "count": len(prescriptions)
            })
        else:
            return jsonify({
                "success": False,
                "message": "Failed to upload prescriptions"
            }), 500
            
    except Exception as e:
        return jsonify({
            "success": False,
            "message": f"Error uploading prescriptions: {str(e)}"
        }), 500

##########################
# 护工系统手机App使用的API #
###########################
@app.route('/patients', methods=['GET'])
def get_patients():
    """返回所有病人的列表，或者根据 auntieId 筛选。"""
    print(f"[{time.ctime()}] App请求 /patients 数据")
    all_patients = read_csv_file('data/patients.csv')
    
    auntie_id = request.args.get('auntieId', type=int)
    if (auntie_id):
        # CSV读出来的值是字符串，所以要和字符串比较
        filtered_patients = [p for p in all_patients if p.get('auntieId') == str(auntie_id)]
        return jsonify(filtered_patients)
        
    return jsonify(all_patients)

@app.route('/timeslots', methods=['GET'])
def get_timeslots():
    """返回所有时间段的列表。"""
    print(f"[{time.ctime()}] App请求 /timeslots 数据")
    timeslots = read_csv_file('data/timeslots.csv')
    return jsonify(timeslots)
    
@app.route('/schedules', methods=['GET'])
def get_schedules():
    """返回所有用药计划，或者根据 auntieId 筛选。"""
    print(f"[{time.ctime()}] App请求 /schedules 数据")
    all_schedules = read_csv_file('data/schedules.csv')    
    auntie_id = request.args.get('auntieId', type=int)
    if auntie_id:
        all_patients = read_csv_file('data/patients.csv')
        her_patient_ids = {p['patientId'] for p in all_patients if p.get('auntieId') == str(auntie_id)}
        filtered_schedules = [s for s in all_schedules if s.get('patientId') in her_patient_ids]
        return jsonify(filtered_schedules)

    return jsonify(all_schedules)


# 为护工数据提供API接口
@app.route('/caregivers', methods=['GET'])
def get_caregivers():
    """返回所有护工的列表。"""
    print(f"[{time.ctime()}] App请求 /caregivers 数据")
    caregivers = read_csv_file('data/caregivers.csv')
    return jsonify(caregivers)

# 为阿姨数据提供API接口
@app.route('/aunties', methods=['GET'])
def get_aunties():
    """返回所有阿姨的列表。"""
    print(f"[{time.ctime()}] App请求 /aunties 数据")
    aunties = read_csv_file('data/aunties.csv')
    return jsonify(aunties)

@app.route('/tasks', methods=['GET'])
def get_tasks():
    """
    获取指定日期的所有任务状态。
    如果当天的任务文件不存在，会自动根据计划创建。
    URL参数: ?date=YYYY-MM-DD
    """
    # 1. 从URL获取日期参数，如果没有，则默认为今天
    date_str = request.args.get('date')
    if not date_str:
        date_str = time.strftime("%Y-%m-%d") # 格式 "2025-08-12"

    print(f"[{time.ctime()}] App请求 {date_str} 的任务数据")
    
    # 2. 根据日期构建文件名
    task_filename = f"data/tasks_{date_str}.csv"
    
    # 3. 检查当天的任务文件是否存在
    if not os.path.exists(task_filename):
        print(f"文件 {task_filename} 不存在，正在根据计划创建...")
        
        # ⭐ 修改：优先读取对应日期的排班文件⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐
        schedule_filename = f"data/schedules_{date_str}.csv"
        all_schedules = read_csv_file(schedule_filename)
        
        if not all_schedules:
            # 如果对应日期的排班文件不存在或为空，尝试自动生成
            print(f"排班文件 {schedule_filename} 不存在或为空，尝试自动生成...")
            try:
                # 调用排班生成函数
                generated_schedules = generate_schedules_for_date(date_str)
                if generated_schedules:
                    print(f"成功自动生成 {len(generated_schedules)} 条排班记录")
                    all_schedules = generated_schedules
                else:
                    print(f"日期 {date_str} 没有有效的处方数据，无法生成排班")
                    return jsonify([])
            except Exception as e:
                print(f"自动生成排班失败: {e}")
                return jsonify([])
        
        # ⭐⭐⭐ 这里是缺失的代码：根据排班创建任务文件 ⭐⭐⭐
        tasks = []
        for schedule in all_schedules:
            task = {
                'patientId': schedule['patientId'],
                'timeSlotName': schedule['timeSlotName'],
                'status': '待服药',
                'completionTime': '',
                'remark': ''
            }
            tasks.append(task)
        
        # 保存任务文件
        fieldnames = ['patientId', 'timeSlotName', 'status', 'completionTime', 'remark']
        write_csv_file(task_filename, tasks, fieldnames)
        print(f"已创建任务文件 {task_filename}，包含 {len(tasks)} 个任务")

    # 4. 读取 (已存在的或刚创建的) 当天的任务文件并返回
    todays_tasks = read_csv_file(task_filename)
    return jsonify(todays_tasks)

@app.route('/task', methods=['PUT']) # 我们用 PUT 表示更新资源
def update_task():
    """
    更新单个任务的状态。
    需要接收包含任务标识和新状态的JSON数据。
    """
    # 1. 从请求体中获取更新数据
    update_data = request.get_json(force=True, silent=True)
    if not update_data:
        return jsonify({"error": "请求体为空或格式错误"}), 400

    # 2. 校验必需的字段是否存在
    required_fields = ['date', 'patientId', 'timeSlotName', 'status']
    if not all(field in update_data for field in required_fields):
        return jsonify({"error": f"请求体缺少必需字段: {required_fields}"}), 400

    date_str = update_data['date']
    task_filename = f"data/tasks_{date_str}.csv"
    
    if not os.path.exists(task_filename):
        return jsonify({"error": f"任务文件 {task_filename} 不存在，无法更新"}), 404

    # 3. 读取整个CSV文件到内存
    all_tasks = read_csv_file(task_filename)
    task_found = False

    # 4. 遍历内存中的任务列表，找到并修改目标任务
    for task in all_tasks:
        if (task.get('patientId') == str(update_data['patientId']) and 
            task.get('timeSlotName') == update_data['timeSlotName']):
            
            print(f"找到了要更新的任务: patientId={task.get('patientId')}, timeSlot={task.get('timeSlotName')}")
            # 更新字段
            task['status'] = update_data['status']
            task['completionTime'] = update_data.get('completionTime', '') # 使用.get()处理可选字段
            task['remark'] = update_data.get('remark', '')
            
            task_found = True
            break # 找到后就停止循环

    # 5. 如果找到了任务，就将修改后的完整列表写回文件
    if task_found:
        fieldnames = ['patientId', 'timeSlotName', 'status', 'completionTime', 'remark']
        write_csv_file(task_filename, all_tasks, fieldnames)
        print("任务更新成功，并已写回CSV文件。")
        return jsonify({"success": True, "message": "任务更新成功"})
    else:
        print("!!! 错误: 尝试更新任务，但在CSV中未找到匹配项。")
        return jsonify({"success": False, "error": "未找到要更新的任务"}), 404
    
##########################
# 护工给药系统后台登陆界面 #
##########################

@app.route('/login', methods=['POST'])
def login():
    auth_data = request.get_json(force=True, silent=True)
    if not auth_data or 'username' not in auth_data or 'password' not in auth_data:
        return jsonify({"error": "请求格式错误，需要 username 和 password"}), 400

    username = auth_data['username']
    password = auth_data['password']
    
    # ⭐ 1. 打印从App接收到的原始数据
    print("="*30)
    print(f"收到登录请求:")
    print(f"  - App发来的Username: '{username}' (类型: {type(username)})")
    print(f"  - App发来的Password: '{password}' (类型: {type(password)})")
    print("="*30)

        # a. 先在 aunties.csv 中查找
    for auntie in read_csv_file('data/aunties.csv'):
        if auntie.get('username') == username and auntie.get('password') == password:
            print(f"阿姨 '{username}' 验证成功！")
            return jsonify({
                "success": True, "userId": int(auntie['auntieId']),
                "role": "auntie", "name": auntie['name']
            })

    # b. 如果不是阿姨，再在 caregivers.csv 中查找
    for caregiver in read_csv_file('data/caregivers.csv'):
        if caregiver.get('username') == username and caregiver.get('password') == password:
            print(f"护工 '{username}' 验证成功！")
            return jsonify({
                "success": True, "userId": int(caregiver['caregiverId']),
                "role": "caregiver", "name": caregiver['name']
            })

    print(f"用户 '{username}' 验证失败！")
    return jsonify({"success": False, "error": "用户名或密码错误"}), 401

################################
# 护工系统Web管理后台的页面路由  #
################################

# --- 首页页面路由  ---
@app.route('/admin')
def admin_dashboard():
    """管理后台首页"""
    return render_template('dashboard.html')

# --- 阿姨管理页面路由  ---
@app.route('/admin/aunties')
def manage_aunties():
    """显示所有阿姨的列表页面"""
    aunties_list = read_csv_file('data/aunties.csv')
    return render_template('aunties.html', aunties=aunties_list)

@app.route('/admin/aunties/add', methods=['GET', 'POST'])
def add_auntie():
    """处理新增阿姨的逻辑"""
    if request.method == 'POST':
        all_aunties = read_csv_file('data/aunties.csv')
        new_auntie = {
            'auntieId': str(int(time.time())), # 用时间戳生成唯一ID
            'name': request.form['name'],
            'username': request.form['username'],
            'password': request.form['password'],
            'caregiverId': request.form['caregiverId']
        }
        all_aunties.append(new_auntie)
        # 写入时需要提供表头
        write_csv_file('data/aunties.csv', all_aunties, fieldnames=['auntieId', 'name', 'username', 'password', 'caregiverId'])
        return redirect(URL_PREFIX + url_for('manage_aunties'))
    return render_template('auntie_form.html', auntie=None)

@app.route('/admin/aunties/edit/<auntie_id>', methods=['GET', 'POST'])
def edit_auntie(auntie_id):
    """处理编辑阿姨的逻辑"""
    all_aunties = read_csv_file('data/aunties.csv')
    auntie_to_edit = next((a for a in all_aunties if a['auntieId'] == auntie_id), None)
    if not auntie_to_edit:
        return "阿姨未找到!", 404

    if request.method == 'POST':
        auntie_to_edit['name'] = request.form['name']
        auntie_to_edit['username'] = request.form['username']
        auntie_to_edit['caregiverId'] = request.form['caregiverId']
        if request.form['password']:
            auntie_to_edit['password'] = request.form['password']
        write_csv_file('data/aunties.csv', all_aunties, fieldnames=['auntieId', 'name', 'username', 'password', 'caregiverId'])
        return redirect(URL_PREFIX + url_for('manage_aunties'))
    
    return render_template('auntie_form.html', auntie=auntie_to_edit)

@app.route('/admin/aunties/delete/<auntie_id>')
def delete_auntie(auntie_id):
    """处理删除阿姨的逻辑"""
    all_aunties = read_csv_file('data/aunties.csv')
    aunties_after_delete = [a for a in all_aunties if a['auntieId'] != auntie_id]
    write_csv_file('data/aunties.csv', aunties_after_delete, fieldnames=['auntieId', 'name', 'username', 'password', 'caregiverId'])
    return redirect(URL_PREFIX + url_for('manage_aunties'))

# --- 护工管理页面路由  ---
@app.route('/admin/caregivers')
def manage_caregivers():
    """显示所有护工的列表页面"""
    caregivers_list = read_csv_file('data/caregivers.csv')
    return render_template('caregivers.html', caregivers=caregivers_list)

@app.route('/admin/caregivers/add', methods=['GET', 'POST'])
def add_caregiver():
    """处理新增护工的逻辑"""
    if request.method == 'POST':
        all_caregivers = read_csv_file('data/caregivers.csv')
        new_caregiver = {
            'caregiverId': str(int(time.time())),
            'name': request.form['name'],
            'username': request.form['username'],
            'password': request.form['password']
        }
        all_caregivers.append(new_caregiver)
        write_csv_file('data/caregivers.csv', all_caregivers, fieldnames=['caregiverId', 'name', 'username', 'password'])
        return redirect(URL_PREFIX + url_for('manage_caregivers'))
    return render_template('caregiver_form.html', caregiver=None)

@app.route('/admin/caregivers/edit/<caregiver_id>', methods=['GET', 'POST'])
def edit_caregiver(caregiver_id):
    """处理编辑护工的逻辑"""
    all_caregivers = read_csv_file('data/caregivers.csv')
    caregiver_to_edit = next((c for c in all_caregivers if c['caregiverId'] == caregiver_id), None)
    if not caregiver_to_edit:
        return "护工未找到!", 404

    if request.method == 'POST':
        caregiver_to_edit['name'] = request.form['name']
        caregiver_to_edit['username'] = request.form['username']
        if request.form['password']:
            caregiver_to_edit['password'] = request.form['password']
        write_csv_file('data/caregivers.csv', all_caregivers, fieldnames=['caregiverId', 'name', 'username', 'password'])
        return redirect(URL_PREFIX + url_for('manage_caregivers'))
    return render_template('caregiver_form.html', caregiver=caregiver_to_edit)

@app.route('/admin/caregivers/delete/<caregiver_id>')
def delete_caregiver(caregiver_id):
    """处理删除护工的逻辑"""
    all_caregivers = read_csv_file('data/caregivers.csv')
    caregivers_after_delete = [c for c in all_caregivers if c['caregiverId'] != caregiver_id]
    write_csv_file('data/caregivers.csv', caregivers_after_delete, fieldnames=['caregiverId', 'name', 'username', 'password'])
    return redirect(URL_PREFIX + url_for('manage_caregivers'))

# --- 患者管理页面路由  ---
@app.route('/admin/patients')
def manage_patients():
    """显示所有患者的列表页面"""
    patients_list = read_csv_file('data/patients.csv')
    return render_template('patients.html', patients=patients_list)

@app.route('/admin/patients/add', methods=['GET', 'POST'])
def add_patient():
    """处理新增患者的逻辑"""
    if request.method == 'POST':
                # --- ⭐ 新增：处理文件上传 ---
        image_filename = ""
        if 'patientImage' in request.files:
            file = request.files['patientImage']
            if file and file.filename != '':
                # 使用 secure_filename 防止恶意文件名
                filename = secure_filename(file.filename) 
                # 你可以用 patientId 或时间戳来生成一个新文件名，避免重复
                new_filename = f"{str(int(time.time()))}_{filename}"
                file.save(os.path.join(app.config['UPLOAD_FOLDER'], new_filename))
                image_filename = new_filename

        all_patients = read_csv_file('data/patients.csv')
        new_patient = {
            'patientId': str(int(time.time())),
            'auntieId': request.form['auntieId'],
            'imageResourceId': image_filename,
            'patientName': request.form['patientName'],
            'patientBedNumber': request.form['patientBedNumber'],
            'patientBarcode': request.form['patientBarcode']
        }
        all_patients.append(new_patient)
        write_csv_file('data/patients.csv', all_patients, fieldnames=['patientId', 'auntieId', 'imageResourceId', 'patientName', 'patientBedNumber', 'patientBarcode'])
        return redirect(URL_PREFIX + url_for('manage_patients'))
    return render_template('patient_form.html', patient=None)

@app.route('/admin/patients/edit/<patient_id>', methods=['GET', 'POST'])
def edit_patient(patient_id):
    """处理编辑患者的逻辑"""
    all_patients = read_csv_file('data/patients.csv')
    patient_to_edit = next((p for p in all_patients if p['patientId'] == patient_id), None)
    if not patient_to_edit:
        return "患者未找到!", 404

    if request.method == 'POST':
        patient_to_edit['auntieId'] = request.form['auntieId']
        # patient_to_edit['imageResourceId'] = request.form['imageResourceId']
        patient_to_edit['patientName'] = request.form['patientName']
        patient_to_edit['patientBedNumber'] = request.form['patientBedNumber']
        patient_to_edit['patientBarcode'] = request.form['patientBarcode']

                # --- 2. ⭐ 新增：处理可能的文件上传 ---
        if 'patientImage' in request.files:
            file = request.files['patientImage']
            
            # a. 检查用户是否真的选择了一个新文件来上传
            if file and file.filename != '':
                # i. 保存新文件
                filename = secure_filename(file.filename)
                new_filename = f"{patient_id}_{filename}" # 使用 patient_id 命名，方便管理
                file_path = os.path.join(app.config['UPLOAD_FOLDER'], new_filename)
                file.save(file_path)
                
                # ii. (可选但推荐) 删除旧的图片文件，避免占用服务器空间
                old_image_filename = patient_to_edit.get('imageResourceId')
                if old_image_filename:
                    old_file_path = os.path.join(app.config['UPLOAD_FOLDER'], old_image_filename)
                    if os.path.exists(old_file_path):
                        os.remove(old_file_path)
                
                # iii. 更新CSV中的文件名记录
                patient_to_edit['imageResourceId'] = new_filename

        write_csv_file('data/patients.csv', all_patients, fieldnames=['patientId', 'auntieId', 'imageResourceId', 'patientName', 'patientBedNumber', 'patientBarcode'])
        return redirect(url_for('manage_patients'))
    
    return render_template('patient_form.html', patient=patient_to_edit)

@app.route('/admin/patients/delete/<patient_id>')
def delete_patient(patient_id):
    """处理删除患者的逻辑"""
    # 1. 删除患者记录
    all_patients = read_csv_file('data/patients.csv')
    patients_after_delete = [p for p in all_patients if p['patientId'] != patient_id]
    write_csv_file('data/patients.csv', patients_after_delete, fieldnames=['patientId', 'auntieId', 'imageResourceId', 'patientName', 'patientBedNumber', 'patientBarcode'])
    
    # 2. 删除对应患者的所有处方记录
    all_prescriptions = read_csv_file('data/local_prescriptions_data.csv')
    prescriptions_after_delete = [p for p in all_prescriptions if p.get('patientId') != patient_id]
    
    if len(all_prescriptions) != len(prescriptions_after_delete):
        # 如果有处方被删除，更新处方文件
        fieldnames = ['patient_name', 'patientId', 'rfid', 'medicine_name', 'morning_dosage',
                     'noon_dosage', 'evening_dosage', 'meal_timing', 'start_date',
                     'duration_days', 'last_dispensed_expiry_date', 'is_active', 'pill_size']
        write_csv_file('data/local_prescriptions_data.csv', prescriptions_after_delete, fieldnames)
        print(f"[{time.ctime()}] 已删除患者 {patient_id} 的 {len(all_prescriptions) - len(prescriptions_after_delete)} 条处方记录")
    
    # 3. 删除对应患者的所有排班记录
    all_schedules = read_csv_file('data/schedules.csv')
    schedules_after_delete = [s for s in all_schedules if s.get('patientId') != patient_id]
    
    if len(all_schedules) != len(schedules_after_delete):
        # 如果有排班被删除，更新排班文件
        write_csv_file('data/schedules.csv', schedules_after_delete, fieldnames=['patientId', 'patientName', 'timeSlotName'])
        print(f"[{time.ctime()}] 已删除患者 {patient_id} 的 {len(all_schedules) - len(schedules_after_delete)} 条排班记录")
    
    # 4. 删除患者图片文件（如果存在）
    patient_to_delete = next((p for p in all_patients if p['patientId'] == patient_id), None)
    if patient_to_delete and patient_to_delete.get('imageResourceId'):
        image_path = os.path.join(app.config['UPLOAD_FOLDER'], patient_to_delete['imageResourceId'])
        if os.path.exists(image_path):
            try:
                os.remove(image_path)
                print(f"[{time.ctime()}] 已删除患者图片文件: {patient_to_delete['imageResourceId']}")
            except Exception as e:
                print(f"[{time.ctime()}] 删除患者图片文件失败: {e}")
    
    print(f"[{time.ctime()}] 已完全删除患者 {patient_id} 的所有相关数据")
    return redirect(URL_PREFIX + url_for('manage_patients'))

# --- 排班管理页面路由 ---
@app.route('/admin/schedules')
def manage_schedules():
    """显示所有排班的列表页面"""
    
     # ⭐ 修改：支持查看指定日期的排班⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐⭐
    selected_date_str = request.args.get('date', time.strftime("%Y-%m-%d"))
    schedule_filename = f"schedules_{selected_date_str}.csv"

    # 优先读取指定日期的排班文件
    schedules_list = read_csv_file(schedule_filename)
    
    # 将同一患者的不同时间段合并到一起
    patient_schedules = {}
    for schedule in schedules_list:
        patient_id = schedule['patientId']
        if patient_id not in patient_schedules:
            patient_schedules[patient_id] = {
                'patientId': patient_id,
                'patientName': schedule['patientName'],
                'timeSlots': []
            }
        patient_schedules[patient_id]['timeSlots'].append(schedule['timeSlotName'])
    
    # 转换为列表格式
    merged_schedules = list(patient_schedules.values())

    sorted_schedules = sorted(merged_schedules, key=lambda item: int(item['patientId']))

    return render_template('schedules.html', schedules=sorted_schedules)

@app.route('/admin/schedules/add', methods=['GET', 'POST'])
def add_schedule():
    """处理新增排班的逻辑"""
    if request.method == 'POST':
        all_schedules = read_csv_file('data/schedules.csv')
        # 从患者列表中获取患者名称
        patients_list = read_csv_file('data/patients.csv')
        patient_name = ""
        for patient in patients_list:
            if patient['patientId'] == request.form['patientId']:
                patient_name = patient['patientName']
                break
        
        # 获取选中的时间段列表（可能是多个）
        selected_time_slots = request.form.getlist('timeSlotName')
        
        # 为每个选中的时间段创建一条记录
        for time_slot in selected_time_slots:
            new_schedule = {
                'patientId': request.form['patientId'],
                'patientName': patient_name,
                'timeSlotName': time_slot
            }
            all_schedules.append(new_schedule)
        
        write_csv_file('data/schedules.csv', all_schedules, fieldnames=['patientId', 'patientName', 'timeSlotName'])
        return redirect(URL_PREFIX + url_for('manage_schedules'))
    
    # 获取患者列表供下拉选择使用
    patients_list = read_csv_file('data/patients.csv')
    return render_template('schedule_form.html', schedule=None, patients=patients_list)

@app.route('/admin/schedules/edit/<patient_id>', methods=['GET', 'POST'])
def edit_schedule_by_patient(patient_id):
    """处理编辑患者排班的逻辑"""
    all_schedules = read_csv_file('data/schedules.csv')
    
    # 找到该患者的所有排班记录
    patient_schedules = [s for s in all_schedules if s['patientId'] == patient_id]
    if not patient_schedules:
        return "患者排班未找到!", 404
    
    if request.method == 'POST':
        # 从患者列表中获取患者名称
        patients_list = read_csv_file('data/patients.csv')
        patient_name = ""
        for patient in patients_list:
            if patient['patientId'] == patient_id:
                patient_name = patient['patientName']
                break
        
        # 如果没有找到患者名称，使用现有记录中的名称
        if not patient_name and patient_schedules:
            patient_name = patient_schedules[0]['patientName']
        
        # 删除该患者的所有现有排班记录
        remaining_schedules = [s for s in all_schedules if s['patientId'] != patient_id]
        
        # 获取新选择的时间段
        selected_time_slots = request.form.getlist('timeSlotName')
        
        # 为每个选中的时间段创建新记录
        for time_slot in selected_time_slots:
            new_schedule = {
                'patientId': patient_id,
                'patientName': patient_name,
                'timeSlotName': time_slot
            }
            remaining_schedules.append(new_schedule)
            
        # 写入更新后的数据
        write_csv_file('data/schedules.csv', remaining_schedules, fieldnames=['patientId', 'patientName', 'timeSlotName'])
        return redirect(URL_PREFIX + url_for('manage_schedules'))
    
    # GET请求：显示编辑表单
    # 获取患者列表和当前选中的时间段
    patients_list = read_csv_file('data/patients.csv')
    current_time_slots = [s['timeSlotName'] for s in patient_schedules]
    
    # 构建schedule对象用于表单显示
    schedule_data = {
        'patientId': patient_id,
        'patientName': patient_schedules[0]['patientName'],
        'timeSlots': current_time_slots
    }
    
    return render_template('schedule_form.html', schedule=schedule_data, patients=patients_list, edit_mode=True)

@app.route('/admin/schedules/delete/<patient_id>')
def delete_schedule_by_patient(patient_id):
    """处理删除患者所有排班的逻辑"""
    all_schedules = read_csv_file('data/schedules.csv')
    # 删除该患者的所有排班记录
    schedules_after_delete = [s for s in all_schedules if s['patientId'] != patient_id]
    write_csv_file('data/schedules.csv', schedules_after_delete, fieldnames=['patientId', 'patientName', 'timeSlotName'])
    return redirect(URL_PREFIX + url_for('manage_schedules'))

# --- 时间段管理页面路由 ---
@app.route('/admin/timeslots')
def manage_timeslots():
    """显示所有时间段的列表页面"""
    timeslots_list = read_csv_file('data/timeslots.csv')
    return render_template('timeslots.html', timeslots=timeslots_list)

@app.route('/admin/timeslots/edit/<name>', methods=['GET', 'POST'])
def edit_timeslot(name):
    """处理编辑时间段的逻辑"""
    all_timeslots = read_csv_file('data/timeslots.csv')
    timeslot_to_edit = next((ts for ts in all_timeslots if ts.get('name') == name), None)

    if not timeslot_to_edit:
        return "时间段未找到!", 404

    if request.method == 'POST':
        # 更新字典中的值
        timeslot_to_edit['displayName'] = request.form['displayName']
        timeslot_to_edit['startHour'] = request.form['startHour']
        timeslot_to_edit['endHour'] = request.form['endHour']
        timeslot_to_edit['startMinute'] = request.form['startMinute']
        
        # 将更新后的完整列表写回CSV文件
        fieldnames = ['name', 'displayName', 'startHour', 'endHour', 'startMinute']
        write_csv_file('data/timeslots.csv', all_timeslots, fieldnames=fieldnames)
        
        return redirect(URL_PREFIX + url_for('manage_timeslots'))
    
    # GET 请求：显示编辑表单
    return render_template('timeslot_form.html', timeslot=timeslot_to_edit)
    
# --- 每日任务记录的可视化路由 ---
@app.route('/admin/tasks')
def manage_tasks():
    """显示每日服药记录的可视化页面"""
    
    # 1. 从URL参数中获取要查询的日期，如果未提供，则默认为今天
    selected_date_str = request.args.get('date', time.strftime("%Y-%m-%d"))
    
    # 2. 根据日期构建任务文件名
    task_filename = f"data/tasks_{selected_date_str}.csv"
    
    tasks_with_details = []
    
    # 3. 检查文件是否存在，如果存在则读取
    if os.path.exists(task_filename):
        tasks_list = read_csv_file(task_filename)
        
        # 4. 为了显示病人姓名和时间段中文名，我们需要关联查询其他CSV文件
        patients_list = read_csv_file('data/patients.csv')
        timeslots_list = read_csv_file('data/timeslots.csv')
        
        # a. 创建快速查找的“字典” (映射表)，提高效率
        patient_name_map = {p['patientId']: p['patientName'] for p in patients_list}
        timeslot_display_map = {ts['name']: ts['displayName'] for ts in timeslots_list}
        
        # b. 遍历当天的任务，并为每一条记录补充额外的信息
        for task in tasks_list:
            task_details = task.copy() # 复制一份，避免修改原始数据
            
            # 补充病人姓名
            task_details['patientName'] = patient_name_map.get(task['patientId'], '未知患者')
            
            # 补充时间段的中文显示名称
            task_details['timeSlot_displayName'] = timeslot_display_map.get(task['timeSlotName'], task['timeSlotName'])
            
            tasks_with_details.append(task_details)

    # 5. 将处理好的数据和选定的日期，一起传递给HTML模板
    return render_template(
        'tasks.html', 
        tasks=tasks_with_details, 
        selected_date=selected_date_str
    )

###################
# 自动生成排班功能 #
###################
def _is_prescription_active(prescription, target_date=None):
    """判断处方在指定日期是否有效"""
    if target_date is None:
        target_date = datetime.now().date()
    elif isinstance(target_date, str):
        target_date = datetime.strptime(target_date, "%Y-%m-%d").date()
    
    try:
        start_date = datetime.strptime(prescription['start_date'], "%Y-%m-%d").date()
        duration_days = int(prescription['duration_days'])
        end_date = start_date + timedelta(days=duration_days - 1)
        
        is_in_date_range = start_date <= target_date <= end_date
        is_active = prescription.get('is_active', '1') == '1'
        
        return is_in_date_range and is_active
        
    except (ValueError, KeyError) as e:
        print(f"处理处方数据时出错: {e}")
        return False

def _get_time_slots_from_dosage(prescription):
    """根据用药剂量和用餐时机生成时间段"""
    time_slots = []
    
    try:
        morning_dosage = int(prescription.get('morning_dosage', 0))
        noon_dosage = int(prescription.get('noon_dosage', 0))
        evening_dosage = int(prescription.get('evening_dosage', 0))
        meal_timing = prescription.get('meal_timing', 'after')
        
        if morning_dosage > 0:
            time_slots.append('BEFORE_BREAKFAST' if meal_timing == 'before' else 'AFTER_BREAKFAST')
        if noon_dosage > 0:
            time_slots.append('BEFORE_LUNCH' if meal_timing == 'before' else 'AFTER_LUNCH')
        if evening_dosage > 0:
            time_slots.append('BEFORE_DINNER' if meal_timing == 'before' else 'AFTER_DINNER')
                
    except (ValueError, KeyError) as e:
        print(f"解析用药剂量时出错: {e}")
    
    return time_slots

def generate_schedules_for_date(target_date=None):
    """为指定日期生成排班数据"""
    if target_date is None:
        target_date = datetime.now().date()
    elif isinstance(target_date, str):
        target_date = datetime.strptime(target_date, "%Y-%m-%d").date()
    
    date_str = target_date.strftime("%Y-%m-%d")
    print(f"[{time.ctime()}] 开始为日期 {date_str} 生成排班...")
    
    # 读取处方数据
    prescriptions = read_csv_file('data/local_prescriptions_data.csv')
    if not prescriptions:
        print(f"[{time.ctime()}] 未找到处方数据文件或文件为空")
        return []
    
    schedules = []
    processed_combinations = set()
    
    for prescription in prescriptions:
        if not _is_prescription_active(prescription, target_date):
            continue
        
        patient_id = prescription.get('patientId', '')
        patient_name = prescription.get('patient_name', '')
        
        if not patient_id or not patient_name:
            continue
        
        time_slots = _get_time_slots_from_dosage(prescription)
        
        for time_slot in time_slots:
            combination_key = f"{patient_id}_{time_slot}"
            
            if combination_key not in processed_combinations:
                schedule = {
                    'patientId': patient_id,
                    'patientName': patient_name,
                    'timeSlotName': time_slot
                }
                schedules.append(schedule)
                processed_combinations.add(combination_key)

    # 写入文件
    if schedules:
        schedule_filename = f"data/schedules_{date_str}.csv"
        write_csv_file(schedule_filename, schedules, fieldnames=['patientId', 'patientName', 'timeSlotName'])
        # 同时更新主schedules.csv文件以保持兼容性
        write_csv_file('data/schedules.csv', schedules, fieldnames=['patientId', 'patientName', 'timeSlotName'])
        print(f"[{time.ctime()}] 成功生成 {len(schedules)} 条排班记录")
    else:
        print(f"[{time.ctime()}] 日期 {date_str} 没有有效的排班记录")
    
    return schedules

def daily_schedule_generation():
    """每日排班生成任务"""
    try:
        print(f"[{time.ctime()}] ===== 开始每日自动排班生成 =====")
        
        # 检查处方数据文件是否存在
        if not os.path.exists('data/local_prescriptions_data.csv'):
            print(f"[{time.ctime()}] 错误: 找不到 local_prescriptions_data.csv 文件")
            return
        
        # 生成今天的排班
        schedules = generate_schedules_for_date()
        
        if schedules:
            print(f"[{time.ctime()}] 每日排班生成完成！生成了 {len(schedules)} 条记录")
        else:
            print(f"[{time.ctime()}] 今天没有需要生成的排班")
            
    except Exception as e:
        print(f"[{time.ctime()}] 每日排班生成过程中发生错误: {e}")
        
def run_scheduler_in_background():
    """运行定时任务调度器"""
    # 设置每天凌晨4点执行排班生成
    schedule.every().day.at("04:00").do(daily_schedule_generation)
    
    print(f"[{time.ctime()}] 定时任务已启动: 每天凌晨4:00自动生成排班")
    
    while True:
        schedule.run_pending()
        time.sleep(60)  # 每分钟检查一次

@app.route('/flask/admin/generate-schedules', methods=['POST'])
def generate_schedules_api():
    """【API接口】手动生成今天的排班数据"""
    try:
        schedules = generate_schedules_for_date()
        return jsonify({
            'success': True,
            'message': f'成功生成 {len(schedules)} 条排班记录',
            'schedules': schedules
        })
    except Exception as e:
        return jsonify({
            'success': False,
            'error': str(e)
        }), 500

# --- 6. 脚本主入口 ---
if __name__ == '__main__':
    # 启动后台定时任务线程
    scheduler_thread = threading.Thread(target=run_scheduler_in_background, daemon=True)
    scheduler_thread.start()

    # 运行Flask服务器
    app.run(host='0.0.0.0', port=5050, debug=True)