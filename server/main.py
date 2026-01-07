from flask import Flask, jsonify, request, render_template, redirect, url_for, session
from functools import wraps
import time
import sqlite3
import os
from werkzeug.utils import secure_filename
from werkzeug.security import generate_password_hash, check_password_hash
from datetime import datetime

# ========================================
# Flask Application Initialization
# ========================================
app = Flask(__name__)
app.secret_key = 'ezdose-secret-key-change-in-production'  # Session密钥，生产环境请修改

# ========================================
# URL Prefix Configuration
# ========================================
# Set to empty string for local development
# Set to '/flask' for remote deployment to handle reverse proxy routing
URL_PREFIX = ''  # Local development mode
# URL_PREFIX = '/flask'  # Uncomment this line for remote deployment

# ========================================
# File Path Configuration
# ========================================
UPLOAD_FOLDER = 'static/images'
DATABASE_FILE = 'data/ezdose.db'

# Ensure directories exist
os.makedirs(UPLOAD_FOLDER, exist_ok=True)
os.makedirs('data', exist_ok=True)

app.config['UPLOAD_FOLDER'] = UPLOAD_FOLDER

# Allowed file extensions for patient photo uploads
ALLOWED_EXTENSIONS = {'png', 'jpg', 'jpeg', 'gif', 'bmp', 'webp'}


# ========================================
# Database Functions
# ========================================

def get_db_connection():
    """
    Get a database connection with row factory for dict-like access.
    
    Returns:
        sqlite3.Connection: Database connection object
    """
    conn = sqlite3.connect(DATABASE_FILE)
    conn.row_factory = sqlite3.Row
    return conn


def init_db():
    """
    Initialize the database with all required tables.
    Creates tables if they don't exist.
    """
    conn = get_db_connection()
    cursor = conn.cursor()
    
    # Users table - for authentication and permissions
    cursor.execute('''
        CREATE TABLE IF NOT EXISTS users (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            username TEXT UNIQUE NOT NULL,
            password_hash TEXT NOT NULL,
            name TEXT,
            can_edit_users INTEGER DEFAULT 0,
            can_edit_patients INTEGER DEFAULT 0,
            can_edit_prescriptions INTEGER DEFAULT 0,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP
        )
    ''')
    
    # Patients table
    cursor.execute('''
        CREATE TABLE IF NOT EXISTS patients (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            patient_name TEXT NOT NULL,
            bed_number TEXT,
            profile_photo_resource_id TEXT,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP
        )
    ''')
    
    # Prescriptions table
    cursor.execute('''
        CREATE TABLE IF NOT EXISTS prescriptions (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            patient_id INTEGER NOT NULL,
            medicine_name TEXT NOT NULL,
            morning_dosage REAL DEFAULT 0,
            noon_dosage REAL DEFAULT 0,
            evening_dosage REAL DEFAULT 0,
            meal_timing TEXT,
            start_date DATE NOT NULL,
            duration_days INTEGER NOT NULL,
            last_dispensed_expiry_date DATE,
            is_active INTEGER DEFAULT 1,
            pill_size TEXT,
            image_resource_id TEXT,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (patient_id) REFERENCES patients(id) ON DELETE CASCADE
        )
    ''')
    
    # Dispense logs table - for tracking medication dispensing
    cursor.execute('''
        CREATE TABLE IF NOT EXISTS dispense_logs (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            dispense_date DATE NOT NULL,
            patient_id INTEGER NOT NULL,
            prescription_id INTEGER NOT NULL,
            medicine_name TEXT NOT NULL,
            dosage REAL NOT NULL,
            time_period TEXT NOT NULL,
            dispensed_by_user_id INTEGER,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (patient_id) REFERENCES patients(id),
            FOREIGN KEY (prescription_id) REFERENCES prescriptions(id),
            FOREIGN KEY (dispensed_by_user_id) REFERENCES users(id)
        )
    ''')
    
    conn.commit()
    
    # Create default admin user if no users exist
    user_count = cursor.execute('SELECT COUNT(*) FROM users').fetchone()[0]
    if user_count == 0:
        admin_password = generate_password_hash('admin123')
        cursor.execute('''
            INSERT INTO users (username, password_hash, name, can_edit_users, can_edit_patients, can_edit_prescriptions)
            VALUES (?, ?, ?, ?, ?, ?)
        ''', ('admin', admin_password, '管理员', 1, 1, 1))
        conn.commit()
        print(f"[{time.ctime()}] Created default admin user (username: admin, password: admin123)")
    
    conn.close()
    print(f"[{time.ctime()}] Database initialized successfully")


