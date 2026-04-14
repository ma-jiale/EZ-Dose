from flask import Flask, jsonify, request, render_template, redirect, url_for, session
from functools import wraps
import time
import sqlite3
import os
import logging
from logging.handlers import RotatingFileHandler
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
LOG_FILE = 'data/ezdose.log'

# Ensure directories exist
os.makedirs(UPLOAD_FOLDER, exist_ok=True)
os.makedirs('data', exist_ok=True)

# ========================================
# Logging Configuration
# ========================================
# Configure file logging for developers
file_handler = RotatingFileHandler(
    LOG_FILE, 
    maxBytes=10*1024*1024,  # 10MB per file
    backupCount=5,  # Keep 5 backup files
    encoding='utf-8'
)
file_handler.setLevel(logging.INFO)
file_handler.setFormatter(logging.Formatter(
    '%(asctime)s [%(levelname)s] %(message)s',
    datefmt='%Y-%m-%d %H:%M:%S'
))

# Create logger
logger = logging.getLogger('ezdose')
logger.setLevel(logging.INFO)
logger.addHandler(file_handler)

# Also log to console
console_handler = logging.StreamHandler()
console_handler.setLevel(logging.INFO)
console_handler.setFormatter(logging.Formatter(
    '[%(asctime)s] %(levelname)s: %(message)s',
    datefmt='%H:%M:%S'
))
logger.addHandler(console_handler)

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


