import os
import pandas as pd
from datetime import datetime, timedelta
import numpy as np
import requests

class PatientPrescriptionManager:
    def __init__(self, server_url="http://localhost:5000"):
        self.df = None
        self.csv_file_path = "local_prescriptions_data.csv"
        # 添加当前处理的处方数据
        self.current_prescription_data = None
        self.current_dispensing_days = {}  # 存储每个药物的配药天数
        self.server_url = server_url

    def load_prescriptions(self):
        """load prescriptions from server, if can't, read local prescriptions"""
        try:
            # Try to fetch from server first
            success = self.fetch_online_prescriptions()
            if success:
                print("[Info] Successfully loaded prescriptions from server")
                return True
            else:
                print("[Warning] Failed to load from server, using local data")
                self.read_local_prescriptions()
                return False
        except Exception as e:
            print(f"[Error] Exception in load_prescriptions: {e}")
            self.read_local_prescriptions()
            return False

    def read_local_prescriptions(self):
        try:
            if os.path.exists(self.csv_file_path):
                self.df = pd.read_csv(self.csv_file_path, encoding='utf-8')
        except Exception as e:
            print(f"[error] fail to load local prescriptions: {e}")

    def write_local_prescriptions(self):
        """write prescriptions in memory into local csv"""
        try:
            if self.df is not None and not self.df.empty:
                self.df.to_csv(self.csv_file_path, index=False, encoding='utf-8')
                print(f"[Info] Successfully wrote {len(self.df)} prescriptions to {self.csv_file_path}")
                return True
            else:
                print("[Warning] No prescription data to write")
                return False
        except Exception as e:
            print(f"[Error] Failed to write local prescriptions: {e}")
            return False

    def fetch_online_prescriptions(self):
        """load prescriptions from server"""
        try:
            response = requests.get(f"{self.server_url}/prescriptions", timeout=10)
            if response.status_code == 200:
                data = response.json()
                if data.get('success') and data.get('data'):
                    # Convert to DataFrame
                    self.df = pd.DataFrame(data['data'])
                    # Save to local file as backup
                    self.write_local_prescriptions()
                    print(f"[Info] Fetched {len(self.df)} prescriptions from server")
                    return True
                else:
                    print("[Warning] Server returned empty or invalid prescription data")
                    return False
            else:
                print(f"[Warning] Server returned status code: {response.status_code}")
                return False
        except requests.exceptions.RequestException as e:
            print(f"[Error] Network error fetching prescriptions: {e}")
            return False
        except Exception as e:
            print(f"[Error] Error fetching online prescriptions: {e}")
            return False

    def upload_prescriptions_to_server(self):
        """Upload local prescriptions to server"""
        try:
            if self.df is None or self.df.empty:
                print("[Warning] No prescription data to upload")
                return False
            
            # Convert DataFrame to list of dictionaries
            prescriptions_data = self.df.to_dict('records')
            
            # Prepare payload for server
            payload = {
                "prescriptions": prescriptions_data
            }
            
            # Send to server
            response = requests.post(
                f"{self.server_url}/prescriptions/upload",
                json=payload,
                headers={'Content-Type': 'application/json'},
                timeout=30
            )
            
            if response.status_code == 200:
                result = response.json()
                if result.get('success'):
                    print(f"[Info] Successfully uploaded {len(prescriptions_data)} prescriptions to server")
                    return True
                else:
                    print(f"[Error] Server rejected upload: {result.get('message', 'Unknown error')}")
                    return False
            else:
                print(f"[Error] Upload failed with status code: {response.status_code}")
                return False
                
        except requests.exceptions.RequestException as e:
            print(f"[Error] Network error uploading prescriptions: {e}")
            return False
        except Exception as e:
            print(f"[Error] Error uploading prescriptions to server: {e}")
            return False

    def get_patient_prescription(self, patientId):
        """
        Get patient prescription information by patient ID
        Args:
            patientId: Patient identifier (RFID or patient ID)
        Returns:
            Tuple[bool, Dict]
            Dict: Patient info and prescription details or error msg
        """
        try:
            # Basic validation
            if self.df is None or self.df.empty:
                return False, {'error': 'Database not available', 'error_code': 503}
            
            if not patientId or not str(patientId).strip():
                return False, {'error': 'Patient ID required', 'error_code': 400}
            
            identifier = str(patientId).strip()
            
            # Find patient records by RFID or ID
            records = self.df[
                (self.df['rfid'].astype(str).str.strip() == identifier) |
                (self.df['patientId'].astype(str).str.strip() == identifier)
            ]
            
            if records.empty:
                return False, {'error': f'No prescription found for: {identifier}', 'error_code': 404}
            
            # Get patient info from first record
            first_record = records.iloc[0]
            patient_info = {
                'patient_name': first_record['patient_name'],
                'patient_id': first_record['patientId'],
                'rfid': first_record['rfid'],
            }
            
            # Extract medicines
            medicines = []
            for _, record in records.iterrows():
                medicine = {
                    'medicine_name': record['medicine_name'],
                    'morning_dosage': int(record['morning_dosage']),
                    'noon_dosage': int(record['noon_dosage']),
                    'evening_dosage': int(record['evening_dosage']),
                    'meal_timing': record['meal_timing'],
                    'start_date': record['start_date'],
                    'duration_days': int(record['duration_days']),
                    'last_dispensed_expiry_date': record['last_dispensed_expiry_date'],
                    'is_active': int(record['is_active']),
                    'pill_size': record['pill_size']  # Add pill_size field
                }
                # put active medicine into medicine list
                if medicine['is_active']:
                    medicines.append(medicine)
                
            patient_prescription = {
                'patient_info': patient_info,
                'medicines': medicines
            }
            return True, patient_prescription
            
        except Exception as e:
            return False, {'error': f'Query error: {str(e)}', 'error_code': 500}

    def generate_pills_dispensing_list(self, patientId, max_days=7):
        """
        Generate pills dispensing list with new dispensing pattern logic
        
        Args:
            patientId: Patient identifier (RFID or patient ID)
            max_days: Maximum days to dispense
            
        Returns:
            Tuple[bool, Dict]: (success flag, dispensing list dict or error message dict)
        """
        try:
            # Get prescription data
            success, prescription_data = self.get_patient_prescription(patientId)
            if not success:
                return False, prescription_data
            
            # 存储当前处方数据
            self.current_prescription_data = prescription_data
            self.current_dispensing_days = {}
            
            medicines = prescription_data['medicines']
            patient_name = prescription_data['patient_info']['patient_name']
            print(f"[Debug] Found {len(medicines)} medicines for patient {patient_name}")
            
            # Check meal timing types
            has_before_meal, has_after_meal = self._check_meal_timing_types(medicines)
            
            # Create dispensing list structure
            pills_dispensing_list = {
                "patient_info": prescription_data['patient_info'],
                "medicines_1": [],
                "medicines_2": []
            }
            
            # Process each medicine based on meal timing logic
            for medicine in medicines:
                # 计算并存储配药天数
                dispensing_days = self._calculate_dispensing_days(medicine, max_days)
                self.current_dispensing_days[medicine['medicine_name']] = dispensing_days
                
                if dispensing_days > 0:
                    if has_before_meal and has_after_meal:
                        # Both meal timings exist - use 6-row logic
                        self._process_medicine_dual_timing(medicine, dispensing_days, pills_dispensing_list)
                    else:
                        # Only one meal timing - use 3-row logic
                        self._process_medicine_single_timing(medicine, dispensing_days, pills_dispensing_list)
            
            return True, pills_dispensing_list
            
        except Exception as e:
            return False, {'error': f'Failed to generate dispensing matrix: {str(e)}', 'error_code': 500}

    def _process_medicine_single_timing(self, medicine, dispensing_days, pills_dispensing_list):
        """Process medicine when only one meal timing exists (4x7 matrix)"""
        medicine_name = medicine['medicine_name']
        meal_time = medicine.get('meal_timing', 'before')
        pill_size = medicine.get('pill_size', 'M')
        
        # Debug info
        daily_total = medicine['morning_dosage'] + medicine['noon_dosage'] + medicine['evening_dosage']
        print(f"  - {medicine_name}: {dispensing_days} days, {daily_total} pills/day, timing: {meal_time}, size: {pill_size} (Single timing)")
        
        # Create 4x7 matrix
        pill_matrix = np.zeros([4, 7], dtype=np.int8)
        
        for day in range(min(dispensing_days, 7)):
            pill_matrix[0, day] = medicine['morning_dosage']   # Morning (row 0)
            pill_matrix[1, day] = medicine['noon_dosage']      # Noon (row 1)
            pill_matrix[2, day] = medicine['evening_dosage']   # Evening (row 2)
            # pill_matrix[3, day] = 0  # 4th row remains 0 (already initialized to 0)
        
        # Add to medicine list
        drug_info = {
            "medicine_name": medicine_name,
            "pill_matrix": pill_matrix,
            "meal_timing": meal_time,
            "dispensing_days": dispensing_days,
            "pill_size": pill_size
        }
        pills_dispensing_list["medicines_1"].append(drug_info)

    def _process_medicine_dual_timing(self, medicine, dispensing_days, pills_dispensing_list):
        """Process medicine when both meal timings exist (4x7 matrix)"""
        medicine_name = medicine['medicine_name']
        meal_time = medicine.get('meal_timing', 'before')
        pill_size = medicine.get('pill_size', 'M')
        
        # Debug info
        daily_total = medicine['morning_dosage'] + medicine['noon_dosage'] + medicine['evening_dosage']
        print(f"  - {medicine_name}: {dispensing_days} days, {daily_total} pills/day, timing: {meal_time}, size: {pill_size} (Dual timing)")
        
        # Create 4x7 matrix for this medicine
        pill_matrix = np.zeros([4, 7], dtype=np.int8)
        
        for day in range(min(dispensing_days, 7)):
            pill_matrix[0, day] = medicine['morning_dosage']   # Morning
            pill_matrix[1, day] = medicine['noon_dosage']      # Noon
            pill_matrix[2, day] = medicine['evening_dosage']   # Evening
            # pill_matrix[3, day] = 0  # 4th row remains 0 (already initialized to 0)
        
        # Create drug info
        drug_info = {
            "medicine_name": medicine_name,
            "pill_matrix": pill_matrix,
            "meal_timing": meal_time,
            "dispensing_days": dispensing_days,
            "pill_size": pill_size
        }
        
        # Place medicine on appropriate plate based on meal timing
        if meal_time == 'before' or meal_time == 'anytime':
            pills_dispensing_list["medicines_1"].append(drug_info)
            print(f"  - {medicine_name}: Placed on medicines_1 (before meal)")
        elif meal_time == 'after':
            pills_dispensing_list["medicines_2"].append(drug_info)
            print(f"  - {medicine_name}: Placed on medicines_2 (after meal)")

    def _check_meal_timing_types(self, medicines):
        """Check if medicines have before/after meal timing"""
        has_before_meal = any(med.get('meal_timing') in ['before', 'anytime'] for med in medicines)
        has_after_meal = any(med.get('meal_timing') == 'after' for med in medicines)
        return has_before_meal, has_after_meal

    def _calculate_dispensing_days(self, medicine, max_days):
        """Calculate actual dispensing days considering already dispensed days"""
        try:
            start_date = datetime.strptime(medicine['start_date'], '%Y-%m-%d')
            last_dispensed_date = datetime.strptime(medicine['last_dispensed_expiry_date'], '%Y-%m-%d')
            already_dispensed_days = (last_dispensed_date - start_date).days + 1
            remaining_days = medicine['duration_days'] - already_dispensed_days
            dispensing_days = min(max_days, max(0, remaining_days))
            return dispensing_days
        except (ValueError, TypeError):
            return min(max_days, medicine['duration_days'])

    def update_medicine_expiry_date(self, medicine_name: str):
        """
        Update medicine expiry date after dispensing
        
        Args:
            medicine_name: Name of the medicine to update
            
        Returns:
            bool: True if successful, False otherwise
        """
        try:
            if not self.current_prescription_data:
                print(f"[Warning] No current prescription data available for {medicine_name}")
                return False
                
            if medicine_name not in self.current_dispensing_days:
                print(f"[Warning] No dispensing days found for {medicine_name}")
                return False
            
            # 找到对应的药物
            medicine = None
            for med in self.current_prescription_data['medicines']:
                if med['medicine_name'] == medicine_name:
                    medicine = med
                    break
            
            if not medicine:
                print(f"[Warning] Medicine {medicine_name} not found in prescription data")
                return False
            
            # 获取配药天数
            dispensing_days = self.current_dispensing_days[medicine_name]
            
            # 计算新的过期日期
            last_dispensed_date = datetime.strptime(medicine['last_dispensed_expiry_date'], '%Y-%m-%d')
            new_expiry_date = last_dispensed_date + timedelta(days=dispensing_days)
            
            # 更新处方数据中的过期日期
            medicine['last_dispensed_expiry_date'] = new_expiry_date.strftime('%Y-%m-%d')
            
            # 更新CSV文件
            self.update_last_dispensed_date_in_csv(
                self.current_prescription_data['patient_info']['patient_id'], 
                medicine_name, 
                new_expiry_date.strftime('%Y-%m-%d')
            )
            
            print(f"Updated expiry date for {medicine_name}: {new_expiry_date.strftime('%Y-%m-%d')}")
            return True
            
        except (ValueError, TypeError) as e:
            print(f"[Warning] Failed to update expiry date for {medicine_name}: {e}")
            return False

    def update_last_dispensed_date_in_csv(self, patient_id, medicine_name, new_date):
        """
        Update the last_dispensed_expiry_date in the CSV file
        
        Args:
            patient_id: Patient ID
            medicine_name: Name of the medicine
            new_date: New expiry date in string format
        """
        try:
            if self.df is not None:
                # Find the specific record to update
                mask = (
                    (self.df['patientId'].astype(str) == str(patient_id)) & 
                    (self.df['medicine_name'] == medicine_name)
                )
                
                # Update the DataFrame
                self.df.loc[mask, 'last_dispensed_expiry_date'] = new_date
                
                # Save back to CSV file
                self.df.to_csv(self.csv_file_path, index=False, encoding='utf-8')
                print(f"[Info] Updated CSV: {medicine_name} expiry date -> {new_date}")
                
        except Exception as e:
            print(f"[Error] Failed to update CSV file: {e}")

    def get_patients_for_today(self, n=2):
        """Get patients whose medicine expiry date is within n days from today"""
        try:
            # Basic validation
            if self.df is None or self.df.empty:
                return False, [{'error': 'Database not available', 'error_code': 503}]
            
            if not isinstance(n, int) or n < 0:
                return False, [{'error': 'Invalid threshold days', 'error_code': 400}]
            
            today = datetime.now().date()
            print(f"[Debug] Today's date: {today}")
            target_patients = []
            
            # Fill NaN values in RFID column to handle empty RFID fields
            df_copy = self.df.copy()
            df_copy['rfid'] = df_copy['rfid'].fillna('')
            
            # Group by patient to avoid duplicates
            patient_groups = df_copy.groupby(['patientId', 'patient_name', 'rfid'])
            print(f"[Debug] Found {len(patient_groups)} patient groups")
            
            for (patient_id, patient_name, rfid), group in patient_groups:
                print(f"[Debug] Processing patient: {patient_name} (ID: {patient_id}, RFID: '{rfid}')")
                expiring_medicines = []
                
                for _, record in group.iterrows():
                    try:
                        # Parse expiry date
                        expiry_date_str = record['last_dispensed_expiry_date']
                        expiry_date = datetime.strptime(expiry_date_str, '%Y-%m-%d').date()
                        days_until_expiry = (expiry_date - today).days
                        
                        print(f"[Debug] Medicine: {record['medicine_name']}, Expiry: {expiry_date_str}, Days until expiry: {days_until_expiry}")
                        
                        # Check if medicine expires within n days AND is active
                        is_active = int(record.get('is_active', 1))
                        
                        if days_until_expiry <= n and is_active == 1:
                            print(f"[Debug] Medicine {record['medicine_name']} matches criteria")
                            medicine_info = {
                                'medicine_name': record['medicine_name'],
                                'last_dispensed_expiry_date': record['last_dispensed_expiry_date'],
                                'days_until_expiry': days_until_expiry
                            }
                            expiring_medicines.append(medicine_info)
                        else:
                            reason = f"days_until_expiry={days_until_expiry} > {n}" if days_until_expiry > n else f"is_active={is_active}"
                            print(f"[Debug] Medicine {record['medicine_name']} does not match criteria ({reason})")
                            
                    except (ValueError, TypeError) as date_error:
                        print(f"[Warning] Invalid date format for patient {patient_id}, medicine {record['medicine_name']}: {date_error}")
                        continue
                
                # If patient has expiring medicines, add to results
                if expiring_medicines:
                    patient_info = {
                        'patient_id': str(patient_id),
                        'patient_name': patient_name,
                        'rfid': str(rfid),
                        'medicines_expiring': expiring_medicines
                    }
                    target_patients.append(patient_info)
            
            print(f"[Info] Found {len(target_patients)} patients with medicines expiring within {n} days")
            return True, target_patients
            
        except Exception as e:
            print(f"[Error] Exception in get_patients_for_today: {e}")
            return False, [{'error': f'Query error: {str(e)}', 'error_code': 500}]