def dict_from_row(row):
    """
    Convert sqlite3.Row object to dictionary.
    
    Args:
        row: sqlite3.Row object
    
    Returns:
        dict: Dictionary representation of the row
    """
    if row is None:
        return None
    return dict(row)


def allowed_file(filename):
    """
    Check if uploaded file has an allowed extension.
    
    Args:
        filename (str): Name of the file to check
    
    Returns:
        bool: True if file extension is allowed, False otherwise
    """
    return '.' in filename and \
           filename.rsplit('.', 1)[1].lower() in ALLOWED_EXTENSIONS


# ========================================
# Authentication & Authorization
# ========================================

def get_current_user():
    """
    Get current logged-in user from session.
    
    Returns:
        dict: User data or None if not logged in
    """
    if 'user_id' not in session:
        return None
    
    conn = get_db_connection()
    user = conn.execute('SELECT * FROM users WHERE id = ?', (session['user_id'],)).fetchone()
    conn.close()
    
    return dict_from_row(user) if user else None


def login_required(f):
    """
    Decorator to require login for a route.
    """
    @wraps(f)
    def decorated_function(*args, **kwargs):
        if 'user_id' not in session:
            return redirect(URL_PREFIX + url_for('login'))
        return f(*args, **kwargs)
    return decorated_function


def permission_required(permission):
    """
    Decorator to require specific permission for a route.
    
    Args:
        permission (str): Permission name ('can_edit_users', 'can_edit_patients', 'can_edit_prescriptions')
    """
    def decorator(f):
        @wraps(f)
        def decorated_function(*args, **kwargs):
            if 'user_id' not in session:
                return redirect(URL_PREFIX + url_for('login'))
            
            user = get_current_user()
            if not user:
                return redirect(URL_PREFIX + url_for('login'))
            
            if not user.get(permission):
                return render_template('access_denied.html', user=user), 403
            
            return f(*args, **kwargs)
        return decorated_function
    return decorator


# Initialize database on startup
init_db()


# Context processor to inject URL_PREFIX into all templates
@app.context_processor
def inject_url_prefix():
    """
    Inject URL_PREFIX and current user into all Flask templates.
    """
    return {
        'URL_PREFIX': URL_PREFIX,
        'current_user': get_current_user()
    }


# ========================================
# Authentication Routes
# ========================================

@app.route('/login', methods=['GET', 'POST'])
def login():
    """
    Handle user login.
    """
    if 'user_id' in session:
        return redirect(URL_PREFIX + url_for('admin_dashboard'))
    
    error = None
    if request.method == 'POST':
        username = request.form.get('username', '').strip()
        password = request.form.get('password', '')
        
        conn = get_db_connection()
        user = conn.execute('SELECT * FROM users WHERE username = ?', (username,)).fetchone()
        conn.close()
        
        if user and check_password_hash(user['password_hash'], password):
            session['user_id'] = user['id']
            session['username'] = user['username']
            return redirect(URL_PREFIX + url_for('admin_dashboard'))
        else:
            error = '用户名或密码错误'
    
    return render_template('login.html', error=error)


@app.route('/logout')
def logout():
    """
    Handle user logout.
    """
    session.clear()
    return redirect(URL_PREFIX + url_for('login'))


