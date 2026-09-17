from flask import Flask, jsonify, request, render_template, redirect, url_for, session, send_file
from functools import wraps
import time
import sqlite3
import os
import logging
import re
from logging.handlers import RotatingFileHandler
from werkzeug.utils import secure_filename
from werkzeug.security import generate_password_hash, check_password_hash
from datetime import datetime
from io import BytesIO

# ========================================
# ReportLab PDF Generation Configuration
# ========================================
from reportlab.lib.pagesizes import A4
from reportlab.platypus import SimpleDocTemplate, Table, TableStyle, Paragraph, PageBreak, Flowable, Frame, PageTemplate, BaseDocTemplate
from reportlab.lib.styles import getSampleStyleSheet, ParagraphStyle
from reportlab.lib import colors
from reportlab.lib.units import mm
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfgen import canvas
from reportlab.graphics.barcode import code128

# Register Chinese TrueType Font (Windows / Linux system fonts)
font_registered = False
for font_path in [
    os.path.join(os.path.dirname(os.path.abspath(__file__)), 'static', 'fonts', 'simhei.ttf'),
    'C:/Windows/Fonts/simhei.ttf',
    'C:/Windows/Fonts/simsun.ttc',
    'C:/Windows/Fonts/msyh.ttc',
    '/usr/share/fonts/truetype/droid/DroidSansFallbackFull.ttf',  # Droid Sans Fallback
    '/usr/share/fonts/truetype/wqy/wqy-zenhei.ttc',               # WenQuanYi Zen Hei
    '/usr/share/fonts/truetype/wqy/wqy-microhei.ttc',             # WenQuanYi Micro Hei
    '/usr/share/fonts/truetype/arphic/gkai00mp.ttf',              # Arphic Kaiti
    '/usr/share/fonts/truetype/arphic/gbsn00lp.ttf',              # Arphic Sungti
    '/usr/share/fonts/opentype/noto/NotoSansCJK-Regular.ttc'      # Noto Sans CJK
]:
    if os.path.exists(font_path):
        try:
            pdfmetrics.registerFont(TTFont('SimHei', font_path))
            font_registered = True
            print(f"[{time.ctime()}] Registered Chinese font: {font_path}")
            break
        except Exception as e:
            print(f"[{time.ctime()}] Failed to register font {font_path}: {e}")

if not font_registered:
    print(f"[{time.ctime()}] Warning: No Chinese font registered. PDF export might not render Chinese characters properly.")


