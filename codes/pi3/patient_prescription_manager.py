import os
import pandas as pd
from datetime import datetime, timedelta
import numpy as np
class PatientPrescriptionManager:
    def __init__(self):
        self.df = None
        self.csv_file_path = "local_prescriptions_data.csv"
        # 添加当前处理的处方数据
        self.current_prescription_data = None
        self.current_dispensing_days = {}  # 存储每个药物的配药天数

    def load_local_prescriptions(self):
        try:
            if os.path.exists(self.csv_file_path):
                self.df = pd.read_csv(self.csv_file_path, encoding='utf-8')
        except Exception as e:
            print(f"[error] fail to load local prescriptions: {e}")

    def fetch_online_prescriptions(self):
        pass

    def get_patient_prescription(self, id):
        """
        Get patient prescription information by patient ID
        Args:
            id: Patient identifier (RFID or patient ID)
        Returns:
            Tuple[bool, Dict]
            Dict: Patient info and prescription details or error msg
        """
        try:
            # Basic validation
            if self.df is None or self.df.empty:
                return False, {'error': 'Database not available', 'error_code': 503}
            
            if not id or not str(id).strip():
                return False, {'error': 'Patient ID required', 'error_code': 400}
            
            identifier = str(id).strip()
            
            # Find patient records by RFID or ID
            records = self.df[
                (self.df['rfid'].astype(str).str.strip() == identifier) |
                (self.df['id'].astype(str).str.strip() == identifier)
            ]
            
            if records.empty:
                return False, {'error': f'No prescription found for: {identifier}', 'error_code': 404}
            
            # Get patient info from first record
            first_record = records.iloc[0]
            patient_info = {
                'patient_name': first_record['patient_name'],
                'patient_id': first_record['id'],
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
                    'last_dispensed_expiry_date': record['last_dispensed_expiry_date']
                }
                medicines.append(medicine)
                
            patient_prescription = {
                'patient_info': patient_info,
                'medicines': medicines
            }
            return True, patient_prescription
            
        except Exception as e:
            return False, {'error': f'Query error: {str(e)}', 'error_code': 500}

    def generate_pills_dispensing_list(self, id, max_days=7):
        """
        Generate pills dispensing list with 4x7 matrix for each medicine
        
        Args:
            id: Patient identifier (RFID or patient ID)
            max_days: Maximum days to dispense
            
        Returns:
            Tuple[bool, Dict]: (success flag, dispensing list dict or error message dict)
        """
        try:
            # Get prescription data
            success, prescription_data = self.get_patient_prescription(id)
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
            
            # Process each medicine
            for medicine in medicines:
                # 计算并存储配药天数
                dispensing_days = self._calculate_dispensing_days(medicine, max_days)
                self.current_dispensing_days[medicine['medicine_name']] = dispensing_days
                
                self._process_medicine_for_dispensing(
                    medicine, prescription_data, max_days, 
                    has_before_meal, has_after_meal, pills_dispensing_list
                )
            
            return True, pills_dispensing_list
            
        except Exception as e:
            return False, {'error': f'Failed to generate dispensing matrix: {str(e)}', 'error_code': 500}

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

    def _create_pill_matrices(self, medicine, dispensing_days, has_before_meal, has_after_meal):
        """Create 4x7 pill matrices for a medicine"""
        pill_matrix_1 = np.zeros([4, 7], dtype=np.int8)
        pill_matrix_2 = np.zeros([4, 7], dtype=np.int8)
        
        meal_time = medicine.get('meal_timing', 'before')
        
        for day in range(dispensing_days):
            col = day
            if has_before_meal and has_after_meal:
                # Use 2 columns per day if both meal timings exist
                col = day * 2
                if meal_time == 'after' or meal_time == 'anytime':
                    col += 1  # After meal medicines go to odd columns
            
            # Fill the appropriate matrix
            if col < 7:
                self._fill_matrix_column(pill_matrix_1, col, medicine)
            elif col < 14:
                self._fill_matrix_column(pill_matrix_2, col - 7, medicine)
        
        return pill_matrix_1, pill_matrix_2

    def _fill_matrix_column(self, matrix, col, medicine):
        """Fill a single column of the pill matrix"""
        matrix[0, col] = medicine['evening_dosage']   # Evening (row 0)
        matrix[1, col] = medicine['noon_dosage']      # Noon (row 1)
        matrix[2, col] = medicine['morning_dosage']   # Morning (row 2)
        matrix[3, col] = 0                            # Reserved (row 3)

    def _process_medicine_for_dispensing(self, medicine, prescription_data, max_days, 
                                       has_before_meal, has_after_meal, pills_dispensing_list):
        """Process a single medicine for dispensing"""
        medicine_name = medicine['medicine_name']
        meal_time = medicine.get('meal_timing', 'before')
        
        # Calculate dispensing days
        dispensing_days = self._calculate_dispensing_days(medicine, max_days)
        
        # # Update expiry date
        # self._update_medicine_expiry_date(medicine, prescription_data, dispensing_days)
        
        # Debug info
        daily_total = medicine['morning_dosage'] + medicine['noon_dosage'] + medicine['evening_dosage']
        print(f"  - {medicine_name}: {dispensing_days} days, {daily_total} pills/day, timing: {meal_time}")
        
        if dispensing_days > 0:
            # Create pill matrices
            pill_matrix_1, pill_matrix_2 = self._create_pill_matrices(
                medicine, dispensing_days, has_before_meal, has_after_meal
            )
            
            # Add to medicine list
            drug_info_1 = {
                "medicine_name": medicine_name,
                "pill_matrix": pill_matrix_1,
                "meal_timing": meal_time,
                "dispensing_days": dispensing_days
            }
            pills_dispensing_list["medicines_1"].append(drug_info_1)
            
            if np.sum(pill_matrix_2) > 0:
                drug_info_2 = {
                    "medicine_name": medicine_name,
                    "pill_matrix": pill_matrix_2,
                    "meal_timing": meal_time,
                    "dispensing_days": dispensing_days
                }
                pills_dispensing_list['medicines_2'].append(drug_info_2)
        
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
                    (self.df['id'].astype(str) == str(patient_id)) & 
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
        """
        Get patients whose medicine expiry date is within n days from today
        
        Args:
            n: Number of days threshold (e.g., if n=3, find patients whose medicine expires within 3 days)
        
        Returns:
            Tuple[bool, List[Dict]]: (success flag, list of patient info or error message)
            Patient info format: {
                'patient_id': str,
                'patient_name': str, 
                'rfid': str,
                'medicines_expiring': [
                    {
                        'medicine_name': str,
                        'last_dispensed_expiry_date': str,
                        'days_until_expiry': int
                    }
                ]
            }
        """
        try:
            # Basic validation
            if self.df is None or self.df.empty:
                return False, [{'error': 'Database not available', 'error_code': 503}]
            
            if not isinstance(n, int) or n < 0:
                return False, [{'error': 'Invalid threshold days', 'error_code': 400}]
            
            today = datetime.now().date()
            target_patients = []
            
            # Group by patient to avoid duplicates
            patient_groups = self.df.groupby(['id', 'patient_name', 'rfid'])
            
            for (patient_id, patient_name, rfid), group in patient_groups:
                expiring_medicines = []
                
                for _, record in group.iterrows():
                    try:
                        # Parse expiry date
                        expiry_date = datetime.strptime(record['last_dispensed_expiry_date'], '%Y-%m-%d').date()
                        days_until_expiry = (expiry_date - today).days
                        
                        # Check if medicine expires within n days
                        if days_until_expiry <= n:
                            medicine_info = {
                                'medicine_name': record['medicine_name'],
                                'last_dispensed_expiry_date': record['last_dispensed_expiry_date'],
                                'days_until_expiry': days_until_expiry
                            }
                            expiring_medicines.append(medicine_info)
                            
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
            return False, [{'error': f'Query error: {str(e)}', 'error_code': 500}]

def main():
    rx_manager = PatientPrescriptionManager()
    rx_manager.load_local_prescriptions()
    rx = rx_manager.generate_pills_dispensing_list("102")
    print(rx)

if __name__ == "__main__":
    main()