# ========================================
# Root Route - Server Status
# ========================================
@app.route('/', methods=['GET'])
def index():
    """
    Root endpoint that returns server status and available API endpoints.
    
    Returns:
        JSON response with server status, timestamp, and list of available endpoints
    """
    return jsonify({
        "message": "EZ-Dose 养老院分药系统服务器运行中!",
        "timestamp": time.time(),
        "database": "SQLite",
        "available_endpoints": [
            "GET / - Server status",
            "GET /packer/patients - Get patient list",
            "GET /packer/prescriptions - Get prescription list",
            "POST /packer/patients/upload - Upload patient data",
            "POST /packer/prescriptions/upload - Upload prescription data",
            "POST /packer/dispense - Record dispense log"
        ]
    })


# ========================================
# Medicine Dispenser API Endpoints
# ========================================

@app.route('/packer/patients', methods=['GET'])
def get_patients_for_dispensing():
    """
    API endpoint to retrieve all patient records.
    
    Returns:
        JSON response containing patient list with success status
    """
    conn = get_db_connection()
    patients = conn.execute('SELECT * FROM patients').fetchall()
    conn.close()
    
    patients_list = [dict_from_row(p) for p in patients]
    return jsonify({
        "success": True,
        "data": patients_list,
        "count": len(patients_list)
    })


@app.route('/packer/prescriptions', methods=['GET'])
def get_prescriptions_for_dispensing():
    """
    API endpoint to retrieve all prescription records with patient info.
    
    Returns:
        JSON response containing prescription list with success status
    """
    conn = get_db_connection()
    prescriptions = conn.execute('''
        SELECT p.*, pt.patient_name 
        FROM prescriptions p
        LEFT JOIN patients pt ON p.patient_id = pt.id
        WHERE p.is_active = 1
    ''').fetchall()
    conn.close()
    
    prescriptions_list = [dict_from_row(p) for p in prescriptions]
    return jsonify({
        "success": True,
        "data": prescriptions_list,
        "count": len(prescriptions_list)
    })


@app.route('/packer/patients/upload', methods=['POST'])
def upload_patients_for_dispensing():
    """
    API endpoint to upload multiple patient records.
    
    Request Body:
        JSON object with 'patients' key containing list of patient dictionaries
    
    Returns:
        JSON response with success status and message
    """
    try:
        data = request.get_json()
        
        if not data or 'patients' not in data or not isinstance(data['patients'], list):
            return jsonify({
                "success": False,
                "message": "Invalid data format. Expected: {'patients': [...]}"
            }), 400
        
        conn = get_db_connection()
        cursor = conn.cursor()
        
        inserted_count = 0
        for patient in data['patients']:
            if not isinstance(patient, dict):
                continue
            
            patient_name = patient.get('patientName') or patient.get('patient_name')
            if not patient_name:
                continue
            
            bed_number = patient.get('patientBedNumber') or patient.get('bed_number', '')
            photo_id = patient.get('imageResourceId') or patient.get('profile_photo_resource_id', '')
            
            cursor.execute('''
                INSERT INTO patients (patient_name, bed_number, profile_photo_resource_id)
                VALUES (?, ?, ?)
            ''', (patient_name, bed_number, photo_id))
            inserted_count += 1
        
        conn.commit()
        conn.close()
        
        return jsonify({
            "success": True,
            "message": f"Successfully uploaded {inserted_count} patients",
            "count": inserted_count
        })
            
    except Exception as e:
        return jsonify({
            "success": False,
            "message": f"Error uploading patients: {str(e)}"
        }), 500


