import os
import pandas as pd
import requests
import json
from typing import Dict, List, Optional, Tuple

class PatientInfoManager:
    """Patient Information Manager"""
    
    def __init__(self, csv_file_path: str = None, server_url: str = "http://localhost:5000"):
        """
        Initialize patient information manager
        
        Args:
            csv_file_path: CSV file path, use default path if None
            server_url: Server URL
        """
        # Intialize parameters
        if csv_file_path is None:
            self.csv_file_path = os.path.join(os.path.dirname(__file__), 'patients.csv')
        else:
            self.csv_file_path = csv_file_path
        self.server_url = server_url.rstrip('/')
        self.patient_df = None

        # Load patients from server or local csv file
        self.load_patient_list()

####################
# For Loading Data #
####################
    def load_patient_list(self):
        """load patient list from server, if can't, read local patient list"""
        try:
            # First try to load from server
            if self.fetch_online_patient_list():
                print("Successfully loaded patient list from server")
                return True
            else:
                print("Server loading failed, using local patient list")
                return self.read_local_patient_list()
        except Exception as e:
            print(f"Error loading patient list: {str(e)}")
            return self.read_local_patient_list()

    def save_patient_list(self):
        """upload self.patient_df to server, and save it to local"""
        try:
            # First save to local
            if self.patient_df is not None and not self.patient_df.empty:
                self.patient_df.to_csv(self.csv_file_path, index=False, encoding='utf-8')
                print("Successfully saved to local file")
                
                # Then upload to server
                if self.upload_patient_list():
                    print("Successfully uploaded to server")
                else:
                    print("Failed to upload to server, but local save successful")
            else:
                print("No patient data to save")
        except Exception as e:
            print(f"Error saving patient list: {str(e)}")

    def _create_empty_patient_csv(self) -> bool:
        """
        Create empty patient CSV file
        
        Returns:
            bool: Whether creation was successful
        """
        try:
            # Update column structure to match new CSV format
            empty_df = pd.DataFrame(columns=['auntieId', 'imageResourceId', 'patientBarcode', 'patientBedNumber', 'patientName', 'patientId'])
            empty_df.to_csv(self.csv_file_path, index=False, encoding='utf-8')
            print(f"Created empty patient file: {self.csv_file_path}")
            return True
        except Exception as e:
            print(f"Failed to create patient file: {str(e)}")
            return False
    
    def read_local_patient_list(self) -> bool:
        """
        Load patient list from CSV file
        
        Returns:
            bool: Whether loading was successful
        """
        try:
            if not os.path.exists(self.csv_file_path):
                print(f"Warning: Patient file does not exist: {self.csv_file_path}")
                # Create empty CSV file
                self._create_empty_patient_csv()
                self.patient_df = pd.DataFrame(columns=['auntieId', 'imageResourceId', 'patientBarcode', 'patientBedNumber', 'patientName', 'patientId'])
                return True
            
            # Read CSV file
            self.patient_df = pd.read_csv(self.csv_file_path, encoding='utf-8')
            
            # Check and add missing columns
            required_columns = ['auntieId', 'imageResourceId', 'patientBarcode', 'patientBedNumber', 'patientName', 'patientId']
            for col in required_columns:
                if col not in self.patient_df.columns:
                    self.patient_df[col] = ''
            
            # Ensure patientName column is string type
            if 'patientName' in self.patient_df.columns:
                self.patient_df['patientName'] = self.patient_df['patientName'].astype(str)
                
                # Clean data - remove empty rows and invalid data
                self.patient_df = self.patient_df.dropna(subset=['patientName'])
                self.patient_df['patientName'] = self.patient_df['patientName'].str.strip()
                self.patient_df = self.patient_df[self.patient_df['patientName'] != '']
                self.patient_df = self.patient_df[self.patient_df['patientName'] != 'nan']  # Remove string 'nan'
            
            print(f"Successfully loaded {len(self.patient_df)} patients:")
            for _, row in self.patient_df.iterrows():
                print(f"  - {row['patientName']} (patientId: {row['patientId']})")
            
            return True
            
        except Exception as e:
            print(f"Failed to load patient list: {str(e)}")
            self.patient_df = pd.DataFrame(columns=['auntieId', 'imageResourceId', 'patientBarcode', 'patientBedNumber', 'patientName', 'patientId'])
            return False
    
    def write_local_patient_list(self, patient_data: Dict[str, str]) -> bool:
        """
        Save patient information to CSV file
        
        Args:
            patient_data: Patient data dictionary containing 'patientName' and 'patientId' fields
        
        Returns:
            bool: Whether saving was successful
        """
        try:
            # Validate required fields
            if 'patientName' not in patient_data or 'patientId' not in patient_data:
                print("Error: Patient data missing required fields")
                return False
            
            # Create complete patient data, set unprovided fields to empty string
            complete_patient_data = {
                'auntieId': patient_data.get('auntieId', ''),
                'imageResourceId': patient_data.get('imageResourceId', ''),
                'patientBarcode': patient_data.get('patientBarcode', ''),
                'patientBedNumber': patient_data.get('patientBedNumber', ''),
                'patientName': patient_data['patientName'],
                'patientId': patient_data['patientId']
            }
            
            # Create new patient DataFrame
            new_patient = pd.DataFrame([complete_patient_data])
            
            # If file exists and has data, append data; otherwise create new file
            if os.path.exists(self.csv_file_path) and self.patient_df is not None and not self.patient_df.empty:
                # Append to existing data
                updated_df = pd.concat([self.patient_df, new_patient], ignore_index=True)
            else:
                # Create new file
                updated_df = new_patient
            
            # Save to file
            updated_df.to_csv(self.csv_file_path, index=False, encoding='utf-8')
            
            # Update data in memory
            self.patient_df = updated_df
            
            print(f"Successfully saved patient: {patient_data['patientName']} (patientId: {patient_data['patientId']})")
            return True
            
        except Exception as e:
            print(f"Failed to save patient information: {str(e)}")
            return False
        
    def fetch_online_patient_list(self) -> bool:
        """load patient list from server"""
        try:
            response = requests.get(f"{self.server_url}/patients", timeout=10)
            
            if response.status_code == 200:
                data = response.json()
                if data.get('success') and 'data' in data:
                    patients_data = data['data']
                    
                    if patients_data:
                        # Convert to DataFrame
                        self.patient_df = pd.DataFrame(patients_data)
                        
                        # Ensure all required columns exist
                        required_columns = ['auntieId', 'imageResourceId', 'patientBarcode', 'patientBedNumber', 'patientName', 'patientId']
                        for col in required_columns:
                            if col not in self.patient_df.columns:
                                self.patient_df[col] = ''
                        
                        # Ensure correct column names
                        if 'patientName' in self.patient_df.columns:
                            self.patient_df['patientName'] = self.patient_df['patientName'].astype(str)
                        
                        # Clean data
                        self.patient_df = self.patient_df.dropna(subset=['patientName'])
                        self.patient_df['patientName'] = self.patient_df['patientName'].str.strip()
                        self.patient_df = self.patient_df[self.patient_df['patientName'] != '']
                        self.patient_df = self.patient_df[self.patient_df['patientName'] != 'nan']
                        
                        print(f"Successfully retrieved {len(self.patient_df)} patients from server")
                        
                        # Also save to local
                        self.patient_df.to_csv(self.csv_file_path, index=False, encoding='utf-8')
                        
                        return True
                    else:
                        # Server returned empty list
                        self.patient_df = pd.DataFrame(columns=['auntieId', 'imageResourceId', 'patientBarcode', 'patientBedNumber', 'patientName', 'patientId'])
                        print("Server returned empty patient list")
                        return True
                else:
                    print(f"Server returned error: {data.get('message', 'Unknown error')}")
                    return False
            else:
                print(f"Server request failed, status code: {response.status_code}")
                return False
                
        except requests.exceptions.RequestException as e:
            print(f"Network request failed: {str(e)}")
            return False
        except Exception as e:
            print(f"Failed to retrieve patient list from server: {str(e)}")
            return False

    def upload_patient_list(self) -> bool:
        """upload self.patient_df to server"""
        try:
            if self.patient_df is None or self.patient_df.empty:
                # Upload empty list
                patients_data = []
            else:
                # Convert DataFrame to list of dictionaries
                patients_data = self.patient_df.to_dict('records')
                
                # Ensure correct data format
                for patient in patients_data:
                    # Ensure ID is string or number
                    if pd.isna(patient.get('patientId')):
                        patient['patientId'] = ''
                    else:
                        patient['patientId'] = str(patient['patientId'])
                    
                    # Ensure patientName exists and is not empty
                    if pd.isna(patient.get('patientName')):
                        patient['patientName'] = ''
                    else:
                        patient['patientName'] = str(patient['patientName']).strip()
            
            # Prepare upload data
            upload_data = {
                'patients': patients_data
            }
            
            # Send POST request
            headers = {'Content-Type': 'application/json'}
            response = requests.post(
                f"{self.server_url}/patients/upload",
                data=json.dumps(upload_data),
                headers=headers,
                timeout=30
            )
            
            if response.status_code == 200:
                data = response.json()
                if data.get('success'):
                    print(f"Successfully uploaded {len(patients_data)} patients to server")
                    return True
                else:
                    print(f"Server returned error: {data.get('message', 'Unknown error')}")
                    return False
            else:
                print(f"Upload failed, status code: {response.status_code}")
                try:
                    error_data = response.json()
                    print(f"Error message: {error_data.get('message', 'Unknown error')}")
                except:
                    print(f"Response content: {response.text}")
                return False
                
        except requests.exceptions.RequestException as e:
            print(f"Network request failed: {str(e)}")
            return False
        except Exception as e:
            print(f"Failed to upload patient list: {str(e)}")
            return False

