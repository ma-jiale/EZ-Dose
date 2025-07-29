import cv2
import time
from camera_controller import CameraController
from prescription_database import PrescriptionDatabase

class QRPrescriptionMatcher:
    """QR码处方匹配器"""
    
    def __init__(self, camera_index=1, csv_file_path="demo_prescriptions.csv"):
        """
        初始化QR码处方匹配器
        
        Args:
            camera_index: 摄像头索引
            csv_file_path: 处方数据库CSV文件路径
        """
        self.camera = CameraController(camera_index=camera_index)
        self.prescription_db = PrescriptionDatabase(csv_file_path)
        
        # QR码检测状态
        self.last_qr_data = None
        self.last_detection_time = 0
        self.detection_cooldown = 2.0  # 2秒冷却时间，避免重复检测
        
    def process_qr_prescription(self, qr_data):
        """
        处理QR码数据，查询处方信息并生成分药清单
        
        Args:
            qr_data: QR码数据
        """
        print(f"\n{'='*50}")
        print(f"检测到QR码: {qr_data}")
        print(f"{'='*50}")
        
        try:
            # 1. 查询处方信息
            print("正在查询处方信息...")
            prescription_result = self.prescription_db.get_patient_prescription(qr_data=qr_data)
            
            if not prescription_result['success']:
                print(f"❌ 查询失败: {prescription_result['error']}")
                return
            
            # 2. 显示患者基本信息
            patient_info = prescription_result['patient_info']
            print(f"✅ 找到患者信息:")
            print(f"   患者姓名: {patient_info['patient_name']}")
            print(f"   处方ID: {patient_info['prescription_id']}")
            print(f"   医生: {patient_info['doctor_name']}")
            print(f"   开始日期: {patient_info['start_date']}")
            
            # 3. 显示药品信息
            medicines = prescription_result['medicines']
            print(f"\n📋 处方药品 ({len(medicines)}种):")
            for i, medicine in enumerate(medicines, 1):
                dosage = medicine['dosage']
                print(f"   {i}. {medicine['medicine_name']}")
                print(f"      用法: 早{dosage['morning']}片 中{dosage['noon']}片 晚{dosage['evening']}片")
                print(f"      服用天数: {medicine['duration_days']}天")
                print(f"      用药时间: {medicine['meal_timing']}")
            
            # 4. 生成分药清单
            print(f"\n🔄 正在生成分药清单...")
            success, dispensing_list, error = self.prescription_db.generate_pills_disensing_list(qr_data=qr_data)
            
            if not success:
                print(f"❌ 生成分药清单失败: {error}")
                return
            
            # 5. 显示分药清单
            print(f"✅ 分药清单生成成功!")
            print(f"   患者: {dispensing_list['name']}")
            print(f"   排药天数: {dispensing_list['max_days']}天")
            print(f"   药品数量: {len(dispensing_list['medicines'])}种")
            
            # 显示每种药品的分药矩阵
            for i, medicine in enumerate(dispensing_list['medicines'], 1):
                print(f"\n   药品{i}: {medicine['medicine_name']} ({medicine['meal_timing']})")
                print(f"   分药矩阵 (4行7列, 行=时段, 列=天数):")
                matrix = medicine['pill_matrix']
                time_labels = ['晚上', '中午', '早上', '备用']
                for row_idx, row in enumerate(matrix):
                    print(f"     {time_labels[row_idx]}: {row}")
            
        except Exception as e:
            print(f"❌ 处理QR码时发生错误: {str(e)}")
    
    def detect_qr_codes_in_frame(self, frame):
        """
        在图像帧中检测QR码
        
        Args:
            frame: 图像帧
            
        Returns:
            tuple: (处理后的帧, 是否检测到新的QR码)
        """
        from pyzbar import pyzbar
        import numpy as np
        
        # 转换为灰度图以提高检测准确率
        gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        
        # 检测QR码
        qr_codes = pyzbar.decode(gray)
        
        detected_new_qr = False
        current_time = time.time()
        
        for qr_code in qr_codes:
            # 获取QR码数据
            qr_data = qr_code.data.decode('utf-8')
            
            # 获取边界框坐标
            points = qr_code.polygon
            if len(points) == 4:
                # 绘制绿色边界框
                pts = np.array([[point.x, point.y] for point in points], dtype=np.int32)
                cv2.polylines(frame, [pts], True, (0, 255, 0), 3)
                
                # 显示QR码信息
                x, y = points[0].x, points[0].y
                cv2.putText(frame, f"QR: {qr_data}", (x, y - 10), 
                           cv2.FONT_HERSHEY_SIMPLEX, 0.8, (0, 255, 0), 2)
                
                # 检查是否是新的QR码且超过冷却时间
                if (qr_data != self.last_qr_data or 
                    current_time - self.last_detection_time > self.detection_cooldown):
                    
                    self.last_qr_data = qr_data
                    self.last_detection_time = current_time
                    detected_new_qr = True
                    
                    # 处理QR码数据
                    self.process_qr_prescription(qr_data)
        
        # 在图像上显示状态信息
        status_text = f"等待QR码扫描... (上次: {self.last_qr_data or 'None'})"
        cv2.putText(frame, status_text, (10, 30), 
                   cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 255), 2)
        
        return frame, detected_new_qr
    
    def start_scanning(self):
        """开始QR码扫描和处方匹配"""
        print("启动QR码处方匹配系统...")
        print("按 'q' 键退出程序")
        print("请将QR码放在摄像头前扫描\n")
        
        try:
            # 初始化摄像头
            if not self.camera.initialize_camera():
                print("❌ 摄像头初始化失败")
                return False
            
            # 设置畸变校正
            self.camera.setup_distortion_correction()
            
            # 开始扫描循环
            self.camera.is_running = True
            while self.camera.is_running:
                # 捕获并校正图像帧
                frame, ret = self.camera.capture_and_correct_frame()
                
                if not ret:
                    print("❌ 无法从摄像头获取图像")
                    break
                
                # 检测QR码并处理
                frame_with_overlay, detected_new = self.detect_qr_codes_in_frame(frame)
                
                # 显示图像
                cv2.imshow('QR码处方匹配系统', frame_with_overlay)
                
                # 处理按键输入
                key = cv2.waitKey(1) & 0xFF
                if key == ord('q') or key == ord('Q'):
                    print("用户请求退出")
                    break
            
            return True
            
        except Exception as e:
            print(f"❌ 扫描过程中发生错误: {e}")
            return False
        finally:
            self._cleanup()
    
    def _cleanup(self):
        """清理资源"""
        print("正在清理资源...")
        if self.camera:
            self.camera._cleanup()
        print("QR码处方匹配系统已停止")

def main():
    """主函数"""
    print("QR码处方匹配系统 Demo")
    print("=" * 40)
    
    try:
        # 创建QR码处方匹配器
        # 请根据实际情况修改摄像头索引和CSV文件路径
        matcher = QRPrescriptionMatcher(
            camera_index=1,  # 修改为您的摄像头索引
            csv_file_path="demo_prescriptions.csv"  # 修改为您的处方数据库文件路径
        )
        
        # 开始扫描
        success = matcher.start_scanning()
        
        if not success:
            print("❌ 系统启动失败")
            return 1
        
    except KeyboardInterrupt:
        print("\n程序被用户中断 (Ctrl+C)")
    except Exception as e:
        print(f"❌ 发生意外错误: {e}")
        return 1
    finally:
        print("程序结束")
    
    return 0

if __name__ == "__main__":
    import sys
    sys.exit(main())