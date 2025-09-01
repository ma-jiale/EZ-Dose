from flask import Flask, jsonify, request
from flask_cors import CORS
import csv
import os

app = Flask(__name__)
CORS(app)  # Enable CORS for all routes

# File paths
PRESCRIPTIONS_FILE = 'prescriptions_data.csv'
PATIENTS_FILE = 'patients.csv'

def read_csv_safe(filename):
    """Safely read CSV file and return data or empty list if file doesn't exist"""
    try:
        if os.path.exists(filename):
            data = []
            with open(filename, 'r', encoding='utf-8') as file:
                csv_reader = csv.DictReader(file)
                for row in csv_reader:
                    data.append(dict(row))
            return data
        else:
            return []
    except Exception as e:
        print(f"Error reading {filename}: {e}")
        return []

def write_csv_safe(filename, data, fieldnames):
    """Safely write data to CSV file"""
    try:
        with open(filename, 'w', newline='', encoding='utf-8') as file:
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

@app.route('/patients', methods=['GET'])
def get_patients_for_dispensing():
    """Get all patients"""
    patients = read_csv_safe(PATIENTS_FILE)
    return jsonify({
        "success": True,
        "data": patients,
        "count": len(patients)
    })

@app.route('/prescriptions', methods=['GET'])
def get_prescriptions_for_dispensing():
    """Get all prescriptions"""
    prescriptions = read_csv_safe(PRESCRIPTIONS_FILE)
    return jsonify({
        "success": True,
        "data": prescriptions,
        "count": len(prescriptions)
    })

@app.route('/patients/upload', methods=['POST'])
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

@app.route('/prescriptions/upload', methods=['POST'])
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

if __name__ == '__main__':
    print("Starting Pill Dispenser Local Server...")
    print(f"Patients file: {PATIENTS_FILE}")
    print(f"Prescriptions file: {PRESCRIPTIONS_FILE}")
     
    # Check if files exist and show their status
    if os.path.exists(PATIENTS_FILE):
        patients_data = read_csv_safe(PATIENTS_FILE)
        print(f"Patients file loaded with {len(patients_data)} records")
    else:
        print(f"Warning: {PATIENTS_FILE} not found!")
        
    if os.path.exists(PRESCRIPTIONS_FILE):
        prescriptions_data = read_csv_safe(PRESCRIPTIONS_FILE)
        print(f"Prescriptions file loaded with {len(prescriptions_data)} records")
    else:
        print(f"Warning: {PRESCRIPTIONS_FILE} not found!")
    
    app.run(host='0.0.0.0', port=5000, debug=True)