########################
# Update prescription #
########################

    def add_patient_prescription(self, prescription_data):
        """
        Add new prescription information to the database
        
        Args:
            prescription_data: Dict with the same format as get_patient_prescription returns
            Format: {
                'patient_info': {
                    'patient_name': str,
                    'patient_id': str,
                    'rfid': str
                },
                'medicines': [
                    {
                        'medicine_name': str,
                        'morning_dosage': int,
                        'noon_dosage': int,
                        'evening_dosage': int,
                        'meal_timing': str,
                        'start_date': str,
                        'duration_days': int,
                        'last_dispensed_expiry_date': str
                    }
                ]
            }
        
        Returns:
            Tuple[bool, Dict]: (success flag, result message or error dict)
        """
        try:
            # Validate input data
            if not prescription_data or not isinstance(prescription_data, dict):
                return False, {'error': 'Invalid prescription data format', 'error_code': 400}
            
            if 'patient_info' not in prescription_data or 'medicines' not in prescription_data:
                return False, {'error': 'Missing patient_info or medicines data', 'error_code': 400}
            
            patient_info = prescription_data['patient_info']
            medicines = prescription_data['medicines']
            
            # Validate patient info
            required_patient_fields = ['patient_name', 'patient_id', 'rfid']
            for field in required_patient_fields:
                if field not in patient_info or not patient_info[field]:
                    return False, {'error': f'Missing required patient field: {field}', 'error_code': 400}
            
            # Validate medicines list
            if not medicines or not isinstance(medicines, list):
                return False, {'error': 'Medicines must be a non-empty list', 'error_code': 400}
            
            # Initialize DataFrame if it doesn't exist
            if self.df is None:
                self.df = pd.DataFrame()
            
            # Prepare new records
            new_records = []
            
            for medicine in medicines:
                # Validate medicine data
                success, error_msg = self._validate_medicine_data(medicine)
                if not success:
                    return False, {'error': error_msg, 'error_code': 400}
                
                # Create record for CSV according to actual structure
                record = {
                    'patient_name': patient_info['patient_name'],
                    'patientId': patient_info['patient_id'],
                    'rfid': patient_info['rfid'],
                    'medicine_name': medicine['medicine_name'],
                    'morning_dosage': int(medicine['morning_dosage']),
                    'noon_dosage': int(medicine['noon_dosage']),
                    'evening_dosage': int(medicine['evening_dosage']),
                    'meal_timing': medicine['meal_timing'],
                    'start_date': medicine['start_date'],
                    'duration_days': int(medicine['duration_days']),
                    'last_dispensed_expiry_date': medicine['last_dispensed_expiry_date'],
                    'is_active': medicine.get('is_active', 1),  # Default to active
                    'pill_size': medicine.get('pill_size', 'M')  # Default to Medium
                }
                new_records.append(record)
            
            # Check for duplicate medicines for the same patient
            patient_id = patient_info['patient_id']
            for record in new_records:
                existing_mask = (
                    (self.df['patientId'].astype(str) == str(patient_id)) & 
                    (self.df['medicine_name'] == record['medicine_name'])
                )
                if not self.df[existing_mask].empty:
                    return False, {
                        'error': f'Medicine {record["medicine_name"]} already exists for patient {patient_id}', 
                        'error_code': 409
                    }
            
            # Add new records to DataFrame
            new_df = pd.DataFrame(new_records)
            self.df = pd.concat([self.df, new_df], ignore_index=True)
            
            # Save to CSV file
            self.df.to_csv(self.csv_file_path, index=False, encoding='utf-8')
            
            print(f"[Info] Added {len(new_records)} medicine(s) for patient {patient_info['patient_name']} (ID: {patient_id})")
            
            return True, {
                'message': f'Successfully added {len(new_records)} medicine(s) for patient {patient_info["patient_name"]}',
                'patient_id': patient_id,
                'medicines_added': len(new_records)
            }
            
        except Exception as e:
            return False, {'error': f'Failed to add prescription: {str(e)}', 'error_code': 500}

    def _validate_medicine_data(self, medicine):
        """
        Validate medicine data format
        
        Args:
            medicine: Dict containing medicine information
            
        Returns:
            Tuple[bool, str]: (is_valid, error_message)
        """
        try:
            # Required fields
            required_fields = [
                'medicine_name', 'morning_dosage', 'noon_dosage', 'evening_dosage',
                'meal_timing', 'start_date', 'duration_days', 'last_dispensed_expiry_date'
            ]
            
            for field in required_fields:
                if field not in medicine:
                    return False, f'Missing required medicine field: {field}'
            
            # Validate dosage fields are integers
            dosage_fields = ['morning_dosage', 'noon_dosage', 'evening_dosage', 'duration_days']
            for field in dosage_fields:
                try:
                    int(medicine[field])
                except (ValueError, TypeError):
                    return False, f'Invalid {field}: must be an integer'
            
            # Validate meal timing
            valid_meal_timings = ['before', 'after', 'anytime']
            if medicine['meal_timing'] not in valid_meal_timings:
                return False, f'Invalid meal_timing: must be one of {valid_meal_timings}'
            
            # Validate date formats
            date_fields = ['start_date', 'last_dispensed_expiry_date']
            for field in date_fields:
                try:
                    datetime.strptime(medicine[field], '%Y-%m-%d')
                except ValueError:
                    return False, f'Invalid {field} format: must be YYYY-MM-DD'
            
            # Validate dosages are non-negative
            for field in ['morning_dosage', 'noon_dosage', 'evening_dosage']:
                if int(medicine[field]) < 0:
                    return False, f'Invalid {field}: must be non-negative'
            
            # Validate duration_days is positive
            if int(medicine['duration_days']) <= 0:
                return False, 'Invalid duration_days: must be positive'
            
            # Validate optional fields
            if 'is_active' in medicine:
                if medicine['is_active'] not in [0, 1, '0', '1']:
                    return False, 'Invalid is_active: must be 0 or 1'
            
            if 'pill_size' in medicine:
                valid_sizes = ['S', 'M', 'L']  # Small, Medium, Large
                if medicine['pill_size'] not in valid_sizes:
                    return False, f'Invalid pill_size: must be one of {valid_sizes}'
            
            return True, ''
            
        except Exception as e:
            return False, f'Validation error: {str(e)}'

    def update_patient_prescription(self, prescription_data):
        """
        Smart update prescription: add new patient/medicines or update existing ones
        
        Logic:
        - If patient doesn't exist: add all medicines as new records
        - If patient exists:
            - For new medicines: insert after patient's last record
            - For existing medicines: update the corresponding record
        
        Args:
            prescription_data: Dict with the same format as get_patient_prescription returns
            
        Returns:
            Tuple[bool, Dict]: (success flag, result message or error dict)
        """
        try:
            if not prescription_data or 'patient_info' not in prescription_data:
                return False, {'error': 'Invalid prescription data format', 'error_code': 400}
            
            patient_info = prescription_data['patient_info']
            medicines = prescription_data['medicines']
            patient_id = patient_info['patient_id']
            
            # Validate input data - rfid is now optional
            required_patient_fields = ['patient_name', 'patient_id']
            for field in required_patient_fields:
                if field not in patient_info or not patient_info[field]:
                    return False, {'error': f'Missing required patient field: {field}', 'error_code': 400}
            
            # Set default rfid if not provided
            rfid = patient_info.get('rfid', '')
            
            if not medicines or not isinstance(medicines, list):
                return False, {'error': 'Medicines must be a non-empty list', 'error_code': 400}
            
            # Initialize DataFrame if it doesn't exist
            if self.df is None:
                self.df = pd.DataFrame()
            
            # Check if patient exists
            patient_records = self.df[self.df['patientId'].astype(str) == str(patient_id)]
            patient_exists = not patient_records.empty
            
            if patient_exists:
                return self._update_existing_patient_prescription(prescription_data, patient_records)
            else:
                return self._add_new_patient_prescription(prescription_data)
                
        except Exception as e:
            return False, {'error': f'Failed to update prescription: {str(e)}', 'error_code': 500}

    def _add_new_patient_prescription(self, prescription_data):
        """Add prescription for a new patient"""
        try:
            patient_info = prescription_data['patient_info']
            medicines = prescription_data['medicines']
            
            # Set default rfid if not provided
            rfid = patient_info.get('rfid', '')
            
            new_records = []
            for medicine in medicines:
                # Validate medicine data
                success, error_msg = self._validate_medicine_data(medicine)
                if not success:
                    return False, {'error': error_msg, 'error_code': 400}
                
                # Create record for CSV
                record = {
                    'patient_name': patient_info['patient_name'],
                    'patientId': patient_info['patient_id'],
                    'rfid': rfid,  # Use default empty string if not provided
                    'medicine_name': medicine['medicine_name'],
                    'morning_dosage': int(medicine['morning_dosage']),
                    'noon_dosage': int(medicine['noon_dosage']),
                    'evening_dosage': int(medicine['evening_dosage']),
                    'meal_timing': medicine['meal_timing'],
                    'start_date': medicine['start_date'],
                    'duration_days': int(medicine['duration_days']),
                    'last_dispensed_expiry_date': medicine['last_dispensed_expiry_date'],
                    'is_active': medicine.get('is_active', 1),
                    'pill_size': medicine.get('pill_size', 'M')
                }
                new_records.append(record)
            
            # Add new records to DataFrame
            new_df = pd.DataFrame(new_records)
            self.df = pd.concat([self.df, new_df], ignore_index=True)
            
            # Save to CSV file
            self.df.to_csv(self.csv_file_path, index=False, encoding='utf-8')
            
            print(f"[Info] Added new patient {patient_info['patient_name']} with {len(new_records)} medicine(s)")
            
            return True, {
                'message': f'Successfully added new patient {patient_info["patient_name"]} with {len(new_records)} medicine(s)',
                'patient_id': patient_info['patient_id'],
                'medicines_added': len(new_records),
                'medicines_updated': 0
            }
            
        except Exception as e:
            return False, {'error': f'Failed to add new patient prescription: {str(e)}', 'error_code': 500}

    def _update_existing_patient_prescription(self, prescription_data, patient_records):
        """Update prescription for an existing patient"""
        try:
            patient_info = prescription_data['patient_info']
            medicines = prescription_data['medicines']
            patient_id = patient_info['patient_id']
            
            # Set default rfid if not provided
            rfid = patient_info.get('rfid', '')
            
            # Get the last index of this patient's records
            last_patient_index = patient_records.index.max()
            
            updated_count = 0
            added_count = 0
            new_records_to_insert = []
            
            for medicine in medicines:
                # Validate medicine data
                success, error_msg = self._validate_medicine_data(medicine)
                if not success:
                    return False, {'error': error_msg, 'error_code': 400}
                
                medicine_name = medicine['medicine_name']
                
                # Check if this medicine already exists for this patient
                existing_medicine_mask = (
                    (self.df['patientId'].astype(str) == str(patient_id)) & 
                    (self.df['medicine_name'] == medicine_name)
                )
                
                if not self.df[existing_medicine_mask].empty:
                    # Update existing medicine
                    self._update_existing_medicine_record(existing_medicine_mask, medicine, patient_info)
                    updated_count += 1
                    print(f"[Info] Updated existing medicine: {medicine_name} for patient {patient_id}")
                else:
                    # Prepare new medicine record to be inserted
                    record = {
                        'patient_name': patient_info['patient_name'],
                        'patientId': patient_info['patient_id'],
                        'rfid': rfid,  # Use default empty string if not provided
                        'medicine_name': medicine['medicine_name'],
                        'morning_dosage': int(medicine['morning_dosage']),
                        'noon_dosage': int(medicine['noon_dosage']),
                        'evening_dosage': int(medicine['evening_dosage']),
                        'meal_timing': medicine['meal_timing'],
                        'start_date': medicine['start_date'],
                        'duration_days': int(medicine['duration_days']),
                        'last_dispensed_expiry_date': medicine['last_dispensed_expiry_date'],
                        'is_active': medicine.get('is_active', 1),
                        'pill_size': medicine.get('pill_size', 'M')
                    }
                    new_records_to_insert.append(record)
                    added_count += 1
                    print(f"[Info] Prepared new medicine: {medicine_name} for patient {patient_id}")
            
            # Insert new records after the last record of this patient
            if new_records_to_insert:
                self._insert_records_after_index(last_patient_index, new_records_to_insert)
            
            # Save to CSV file
            self.df.to_csv(self.csv_file_path, index=False, encoding='utf-8')
            
            return True, {
                'message': f'Successfully updated patient {patient_info["patient_name"]}: {updated_count} updated, {added_count} added',
                'patient_id': patient_id,
                'medicines_updated': updated_count,
                'medicines_added': added_count
            }
            
        except Exception as e:
            return False, {'error': f'Failed to update existing patient prescription: {str(e)}', 'error_code': 500}

    def _update_existing_medicine_record(self, mask, medicine, patient_info):
        """Update an existing medicine record"""
        # Set default rfid if not provided
        rfid = patient_info.get('rfid', '')
        
        self.df.loc[mask, 'patient_name'] = patient_info['patient_name']
        self.df.loc[mask, 'rfid'] = rfid  # Use default empty string if not provided
        self.df.loc[mask, 'morning_dosage'] = int(medicine['morning_dosage'])
        self.df.loc[mask, 'noon_dosage'] = int(medicine['noon_dosage'])
        self.df.loc[mask, 'evening_dosage'] = int(medicine['evening_dosage'])
        self.df.loc[mask, 'meal_timing'] = medicine['meal_timing']
        self.df.loc[mask, 'start_date'] = medicine['start_date']
        self.df.loc[mask, 'duration_days'] = int(medicine['duration_days'])
        self.df.loc[mask, 'last_dispensed_expiry_date'] = medicine['last_dispensed_expiry_date']
        self.df.loc[mask, 'is_active'] = medicine.get('is_active', 1)
        self.df.loc[mask, 'pill_size'] = medicine.get('pill_size', 'M')

    def _insert_records_after_index(self, insert_after_index, new_records):
        """Insert new records after a specific index in the DataFrame"""
        try:
            # Create DataFrame for new records
            new_df = pd.DataFrame(new_records)
            
            # Split the original DataFrame
            before_part = self.df.iloc[:insert_after_index + 1].copy()
            after_part = self.df.iloc[insert_after_index + 1:].copy()
            
            # Concatenate: before + new records + after
            self.df = pd.concat([before_part, new_df, after_part], ignore_index=True)
            
            print(f"[Info] Inserted {len(new_records)} new record(s) after index {insert_after_index}")
            
        except Exception as e:
            print(f"[Error] Failed to insert records: {e}")
            # Fallback: append to the end
            new_df = pd.DataFrame(new_records)
            self.df = pd.concat([self.df, new_df], ignore_index=True)
            print(f"[Info] Fallback: Appended {len(new_records)} record(s) to the end")