class DashedLabelFlowable(Flowable):
    """
    Custom Flowable to draw a square label with a solid border outline,
    the patient's name, and their patient ID barcode.
    """
    def __init__(self, patient_id, name, bed_number, width, height, font_name, font_size=12):
        Flowable.__init__(self)
        self.patient_id = patient_id
        self.name = name
        self.bed_number = bed_number
        self.width = width
        self.height = height
        self.font_name = font_name
        self.font_size = font_size
        
    def wrap(self, availWidth, availHeight):
        return self.width, self.height
        
    def draw(self):
        self.canv.saveState()
        
        # 1. Set solid line properties for the border
        self.canv.setStrokeColor(colors.HexColor('#CCCCCC'))
        self.canv.setLineWidth(0.5)
        
        # Draw the rectangle exactly on the boundary so adjacent cells share a border (zero gaps)
        self.canv.rect(0, 0, self.width, self.height)
        
        # 2. Draw bold patient name (centered horizontally in upper area)
        # Shifted up to 17.3mm to accommodate slightly larger font size (11pt) and faux bold stroke
        self.canv.saveState()
        self.canv.setStrokeColor(colors.black)
        self.canv.setLineWidth(0.2) # Stroke line width for faux bold
        
        text_name = self.canv.beginText()
        text_name.setTextRenderMode(2) # Fill and Stroke (faux bold)
        text_name.setFont(self.font_name, 11)
        text_width = self.canv.stringWidth(self.name, self.font_name, 11)
        name_x = (self.width - text_width) / 2
        text_name.setTextOrigin(name_x, 17.3 * mm)
        text_name.textOut(self.name)
        self.canv.drawText(text_name)
        
        # 2.5 Draw bold bed number (centered horizontally between barcode and name, only showing digits)
        # Positioned at 11.7mm to be mathematically centered between barcode top (9.9mm) and name baseline (17.3mm)
        bed_str = ''.join(c for c in str(self.bed_number) if c.isdigit())
        if not bed_str:
            bed_str = str(self.bed_number)
        
        text_bed = self.canv.beginText()
        text_bed.setTextRenderMode(2) # Fill and Stroke (faux bold)
        text_bed.setFont(self.font_name, 11)
        bed_width = self.canv.stringWidth(bed_str, self.font_name, 11)
        bed_x = (self.width - bed_width) / 2
        text_bed.setTextOrigin(bed_x, 11.7 * mm)
        text_bed.textOut(bed_str)
        self.canv.drawText(text_bed)
        
        self.canv.restoreState()
        
        # 3. Draw barcode (Code128)
        # Target size is 180px x 90px (18mm x 9mm)
        # Left-top position in Figma is (21, 121). Since height is 90px, bottom-left in ReportLab is:
        # x_pdf = 21 * 0.1 * mm = 2.1 * mm
        # y_pdf = (220 - 121 - 90) * 0.1 * mm = 9 * 0.1 * mm = 0.9 * mm
        try:
            # Generate Code128 barcode. The patient_id is zero-padded 6-digit.
            # Set quiet zone parameters to 0 so the barcode lines occupy the full 18mm width when scaled.
            barcode = code128.Code128(self.patient_id, barHeight=9 * mm, lquiet=0, rquiet=0, quiet=0)
            
            # Scale barcode dynamically to fit exactly 18mm x 9mm
            scale_x = (18 * mm) / barcode.width
            scale_y = (9 * mm) / barcode.height
            
            self.canv.saveState()
            self.canv.translate(2.1 * mm, 0.9 * mm)
            self.canv.scale(scale_x, scale_y)
            barcode.drawOn(self.canv, 0, 0)
            self.canv.restoreState()
        except Exception as e:
            # Fallback if barcode fails to render (print ID text instead)
            self.canv.setFont(self.font_name, 8)
            id_w = self.canv.stringWidth(self.patient_id, self.font_name, 8)
            self.canv.drawString((self.width - id_w) / 2, 4 * mm, self.patient_id)
            
        self.canv.restoreState()


class ZeroPaddingDocTemplate(SimpleDocTemplate):
    """
    Subclass of SimpleDocTemplate that enforces 0 margins/paddings on the
    underlying PDF Frame, preventing unwanted extra blank pages during generation.
    """
    def build(self, flowables, onFirstPage=lambda *a: None, onLaterPages=lambda *a: None, canvasmaker=canvas.Canvas):
        self._calc()
        frameT = Frame(
            self.leftMargin, 
            self.bottomMargin, 
            self.width, 
            self.height, 
            id='normal',
            leftPadding=0, 
            rightPadding=0, 
            topPadding=0, 
            bottomPadding=0
        )
        self.addPageTemplates([
            PageTemplate(id='First', frames=frameT, onPage=onFirstPage, pagesize=self.pagesize),
            PageTemplate(id='Later', frames=frameT, onPage=onLaterPages, pagesize=self.pagesize)
        ])
        BaseDocTemplate.build(self, flowables, canvasmaker=canvasmaker)


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
# URL_PREFIX = ''  # Local development mode
URL_PREFIX = '/nursing-rx'  # Uncomment this line for remote deployment

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
    conn.execute('PRAGMA foreign_keys = ON')
    return conn


def normalize_rfid_uid(value):
    """Normalize a hardware RFID UID to the canonical uppercase hex form."""
    raw = str(value or '').strip()
    if raw.upper().startswith('UID:'):
        raw = raw[4:]
    normalized = ''.join(raw.split()).upper()
    if not normalized:
        raise ValueError('RFID UID 不能为空')
    if len(normalized) > 64 or not re.fullmatch(r'[0-9A-F]+', normalized):
        raise ValueError(f'RFID UID "{value}" 格式无效，只允许十六进制字符')
    return normalized