@app.route('/packer/prescriptions/upload', methods=['POST'])
def upload_prescriptions_for_dispensing():
    """
    API endpoint to upload multiple prescription records.
    
    Request Body:
        JSON object with 'prescriptions' key containing list of prescription dictionaries
    
    Returns:
        JSON response with success status and message
    """
    try:
        data = request.get_json()
        
        if not data or 'prescriptions' not in data or not isinstance(data['prescriptions'], list):
            return jsonify({
                "success": False,
                "message": "Invalid data format. Expected: {'prescriptions': [...]}"
            }), 400
        
        conn = get_db_connection()
        cursor = conn.cursor()
        
        inserted_count = 0
        for rx in data['prescriptions']:
            if not isinstance(rx, dict):
                continue
            
            patient_id = rx.get('patient_id') or rx.get('patientId')
            medicine_name = rx.get('medicine_name')
            
            if not patient_id or not medicine_name:
                continue
            
            cursor.execute('''
                INSERT INTO prescriptions (
                    patient_id, medicine_name, morning_dosage, noon_dosage, evening_dosage,
                    meal_timing, start_date, duration_days, last_dispensed_expiry_date,
                    is_active, pill_size, image_resource_id
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            ''', (
                patient_id,
                medicine_name,
                float(rx.get('morning_dosage', 0)),
                float(rx.get('noon_dosage', 0)),
                float(rx.get('evening_dosage', 0)),
                rx.get('meal_timing', ''),
                rx.get('start_date', datetime.now().strftime('%Y-%m-%d')),
                int(rx.get('duration_days', 7)),
                rx.get('last_dispensed_expiry_date'),
                int(rx.get('is_active', 1)),
                rx.get('pill_size', ''),
                rx.get('image_resource_id', '')
            ))
            inserted_count += 1
        
        conn.commit()
        conn.close()
        
        return jsonify({
            "success": True,
            "message": f"Successfully uploaded {inserted_count} prescriptions",
            "count": inserted_count
        })
            
    except Exception as e:
        return jsonify({
            "success": False,
            "message": f"Error uploading prescriptions: {str(e)}"
        }), 500


@app.route('/packer/dispense', methods=['POST'])
def record_dispense_log():
    """
    API endpoint to record a medication dispense event.
    
    Request Body:
        JSON object with dispense details:
        - dispense_date: Date of dispense (YYYY-MM-DD)
        - patient_id: Patient ID
        - prescription_id: Prescription ID
        - medicine_name: Name of medicine
        - dosage: Amount dispensed
        - time_period: morning/noon/evening
        - user_id: ID of user who dispensed (optional)
    
    Returns:
        JSON response with success status
    """
    try:
        data = request.get_json()
        
        required_fields = ['patient_id', 'prescription_id', 'medicine_name', 'dosage', 'time_period']
        for field in required_fields:
            if field not in data:
                return jsonify({
                    "success": False,
                    "message": f"Missing required field: {field}"
                }), 400
        
        conn = get_db_connection()
        cursor = conn.cursor()
        
        cursor.execute('''
            INSERT INTO dispense_logs (
                dispense_date, patient_id, prescription_id, medicine_name,
                dosage, time_period, dispensed_by_user_id
            ) VALUES (?, ?, ?, ?, ?, ?, ?)
        ''', (
            data.get('dispense_date', datetime.now().strftime('%Y-%m-%d')),
            data['patient_id'],
            data['prescription_id'],
            data['medicine_name'],
            float(data['dosage']),
            data['time_period'],
            data.get('user_id')
        ))
        
        conn.commit()
        log_id = cursor.lastrowid
        conn.close()
        
        return jsonify({
            "success": True,
            "message": "Dispense log recorded",
            "log_id": log_id
        })
            
    except Exception as e:
        return jsonify({
            "success": False,
            "message": f"Error recording dispense log: {str(e)}"
        }), 500


@app.route('/packer/dispense_logs', methods=['GET'])
def get_dispense_logs():
    """
    API endpoint to retrieve dispense logs.
    
    Query Parameters:
        - date: Filter by date (YYYY-MM-DD)
        - patient_id: Filter by patient
    
    Returns:
        JSON response with dispense logs
    """
    conn = get_db_connection()
    
    query = '''
        SELECT dl.*, p.patient_name, u.username as dispensed_by
        FROM dispense_logs dl
        LEFT JOIN patients p ON dl.patient_id = p.id
        LEFT JOIN users u ON dl.dispensed_by_user_id = u.id
        WHERE 1=1
    '''
    params = []
    
    if request.args.get('date'):
        query += ' AND dl.dispense_date = ?'
        params.append(request.args.get('date'))
    
    if request.args.get('patient_id'):
        query += ' AND dl.patient_id = ?'
        params.append(request.args.get('patient_id'))
    
    query += ' ORDER BY dl.created_at DESC'
    
    logs = conn.execute(query, params).fetchall()
    conn.close()
    
    logs_list = [dict_from_row(log) for log in logs]
    return jsonify({
        "success": True,
        "data": logs_list,
        "count": len(logs_list)
    })