def generate_next_patient_id():
    """
    Generate the next available patient ID in 6-digit zero-padded format.
    
    Patient IDs follow the format: 000001 to 999999
    This format is optimized for Code128 barcode scanning - the fixed 6-digit
    length produces consistent barcode widths that are easier to scan.
    
    Returns:
        str: Next available patient ID (e.g., "000001", "000042", "001234")
    """
    conn = get_db_connection()
    # Get the maximum current ID (as integer for proper comparison)
    result = conn.execute('SELECT MAX(CAST(id AS INTEGER)) as max_id FROM patients').fetchone()
    conn.close()
    
    if result['max_id'] is None:
        # No patients exist yet, start with 000001
        next_id = 1
    else:
        next_id = result['max_id'] + 1
    
    # Validate ID range (6-digit limit)
    if next_id > 999999:
        raise ValueError("Patient ID limit exceeded. Maximum is 999999.")
    
    # Format as 6-digit zero-padded string
    return f"{next_id:06d}"


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
            can_edit_users INTEGER DEFAULT 0,
            can_edit_patients INTEGER DEFAULT 0,
            can_edit_prescriptions INTEGER DEFAULT 0,
            can_view_logs INTEGER DEFAULT 0,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP
        )
    ''')
    
    # Patients table
    # Patient ID uses 6-digit zero-padded format (000001-999999) for better barcode scanning
    cursor.execute('''
        CREATE TABLE IF NOT EXISTS patients (
            id TEXT PRIMARY KEY,
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
            pill_size_area REAL,
            image_resource_id TEXT,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (patient_id) REFERENCES patients(id) ON DELETE CASCADE
        )
    ''')
    
    # System settings table - for calibration configuration
    cursor.execute('''
        CREATE TABLE IF NOT EXISTS system_settings (
            key TEXT PRIMARY KEY,
            value TEXT NOT NULL
        )
    ''')
    
    # Dispense logs table - for tracking medication dispensing
    cursor.execute('''
        CREATE TABLE IF NOT EXISTS dispense_logs (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            dispense_date DATE NOT NULL,
            patient_id TEXT NOT NULL,
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
    
    # Operation logs table - for tracking web admin operations
    cursor.execute('''
        CREATE TABLE IF NOT EXISTS operation_logs (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            operation_type TEXT NOT NULL,
            operation_category TEXT NOT NULL,
            target_type TEXT NOT NULL,
            target_id INTEGER,
            target_name TEXT,
            details TEXT,
            user_id INTEGER,
            user_name TEXT,
            ip_address TEXT,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (user_id) REFERENCES users(id)
        )
    ''')
    
    conn.commit()
    
    # Create default admin user if no users exist
    user_count = cursor.execute('SELECT COUNT(*) FROM users').fetchone()[0]
    if user_count == 0:
        admin_password = generate_password_hash('admin123')
        cursor.execute('''
            INSERT INTO users (username, password_hash, can_edit_users, can_edit_patients, can_edit_prescriptions, can_view_logs)
            VALUES (?, ?, ?, ?, ?, ?)
        ''', ('admin', admin_password, 1, 1, 1, 1))
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


def log_operation(operation_type, operation_category, target_type, target_id=None, target_name=None, details=None):
    """
    Record an operation log to database and file.
    
    Args:
        operation_type (str): Type of operation ('add', 'edit', 'delete', 'login', 'logout')
        operation_category (str): Category ('user', 'patient', 'prescription', 'auth')
        target_type (str): Type of target object ('用户', '患者', '处方', '系统')
        target_id (int): ID of target object
        target_name (str): Name of target object
        details (str): Additional details about the operation
    """
    try:
        user = get_current_user()
        user_id = user['id'] if user else None
        user_name = user.get('username', '未知用户') if user else '系统'
        
        # Get IP address
        ip_address = request.remote_addr if request else 'N/A'
        
        conn = get_db_connection()
        cursor = conn.cursor()
        cursor.execute('''
            INSERT INTO operation_logs (
                operation_type, operation_category, target_type, target_id, 
                target_name, details, user_id, user_name, ip_address
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
        ''', (
            operation_type, operation_category, target_type, target_id,
            target_name, details, user_id, user_name, ip_address
        ))
        conn.commit()
        conn.close()
        
        # Also log to file
        log_message = f"[{operation_category.upper()}] {user_name}@{ip_address} - {operation_type} {target_type}"
        if target_name:
            log_message += f": {target_name}"
        if target_id:
            log_message += f" (ID: {target_id})"
        if details:
            log_message += f" | {details}"
        
        logger.info(log_message)
        
    except Exception as e:
        logger.error(f"Failed to log operation: {e}")


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
            # Log login event
            log_operation('login', 'auth', '系统', target_name=username, details='用户登录成功')
            logger.info(f"User '{username}' logged in from {request.remote_addr}")
            return redirect(URL_PREFIX + url_for('admin_dashboard'))
        else:
            error = '用户名或密码错误'
            logger.warning(f"Failed login attempt for username '{username}' from {request.remote_addr}")
    
    return render_template('login.html', error=error)


@app.route('/logout')
def logout():
    """
    Handle user logout.
    """
    user = get_current_user()
    if user:
        log_operation('logout', 'auth', '系统', target_name=user['username'], details='用户登出')
        logger.info(f"User '{user['username']}' logged out")
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
            "POST /packer/dispense - Record dispense log",
            "GET/POST /packer/settings/calibration - Calibration settings",
            "POST /packer/prescription/<id>/pill-size - Update pill size area"
        ]
    })


# ========================================
# Calibration Settings API Endpoints
# ========================================

