from PySide6.QtCore import Signal, Slot, QTimer, QObject
from dispenser_controller import DispenserController
from patient_prescription_manager import PatientPrescriptionManager
import numpy as np

class MainController(QObject):
    
    def __init__(self):
        super().__init__()
        self.dispenser_controller = DispenserController()
        self.rx_manager = PatientPrescriptionManager()

        self.current_pills_dispensing_list = {}
        self.current_medicines = []
        self.current_medicine_index = 0
        self.is_dispensing = False

    @Slot()
    def initialize_hardware(self):
        self.dispenser_controller.start_dispenser_feedback_handler()
        self.dispenser_controller.initialize_dispenser()
        
    @Slot()
    def connect_database(self):
        self.rx_manager.load_local_prescriptions()

    @Slot(object)
    def generate_pills_dispensing_list(self, id):
        try:
            self.current_pills_dispensing_list = {}
            self.current_medicines = []
            self.current_medicine_index = 0

            success, pills_disensing_list = self.rx_manager.generate_pills_dispensing_list(id)

            if not success:
                print(f"[错误] 生成分药矩阵失败")
                return
            
            # 保存处方信息和分药矩阵
            self.current_pills_dispensing_list = pills_disensing_list

            self.current_medicines = pills_disensing_list["medicines_1"]
            self.current_medicine_index = 0
        except Exception as e:
            print(f"[错误] 加载分药清单异常: {str(e)}")

    @Slot()
    def open_tray(self):
        self.dispenser_controller.open_tray()

    @Slot()
    def close_tray(self):
        self.dispenser_controller.close_tray()

####################
# Dispensing logic #
#####################
    @Slot()
    def start_dispensing(self):
        """开始分药流程"""
        self.monitor_timer = QTimer()
        self.monitor_timer.timeout.connect(self.monitor_dispensing_status)
        try:
            if not self.current_medicines:
                print("[错误] 没有药品需要分发")
                return
            
            if not self.dispenser_controller:
                print("[错误] 分药机未初始化")
                return
            
            self.is_dispensing = True
            self.current_medicine_index = 0
        
            # 开始分发第一种药品
            self.dispense_next_medicine()
        except Exception as e:
            print(f"[错误] 启动分药异常: {str(e)}")

    def dispense_next_medicine(self):
        """分发下一种药品"""
        try:
            if self.current_medicine_index >= len(self.current_medicines):
                # 所有药品分发完成
                print("[分药] 所有药品已分发完成")
                self.complete_dispensing()
                return
            
            current_medicine = self.current_medicines[self.current_medicine_index]
            medicine_name = current_medicine['medicine_name']
            
            print(f"[分药] 开始分发第 {self.current_medicine_index + 1}/{len(self.current_medicines)} 种药品: {medicine_name}")
            pill_matrix = current_medicine["pill_matrix"]

            # 打印当前分药矩阵信息
            print(f"[分药] {medicine_name} 分药矩阵:")
            time_labels = ["早上", "中午", "晚上", "夜间"]
            for i, label in enumerate(time_labels):
                print(f"  {label}: {' '.join(f'{x:2}' for x in pill_matrix[i])}")

            # 发送分药进度信号
            self.total_pill = np.sum(pill_matrix)
            
            # 发送分药数据到分药机
            print(f"[分药] 发送分药数据到分药机...")
            ack = self.dispenser_controller.send_pill_matrix(pill_matrix)
            
            if not ack:
                error_msg = f"发送分药数据失败: {medicine_name}"
                print(f"[错误] {error_msg}")
                return
            
            print(f"[分药] 数据发送成功，等待分药完成...")
            
            # 启动监控定时器
            self.monitor_timer.start(1000)  # 每秒检查一次
        except Exception as e:
            error_msg = f"分药异常: {str(e)}"
            print(f"[错误] {error_msg}")

    def monitor_dispensing_status(self):
        """监控分药状态"""
        try:
            # 如果没有分药机或未处于分药状态，停止监控
            if not self.dispenser_controller or not self.is_dispensing:
                self.monitor_timer.stop()
                return
            
            self.pill_remain = self.dispenser_controller.pill_remain if hasattr(self.dispenser_controller, 'pill_remain') else 0
            # self.dispensing_progress_signal.emit((self.total_pill - self.pill_remain) / self.total_pill * 100)

            # # 发射药品转换信号
            # if self.dispenser.pill_remain == 0 and self.current_medicine_index < len(self.current_medicines):
            #     current_medicine_name = self.current_medicines[self.current_medicine_index]['medicine_name']
            #     next_medicine_index = self.current_medicine_index + 1
            #     if next_medicine_index < len(self.current_medicines):
            #         next_medicine_name = self.current_medicines[next_medicine_index]['medicine_name']
            #         self.medicine_transition_signal.emit(current_medicine_name, next_medicine_name)
            #     else:
            #         # 最后一个药品分发完成
            #         self.medicine_transition_signal.emit(current_medicine_name, "")

            # 检查分药机状态
            print(f"[监控] 分药机状态: {self.dispenser_controller.machine_state}, 错误码: {self.dispenser_controller.err_code}, 未分发药片: {self.dispenser_controller.pill_remain}")
            
            if self.dispenser_controller.machine_state == 3:  # 分药完成
                self.monitor_timer.stop()                
                current_medicine = self.current_medicines[self.current_medicine_index]
                medicine_name = current_medicine['medicine_name']
                
                if self.dispenser_controller.err_code == 0:
                    print(f"[分药] {medicine_name} 分发完成")
                    if hasattr(self.dispenser_controller, 'pill_remain'):
                        print(f"[分药] 剩余药片: {self.dispenser_controller.pill_remain}")


                    # 准备分发下一种药品
                    self.current_medicine_index += 1
                    # 等待一小段时间再分发下一种药品
                    self.dispense_next_medicine()
                    
                else:
                    error_msg = f"{medicine_name} 分药错误，错误码: {self.dispenser.err_code}"
                    print(f"[错误] {error_msg}")

                    # 目前分药失败后仍继续分发下一种药品
                    # 准备分发下一种药品
                    self.current_medicine_index += 1
                    # 等待一小段时间再分发下一种药品
                    self.dispense_next_medicine()
                    
            elif self.dispenser_controller.machine_state == 2:  # 暂停状态
                print("[分药] 分药已暂停")
                
            elif self.dispenser_controller.machine_state == 1:  # 分药中
                print("[分药] 正在分药...")
                
        except Exception as e:
            error_msg = f"监控分药状态异常: {str(e)}"
            print(f"[错误] {error_msg}")

    def complete_dispensing(self):
        """完成分药流程"""
        try:
            self.is_dispensing = False
            self.monitor_timer.stop()
            print("[分药] 所有药品分发完成")
            # self.dispensing_completed_signal.emit()

            # 开启药盘
            print("[分药] 开启药盘...")
            if self.open_tray():
                print("[警告] 开启药盘失败")
            
        except Exception as e:
            error_msg = f"完成分药异常: {str(e)}"
            print(f"[错误] {error_msg}")

    