# ========================================
# Web Admin Panel Routes
# ========================================

@app.route('/admin')
@login_required
def admin_dashboard():
    """
    Display the admin dashboard homepage with statistics.
    """
    conn = get_db_connection()
    
    patient_count = conn.execute('SELECT COUNT(*) FROM patients').fetchone()[0]
    prescription_count = conn.execute('SELECT COUNT(*) FROM prescriptions WHERE is_active = 1').fetchone()[0]
    user_count = conn.execute('SELECT COUNT(*) FROM users').fetchone()[0]
    today = datetime.now().strftime('%Y-%m-%d')
    dispense_today = conn.execute(
        'SELECT COUNT(*) FROM dispense_logs WHERE dispense_date = ?', (today,)
    ).fetchone()[0]
    
    conn.close()
    
    stats = {
        'patients': patient_count,
        'prescriptions': prescription_count,
        'users': user_count,
        'dispense_today': dispense_today
    }
    
    return render_template('dashboard.html', stats=stats)


# ========================================
# User Management Routes
# ========================================

@app.route('/admin/users')
@permission_required('can_edit_users')
def manage_users():
    """
    Display list of all users.
    """
    conn = get_db_connection()
    users = conn.execute('SELECT * FROM users ORDER BY id').fetchall()
    conn.close()
    
    users_list = [dict_from_row(u) for u in users]
    return render_template('users.html', users=users_list)


@app.route('/admin/users/add', methods=['GET', 'POST'])
@permission_required('can_edit_users')
def add_user():
    """
    Handle adding a new user.
    """
    if request.method == 'POST':
        conn = get_db_connection()
        cursor = conn.cursor()
        
        try:
            password_hash = generate_password_hash(request.form['password'])
            cursor.execute('''
                INSERT INTO users (username, password_hash, name, can_edit_users, can_edit_patients, can_edit_prescriptions)
                VALUES (?, ?, ?, ?, ?, ?)
            ''', (
                request.form['username'],
                password_hash,
                request.form.get('name', ''),
                1 if request.form.get('can_edit_users') else 0,
                1 if request.form.get('can_edit_patients') else 0,
                1 if request.form.get('can_edit_prescriptions') else 0
            ))
            conn.commit()
        except sqlite3.IntegrityError:
            conn.close()
            return render_template('user_form.html', user=None, error="用户名已存在")
        
        conn.close()
        return redirect(URL_PREFIX + url_for('manage_users'))
    
    return render_template('user_form.html', user=None)


@app.route('/admin/users/edit/<int:user_id>', methods=['GET', 'POST'])
@permission_required('can_edit_users')
def edit_user(user_id):
    """
    Handle editing an existing user.
    """
    conn = get_db_connection()
    user = conn.execute('SELECT * FROM users WHERE id = ?', (user_id,)).fetchone()
    
    if not user:
        conn.close()
        return "User not found!", 404

    if request.method == 'POST':
        cursor = conn.cursor()
        
        if request.form.get('password'):
            password_hash = generate_password_hash(request.form['password'])
            cursor.execute('''
                UPDATE users SET username=?, password_hash=?, name=?, 
                can_edit_users=?, can_edit_patients=?, can_edit_prescriptions=?
                WHERE id=?
            ''', (
                request.form['username'],
                password_hash,
                request.form.get('name', ''),
                1 if request.form.get('can_edit_users') else 0,
                1 if request.form.get('can_edit_patients') else 0,
                1 if request.form.get('can_edit_prescriptions') else 0,
                user_id
            ))
        else:
            cursor.execute('''
                UPDATE users SET username=?, name=?, 
                can_edit_users=?, can_edit_patients=?, can_edit_prescriptions=?
                WHERE id=?
            ''', (
                request.form['username'],
                request.form.get('name', ''),
                1 if request.form.get('can_edit_users') else 0,
                1 if request.form.get('can_edit_patients') else 0,
                1 if request.form.get('can_edit_prescriptions') else 0,
                user_id
            ))
        
        conn.commit()
        conn.close()
        return redirect(URL_PREFIX + url_for('manage_users'))
    
    conn.close()
    return render_template('user_form.html', user=dict_from_row(user))