@app.route('/packer/settings/calibration', methods=['GET', 'POST'])
def calibration_settings():
    """
    API endpoint to get/set calibration settings.
    
    GET - Returns current calibration settings:
        - reference_pill_diameter_mm (default: 9.0)
        
    POST - Updates calibration settings:
        - reference_pill_diameter_mm: Reference pill diameter in mm
    
    Returns:
        JSON response with calibration settings
    """
    conn = get_db_connection()
    cursor = conn.cursor()
    
    if request.method == 'POST':
        data = request.get_json()
        if not data:
            conn.close()
            return jsonify({"success": False, "message": "No data provided"}), 400
        
        # Update reference pill diameter if provided
        if 'reference_pill_diameter_mm' in data:
            diameter = float(data['reference_pill_diameter_mm'])
            cursor.execute('''
                INSERT INTO system_settings (key, value) VALUES (?, ?)
                ON CONFLICT(key) DO UPDATE SET value = excluded.value
            ''', ('reference_pill_diameter_mm', str(diameter)))
            conn.commit()
    
    # Get current settings
    settings = {}
    rows = cursor.execute('SELECT key, value FROM system_settings').fetchall()
    for row in rows:
        settings[row['key']] = row['value']
    
    conn.close()
    
    return jsonify({
        "success": True,
        "data": {
            "reference_pill_diameter_mm": float(settings.get('reference_pill_diameter_mm', 9.0))
        }
    })


@app.route('/packer/prescription/<int:prescription_id>/pill-size', methods=['POST'])
def update_prescription_pill_size(prescription_id):
    """
    API endpoint to update pill size area for a specific prescription.
    Called by the device after calibrating a new medicine.
    
    Request Body:
        - pill_size_area: Calibrated pill area in mm²
    
    Returns:
        JSON response with success status
    """
    try:
        data = request.get_json()
        if not data or 'pill_size_area' not in data:
            return jsonify({
                "success": False,
                "message": "pill_size_area is required"
            }), 400
        
        pill_size_area = float(data['pill_size_area'])
        
        conn = get_db_connection()
        cursor = conn.cursor()
        
        # Check if prescription exists
        prescription = cursor.execute(
            'SELECT id, medicine_name FROM prescriptions WHERE id = ?', 
            (prescription_id,)
        ).fetchone()
        
        if not prescription:
            conn.close()
            return jsonify({
                "success": False,
                "message": f"Prescription {prescription_id} not found"
            }), 404
        
        # Update pill size area
        cursor.execute(
            'UPDATE prescriptions SET pill_size_area = ? WHERE id = ?',
            (pill_size_area, prescription_id)
        )
        conn.commit()
        
        logger.info(f"Updated pill_size_area for prescription {prescription_id} ({prescription['medicine_name']}): {pill_size_area:.2f} mm²")
        conn.close()
        
        return jsonify({
            "success": True,
            "message": f"Pill size updated to {pill_size_area:.2f} mm²"
        })
        
    except Exception as e:
        logger.error(f"Error updating pill size: {str(e)}")
        return jsonify({
            "success": False,
            "message": f"Error updating pill size: {str(e)}"
        }), 500