import numpy as np
from patient_prescription_manager import PatientPrescriptionManager

def print_matrix(matrix, medicine_name, matrix_type=""):
    """Helper function to print pill matrix in a readable format"""
    print(f"\n{medicine_name} {matrix_type}:")
    print("Days:  ", end="")
    for day in range(7):
        print(f"Day{day+1:2d} ", end="")
    print()
    
    # Extract meal timing from matrix_type for better labels
    if "before" in matrix_type.lower():
        row_labels = ["Morn before:", "Noon before:", "Even before:", "Row4      :"]
    elif "after" in matrix_type.lower():
        row_labels = ["Morn after: ", "Noon after: ", "Even after: ", "Row4      :"]
    else:
        row_labels = ["Morn ", "Noon ", "Even ", "Row4 "]
    
    for i in range(4):
        print(f"{row_labels[i]} ", end="")
        for j in range(7):
            print(f"{matrix[i,j]:4d} ", end="")
        print()



def test_with_real_csv_data():
    """Test with real CSV data"""
    print("\n" + "="*60)
    print("TEST 4: Real CSV Data Test")
    print("="*60)
    
    manager = PatientPrescriptionManager()
    
    # Load prescriptions from CSV
    manager.read_local_prescriptions()
    
    if manager.df is None or manager.df.empty:
        print("No CSV data found. Please ensure local_prescriptions_data.csv exists.")
        return
    
    # Test with patient ID 101
    patient_id = "101"
    print(f"Testing with patient ID: {patient_id}")
    
    success, result = manager.generate_pills_dispensing_list(patient_id, max_days=7)
    
    if success:
        print(f"\nPatient: {result['patient_info']['patient_name']}")
        print(f"medicines_1 count: {len(result['medicines_1'])}")
        print(f"medicines_2 count: {len(result['medicines_2'])}")
        
        print("\n--- MEDICINES_1 (First plate) ---")
        for med in result['medicines_1']:
            print_matrix(med['pill_matrix'], med['medicine_name'], 
                        f"({med['meal_timing']}, {med['pill_size']}, {med['dispensing_days']} days)")
        
        if result['medicines_2']:
            print("\n--- MEDICINES_2 (Second plate) ---")
            for med in result['medicines_2']:
                print_matrix(med['pill_matrix'], med['medicine_name'], 
                            f"({med['meal_timing']}, {med['pill_size']}, {med['dispensing_days']} days)")
        else:
            print("\n--- MEDICINES_2 (Second plate) ---")
            print("No medicines require a second plate.")
    else:
        print(f"Error: {result}")

def main():
    """Main test function"""
    print("Testing New Dispensing Pattern Logic")
    print("====================================")
    
    # Test 4: Real CSV data
    test_with_real_csv_data()
    
    print("\n" + "="*60)
    print("All tests completed!")
    print("="*60)

if __name__ == "__main__":
    main()