@app.route('/admin/users/delete/<int:user_id>')
@permission_required('can_edit_users')
def delete_user(user_id):
    """
    Handle deleting a user.
    """
    conn = get_db_connection()
    conn.execute('DELETE FROM users WHERE id = ?', (user_id,))
    conn.commit()
    conn.close()
    
    return redirect(URL_PREFIX + url_for('manage_users'))


# ========================================
# Patient Management Routes
# ========================================

@app.route('/admin/patients')
@permission_required('can_edit_patients')
def manage_patients():
    """
    Display list of all patients with optional search.
    """
    conn = get_db_connection()
    
    search_query = request.args.get('search', '').strip()
    
    if search_query:
        # Search by name or bed number
        patients = conn.execute('''
            SELECT * FROM patients 
            WHERE patient_name LIKE ? OR bed_number LIKE ?
            ORDER BY id
        ''', (f'%{search_query}%', f'%{search_query}%')).fetchall()
    else:
        patients = conn.execute('SELECT * FROM patients ORDER BY id').fetchall()
    
    conn.close()
    
    patients_list = [dict_from_row(p) for p in patients]
    return render_template('patients.html', patients=patients_list, search_query=search_query)


@app.route('/admin/patients/add', methods=['GET', 'POST'])
@permission_required('can_edit_patients')
def add_patient():
    """
    Handle adding a new patient.
    """
    if request.method == 'POST':
        image_filename = ""
        if 'patientImage' in request.files:
            file = request.files['patientImage']
            if file and file.filename != '' and allowed_file(file.filename):
                filename = secure_filename(file.filename)
                new_filename = f"{int(time.time())}_{filename}"
                file.save(os.path.join(app.config['UPLOAD_FOLDER'], new_filename))
                image_filename = new_filename
        
        conn = get_db_connection()
        cursor = conn.cursor()
        cursor.execute('''
            INSERT INTO patients (patient_name, bed_number, profile_photo_resource_id)
            VALUES (?, ?, ?)
        ''', (
            request.form['patient_name'],
            request.form.get('bed_number', ''),
            image_filename
        ))
        conn.commit()
        conn.close()
        
        return redirect(URL_PREFIX + url_for('manage_patients'))
    
    return render_template('patient_form.html', patient=None)


@app.route('/admin/patients/edit/<int:patient_id>', methods=['GET', 'POST'])
@permission_required('can_edit_patients')
def edit_patient(patient_id):
    """
    Handle editing an existing patient.
    """
    conn = get_db_connection()
    patient = conn.execute('SELECT * FROM patients WHERE id = ?', (patient_id,)).fetchone()
    
    if not patient:
        conn.close()
        return "Patient not found!", 404

    if request.method == 'POST':
        image_filename = patient['profile_photo_resource_id']
        
        if 'patientImage' in request.files:
            file = request.files['patientImage']
            if file and file.filename != '' and allowed_file(file.filename):
                filename = secure_filename(file.filename)
                new_filename = f"{patient_id}_{filename}"
                file.save(os.path.join(app.config['UPLOAD_FOLDER'], new_filename))
                
                # Delete old photo if exists
                if patient['profile_photo_resource_id']:
                    old_path = os.path.join(app.config['UPLOAD_FOLDER'], patient['profile_photo_resource_id'])
                    if os.path.exists(old_path):
                        os.remove(old_path)
                
                image_filename = new_filename
        
        cursor = conn.cursor()
        cursor.execute('''
            UPDATE patients SET patient_name=?, bed_number=?, profile_photo_resource_id=?
            WHERE id=?
        ''', (
            request.form['patient_name'],
            request.form.get('bed_number', ''),
            image_filename,
            patient_id
        ))
        conn.commit()
        conn.close()
        
        return redirect(URL_PREFIX + url_for('manage_patients'))
    
    conn.close()
    return render_template('patient_form.html', patient=dict_from_row(patient))


