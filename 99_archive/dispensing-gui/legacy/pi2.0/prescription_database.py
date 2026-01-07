import pandas as pd
import numpy as np
from typing import Dict, Tuple
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
        self._load_local_database()

    def _load_local_database(self):
        """加载本地处方数据库"""
        try:
            if os.path.exists(self.csv_file_path):
                self.df = pd.read_csv(self.csv_file_path, encoding='utf-8')
                print(f"[成功] 加载处方数据库: {len(self.df)} 条记录")
            else:
                print(f"[警告] 数据库文件不存在")
        except Exception as e:
            print(f"[错误] 加载数据库失败: {e}")
    
    def _load_database_from_url(self, url: str):
        """从URL加载处方数据库"""
        return NotImplementedError("从URL加载数据库功能尚未实现")
    
    def get_patient_prescription(self, rfid: str = None, qr_data: str = None) -> Dict:
        """
        根据RFID或QR码数据获取患者处方信息
        
        Args:
            rfid: 患者RFID卡号 (可选)
            qr_data: 二维码数据 (可选)
            
        Returns:
            Dict: 包含患者信息和处方详情的字典
        """
        try:
            # 根据不同种类查询标识符查询匹配的处方记录
            identifier = None
            patient_records = pd.DataFrame()
            if rfid:
                identifier = str(rfid).strip()
                patient_records = self.df[self.df['rfid'].astype(str) == identifier]
            elif qr_data:
                identifier = str(qr_data).strip()
                patient_records = self.df[self.df['qr_data'].astype(str) == identifier]
            else:
                return {
                    'success': False,
                    'error': '必须提供RFID或QR码数据',
                    'error_code': 400
                }
            if patient_records.empty:
                return {
                    'success': False,
                    'error': f'未找到标识符 {identifier} 对应的处方信息',
                    'error_code': 404
                }
            
            # 获取患者基本信息
            first_record = patient_records.iloc[0]
            patient_info = {
                'patient_name': first_record['patient_name'],
                'rfid': identifier,
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
                    },
                    'duration_days': int(record['duration_days']),
                    'daily_total': int(record['morning_dosage'] + record['noon_dosage'] + 
                                     record['evening_dosage']),
                    'meal_timing': record['meal_timing']
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
    
    def generate_pills_disensing_list(self, rfid: str = None, qr_data: str = None) -> Tuple[bool, Dict, str]:
        """
        根据RFID生成每种药品的分药清单，包括患者姓名和分药矩阵
        
        Args:
            rfid: 患者RFID卡号
            QR_data: 二维码数据

        Returns:
            Tuple[bool, Dict, str]: (成功标志, 分药清单字典, 错误信息)
            字典格式: {
                "name": "患者姓名", 
                "medicines": [
                    {"medicine_name": "药品名称", "pill_matrix": 4x7矩阵, "meal_timing": "用药时间"},
                    {"medicine_name": "药品名称", "pill_matrix": 4x7矩阵, "meal_timing": "用药时间"}
                ],
                "max_days": 实际排药天数
            }
        """
        try:
            # 获取处方数据
            prescription_data = self.get_patient_prescription(rfid=rfid, qr_data=qr_data)
    
            if not prescription_data['success']:
                return False, {}, prescription_data['error']
            
            medicines = prescription_data['medicines']
            patient_name = prescription_data['patient_info']['patient_name']
            
            print(f"[调试] 为患者 {patient_name} 找到 {len(medicines)} 种药品:")
            
            # 检查是否有不同meal_timing的药物
            has_before_meal = any(med.get('meal_timing') == 'before' or med.get('meal_timing') == 'anytime' for med in medicines)
            has_after_meal = any(med.get('meal_timing') == 'after' for med in medicines)
            
            # 根据药物类型确定最多排几天药
            max_days = 7  # 默认7天
            if has_before_meal and has_after_meal:
                max_days = 3  # 如果既有饭前也有饭后药，最多3天
            
            # 创建新格式的分药清单
            pills_disensing_list = {
                "name": patient_name,
                "medicines": [],
                "max_days": max_days
            }
            
            for medicine in medicines:
                medicine_name = medicine['medicine_name']
                dosage = medicine['dosage']
                meal_time = medicine.get('meal_timing', 'before')  # 默认饭前服用
                duration_days = min(medicine['duration_days'], max_days)  # 根据前面计算的max_days限制天数
                
                # 为每种药品创建独立的4x7矩阵
                pill_matrix = np.zeros([4, 7], dtype=np.byte)
                
                # 根据用药时间分配列
                for day in range(duration_days):
                    # 计算当前药物应放在第几列
                    col = day
                    if has_before_meal and has_after_meal:
                        # 如果既有饭前也有饭后药，每天占用2列
                        col = day * 2
                        if meal_time == 'after':
                            col += 1  # 饭后药放在奇数列
                    
                    # 确保不超出矩阵列数
                    if col < 7:
                        pill_matrix[2, col] = dosage['morning']   # 早上
                        pill_matrix[1, col] = dosage['noon']      # 中午
                        pill_matrix[0, col] = dosage['evening']   # 晚上
                        pill_matrix[3, col] = 0    # 第四列默认为0
                
                # 添加到药品矩阵列表
                drug_info = {
                    "medicine_name": medicine_name,
                    "pill_matrix": pill_matrix,
                    "meal_timing": meal_time
                }
                pills_disensing_list["medicines"].append(drug_info)
                
                # 打印调试信息
                print(f"  - {medicine_name}: 持续{duration_days}天，每日总量{medicine['daily_total']}片，服用时间：{meal_time}")
            
            print(f"[成功] 生成患者 {patient_name} 的分药清单，包含 {len(pills_disensing_list['medicines'])} 种药品，最多排药 {max_days} 天")
            
            return True, pills_disensing_list, ""
            
        except Exception as e:
            return False, {}, f"生成分药矩阵失败: {str(e)}"

# 示例使用函数
def demo_usage():
    """演示如何使用处方数据库类"""
    print("=== 处方数据库演示 ===")
    
    # 创建数据库实例
    db = PrescriptionDatabase("demo_prescriptions.csv")
    
    # print("\n1. 获取患者处方信息:")
    # result = db.get_patient_prescription(rfid="1111222277774028C8262910")
    # if result['success']:
    #     print(f"患者: {result['patient_info']['patient_name']}")
    #     print(f"处方ID: {result['patient_info']['prescription_id']}")
    #     print(f"药品数量: {result['prescription_summary']['total_medicines']}")
    #     for med in result['medicines']:
    #         print(f"  - {med['medicine_name']}: 早{med['dosage']['morning']} 中{med['dosage']['noon']} 晚{med['dosage']['evening']}")
    # else:
    #     print(f"错误: {result['error']}")
    
    print("\n2. 生成分药清单:")
    success, pills_disensing_list, error = db.generate_pills_disensing_list(qr_data="1001")
    if success:
        print("分药清单:")
        print(pills_disensing_list)
    else:
        print(f"错误: {error}")

if __name__ == "__main__":
    demo_usage()