###################
# Basic Operation #
###################
    def check_patient_exists(self, patient_name: str = None, patient_id: str = None) -> bool:
        """
        Check if patient already exists
        
        Args:
            patient_name: Patient name
            patient_id: Patient ID
        
        Returns:
            bool: Whether patient exists
        """
        if self.patient_df is None or self.patient_df.empty:
            return False
        
        try:
            # Check if name or ID already exists
            name_exists = False
            id_exists = False
            
            if patient_name:
                name_exists = (self.patient_df['patientName'] == patient_name).any()
            
            if patient_id:
                id_exists = (self.patient_df['patientId'].astype(str) == str(patient_id)).any()
            
            return name_exists or id_exists
            
        except Exception as e:
            print(f"Error checking if patient exists: {str(e)}")
            return False
    
    def find_patient_by_name(self, patient_name: str, exact_match: bool = False) -> Optional[Dict[str, str]]:
        """
        Find patient by name
        
        Args:
            patient_name: Patient name
            exact_match: Whether to use exact match, False for fuzzy matching
        
        Returns:
            Optional[Dict]: Patient information dictionary, returns None if not found
        """
        if self.patient_df is None or self.patient_df.empty:
            return None
        
        try:
            if exact_match:
                # Exact match
                exact_match_df = self.patient_df[self.patient_df['patientName'] == patient_name]
                if not exact_match_df.empty:
                    patient_row = exact_match_df.iloc[0]
                else:
                    return None
            else:
                # First try exact match
                exact_match_df = self.patient_df[self.patient_df['patientName'] == patient_name]
                if not exact_match_df.empty:
                    patient_row = exact_match_df.iloc[0]
                else:
                    # If exact match fails, try fuzzy matching
                    matching_patients = self.patient_df[
                        self.patient_df['patientName'].str.contains(patient_name, na=False, case=False)
                    ]
                    if matching_patients.empty:
                        return None
                    patient_row = matching_patients.iloc[0]
            
            return {
                'patient_name': patient_row['patientName'],
                'patient_id': str(patient_row['patientId']) if pd.notna(patient_row['patientId']) else 'Unknown'
            }
            
        except Exception as e:
            print(f"Error finding patient: {str(e)}")
            return None
    
    def find_patient_by_id(self, patient_id: str) -> Optional[Dict[str, str]]:
        """
        Find patient by ID
        
        Args:
            patient_id: Patient ID
        
        Returns:
            Optional[Dict]: Patient information dictionary, returns None if not found
        """
        if self.patient_df is None or self.patient_df.empty:
            return None
        
        try:
            matching_patients = self.patient_df[self.patient_df['patientId'].astype(str) == str(patient_id)]
            
            if matching_patients.empty:
                return None
            
            patient_row = matching_patients.iloc[0]
            return {
                'patient_name': patient_row['patientName'],
                'patient_id': str(patient_row['patientId']) if pd.notna(patient_row['patientId']) else 'Unknown'
            }
            
        except Exception as e:
            print(f"Error finding patient by ID: {str(e)}")
            return None
    
    def get_all_patients(self) -> List[Dict[str, str]]:
        """
        Get all patients list
        
        Returns:
            List[Dict]: Patient information list
        """
        if self.patient_df is None or self.patient_df.empty:
            return []
        
        try:
            patients = []
            for _, row in self.patient_df.iterrows():
                patients.append({
                    'patient_name': row['patientName'],
                    'patient_id': str(row['patientId']) if pd.notna(row['patientId']) else 'Unknown'
                })
            return patients
            
        except Exception as e:
            print(f"Error getting all patients: {str(e)}")
            return []
    
    def update_patient(self, old_patient_id: str, new_patient_data: Dict[str, str]) -> bool:
        """
        Update patient information
        
        Args:
            old_patient_id: Original patient ID
            new_patient_data: New patient data
        
        Returns:
            bool: Whether update was successful
        """
        if self.patient_df is None or self.patient_df.empty:
            return False
        
        try:
            # Find patient
            mask = self.patient_df['patientId'].astype(str) == str(old_patient_id)
            if not mask.any():
                print(f"Patient with ID {old_patient_id} not found")
                return False
            
            # Update data
            for key, value in new_patient_data.items():
                if key in self.patient_df.columns:
                    self.patient_df.loc[mask, key] = value
            
            # Save to file
            self.patient_df.to_csv(self.csv_file_path, index=False, encoding='utf-8')
            
            print(f"Successfully updated patient information: {new_patient_data}")
            return True
            
        except Exception as e:
            print(f"Failed to update patient information: {str(e)}")
            return False
    
    def delete_patient(self, patient_id: str) -> bool:
        """
        Delete patient
        
        Args:
            patient_id: Patient ID
        
        Returns:
            bool: Whether deletion was successful
        """
        if self.patient_df is None or self.patient_df.empty:
            return False
        
        try:
            # Find patient
            mask = self.patient_df['patientId'].astype(str) == str(patient_id)
            if not mask.any():
                print(f"Patient with ID {patient_id} not found")
                return False
            
            # Delete patient
            self.patient_df = self.patient_df[~mask]
            
            # Save to file
            self.patient_df.to_csv(self.csv_file_path, index=False, encoding='utf-8')
            
            print(f"Successfully deleted patient ID: {patient_id}")
            return True
            
        except Exception as e:
            print(f"Failed to delete patient: {str(e)}")
            return False
    
    def get_patient_count(self) -> int:
        """
        Get total number of patients
        
        Returns:
            int: Total number of patients
        """
        if self.patient_df is None or self.patient_df.empty:
            return 0
        return len(self.patient_df)
    
    def validate_patient_data(self, patient_data: Dict[str, str]) -> Tuple[bool, str]:
        """
        Validate patient data, only a patient_data with both patient name and patien id is valid
        
        Args:
            patient_data: Patient data dictionary
        
        Returns:
            Tuple[bool, str]: (Whether valid, error message)
        """
        if not isinstance(patient_data, dict):
            return False, "Patient data must be in dictionary format"
        
        if 'patientName' not in patient_data:
            return False, "Missing patient name"
        
        if 'patientId' not in patient_data:
            return False, "Missing patient ID"
        
        patient_name = patient_data['patientName'].strip() if patient_data['patientName'] else ''
        patient_id = str(patient_data['patientId']).strip() if patient_data['patientId'] else ''
        
        if not patient_name:
            return False, "Patient name cannot be empty"
        
        if not patient_id:
            return False, "Patient ID cannot be empty"
        
        # Validate if ID is numeric
        try:
            int(patient_id)
        except ValueError:
            return False, "Patient ID must be numeric"
        
        return True, ""
    
    def refresh_data(self) -> bool:
        """
        Refresh data
        
        Returns:
            bool: Whether refresh was successful
        """
        return self.load_patient_list()