@app.route('/admin/patients/delete/<int:patient_id>')
@permission_required('can_edit_patients')
def delete_patient(patient_id):
    """
    Handle deleting a patient and all associated data.
    """
    conn = get_db_connection()
    
    # Get patient photo to delete
    patient = conn.execute('SELECT * FROM patients WHERE id = ?', (patient_id,)).fetchone()
    
    if patient and patient['profile_photo_resource_id']:
        image_path = os.path.join(app.config['UPLOAD_FOLDER'], patient['profile_photo_resource_id'])
        if os.path.exists(image_path):
            try:
                os.remove(image_path)
                print(f"[{time.ctime()}] Deleted patient photo: {patient['profile_photo_resource_id']}")
            except Exception as e:
                print(f"[{time.ctime()}] Failed to delete photo: {e}")
    
    # Delete prescriptions first (cascade)
    conn.execute('DELETE FROM prescriptions WHERE patient_id = ?', (patient_id,))
    # Delete dispense logs
    conn.execute('DELETE FROM dispense_logs WHERE patient_id = ?', (patient_id,))
    # Delete patient
    conn.execute('DELETE FROM patients WHERE id = ?', (patient_id,))
    
    conn.commit()
    conn.close()
    
    print(f"[{time.ctime()}] Deleted patient {patient_id} and all associated data")
    return redirect(URL_PREFIX + url_for('manage_patients'))


# ========================================
# Prescription Management Routes
# ========================================

@app.route('/admin/prescriptions')
@permission_required('can_edit_prescriptions')
def manage_prescriptions():
    """
    Display list of all prescriptions.
    """
    conn = get_db_connection()
    prescriptions = conn.execute('''
        SELECT p.*, pt.patient_name 
        FROM prescriptions p
        LEFT JOIN patients pt ON p.patient_id = pt.id
        ORDER BY p.id DESC
    ''').fetchall()
    conn.close()
    
    prescriptions_list = [dict_from_row(p) for p in prescriptions]
    return render_template('prescriptions.html', prescriptions=prescriptions_list)


@app.route('/admin/prescriptions/add', methods=['GET', 'POST'])
@permission_required('can_edit_prescriptions')
def add_prescription():
    """
    Handle adding a new prescription.
    """
    conn = get_db_connection()
    patients = conn.execute('SELECT id, patient_name, bed_number FROM patients ORDER BY patient_name').fetchall()
    
    if request.method == 'POST':
        cursor = conn.cursor()
        cursor.execute('''
            INSERT INTO prescriptions (
                patient_id, medicine_name, morning_dosage, noon_dosage, evening_dosage,
                meal_timing, start_date, duration_days, is_active, pill_size
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        ''', (
            request.form['patient_id'],
            request.form['medicine_name'],
            float(request.form.get('morning_dosage', 0)),
            float(request.form.get('noon_dosage', 0)),
            float(request.form.get('evening_dosage', 0)),
            request.form.get('meal_timing', ''),
            request.form['start_date'],
            int(request.form.get('duration_days', 7)),
            1 if request.form.get('is_active') else 0,
            request.form.get('pill_size', '')
        ))
        conn.commit()
        conn.close()
        
        return redirect(URL_PREFIX + url_for('manage_prescriptions'))
    
    conn.close()
    patients_list = [dict_from_row(p) for p in patients]
    return render_template('prescription_form.html', prescription=None, patients=patients_list)


