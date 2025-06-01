import pandas as pd
import numpy as np
from typing import Dict, List, Optional, Tuple
from datetime import datetime, timedelta
import os

class PrescriptionDatabase:
    """处方数据库类，用于管理患者处方信息"""
    
    def __init__(self, csv_file_path: str = "prescriptions.csv"):
        """
        初始化处方数据库
        
        Args:
            csv_file_path: CSV文件路径
        """
        self.csv_file_path = csv_file_path
        self.df = None
        self._load_database()
    
    def _load_database(self):
        """加载处方数据库"""
        try:
            if os.path.exists(self.csv_file_path):
                self.df = pd.read_csv(self.csv_file_path, encoding='utf-8')
                print(f"[成功] 加载处方数据库: {len(self.df)} 条记录")
            else:
                print(f"[警告] 数据库文件不存在，创建示例数据库: {self.csv_file_path}")
                self._create_sample_database()
        except Exception as e:
            print(f"[错误] 加载数据库失败: {e}")
            self._create_sample_database()
    
    def _create_sample_database(self):
        """创建示例处方数据库"""
        sample_data = [
            {
                'patient_name': '张三',
                'rfid': '12345678',
                'medicine_name': '阿司匹林',
                'morning_dosage': 1,
                'noon_dosage': 0,
                'evening_dosage': 1,
                'night_dosage': 0,
                'duration_days': 7,
                'start_date': '2025-05-29',
                'prescription_id': 'RX001',
                'doctor_name': '李医生',
                'notes': '饭后服用'
            },
            {
                'patient_name': '张三',
                'rfid': '12345678',
                'medicine_name': '维生素C',
                'morning_dosage': 1,
                'noon_dosage': 1,
                'evening_dosage': 1,
                'night_dosage': 0,
                'duration_days': 14,
                'start_date': '2025-05-29',
                'prescription_id': 'RX001',
                'doctor_name': '李医生',
                'notes': '随餐服用'
            },
            {
                'patient_name': '李四',
                'rfid': '87654321',
                'medicine_name': '降压片',
                'morning_dosage': 1,
                'noon_dosage': 0,
                'evening_dosage': 1,
                'night_dosage': 0,
                'duration_days': 30,
                'start_date': '2025-05-29',
                'prescription_id': 'RX002',
                'doctor_name': '王医生',
                'notes': '空腹服用'
            },
            {
                'patient_name': '王五',
                'rfid': '11223344',
                'medicine_name': '感冒颗粒',
                'morning_dosage': 1,
                'noon_dosage': 1,
                'evening_dosage': 1,
                'night_dosage': 1,
                'duration_days': 3,
                'start_date': '2025-05-29',
                'prescription_id': 'RX003',
                'doctor_name': '张医生',
                'notes': '温水冲服'
            }
        ]
        
        self.df = pd.DataFrame(sample_data)
        self.save_database()
        print(f"[创建] 示例数据库已创建，包含 {len(self.df)} 条记录")
    
    def get_patient_prescription(self, rfid: str) -> Dict:
        """
        根据RFID获取患者处方信息
        
        Args:
            rfid: 患者RFID卡号
            
        Returns:
            Dict: 包含患者信息和处方详情的字典
        """
        try:
            # 转换RFID为字符串进行匹配
            rfid_str = str(rfid).strip()
            
            # 查找匹配的处方记录
            patient_records = self.df[self.df['rfid'].astype(str) == rfid_str]
            
            if patient_records.empty:
                return {
                    'success': False,
                    'error': f'未找到RFID {rfid_str} 对应的处方信息',
                    'error_code': 404
                }
            
            # 获取患者基本信息
            first_record = patient_records.iloc[0]
            patient_info = {
                'patient_name': first_record['patient_name'],
                'rfid': rfid_str,
                'prescription_id': first_record['prescription_id'],
                'doctor_name': first_record['doctor_name'],
                'start_date': first_record['start_date']
            }
            
            # 获取所有药品信息
            medicines = []
            for _, record in patient_records.iterrows():
                medicine = {
                    'medicine_name': record['medicine_name'],
                    'dosage': {
                        'morning': int(record['morning_dosage']),
                        'noon': int(record['noon_dosage']),
                        'evening': int(record['evening_dosage']),
                        'night': int(record['night_dosage'])
                    },
                    'duration_days': int(record['duration_days']),
                    'notes': record.get('notes', ''),
                    'daily_total': int(record['morning_dosage'] + record['noon_dosage'] + 
                                     record['evening_dosage'] + record['night_dosage'])
                }
                medicines.append(medicine)
            
            # 计算总的服药天数和剩余天数
            start_date = pd.to_datetime(first_record['start_date'])
            current_date = pd.to_datetime(datetime.now().date())
            days_elapsed = (current_date - start_date).days
            
            max_duration = max([med['duration_days'] for med in medicines])
            remaining_days = max(0, max_duration - days_elapsed)
            
            return {
                'success': True,
                'patient_info': patient_info,
                'medicines': medicines,
                'prescription_summary': {
                    'total_medicines': len(medicines),
                    'max_duration_days': max_duration,
                    'days_elapsed': days_elapsed,
                    'remaining_days': remaining_days,
                    'is_active': remaining_days > 0
                }
            }
            
        except Exception as e:
            return {
                'success': False,
                'error': f'查询处方信息时发生错误: {str(e)}',
                'error_code': 500
            }
    
    def generate_pills_disensing_list(self, rfid: str) -> Tuple[bool, Dict, str]:
        """
        根据RFID生成每种药品的分药清单，包括患者姓名和分药矩阵
        
        Args:
            rfid: 患者RFID卡号
            
        Returns:
            Tuple[bool, Dict, str]: (成功标志, 分药清单字典, 错误信息)
            字典格式: {
                "name": "患者姓名", 
                "medicines": [
                    {"medicine_name": "药品名称", "pill_matrix": 4x7矩阵},
                    {"medicine_name": "药品名称", "pill_matrix": 4x7矩阵}
                ]
            }
        """
        try:
            prescription_data = self.get_patient_prescription(rfid)
            
            if not prescription_data['success']:
                return False, {}, prescription_data['error']
            
            medicines = prescription_data['medicines']
            patient_name = prescription_data['patient_info']['patient_name']
            
            print(f"[调试] 为患者 {patient_name} 找到 {len(medicines)} 种药品:")
            
            # 创建新格式的分药清单
            pills_disensing_list = {
                "name": patient_name,
                "medicines": []
            }
            
            for medicine in medicines:
                medicine_name = medicine['medicine_name']
                dosage = medicine['dosage']
                duration_days = min(medicine['duration_days'], 7)  # 最多7天
                
                # 为每种药品创建独立的4x7矩阵
                pill_matrix = np.zeros([4, 7], dtype=np.byte)
                
                for day in range(duration_days):
                    pill_matrix[0, day] = dosage['morning']   # 早上
                    pill_matrix[1, day] = dosage['noon']      # 中午
                    pill_matrix[2, day] = dosage['evening']   # 晚上
                    pill_matrix[3, day] = dosage['night']     # 夜间
                
                # 添加到药品矩阵列表
                drug_info = {
                    "medicine_name": medicine_name,
                    "pill_matrix": pill_matrix
                }
                pills_disensing_list["medicines"].append(drug_info)
                
                # 打印调试信息
                print(f"  - {medicine_name}: 持续{duration_days}天，每日总量{medicine['daily_total']}片")
            
            print(f"[成功] 生成患者 {patient_name} 的分药清单，包含 {len(pills_disensing_list['medicines'])} 种药品")
            
            return True, pills_disensing_list, ""
            
        except Exception as e:
            return False, {}, f"生成分药矩阵失败: {str(e)}"

    def save_database(self):
        """保存数据库到CSV文件"""
        try:
            self.df.to_csv(self.csv_file_path, index=False, encoding='utf-8')
            print(f"[保存] 数据库已保存到 {self.csv_file_path}")
        except Exception as e:
            print(f"[错误] 保存数据库失败: {e}")
    
    def get_all_patients(self) -> List[Dict]:
        """获取所有患者列表"""
        try:
            patients = self.df.groupby(['patient_name', 'rfid']).agg({
                'prescription_id': 'first',
                'doctor_name': 'first',
                'start_date': 'first',
                'medicine_name': 'count'
            }).reset_index()
            
            patients.rename(columns={'medicine_name': 'medicine_count'}, inplace=True)
            
            return patients.to_dict('records')
        except Exception as e:
            print(f"[错误] 获取患者列表失败: {e}")
            return []
    
    def get_statistics(self) -> Dict:
        """获取数据库统计信息"""
        try:
            stats = {
                'total_prescriptions': len(self.df),
                'total_patients': self.df['rfid'].nunique(),
                'total_medicines': self.df['medicine_name'].nunique(),
                'total_doctors': self.df['doctor_name'].nunique(),
                'most_prescribed_medicine': self.df['medicine_name'].mode().iloc[0] if not self.df.empty else 'N/A',
                'avg_medicines_per_patient': round(len(self.df) / self.df['rfid'].nunique(), 2) if self.df['rfid'].nunique() > 0 else 0
            }
            return stats
        except Exception as e:
            print(f"[错误] 获取统计信息失败: {e}")
            return {}