def parse_rfid_uids(value):
    """Parse comma/newline/whitespace separated UIDs and remove duplicates."""
    if not str(value or '').strip():
        return []

    result = []
    seen = set()
    for item in re.split(r'[,;\s]+', str(value).strip()):
        if not item:
            continue
        uid = normalize_rfid_uid(item)
        if uid not in seen:
            seen.add(uid)
            result.append(uid)
    return result


def sync_patient_pill_boxes(conn, patient_id, rfid_uids):
    """Replace a patient's RFID bindings while preserving global UID uniqueness."""
    for uid in rfid_uids:
        owner = conn.execute(
            'SELECT patient_id FROM pill_boxes WHERE rfid_uid = ?', (uid,)
        ).fetchone()
        if owner and owner['patient_id'] != patient_id:
            raise ValueError(f'RFID UID {uid} 已绑定到患者 {owner["patient_id"]}')

    conn.execute('DELETE FROM pill_boxes WHERE patient_id = ?', (patient_id,))
    for uid in rfid_uids:
        conn.execute('''
            INSERT INTO pill_boxes (patient_id, rfid_uid, box_type, is_active)
            VALUES (?, ?, 'GENERAL', 1)
        ''', (patient_id, uid))


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

    # Physical pill boxes. A patient may use multiple RFID-tagged box shapes.
    cursor.execute('''
        CREATE TABLE IF NOT EXISTS pill_boxes (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            patient_id TEXT NOT NULL,
            rfid_uid TEXT NOT NULL UNIQUE,
            box_type TEXT NOT NULL DEFAULT 'GENERAL',
            display_name TEXT,
            is_active INTEGER NOT NULL DEFAULT 1,
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (patient_id) REFERENCES patients(id) ON DELETE CASCADE
        )
    ''')
    cursor.execute('''
        CREATE INDEX IF NOT EXISTS idx_pill_boxes_patient_id
        ON pill_boxes(patient_id)
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
            motor_speed REAL,
            servo_angle REAL,
            image_resource_id TEXT,
            dosage_spec TEXT DEFAULT '',
            created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (patient_id) REFERENCES patients(id) ON DELETE CASCADE
        )
    ''')
    
    # Migration: Add columns if they don't exist
    cursor.execute("PRAGMA table_info(prescriptions)")
    columns = [row[1] for row in cursor.fetchall()]
    if 'dosage_spec' not in columns:
        cursor.execute("ALTER TABLE prescriptions ADD COLUMN dosage_spec TEXT DEFAULT ''")
    if 'motor_speed' not in columns:
        cursor.execute("ALTER TABLE prescriptions ADD COLUMN motor_speed REAL")
    if 'servo_angle' not in columns:
        cursor.execute("ALTER TABLE prescriptions ADD COLUMN servo_angle REAL")
    
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
            "GET /packer/pill-boxes - Get active RFID pill-box bindings",
            "GET /packer/prescriptions - Get prescription list",
            "POST /packer/patients/upload - Upload patient data",
            "POST /packer/prescriptions/upload - Upload prescription data",
            "POST /packer/dispense - Record dispense log",
            "GET/POST /packer/settings/calibration - Calibration settings",
            "POST /packer/prescription/<id>/dispenser-settings - Update dispenser settings"
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