@app.route('/admin/prescriptions/edit/<int:prescription_id>', methods=['GET', 'POST'])
@permission_required('can_edit_prescriptions')
def edit_prescription(prescription_id):
    """
    Handle editing an existing prescription.
    """
    conn = get_db_connection()
    prescription = conn.execute('SELECT * FROM prescriptions WHERE id = ?', (prescription_id,)).fetchone()
    patients = conn.execute('SELECT id, patient_name, bed_number FROM patients ORDER BY patient_name').fetchall()
    
    if not prescription:
        conn.close()
        return "Prescription not found!", 404

    if request.method == 'POST':
        cursor = conn.cursor()
        cursor.execute('''
            UPDATE prescriptions SET
                patient_id=?, medicine_name=?, morning_dosage=?, noon_dosage=?, evening_dosage=?,
                meal_timing=?, start_date=?, duration_days=?, is_active=?, pill_size=?
            WHERE id=?
        ''', (
            request.form['patient_id'],
            request.form['medicine_name'],
            float(request.form.get('morning_dosage', 0)),
            float(request.form.get('noon_dosage', 0)),
            float(request.form.get('evening_dosage', 0)),
            request.form.get('meal_timing', ''),
            request.form['start_date'],
            int(request.form.get('duration_days', 7)),
            1 if request.form.get('is_active') else 0,
            request.form.get('pill_size', ''),
            prescription_id
        ))
        conn.commit()
        conn.close()
        
        return redirect(URL_PREFIX + url_for('manage_prescriptions'))
    
    conn.close()
    return render_template('prescription_form.html', 
                          prescription=dict_from_row(prescription), 
                          patients=[dict_from_row(p) for p in patients])


@app.route('/admin/prescriptions/delete/<int:prescription_id>')
@permission_required('can_edit_prescriptions')
def delete_prescription(prescription_id):
    """
    Handle deleting a prescription.
    """
    conn = get_db_connection()
    conn.execute('DELETE FROM prescriptions WHERE id = ?', (prescription_id,))
    conn.commit()
    conn.close()
    
    return redirect(URL_PREFIX + url_for('manage_prescriptions'))


# ========================================
# Dispense Logs Routes
# ========================================

@app.route('/admin/dispense_logs')
@login_required
def manage_dispense_logs():
    """
    Display dispense logs with optional filtering.
    """
    conn = get_db_connection()
    
    query = '''
        SELECT dl.*, p.patient_name, u.name as dispensed_by_name
        FROM dispense_logs dl
        LEFT JOIN patients p ON dl.patient_id = p.id
        LEFT JOIN users u ON dl.dispensed_by_user_id = u.id
        WHERE 1=1
    '''
    params = []
    
    date_filter = request.args.get('date')
    patient_filter = request.args.get('patient_id')
    
    if date_filter:
        query += ' AND dl.dispense_date = ?'
        params.append(date_filter)
    
    if patient_filter:
        query += ' AND dl.patient_id = ?'
        params.append(patient_filter)
    
    query += ' ORDER BY dl.created_at DESC LIMIT 100'
    
    logs = conn.execute(query, params).fetchall()
    patients = conn.execute('SELECT id, patient_name FROM patients ORDER BY patient_name').fetchall()
    conn.close()
    
    return render_template('dispense_logs.html', 
                          logs=[dict_from_row(l) for l in logs],
                          patients=[dict_from_row(p) for p in patients],
                          date_filter=date_filter,
                          patient_filter=patient_filter)

# ========================================
# Application Entry Point
# ========================================
if __name__ == '__main__':
    # Start Flask development server
    # host='0.0.0.0' allows external connections (accessible from other devices on network)
    # port=5050 runs on custom port (default Flask port is 5000)
    # debug=True enables auto-reload on code changes and detailed error messages
    # WARNING: Never use debug=True in production deployment!
    app.run(host='0.0.0.0', port=5050, debug=True)