# 示例使用函数
def demo_usage():
    """演示如何使用处方数据库类"""
    print("=== 处方数据库演示 ===")
    
    # 创建数据库实例
    db = PrescriptionDatabase("demo_prescriptions.csv")
    
    print("\n1. 获取患者处方信息:")
    result = db.get_patient_prescription("1111222277774028C8262910")
    if result['success']:
        print(f"患者: {result['patient_info']['patient_name']}")
        print(f"处方ID: {result['patient_info']['prescription_id']}")
        print(f"药品数量: {result['prescription_summary']['total_medicines']}")
        for med in result['medicines']:
            print(f"  - {med['medicine_name']}: 早{med['dosage']['morning']} 中{med['dosage']['noon']} 晚{med['dosage']['evening']} 夜{med['dosage']['night']}")
    else:
        print(f"错误: {result['error']}")
    
    print("\n2. 生成分药清单:")
    success, pills_disensing_list, error = db.generate_pills_disensing_list("1111222277774028C8262910")
    if success:
        print("分药清单:")
        print(pills_disensing_list)
    else:
        print(f"错误: {error}")
    
    # print("\n3. 数据库统计信息:")
    # stats = db.get_statistics()
    # for key, value in stats.items():
    #     print(f"  {key}: {value}")

if __name__ == "__main__":
    demo_usage()