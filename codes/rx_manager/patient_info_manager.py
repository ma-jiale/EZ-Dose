import os
import pandas as pd
import requests
import json
from typing import Dict, List, Optional, Tuple

class PatientInfoManager:
    """患者信息管理器 - 负责患者CSV文件的操作"""
    
    def __init__(self, csv_file_path: str = None, server_url: str = "http://localhost:5000"):
        """
        初始化患者信息管理器
        
        Args:
            csv_file_path: CSV文件路径，如果为None则使用默认路径
            server_url: 服务器URL
        """
        if csv_file_path is None:
            self.csv_file_path = os.path.join(os.path.dirname(__file__), 'patient.csv')
        else:
            self.csv_file_path = csv_file_path
        
        self.server_url = server_url.rstrip('/')
        self.patient_df = None
        self.load_patient_list()

    def create_empty_patient_csv(self) -> bool:
        """
        创建空的患者CSV文件
        
        Returns:
            bool: 创建是否成功
        """
        try:
            empty_df = pd.DataFrame(columns=['patientName', 'id'])
            empty_df.to_csv(self.csv_file_path, index=False, encoding='utf-8')
            print(f"创建空的患者文件: {self.csv_file_path}")
            return True
        except Exception as e:
            print(f"创建患者文件失败: {str(e)}")
            return False
        
    def load_patient_list(self):
        """load patient list from server, if can't, read local patient list"""
        try:
            # 首先尝试从服务器加载
            if self.fetch_online_patient_list():
                print("成功从服务器加载患者列表")
            else:
                print("服务器加载失败，使用本地患者列表")
                self.read_local_patient_list()
        except Exception as e:
            print(f"加载患者列表时出错: {str(e)}")
            self.read_local_patient_list()

    def save_patient_list(self):
        """upload self.patient_df to server, and save it to local"""
        try:
            # 首先保存到本地
            if self.patient_df is not None and not self.patient_df.empty:
                self.patient_df.to_csv(self.csv_file_path, index=False, encoding='utf-8')
                print("成功保存到本地文件")
                
                # 然后上传到服务器
                if self.upload_patient_list():
                    print("成功上传到服务器")
                else:
                    print("上传到服务器失败，但本地保存成功")
            else:
                print("没有患者数据需要保存")
        except Exception as e:
            print(f"保存患者列表时出错: {str(e)}")
    
    def read_local_patient_list(self) -> bool:
        """
        从CSV文件中加载患者列表
        
        Returns:
            bool: 加载是否成功
        """
        try:
            if not os.path.exists(self.csv_file_path):
                print(f"警告: 患者文件不存在: {self.csv_file_path}")
                # 创建空的CSV文件
                self.create_empty_patient_csv()
                self.patient_df = pd.DataFrame(columns=['patientName', 'id'])
                return True
            
            # 读取CSV文件
            self.patient_df = pd.read_csv(self.csv_file_path, encoding='utf-8')
            
            # 确保patientName列是字符串类型
            if 'patientName' in self.patient_df.columns:
                self.patient_df['patientName'] = self.patient_df['patientName'].astype(str)
                
                # 清理数据 - 去除空白行和无效数据
                self.patient_df = self.patient_df.dropna(subset=['patientName'])
                self.patient_df['patientName'] = self.patient_df['patientName'].str.strip()
                self.patient_df = self.patient_df[self.patient_df['patientName'] != '']
                self.patient_df = self.patient_df[self.patient_df['patientName'] != 'nan']  # 去除字符串'nan'
            
            print(f"成功加载 {len(self.patient_df)} 个患者:")
            for _, row in self.patient_df.iterrows():
                print(f"  - {row['patientName']} (ID: {row['id']})")
            
            return True
            
        except Exception as e:
            print(f"加载患者列表失败: {str(e)}")
            self.patient_df = pd.DataFrame(columns=['patientName', 'id'])
            return False
    
    def write_local_patient_list(self, patient_data: Dict[str, str]) -> bool:
        """
        保存患者信息到CSV文件
        
        Args:
            patient_data: 患者数据字典，包含 'patientName' 和 'id' 字段
        
        Returns:
            bool: 保存是否成功
        """
        try:
            # 验证必要字段
            if 'patientName' not in patient_data or 'id' not in patient_data:
                print("错误: 患者数据缺少必要字段")
                return False
            
            # 创建新患者的DataFrame
            new_patient = pd.DataFrame([patient_data])
            
            # 如果文件存在且有数据，追加数据；否则创建新文件
            if os.path.exists(self.csv_file_path) and self.patient_df is not None and not self.patient_df.empty:
                # 追加到现有数据
                updated_df = pd.concat([self.patient_df, new_patient], ignore_index=True)
            else:
                # 创建新文件
                updated_df = new_patient
            
            # 保存到文件
            updated_df.to_csv(self.csv_file_path, index=False, encoding='utf-8')
            
            # 更新内存中的数据
            self.patient_df = updated_df
            
            print(f"成功保存患者: {patient_data['patientName']} (ID: {patient_data['id']})")
            return True
            
        except Exception as e:
            print(f"保存患者信息失败: {str(e)}")
            return False
        
    def fetch_online_patient_list(self) -> bool:
        """load patient list from server"""
        try:
            response = requests.get(f"{self.server_url}/api/patients", timeout=10)
            
            if response.status_code == 200:
                data = response.json()
                if data.get('success') and 'data' in data:
                    patients_data = data['data']
                    
                    if patients_data:
                        # 转换为DataFrame
                        self.patient_df = pd.DataFrame(patients_data)
                        
                        # 确保列名正确
                        if 'patientName' in self.patient_df.columns:
                            self.patient_df['patientName'] = self.patient_df['patientName'].astype(str)
                        
                        # 清理数据
                        self.patient_df = self.patient_df.dropna(subset=['patientName'])
                        self.patient_df['patientName'] = self.patient_df['patientName'].str.strip()
                        self.patient_df = self.patient_df[self.patient_df['patientName'] != '']
                        self.patient_df = self.patient_df[self.patient_df['patientName'] != 'nan']
                        
                        print(f"从服务器成功获取 {len(self.patient_df)} 个患者")
                        
                        # 同时保存到本地
                        self.patient_df.to_csv(self.csv_file_path, index=False, encoding='utf-8')
                        
                        return True
                    else:
                        # 服务器返回空列表
                        self.patient_df = pd.DataFrame(columns=['patientName', 'id'])
                        print("服务器返回空的患者列表")
                        return True
                else:
                    print(f"服务器返回错误: {data.get('message', '未知错误')}")
                    return False
            else:
                print(f"服务器请求失败，状态码: {response.status_code}")
                return False
                
        except requests.exceptions.RequestException as e:
            print(f"网络请求失败: {str(e)}")
            return False
        except Exception as e:
            print(f"从服务器获取患者列表失败: {str(e)}")
            return False

    def upload_patient_list(self) -> bool:
        """upload self.patient_df to server"""
        try:
            if self.patient_df is None or self.patient_df.empty:
                # 上传空列表
                patients_data = []
            else:
                # 将DataFrame转换为字典列表
                patients_data = self.patient_df.to_dict('records')
                
                # 确保数据格式正确
                for patient in patients_data:
                    # 确保ID是字符串或数字
                    if pd.isna(patient.get('id')):
                        patient['id'] = ''
                    else:
                        patient['id'] = str(patient['id'])
                    
                    # 确保patientName存在且不为空
                    if pd.isna(patient.get('patientName')):
                        patient['patientName'] = ''
                    else:
                        patient['patientName'] = str(patient['patientName']).strip()
            
            # 准备上传数据
            upload_data = {
                'patients': patients_data
            }
            
            # 发送POST请求
            headers = {'Content-Type': 'application/json'}
            response = requests.post(
                f"{self.server_url}/api/patients/upload",
                data=json.dumps(upload_data),
                headers=headers,
                timeout=30
            )
            
            if response.status_code == 200:
                data = response.json()
                if data.get('success'):
                    print(f"成功上传 {len(patients_data)} 个患者到服务器")
                    return True
                else:
                    print(f"服务器返回错误: {data.get('message', '未知错误')}")
                    return False
            else:
                print(f"上传失败，状态码: {response.status_code}")
                try:
                    error_data = response.json()
                    print(f"错误信息: {error_data.get('message', '未知错误')}")
                except:
                    print(f"响应内容: {response.text}")
                return False
                
        except requests.exceptions.RequestException as e:
            print(f"网络请求失败: {str(e)}")
            return False
        except Exception as e:
            print(f"上传患者列表失败: {str(e)}")
            return False
    
    def check_patient_exists(self, patient_name: str = None, patient_id: str = None) -> bool:
        """
        检查患者是否已存在
        
        Args:
            patient_name: 患者姓名
            patient_id: 患者ID
        
        Returns:
            bool: 患者是否存在
        """
        if self.patient_df is None or self.patient_df.empty:
            return False
        
        try:
            # 检查姓名或ID是否已存在
            name_exists = False
            id_exists = False
            
            if patient_name:
                name_exists = (self.patient_df['patientName'] == patient_name).any()
            
            if patient_id:
                id_exists = (self.patient_df['id'].astype(str) == str(patient_id)).any()
            
            return name_exists or id_exists
            
        except Exception as e:
            print(f"检查患者是否存在时出错: {str(e)}")
            return False
    
    def find_patient_by_name(self, patient_name: str, exact_match: bool = False) -> Optional[Dict[str, str]]:
        """
        根据姓名查找患者
        
        Args:
            patient_name: 患者姓名
            exact_match: 是否精确匹配，False时进行模糊匹配
        
        Returns:
            Optional[Dict]: 患者信息字典，如果未找到返回None
        """
        if self.patient_df is None or self.patient_df.empty:
            return None
        
        try:
            if exact_match:
                # 精确匹配
                exact_match_df = self.patient_df[self.patient_df['patientName'] == patient_name]
                if not exact_match_df.empty:
                    patient_row = exact_match_df.iloc[0]
                else:
                    return None
            else:
                # 首先尝试精确匹配
                exact_match_df = self.patient_df[self.patient_df['patientName'] == patient_name]
                if not exact_match_df.empty:
                    patient_row = exact_match_df.iloc[0]
                else:
                    # 如果精确匹配失败，尝试模糊匹配
                    matching_patients = self.patient_df[
                        self.patient_df['patientName'].str.contains(patient_name, na=False, case=False)
                    ]
                    if matching_patients.empty:
                        return None
                    patient_row = matching_patients.iloc[0]
            
            return {
                'patient_name': patient_row['patientName'],
                'patient_id': str(patient_row['id']) if pd.notna(patient_row['id']) else '未知'
            }
            
        except Exception as e:
            print(f"查找患者时出错: {str(e)}")
            return None
    
    def find_patient_by_id(self, patient_id: str) -> Optional[Dict[str, str]]:
        """
        根据ID查找患者
        
        Args:
            patient_id: 患者ID
        
        Returns:
            Optional[Dict]: 患者信息字典，如果未找到返回None
        """
        if self.patient_df is None or self.patient_df.empty:
            return None
        
        try:
            matching_patients = self.patient_df[self.patient_df['id'].astype(str) == str(patient_id)]
            
            if matching_patients.empty:
                return None
            
            patient_row = matching_patients.iloc[0]
            return {
                'patient_name': patient_row['patientName'],
                'patient_id': str(patient_row['id']) if pd.notna(patient_row['id']) else '未知'
            }
            
        except Exception as e:
            print(f"根据ID查找患者时出错: {str(e)}")
            return None
    
    def get_all_patients(self) -> List[Dict[str, str]]:
        """
        获取所有患者列表
        
        Returns:
            List[Dict]: 患者信息列表
        """
        if self.patient_df is None or self.patient_df.empty:
            return []
        
        try:
            patients = []
            for _, row in self.patient_df.iterrows():
                patients.append({
                    'patient_name': row['patientName'],
                    'patient_id': str(row['id']) if pd.notna(row['id']) else '未知'
                })
            return patients
            
        except Exception as e:
            print(f"获取所有患者时出错: {str(e)}")
            return []
    
    def update_patient(self, old_patient_id: str, new_patient_data: Dict[str, str]) -> bool:
        """
        更新患者信息
        
        Args:
            old_patient_id: 原患者ID
            new_patient_data: 新的患者数据
        
        Returns:
            bool: 更新是否成功
        """
        if self.patient_df is None or self.patient_df.empty:
            return False
        
        try:
            # 查找患者
            mask = self.patient_df['id'].astype(str) == str(old_patient_id)
            if not mask.any():
                print(f"未找到ID为 {old_patient_id} 的患者")
                return False
            
            # 更新数据
            for key, value in new_patient_data.items():
                if key in self.patient_df.columns:
                    self.patient_df.loc[mask, key] = value
            
            # 保存到文件
            self.patient_df.to_csv(self.csv_file_path, index=False, encoding='utf-8')
            
            print(f"成功更新患者信息: {new_patient_data}")
            return True
            
        except Exception as e:
            print(f"更新患者信息失败: {str(e)}")
            return False
    
    def delete_patient(self, patient_id: str) -> bool:
        """
        删除患者
        
        Args:
            patient_id: 患者ID
        
        Returns:
            bool: 删除是否成功
        """
        if self.patient_df is None or self.patient_df.empty:
            return False
        
        try:
            # 查找患者
            mask = self.patient_df['id'].astype(str) == str(patient_id)
            if not mask.any():
                print(f"未找到ID为 {patient_id} 的患者")
                return False
            
            # 删除患者
            self.patient_df = self.patient_df[~mask]
            
            # 保存到文件
            self.patient_df.to_csv(self.csv_file_path, index=False, encoding='utf-8')
            
            print(f"成功删除患者ID: {patient_id}")
            return True
            
        except Exception as e:
            print(f"删除患者失败: {str(e)}")
            return False
    
    def get_patient_count(self) -> int:
        """
        获取患者总数
        
        Returns:
            int: 患者总数
        """
        if self.patient_df is None or self.patient_df.empty:
            return 0
        return len(self.patient_df)
    
    def validate_patient_data(self, patient_data: Dict[str, str]) -> Tuple[bool, str]:
        """
        验证患者数据
        
        Args:
            patient_data: 患者数据字典
        
        Returns:
            Tuple[bool, str]: (是否有效, 错误信息)
        """
        if not isinstance(patient_data, dict):
            return False, "患者数据必须是字典格式"
        
        if 'patientName' not in patient_data:
            return False, "缺少患者姓名"
        
        if 'id' not in patient_data:
            return False, "缺少患者ID"
        
        patient_name = patient_data['patientName'].strip() if patient_data['patientName'] else ''
        patient_id = str(patient_data['id']).strip() if patient_data['id'] else ''
        
        if not patient_name:
            return False, "患者姓名不能为空"
        
        if not patient_id:
            return False, "患者ID不能为空"
        
        # 验证ID是否为数字
        try:
            int(patient_id)
        except ValueError:
            return False, "患者ID必须是数字"
        
        return True, ""
    
    def refresh_data(self) -> bool:
        """
        刷新数据（重新加载CSV文件）
        
        Returns:
            bool: 刷新是否成功
        """
        return self.load_patient_list()