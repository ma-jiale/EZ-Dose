import os
import pandas as pd
from datetime import datetime, timedelta
import numpy as np
import requests

class PatientPrescriptionManager:
    def __init__(self, server_url="http://localhost:5000"):
        self.df = None
        self.csv_file_path = "local_prescriptions_data.csv"
        self.server_url = server_url

####################
# For Loading Data #
####################
    def load_prescriptions(self):
        """load prescriptions from server, if can't, read local prescriptions"""
        try:
            # Try to fetch from server first
            server_result = self.fetch_online_prescriptions()
            if server_result is not None:  # 服务器响应成功（包括空数据）
                if server_result:
                    print("[Info] Successfully loaded prescriptions from server")
                else:
                    print("[Info] Server returned empty prescription data, cleared local data")
                return True
            else:
                # 只有在服务器完全无法连接时才使用本地数据
                print("[Warning] Server connection failed, using local data")
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
                if data.get('success'):
                    # 无论数据是否为空，都要处理
                    prescription_data = data.get('data', [])
                    
                    if prescription_data:
                        # 有数据：转换为DataFrame并保存
                        self.df = pd.DataFrame(prescription_data)
                        self.write_local_prescriptions()
                        print(f"[Info] Fetched {len(self.df)} prescriptions from server")
                    else:
                        # 空数据：创建空的DataFrame但保留列结构
                        columns = [
                            'duration_days', 'evening_dosage', 'is_active', 
                            'last_dispensed_expiry_date', 'meal_timing', 'medicine_name',
                            'morning_dosage', 'noon_dosage', 'patientId', 'patient_name',
                            'pill_size', 'rfid', 'start_date'
                        ]
                        self.df = pd.DataFrame(columns=columns)
                
                        # 写入只有表头的CSV文件
                        try:
                            self.df.to_csv(self.csv_file_path, index=False, encoding='utf-8')
                            print("[Info] Server returned empty data, cleared local prescription data")
                        except Exception as e:
                            print(f"[Warning] Failed to clear local prescriptions file: {e}")
                    
                    return True  # 服务器响应成功
                else:
                    print("[Warning] Server returned unsuccessful response")
                    return None  # 服务器响应失败
            else:
                print(f"[Warning] Server returned status code: {response.status_code}")
                return None  # 服务器响应失败
        except requests.exceptions.RequestException as e:
            print(f"[Error] Network error fetching prescriptions: {e}")
            return None  # 网络错误
        except Exception as e:
            print(f"[Error] Error fetching online prescriptions: {e}")
            return None  # 其他错误           

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

###################
# Basic Operation #
###################

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

    def delete_medicine(self, patient_id, medicine_name):
        """
        删除指定患者的指定药品
        
        Args:
            patient_id: 患者ID
            medicine_name: 药品名称
            
        Returns:
            Tuple[bool, Dict]: (success flag, result message or error dict)
        """
        try:
            if self.df is None or self.df.empty:
                return False, {'error': 'No prescription data available', 'error_code': 404}
            
            # 查找要删除的记录
            delete_mask = (
                (self.df['patientId'].astype(str) == str(patient_id)) & 
                (self.df['medicine_name'] == medicine_name)
            )
            
            if not delete_mask.any():
                return False, {'error': f'Medicine "{medicine_name}" not found for patient {patient_id}', 'error_code': 404}
            
            # 删除记录
            self.df = self.df[~delete_mask].reset_index(drop=True)
            
            # 保存到CSV文件
            self.write_local_prescriptions()
            
            print(f"[Info] Deleted medicine '{medicine_name}' for patient {patient_id}")
            
            return True, {
                'message': f'Successfully deleted medicine "{medicine_name}"',
                'patient_id': patient_id,
                'medicine_name': medicine_name
            }
            
        except Exception as e:
            return False, {'error': f'Failed to delete medicine: {str(e)}', 'error_code': 500}