@app.route('/packer/prescription/<int:prescription_id>/calibration', methods=['POST'])
def update_prescription_calibration(prescription_id):
    """
    API endpoint to update pill calibration data including optional image upload.
    Accepts multipart/form-data with:
        - pill_size_area: Calibrated pill area in mm² (required)
        - pill_image: Optional JPG image file
    
    Returns:
        JSON response with success status and image_resource_id if image was uploaded
    """
    try:
        # Get pill_size_area from form data
        pill_size_area_str = request.form.get('pill_size_area')
        if not pill_size_area_str:
            return jsonify({
                "success": False,
                "message": "pill_size_area is required"
            }), 400
        
        pill_size_area = float(pill_size_area_str)
        image_resource_id = None
        
        conn = get_db_connection()
        cursor = conn.cursor()
        
        # Check if prescription exists
        prescription = cursor.execute(
            'SELECT id, medicine_name FROM prescriptions WHERE id = ?', 
            (prescription_id,)
        ).fetchone()
        
        if not prescription:
            conn.close()
            return jsonify({
                "success": False,
                "message": f"Prescription {prescription_id} not found"
            }), 404
        
        # Handle image upload if provided
        if 'pill_image' in request.files:
            image_file = request.files['pill_image']
            if image_file and image_file.filename:
                # Generate unique filename: pill_{prescription_id}_{timestamp}.jpg
                filename = f"pill_{prescription_id}_{int(time.time())}.jpg"
                filepath = os.path.join(UPLOAD_FOLDER, filename)
                
                # Ensure upload folder exists
                os.makedirs(UPLOAD_FOLDER, exist_ok=True)
                
                # Save image file
                image_file.save(filepath)
                image_resource_id = filename
                logger.info(f"Saved pill image: {filename}")
        
        # Update database with pill_size_area and image_resource_id
        if image_resource_id:
            cursor.execute(
                'UPDATE prescriptions SET pill_size_area = ?, image_resource_id = ? WHERE id = ?',
                (pill_size_area, image_resource_id, prescription_id)
            )
        else:
            cursor.execute(
                'UPDATE prescriptions SET pill_size_area = ? WHERE id = ?',
                (pill_size_area, prescription_id)
            )
        
        conn.commit()
        
        logger.info(f"Updated calibration for prescription {prescription_id} ({prescription['medicine_name']}): "
                    f"area={pill_size_area:.2f}mm², image={image_resource_id or 'none'}")
        conn.close()
        
        return jsonify({
            "success": True,
            "message": f"Calibration updated successfully",
            "image_resource_id": image_resource_id
        })
        
    except Exception as e:
        logger.error(f"Error updating calibration: {str(e)}")
        return jsonify({
            "success": False,
            "message": f"Error updating calibration: {str(e)}"
        }), 500

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
        SELECT p.*, pt.patient_name, pt.bed_number 
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
            
            # Generate 6-digit zero-padded patient ID for barcode compatibility
            new_patient_id = generate_next_patient_id()
            
            cursor.execute('''
                INSERT INTO patients (id, patient_name, bed_number, profile_photo_resource_id)
                VALUES (?, ?, ?, ?)
            ''', (new_patient_id, patient_name, bed_number, photo_id))
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
            
            rx_id = rx.get('id')
            
            if rx_id:
                # Update existing record
                # NOTE: pill_size_area is preserved if client sends 0 or null
                # This prevents overwriting the calibrated value during sync
                client_pill_size = rx.get('pill_size_area')
                client_pill_size_value = float(client_pill_size) if client_pill_size and float(client_pill_size) > 0 else None
                
                # Preserve image_resource_id if client sends empty/null (same as pill_size_area)
                client_image_id = rx.get('image_resource_id')
                client_image_id_value = client_image_id if client_image_id else None
                
                cursor.execute('''
                    UPDATE prescriptions SET
                        patient_id = ?, medicine_name = ?, morning_dosage = ?, 
                        noon_dosage = ?, evening_dosage = ?, meal_timing = ?,
                        start_date = ?, duration_days = ?, last_dispensed_expiry_date = ?,
                        is_active = ?, 
                        pill_size_area = COALESCE(?, pill_size_area),
                        image_resource_id = COALESCE(?, image_resource_id)
                    WHERE id = ?
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
                    client_pill_size_value,  # NULL preserves existing value via COALESCE
                    client_image_id_value,   # NULL preserves existing value via COALESCE
                    rx_id
                ))
            else:
                # Insert new record
                cursor.execute('''
                    INSERT INTO prescriptions (
                        patient_id, medicine_name, morning_dosage, noon_dosage, evening_dosage,
                        meal_timing, start_date, duration_days, last_dispensed_expiry_date,
                        is_active, pill_size_area, image_resource_id
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
                    float(rx.get('pill_size_area', 0)) if rx.get('pill_size_area') else None,
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
    Display list of all users with optional search.
    """
    conn = get_db_connection()
    
    search_query = request.args.get('search', '').strip()
    
    if search_query:
        users = conn.execute('''
            SELECT * FROM users 
            WHERE username LIKE ?
            ORDER BY id
        ''', (f'%{search_query}%',)).fetchall()
    else:
        users = conn.execute('SELECT * FROM users ORDER BY id').fetchall()
    
    conn.close()
    
    users_list = [dict_from_row(u) for u in users]
    return render_template('users.html', users=users_list, search_query=search_query)


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
                INSERT INTO users (username, password_hash, can_edit_users, can_edit_patients, can_edit_prescriptions, can_view_logs)
                VALUES (?, ?, ?, ?, ?, ?)
            ''', (
                request.form['username'],
                password_hash,
                1 if request.form.get('can_edit_users') else 0,
                1 if request.form.get('can_edit_patients') else 0,
                1 if request.form.get('can_edit_prescriptions') else 0,
                1 if request.form.get('can_view_logs') else 0
            ))
            conn.commit()
            new_user_id = cursor.lastrowid
            log_operation('add', 'user', '用户', target_id=new_user_id, target_name=request.form['username'], 
                         details='新增用户')
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
                UPDATE users SET username=?, password_hash=?, 
                can_edit_users=?, can_edit_patients=?, can_edit_prescriptions=?, can_view_logs=?
                WHERE id=?
            ''', (
                request.form['username'],
                password_hash,
                1 if request.form.get('can_edit_users') else 0,
                1 if request.form.get('can_edit_patients') else 0,
                1 if request.form.get('can_edit_prescriptions') else 0,
                1 if request.form.get('can_view_logs') else 0,
                user_id
            ))
        else:
            cursor.execute('''
                UPDATE users SET username=?, 
                can_edit_users=?, can_edit_patients=?, can_edit_prescriptions=?, can_view_logs=?
                WHERE id=?
            ''', (
                request.form['username'],
                1 if request.form.get('can_edit_users') else 0,
                1 if request.form.get('can_edit_patients') else 0,
                1 if request.form.get('can_edit_prescriptions') else 0,
                1 if request.form.get('can_view_logs') else 0,
                user_id
            ))
        
        conn.commit()
        log_operation('edit', 'user', '用户', target_id=user_id, target_name=request.form['username'],
                     details='编辑用户信息')
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
    # Get user info before deletion for logging
    user = conn.execute('SELECT username FROM users WHERE id = ?', (user_id,)).fetchone()
    user_name = user['username'] if user else '未知'
    
    conn.execute('DELETE FROM users WHERE id = ?', (user_id,))
    conn.commit()
    conn.close()
    
    log_operation('delete', 'user', '用户', target_id=user_id, target_name=user_name,
                 details='删除用户')
    
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
        bed_number = request.form.get('bed_number', '').strip()
        
        # Check for duplicate bed number
        if bed_number:
            conn = get_db_connection()
            existing = conn.execute(
                'SELECT id FROM patients WHERE bed_number = ?', (bed_number,)
            ).fetchone()
            conn.close()
            if existing:
                return render_template('patient_form.html', patient=None, 
                                     error=f'床号 "{bed_number}" 已被使用，请选择其他床号')
        
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
        
        # Generate 6-digit zero-padded patient ID for barcode compatibility
        new_patient_id = generate_next_patient_id()
        
        cursor.execute('''
            INSERT INTO patients (id, patient_name, bed_number, profile_photo_resource_id)
            VALUES (?, ?, ?, ?)
        ''', (
            new_patient_id,
            request.form['patient_name'],
            bed_number,
            image_filename
        ))
        conn.commit()
        conn.close()
        
        log_operation('add', 'patient', '患者', target_id=new_patient_id, target_name=request.form['patient_name'],
                     details=f"床号: {request.form.get('bed_number', '-')}")
        
        return redirect(URL_PREFIX + url_for('manage_patients'))
    
    return render_template('patient_form.html', patient=None)


@app.route('/admin/patients/edit/<patient_id>', methods=['GET', 'POST'])
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
        bed_number = request.form.get('bed_number', '').strip()
        
        # Check for duplicate bed number (excluding current patient)
        if bed_number:
            existing = conn.execute(
                'SELECT id FROM patients WHERE bed_number = ? AND id != ?', (bed_number, patient_id)
            ).fetchone()
            if existing:
                conn.close()
                return render_template('patient_form.html', patient=dict_from_row(patient), 
                                     error=f'床号 "{bed_number}" 已被使用，请选择其他床号')
        
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
            bed_number,
            image_filename,
            patient_id
        ))
        conn.commit()
        conn.close()
        
        log_operation('edit', 'patient', '患者', target_id=patient_id, target_name=request.form['patient_name'],
                     details=f"床号: {request.form.get('bed_number', '-')}")
        
        return redirect(URL_PREFIX + url_for('manage_patients'))
    
    conn.close()
    return render_template('patient_form.html', patient=dict_from_row(patient))


@app.route('/admin/patients/delete/<patient_id>')
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
    
    patient_name = patient['patient_name'] if patient else '未知'
    log_operation('delete', 'patient', '患者', target_id=patient_id, target_name=patient_name,
                 details='删除患者及其所有关联数据')
    logger.info(f"Deleted patient {patient_id} ({patient_name}) and all associated data")
    return redirect(URL_PREFIX + url_for('manage_patients'))


# ========================================
# Prescription Management Routes
# ========================================

@app.route('/admin/prescriptions')
@permission_required('can_edit_prescriptions')
def manage_prescriptions():
    """
    Display list of all prescriptions with optional search.
    """
    conn = get_db_connection()
    
    search_query = request.args.get('search', '').strip()
    
    if search_query:
        prescriptions = conn.execute('''
            SELECT p.*, pt.patient_name, pt.bed_number 
            FROM prescriptions p
            LEFT JOIN patients pt ON p.patient_id = pt.id
            WHERE pt.patient_name LIKE ? OR p.medicine_name LIKE ? OR pt.bed_number LIKE ?
            ORDER BY p.id DESC
        ''', (f'%{search_query}%', f'%{search_query}%', f'%{search_query}%')).fetchall()
    else:
        prescriptions = conn.execute('''
            SELECT p.*, pt.patient_name, pt.bed_number 
            FROM prescriptions p
            LEFT JOIN patients pt ON p.patient_id = pt.id
            ORDER BY p.id DESC
        ''').fetchall()
    
    conn.close()
    
    prescriptions_list = [dict_from_row(p) for p in prescriptions]
    return render_template('prescriptions.html', prescriptions=prescriptions_list, search_query=search_query)


@app.route('/admin/prescriptions/add', methods=['GET', 'POST'])
@permission_required('can_edit_prescriptions')
def add_prescription():
    """
    Handle adding a new prescription with optional medicine image upload.
    """
    conn = get_db_connection()
    patients = conn.execute('SELECT id, patient_name, bed_number FROM patients ORDER BY patient_name').fetchall()
    
    if request.method == 'POST':
        cursor = conn.cursor()
        cursor.execute('''
            INSERT INTO prescriptions (
                patient_id, medicine_name, morning_dosage, noon_dosage, evening_dosage,
                meal_timing, start_date, duration_days, is_active, pill_size_area
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
            float(request.form['pill_size_area']) if request.form.get('pill_size_area') else None
        ))
        conn.commit()
        new_rx_id = cursor.lastrowid
        
        # Handle medicine image upload if provided
        image_resource_id = None
        if 'medicine_image' in request.files:
            file = request.files['medicine_image']
            if file and file.filename and allowed_file(file.filename):
                ext = file.filename.rsplit('.', 1)[1].lower()
                new_filename = f"med_{new_rx_id}_{int(time.time())}.{ext}"
                filepath = os.path.join(app.config['UPLOAD_FOLDER'], new_filename)
                file.save(filepath)
                image_resource_id = new_filename
                
                # Update the just-inserted record with image_resource_id
                cursor.execute(
                    'UPDATE prescriptions SET image_resource_id = ? WHERE id = ?',
                    (image_resource_id, new_rx_id)
                )
                conn.commit()
                logger.info(f"Saved medicine image for new prescription {new_rx_id}: {new_filename}")
        
        # Get patient name for log
        patient = conn.execute('SELECT patient_name FROM patients WHERE id = ?', 
                              (request.form['patient_id'],)).fetchone()
        patient_name = patient['patient_name'] if patient else '未知'
        conn.close()
        
        log_operation('add', 'prescription', '处方', target_id=new_rx_id, 
                     target_name=request.form['medicine_name'],
                     details=f"患者: {patient_name}" + (f", 图片: {image_resource_id}" if image_resource_id else ""))
        
        return redirect(URL_PREFIX + url_for('manage_prescriptions'))
    
    conn.close()
    patients_list = [dict_from_row(p) for p in patients]
    return render_template('prescription_form.html', prescription=None, patients=patients_list)


@app.route('/admin/prescriptions/edit/<int:prescription_id>', methods=['GET', 'POST'])
@permission_required('can_edit_prescriptions')
def edit_prescription(prescription_id):
    """
    Handle editing an existing prescription with optional medicine image upload/delete.
    """
    conn = get_db_connection()
    prescription = conn.execute('SELECT * FROM prescriptions WHERE id = ?', (prescription_id,)).fetchone()
    patients = conn.execute('SELECT id, patient_name, bed_number FROM patients ORDER BY patient_name').fetchall()
    
    if not prescription:
        conn.close()
        return "Prescription not found!", 404

    if request.method == 'POST':
        cursor = conn.cursor()
        pill_size_area = float(request.form['pill_size_area']) if request.form.get('pill_size_area') else None
        
        # Determine the image_resource_id to save
        current_image_id = prescription['image_resource_id']
        image_resource_id = current_image_id  # Default: keep existing
        
        # Check if user wants to delete current image
        if request.form.get('delete_image'):
            # Delete the file from disk
            if current_image_id:
                old_path = os.path.join(app.config['UPLOAD_FOLDER'], current_image_id)
                if os.path.exists(old_path):
                    try:
                        os.remove(old_path)
                        logger.info(f"Deleted medicine image: {current_image_id}")
                    except Exception as e:
                        logger.error(f"Failed to delete medicine image {current_image_id}: {e}")
            image_resource_id = None
        
        # Handle new image upload (overrides delete if both are set)
        if 'medicine_image' in request.files:
            file = request.files['medicine_image']
            if file and file.filename and allowed_file(file.filename):
                # Delete old image file if it exists
                if current_image_id:
                    old_path = os.path.join(app.config['UPLOAD_FOLDER'], current_image_id)
                    if os.path.exists(old_path):
                        try:
                            os.remove(old_path)
                        except Exception as e:
                            logger.error(f"Failed to delete old medicine image {current_image_id}: {e}")
                
                ext = file.filename.rsplit('.', 1)[1].lower()
                new_filename = f"med_{prescription_id}_{int(time.time())}.{ext}"
                filepath = os.path.join(app.config['UPLOAD_FOLDER'], new_filename)
                file.save(filepath)
                image_resource_id = new_filename
                logger.info(f"Updated medicine image for prescription {prescription_id}: {new_filename}")
        
        cursor.execute('''
            UPDATE prescriptions SET
                patient_id=?, medicine_name=?, morning_dosage=?, noon_dosage=?, evening_dosage=?,
                meal_timing=?, start_date=?, duration_days=?, is_active=?, pill_size_area=?,
                image_resource_id=?
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
            pill_size_area,
            image_resource_id,
            prescription_id
        ))
        conn.commit()
        
        # Get patient name for log
        patient = conn.execute('SELECT patient_name FROM patients WHERE id = ?',
                              (request.form['patient_id'],)).fetchone()
        patient_name = patient['patient_name'] if patient else '未知'
        conn.close()
        
        log_operation('edit', 'prescription', '处方', target_id=prescription_id,
                     target_name=request.form['medicine_name'],
                     details=f"患者: {patient_name}")
        
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
    # Get prescription info before deletion for logging
    rx = conn.execute('''
        SELECT p.medicine_name, pt.patient_name 
        FROM prescriptions p
        LEFT JOIN patients pt ON p.patient_id = pt.id
        WHERE p.id = ?
    ''', (prescription_id,)).fetchone()
    medicine_name = rx['medicine_name'] if rx else '未知'
    patient_name = rx['patient_name'] if rx else '未知'
    
    conn.execute('DELETE FROM prescriptions WHERE id = ?', (prescription_id,))
    conn.commit()
    conn.close()
    
    log_operation('delete', 'prescription', '处方', target_id=prescription_id,
                 target_name=medicine_name, details=f"患者: {patient_name}")
    
    return redirect(URL_PREFIX + url_for('manage_prescriptions'))


# ========================================
# Dispense Logs Routes
# ========================================

@app.route('/admin/dispense_logs')
@permission_required('can_view_logs')
def manage_dispense_logs():
    """
    Display dispense logs with optional filtering.
    """
    conn = get_db_connection()
    
    query = '''
        SELECT dl.*, p.patient_name, u.username as dispensed_by_name
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
# Logs Home & Operation Logs Routes
# ========================================

@app.route('/admin/logs')
@permission_required('can_view_logs')
def logs_home():
    """
    Display logs home page with links to different log types.
    """
    conn = get_db_connection()
    
    # Get statistics
    dispense_count = conn.execute('SELECT COUNT(*) FROM dispense_logs').fetchone()[0]
    operation_count = conn.execute('SELECT COUNT(*) FROM operation_logs').fetchone()[0]
    
    # Get recent records
    recent_dispense = conn.execute('''
        SELECT dl.*, p.patient_name
        FROM dispense_logs dl
        LEFT JOIN patients p ON dl.patient_id = p.id
        ORDER BY dl.created_at DESC LIMIT 5
    ''').fetchall()
    
    recent_operations = conn.execute('''
        SELECT * FROM operation_logs
        ORDER BY created_at DESC LIMIT 5
    ''').fetchall()
    
    conn.close()
    
    stats = {
        'dispense_count': dispense_count,
        'operation_count': operation_count
    }
    
    return render_template('logs_home.html',
                          stats=stats,
                          recent_dispense=[dict_from_row(l) for l in recent_dispense],
                          recent_operations=[dict_from_row(l) for l in recent_operations])


@app.route('/admin/operation_logs')
@permission_required('can_view_logs')
def manage_operation_logs():
    """
    Display operation logs with optional filtering.
    """
    conn = get_db_connection()
    
    query = 'SELECT * FROM operation_logs WHERE 1=1'
    params = []
    
    date_filter = request.args.get('date')
    category_filter = request.args.get('category')
    type_filter = request.args.get('operation_type')
    user_filter = request.args.get('user_name')
    
    if date_filter:
        query += ' AND DATE(created_at) = ?'
        params.append(date_filter)
    
    if category_filter:
        query += ' AND operation_category = ?'
        params.append(category_filter)
    
    if type_filter:
        query += ' AND operation_type = ?'
        params.append(type_filter)
    
    if user_filter:
        query += ' AND user_name = ?'
        params.append(user_filter)
    
    query += ' ORDER BY created_at DESC LIMIT 200'
    
    logs = conn.execute(query, params).fetchall()
    users = conn.execute('SELECT id, username FROM users ORDER BY username').fetchall()
    conn.close()
    
    return render_template('operation_logs.html',
                          logs=[dict_from_row(l) for l in logs],
                          users=[dict_from_row(u) for u in users],
                          date_filter=date_filter,
                          category_filter=category_filter,
                          type_filter=type_filter,
                          user_filter=user_filter)


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