@app.route('/packer/prescription/<int:prescription_id>/dispenser-settings', methods=['POST'])
@app.route('/packer/prescription/<int:prescription_id>/pill-size', methods=['POST'])  # Deprecated alias
def update_prescription_dispenser_settings(prescription_id):
    """
    API endpoint to update motor speed and servo angle for a specific prescription.
    Called by the device after learning parameters from pulse width.
    
    Request Body:
        - motor_speed: Turntable motor speed (0.1 ~ 1.4)
        - servo_angle: Servo aperture angle (0.1 ~ 1.0)
    """
    try:
        data = request.get_json() or {}
        motor_speed = data.get('motor_speed')
        servo_angle = data.get('servo_angle')
        
        # Backward compatibility fallback
        if motor_speed is None and 'pill_size_area' in data:
            motor_speed = 0.3
            servo_angle = 0.7

        if motor_speed is None or servo_angle is None:
            return jsonify({
                "success": False,
                "message": "motor_speed and servo_angle are required"
            }), 400
        
        motor_speed = float(motor_speed)
        servo_angle = float(servo_angle)
        
        conn = get_db_connection()
        cursor = conn.cursor()
        
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
        
        cursor.execute(
            'UPDATE prescriptions SET motor_speed = ?, servo_angle = ? WHERE id = ?',
            (motor_speed, servo_angle, prescription_id)
        )
        conn.commit()
        
        logger.info(f"Updated dispenser settings for prescription {prescription_id} ({prescription['medicine_name']}): motor={motor_speed:.2f}, servo={servo_angle:.2f}")
        conn.close()
        
        return jsonify({
            "success": True,
            "message": f"Dispenser settings updated: motor={motor_speed:.2f}, servo={servo_angle:.2f}"
        })
        
    except Exception as e:
        logger.error(f"Error updating dispenser settings: {str(e)}")
        return jsonify({
            "success": False,
            "message": f"Error updating dispenser settings: {str(e)}"
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
        # Get motor_speed and servo_angle from form data
        motor_speed_str = request.form.get('motor_speed')
        servo_angle_str = request.form.get('servo_angle')
        
        # Fallback for old forms sending pill_size_area
        if not motor_speed_str and request.form.get('pill_size_area'):
            motor_speed_str = "0.3"
            servo_angle_str = "0.7"

        motor_speed = float(motor_speed_str) if motor_speed_str else None
        servo_angle = float(servo_angle_str) if servo_angle_str else None
        image_resource_id = None
        
        conn = get_db_connection()
        cursor = conn.cursor()
        
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
                filename = f"pill_{prescription_id}_{int(time.time())}.jpg"
                filepath = os.path.join(UPLOAD_FOLDER, filename)
                os.makedirs(UPLOAD_FOLDER, exist_ok=True)
                image_file.save(filepath)
                image_resource_id = filename
                logger.info(f"Saved pill image: {filename}")
        
        # Update database with motor_speed, servo_angle and image_resource_id
        if image_resource_id and motor_speed is not None:
            cursor.execute(
                'UPDATE prescriptions SET motor_speed = ?, servo_angle = ?, image_resource_id = ? WHERE id = ?',
                (motor_speed, servo_angle, image_resource_id, prescription_id)
            )
        elif motor_speed is not None:
            cursor.execute(
                'UPDATE prescriptions SET motor_speed = ?, servo_angle = ? WHERE id = ?',
                (motor_speed, servo_angle, prescription_id)
            )
        elif image_resource_id:
            cursor.execute(
                'UPDATE prescriptions SET image_resource_id = ? WHERE id = ?',
                (image_resource_id, prescription_id)
            )
        
        conn.commit()
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


@app.route('/packer/pill-boxes', methods=['GET'])
def get_pill_boxes_for_dispensing():
    """Return active RFID-to-patient bindings for the Windows client."""
    conn = get_db_connection()
    boxes = conn.execute('''
        SELECT pb.id, pb.patient_id, pb.rfid_uid, pb.box_type,
               pb.display_name, pb.is_active, pb.created_at,
               pt.patient_name, pt.bed_number
        FROM pill_boxes pb
        JOIN patients pt ON pt.id = pb.patient_id
        WHERE pb.is_active = 1
        ORDER BY pb.patient_id, pb.id
    ''').fetchall()
    conn.close()

    boxes_list = [dict_from_row(box) for box in boxes]
    return jsonify({
        "success": True,
        "data": boxes_list,
        "count": len(boxes_list)
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
                # NOTE: motor_speed and servo_angle are preserved via COALESCE if client sends 0 or null
                client_motor = rx.get('motor_speed')
                client_motor_value = float(client_motor) if client_motor and float(client_motor) > 0 else None

                client_servo = rx.get('servo_angle')
                client_servo_value = float(client_servo) if client_servo and float(client_servo) > 0 else None
                
                client_image_id = rx.get('image_resource_id')
                client_image_id_value = client_image_id if client_image_id else None
                
                cursor.execute('''
                    UPDATE prescriptions SET
                        patient_id = ?, medicine_name = ?, morning_dosage = ?, 
                        noon_dosage = ?, evening_dosage = ?, meal_timing = ?,
                        start_date = ?, duration_days = ?, last_dispensed_expiry_date = ?,
                        is_active = ?, 
                        motor_speed = COALESCE(?, motor_speed),
                        servo_angle = COALESCE(?, servo_angle),
                        image_resource_id = COALESCE(?, image_resource_id),
                        dosage_spec = ?
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
                    client_motor_value,   # NULL preserves existing value via COALESCE
                    client_servo_value,   # NULL preserves existing value via COALESCE
                    client_image_id_value,   # NULL preserves existing value via COALESCE
                    rx.get('dosage_spec', ''),
                    rx_id
                ))
            else:
                # Insert new record
                cursor.execute('''
                    INSERT INTO prescriptions (
                        patient_id, medicine_name, morning_dosage, noon_dosage, evening_dosage,
                        meal_timing, start_date, duration_days, last_dispensed_expiry_date,
                        is_active, motor_speed, servo_angle, image_resource_id, dosage_spec
                    ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
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
                    float(rx.get('motor_speed', 0)) if rx.get('motor_speed') else None,
                    float(rx.get('servo_angle', 0)) if rx.get('servo_angle') else None,
                    rx.get('image_resource_id', ''),
                    rx.get('dosage_spec', '')
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
            SELECT p.*, GROUP_CONCAT(pb.rfid_uid, ', ') AS rfid_uids
            FROM patients p
            LEFT JOIN pill_boxes pb ON pb.patient_id = p.id AND pb.is_active = 1
            WHERE p.patient_name LIKE ? OR p.bed_number LIKE ?
            GROUP BY p.id
            ORDER BY p.id
        ''', (f'%{search_query}%', f'%{search_query}%')).fetchall()
    else:
        patients = conn.execute('''
            SELECT p.*, GROUP_CONCAT(pb.rfid_uid, ', ') AS rfid_uids
            FROM patients p
            LEFT JOIN pill_boxes pb ON pb.patient_id = p.id AND pb.is_active = 1
            GROUP BY p.id
            ORDER BY p.id
        ''').fetchall()
    
    conn.close()
    
    patients_list = [dict_from_row(p) for p in patients]
    return render_template('patients.html', patients=patients_list, search_query=search_query)


# ========================================
# Patient PDF Label Export Routes
# ========================================

@app.route('/admin/patients/labels', methods=['GET'])
@permission_required('can_edit_patients')
def export_patient_labels_view():
    """
    Display page to select patient label quantities for A4 PDF export.
    """
    conn = get_db_connection()
    patients = conn.execute('SELECT * FROM patients ORDER BY id').fetchall()
    conn.close()
    
    patients_list = [dict_from_row(p) for p in patients]
    return render_template('patient_labels.html', patients=patients_list)


@app.route('/admin/patients/labels/export', methods=['POST'])
@permission_required('can_edit_patients')
def export_patient_labels_pdf():
    """
    Generate and download print-ready A4 PDF with 2.2cm x 2.2cm patient labels.
    """
    conn = get_db_connection()
    patients = conn.execute('SELECT * FROM patients').fetchall()
    conn.close()
    
    label_items = []
    for p in patients:
        qty_field = f'qty_{p["id"]}'
        qty_val = request.form.get(qty_field, '0')
        try:
            qty = int(qty_val)
        except ValueError:
            qty = 0
            
        # Add patient details qty times
        for _ in range(qty):
            label_items.append({
                'id': p['id'],
                'name': p['patient_name'],
                'bed_number': p['bed_number']
            })
            
    if not label_items:
        return redirect(URL_PREFIX + url_for('export_patient_labels_view'))
        
    # Generate PDF in memory
    buffer = BytesIO()
    
    # Page dimensions (A4 is 210mm x 297mm)
    # Target grid is 8 cols x 12 rows = 96 labels per page.
    # Label is 22mm x 22mm.
    # Margins: Left/Right = (210 - 176)/2 = 17mm, Top/Bottom = 15mm (provides 3mm buffer to prevent empty page overflow)
    doc = ZeroPaddingDocTemplate(
        buffer,
        pagesize=A4,
        leftMargin=17 * mm,
        rightMargin=17 * mm,
        topMargin=15 * mm,
        bottomMargin=15 * mm
    )
    
    styles = getSampleStyleSheet()
    label_style = ParagraphStyle(
        name='PatientLabelStyle',
        fontName='SimHei' if font_registered else 'Helvetica',
        fontSize=12,
        leading=14,
        alignment=1,  # Center
        textColor=colors.HexColor('#000000')
    )
    
    story = []
    
    # Split label_items into pages (96 labels per page)
    labels_per_page = 96
    cols = 8
    rows = 12
    
    for page_idx in range(0, len(label_items), labels_per_page):
        page_items = label_items[page_idx : page_idx + labels_per_page]
        
        # Build 12x8 grid
        grid_data = []
        for r in range(rows):
            row_data = []
            for c in range(cols):
                item_idx = r * cols + c
                if item_idx < len(page_items):
                    item = page_items[item_idx]
                    # Wrap in DashedLabelFlowable for outline, name, bed number and barcode
                    row_data.append(DashedLabelFlowable(
                        item['id'],
                        item['name'], 
                        item.get('bed_number', ''),
                        22 * mm, 
                        22 * mm, 
                        'SimHei' if font_registered else 'Helvetica', 
                        12
                    ))
                else:
                    row_data.append("")
            grid_data.append(row_data)
            
        # Create Table for this page
        t = Table(grid_data, colWidths=[22 * mm] * cols, rowHeights=[22 * mm] * rows)
        t.setStyle(TableStyle([
            # No global GRID: borders are drawn by DashedLabelFlowable only on active cells
            ('VALIGN', (0, 0), (-1, -1), 'MIDDLE'),
            ('ALIGN', (0, 0), (-1, -1), 'CENTER'),
            ('BOTTOMPADDING', (0, 0), (-1, -1), 0),
            ('TOPPADDING', (0, 0), (-1, -1), 0),
            ('LEFTPADDING', (0, 0), (-1, -1), 0),
            ('RIGHTPADDING', (0, 0), (-1, -1), 0),
        ]))
        
        story.append(t)
        
        # If there are more pages, append a PageBreak
        if page_idx + labels_per_page < len(label_items):
            story.append(PageBreak())
            
    # Build PDF
    doc.build(story)
    
    # Log operation
    log_operation('export', 'patient', '患者', details=f"导出 A4 PDF 标签，共 {len(label_items)} 张")
    
    buffer.seek(0)
    return send_file(
        buffer,
        as_attachment=True,
        download_name=f"patient_labels_{int(time.time())}.pdf",
        mimetype='application/pdf'
    )


@app.route('/admin/patients/add', methods=['GET', 'POST'])
@permission_required('can_edit_patients')
def add_patient():
    """
    Handle adding a new patient.
    """
    if request.method == 'POST':
        bed_number = request.form.get('bed_number', '').strip()
        try:
            rfid_uids = parse_rfid_uids(request.form.get('rfid_uids', ''))
        except ValueError as exc:
            return render_template('patient_form.html', patient=dict(request.form), error=str(exc))
        
        # Check for duplicate bed number
        if bed_number:
            conn = get_db_connection()
            existing = conn.execute(
                'SELECT id FROM patients WHERE bed_number = ?', (bed_number,)
            ).fetchone()
            conn.close()
            if existing:
                return render_template('patient_form.html', patient=dict(request.form),
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
        
        try:
            cursor.execute('''
                INSERT INTO patients (id, patient_name, bed_number, profile_photo_resource_id)
                VALUES (?, ?, ?, ?)
            ''', (
                new_patient_id,
                request.form['patient_name'],
                bed_number,
                image_filename
            ))
            sync_patient_pill_boxes(conn, new_patient_id, rfid_uids)
            conn.commit()
        except ValueError as exc:
            conn.rollback()
            conn.close()
            return render_template('patient_form.html', patient=dict(request.form), error=str(exc))
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
        try:
            rfid_uids = parse_rfid_uids(request.form.get('rfid_uids', ''))
        except ValueError as exc:
            patient_data = dict_from_row(patient)
            patient_data.update(dict(request.form))
            conn.close()
            return render_template('patient_form.html', patient=patient_data, error=str(exc))
        
        # Check for duplicate bed number (excluding current patient)
        if bed_number:
            existing = conn.execute(
                'SELECT id FROM patients WHERE bed_number = ? AND id != ?', (bed_number, patient_id)
            ).fetchone()
            if existing:
                patient_data = dict_from_row(patient)
                patient_data.update(dict(request.form))
                conn.close()
                return render_template('patient_form.html', patient=patient_data,
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
        try:
            cursor.execute('''
                UPDATE patients SET patient_name=?, bed_number=?, profile_photo_resource_id=?
                WHERE id=?
            ''', (
                request.form['patient_name'],
                bed_number,
                image_filename,
                patient_id
            ))
            sync_patient_pill_boxes(conn, patient_id, rfid_uids)
            conn.commit()
        except ValueError as exc:
            conn.rollback()
            patient_data = dict_from_row(patient)
            patient_data.update(dict(request.form))
            conn.close()
            return render_template('patient_form.html', patient=patient_data, error=str(exc))
        conn.close()
        
        log_operation('edit', 'patient', '患者', target_id=patient_id, target_name=request.form['patient_name'],
                     details=f"床号: {request.form.get('bed_number', '-')}")
        
        return redirect(URL_PREFIX + url_for('manage_patients'))
    
    patient_data = dict_from_row(patient)
    box_rows = conn.execute(
        'SELECT rfid_uid FROM pill_boxes WHERE patient_id = ? AND is_active = 1 ORDER BY id',
        (patient_id,)
    ).fetchall()
    patient_data['rfid_uids'] = '\n'.join(row['rfid_uid'] for row in box_rows)
    conn.close()
    return render_template('patient_form.html', patient=patient_data)


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
    # Delete RFID pill-box bindings
    conn.execute('DELETE FROM pill_boxes WHERE patient_id = ?', (patient_id,))
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
    Display list of all prescriptions with optional search and patient filtering.
    """
    conn = get_db_connection()
    
    search_query = request.args.get('search', '').strip()
    patient_id = request.args.get('patient_id', '').strip()
    
    filtered_patient_name = None
    if patient_id:
        patient_row = conn.execute('SELECT patient_name FROM patients WHERE id = ?', (patient_id,)).fetchone()
        if patient_row:
            filtered_patient_name = patient_row['patient_name']
            
    if patient_id and search_query:
        prescriptions = conn.execute('''
            SELECT p.*, pt.patient_name, pt.bed_number 
            FROM prescriptions p
            LEFT JOIN patients pt ON p.patient_id = pt.id
            WHERE p.patient_id = ? AND (p.medicine_name LIKE ? OR pt.bed_number LIKE ?)
            ORDER BY p.id DESC
        ''', (patient_id, f'%{search_query}%', f'%{search_query}%')).fetchall()
    elif patient_id:
        prescriptions = conn.execute('''
            SELECT p.*, pt.patient_name, pt.bed_number 
            FROM prescriptions p
            LEFT JOIN patients pt ON p.patient_id = pt.id
            WHERE p.patient_id = ?
            ORDER BY p.id DESC
        ''', (patient_id,)).fetchall()
    elif search_query:
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
    return render_template('prescriptions.html', 
                           prescriptions=prescriptions_list, 
                           search_query=search_query,
                           patient_id=patient_id,
                           filtered_patient_name=filtered_patient_name)


@app.route('/admin/prescriptions/add', methods=['GET', 'POST'])
@permission_required('can_edit_prescriptions')
def add_prescription():
    """
    Handle adding a new prescription with optional medicine image upload.
    """
    conn = get_db_connection()
    patients = conn.execute('SELECT id, patient_name, bed_number FROM patients ORDER BY patient_name').fetchall()
    default_patient_id = request.args.get('patient_id', '').strip()
    
    if request.method == 'POST':
        is_long_term = request.form.get('is_long_term')
        if is_long_term:
            duration_days = 99999
        else:
            try:
                duration_days = int(request.form.get('duration_days', 7))
            except ValueError:
                duration_days = 7
                
        motor_speed = float(request.form['motor_speed']) if (request.form.get('motor_speed') and float(request.form['motor_speed']) > 0) else None
        servo_angle = float(request.form['servo_angle']) if (request.form.get('servo_angle') and float(request.form['servo_angle']) > 0) else None

        cursor = conn.cursor()
        cursor.execute('''
            INSERT INTO prescriptions (
                patient_id, medicine_name, morning_dosage, noon_dosage, evening_dosage,
                meal_timing, start_date, duration_days, is_active, motor_speed, servo_angle, dosage_spec
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
        ''', (
            request.form['patient_id'],
            request.form['medicine_name'],
            float(request.form.get('morning_dosage', 0)),
            float(request.form.get('noon_dosage', 0)),
            float(request.form.get('evening_dosage', 0)),
            request.form.get('meal_timing', ''),
            request.form['start_date'],
            duration_days,
            1 if request.form.get('is_active') else 0,
            motor_speed,
            servo_angle,
            request.form.get('dosage_spec', '')
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
        
        if default_patient_id:
            return redirect(URL_PREFIX + url_for('manage_prescriptions', patient_id=default_patient_id))
        else:
            return redirect(URL_PREFIX + url_for('manage_prescriptions'))
    
    conn.close()
    patients_list = [dict_from_row(p) for p in patients]
    return render_template('prescription_form.html', prescription=None, patients=patients_list, default_patient_id=default_patient_id)


@app.route('/admin/prescriptions/edit/<int:prescription_id>', methods=['GET', 'POST'])
@permission_required('can_edit_prescriptions')
def edit_prescription(prescription_id):
    """
    Handle editing an existing prescription with optional medicine image upload/delete.
    """
    conn = get_db_connection()
    prescription = conn.execute('SELECT * FROM prescriptions WHERE id = ?', (prescription_id,)).fetchone()
    patients = conn.execute('SELECT id, patient_name, bed_number FROM patients ORDER BY patient_name').fetchall()
    default_patient_id = request.args.get('patient_id', '').strip()
    
    if not prescription:
        conn.close()
        return "Prescription not found!", 404

    if request.method == 'POST':
        cursor = conn.cursor()
        
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
        is_long_term = request.form.get('is_long_term')
        if is_long_term:
            duration_days = 99999
        else:
            try:
                duration_days = int(request.form.get('duration_days', 7))
            except ValueError:
                duration_days = 7
                
        motor_speed = float(request.form['motor_speed']) if request.form.get('motor_speed') else None
        servo_angle = float(request.form['servo_angle']) if request.form.get('servo_angle') else None
        
        cursor.execute('''
            UPDATE prescriptions SET
                patient_id=?, medicine_name=?, morning_dosage=?, noon_dosage=?, evening_dosage=?,
                meal_timing=?, start_date=?, duration_days=?, is_active=?, motor_speed=?, servo_angle=?,
                image_resource_id=?, dosage_spec=?
            WHERE id=?
        ''', (
            request.form['patient_id'],
            request.form['medicine_name'],
            float(request.form.get('morning_dosage', 0)),
            float(request.form.get('noon_dosage', 0)),
            float(request.form.get('evening_dosage', 0)),
            request.form.get('meal_timing', ''),
            request.form['start_date'],
            duration_days,
            1 if request.form.get('is_active') else 0,
            motor_speed,
            servo_angle,
            image_resource_id,
            request.form.get('dosage_spec', ''),
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
        
        if default_patient_id:
            return redirect(URL_PREFIX + url_for('manage_prescriptions', patient_id=default_patient_id))
        else:
            return redirect(URL_PREFIX + url_for('manage_prescriptions'))
    
    conn.close()
    return render_template('prescription_form.html', 
                          prescription=dict_from_row(prescription), 
                          patients=[dict_from_row(p) for p in patients],
                          default_patient_id=default_patient_id)


@app.route('/admin/prescriptions/delete/<int:prescription_id>')
@permission_required('can_edit_prescriptions')
def delete_prescription(prescription_id):
    """
    Handle deleting a prescription.
    """
    conn = get_db_connection()
    patient_id = request.args.get('patient_id', '').strip()
    
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
    
    if patient_id:
        return redirect(URL_PREFIX + url_for('manage_prescriptions', patient_id=patient_id))
    else:
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
    app.run(host='0.0.0.0', port=5068, debug=True)
