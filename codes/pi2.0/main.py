import sys
from PySide6.QtWidgets import QApplication, QMainWindow
from PySide6.QtCore import QObject, Signal, Slot, QTimer, QThread
from main_window_ui import Ui_MainWindow
from dispenser import Dispenser
from rfid_reader import RFIDReader
from prescription_database import PrescriptionDatabase
import time
import numpy as np

class DispenserController(QObject):
    """分药机控制器，运行在单独的线程中"""
    rfid_detected = Signal(str)  # 信号：检测到RFID
    error_occurred = Signal(str)  # 信号：发生错误
    pills_dispensing_list_loaded = Signal(dict)  # 处方加载成功信号
    dispensing_started = Signal()  # 分药流程开始信号
    current_medicine_info = Signal(str, int, int, int)  # 分药进度信号 (药品名称, 药片总数，当前索引, 总数)
    dispensing_completed = Signal()  # 分药流程完成信号
    dispensing_progress = Signal(int)

    def __init__(self, dispenser_port="COM6", rfid_port="COM9"):
        super().__init__()

        # 初始化分药机和RFID读卡器的端口
        self.dispenser_port = dispenser_port
        self.rfid_port = rfid_port
        
        # 初始化组件
        self.dispenser = None
        self.rfid_reader = None
        self.database = PrescriptionDatabase("demo_prescriptions.csv")

        # 初始化当前状态
        self.current_rfid = None
        self.current_pills_dispensing_list = {}
        self.current_medicines = []  
        self.current_medicine_index = 0
        self.pill_remain = 0 #剩余药片数量
        self.total_pill = 0 # 一个药片矩阵总药片数量
        self.is_dispensing = False

    def initialize_hardware(self):
        """初始化硬件设备"""
        try:
            # 初始化分药机
            print("[初始化] 连接分药机...")
            self.dispenser = Dispenser(self.dispenser_port)
            if self.dispenser.ser is None:
                raise Exception("分药机连接失败")
            
            # 启动分药机反馈处理
            self.dispenser.start_dispenser_feedback_handler()
            
            # 初始化分药机
            init_result = self.dispenser.reset_dispenser()
            if init_result != 0:
                raise Exception(f"分药机初始化失败，错误码: {init_result}")
            
            # 初始化RFID读卡器
            print("[初始化] 连接RFID读卡器...")
            self.rfid_reader = RFIDReader(self.rfid_port)
            if not self.rfid_reader.connect():
                raise Exception("RFID读卡器连接失败")
            
            print("[成功] 硬件初始化完成")
            return True
            
        except Exception as e:
            print(f"[错误] 硬件初始化失败: {str(e)}")
            self.error_occurred.emit(f"硬件初始化失败: {str(e)}")
            return False
        
    def cleanup_hardware(self):
        """清理硬件资源"""
        try:
            if self.monitor_timer.isActive():
                self.monitor_timer.stop()
                
            if self.dispenser:
                self.dispenser.stop_dispenser_feedback_handler()
                self.dispenser.close_plate()  # 确保药盘关闭
            
            if self.rfid_reader:
                self.rfid_reader.disconnect()
                
            print("[清理] 硬件资源已释放")
        except Exception as e:
            print(f"[错误] 清理硬件资源时发生错误: {e}")

    @Slot()
    def open_plate(self):
        """打开药盘"""
        try:
            if not self.dispenser:
                self.error_occurred.emit("分药机未初始化")
                return False
            
            print("[分药机] 正在打开药盘...")
            open_result = self.dispenser.open_plate()
            
            if open_result != 0:
                self.error_occurred.emit(f"打开药盘失败，错误码: {open_result}")
                return False
            
            print("[分药机] 药盘打开成功")
            return True

        except Exception as e:
            self.error_occurred.emit(f"打开药盘失败: {str(e)}")
            return False
        
    @Slot()
    def prepare_for_rfid_detection(self):
        """为RFID检测做准备, 打开药盘"""
        try:
            if not self.dispenser:
                self.error_occurred.emit("分药机未初始化")
                return False
            
            print("[分药机] 正在打开药盘...")
            open_result = self.dispenser.open_plate()
            
            if open_result != 0:
                self.error_occurred.emit(f"打开药盘失败，错误码: {open_result}")
                return False
            
            print("[分药机] 药盘打开成功，准备检测RFID")
            return True

        except Exception as e:
            self.error_occurred.emit(f"准备RFID检测失败: {str(e)}")
            return False   
        
    @Slot()
    def start_rfid_detection(self):
        """开始RFID检测"""
        try:            
            if not self.rfid_reader:
                self.error_occurred.emit("RFID读卡器未初始化")
                return
            
            print("[RFID] 开始检测RFID...")
            # 读取RFID
            result = self.rfid_reader.read_single(timeout=20.0)
            
            if result["error_code"] == 0 and result["epc"]:
                epc = result["epc"]
                print(f"[RFID] 检测到卡片: {epc}")
                
                # 将EPC转换为RFID（这里可能需要根据实际情况调整）
                rfid = epc
                
                self.current_rfid = rfid
                self.rfid_detected.emit(rfid)
                
                # 自动加载药品矩阵
                self.load_pills_disensing_list(rfid)
                
            else:
                error_msg = result.get('error', '未知错误')
                print(f"[RFID] 读取失败: {error_msg}")
                self.error_occurred.emit(f"RFID读取失败: {error_msg}")
                
        except Exception as e:
            print(f"[错误] RFID检测异常: {str(e)}")
            self.error_occurred.emit(f"RFID检测异常: {str(e)}")

    def load_pills_disensing_list(self, rfid):
        """加载处方信息并生成分药清单"""
        try:
            # 从数据库获取处方信息和分药矩阵
            success, pills_disensing_list, error = self.database.generate_pills_disensing_list(rfid)
            
            if not success:
                self.error_occurred.emit(f"生成分药矩阵失败: {error}")
                return

            print(pills_disensing_list)
            
            # 保存处方信息和分药矩阵
            self.current_pills_dispensing_list = pills_disensing_list
            self.current_medicines = pills_disensing_list["medicines"]
            self.current_medicine_index = 0
            
            # 发送信号，包含处方信息和分药矩阵
            enhanced_prescription_data = pills_disensing_list.copy()  
            self.pills_dispensing_list_loaded.emit(enhanced_prescription_data)
        
        except Exception as e:
            print(f"[错误] 加载分药清单异常: {str(e)}")
            self.error_occurred.emit(f"加载分药清单异常: {str(e)}")

    @Slot()
    def start_dispensing(self):
        """开始分药流程"""

        self.monitor_timer = QTimer()
        self.monitor_timer.timeout.connect(self.monitor_dispensing_status)
        try:
            if not self.current_medicines:
                print("[错误] 没有药品需要分发")
                self.error_occurred.emit("没有药品需要分发")
                return
            
            if not self.dispenser:
                print("[错误] 分药机未初始化")
                self.error_occurred.emit("分药机未初始化")
                return
            
            self.is_dispensing = True
            self.current_medicine_index = 0
            
            print("[分药] 开始分药流程")
            self.dispensing_started.emit()
            
            # 开始分发第一种药品
            self.dispense_next_medicine()
            
        except Exception as e:
            print(f"[错误] 启动分药异常: {str(e)}")
            self.error_occurred.emit(f"启动分药异常: {str(e)}")

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
            self.current_medicine_info.emit(
                medicine_name,
                self.total_pill,
                self.current_medicine_index + 1, 
                len(self.current_medicines)
            )
            
            # 发送分药数据到分药机
            print(f"[分药] 发送分药数据到分药机...")
            ack = self.dispenser.send_pill_matrix(pill_matrix)
            
            if not ack:
                error_msg = f"发送分药数据失败: {medicine_name}"
                print(f"[错误] {error_msg}")
                self.error_occurred.emit(error_msg)
                return
            
            print(f"[分药] 数据发送成功，等待分药完成...")
            
            # 启动监控定时器
            self.monitor_timer.start(1000)  # 每秒检查一次

        except Exception as e:
            error_msg = f"分药异常: {str(e)}"
            print(f"[错误] {error_msg}")
            self.error_occurred.emit(error_msg)

    def monitor_dispensing_status(self):
        """监控分药状态"""
        try:
            # 如果没有分药机或未处于分药状态，停止监控
            if not self.dispenser or not self.is_dispensing:
                self.monitor_timer.stop()
                return
            
            self.pill_remain = self.dispenser.pill_remain if hasattr(self.dispenser, 'pill_remain') else 0
            self.dispensing_progress.emit((self.total_pill - self.pill_remain) / self.total_pill * 100)
            
            # 检查分药机状态
            print(f"[监控] 分药机状态: {self.dispenser.machine_state}, 错误码: {self.dispenser.err_code}, 未分发药片: {self.dispenser.pill_remain}")
            
            if self.dispenser.machine_state == 3:  # 分药完成
                self.monitor_timer.stop()
                
                current_medicine = self.current_medicines[self.current_medicine_index]
                medicine_name = current_medicine['medicine_name']
                
                if self.dispenser.err_code == 0:
                    print(f"[分药] {medicine_name} 分发完成")
                    if hasattr(self.dispenser, 'pill_remain'):
                        print(f"[分药] 剩余药片: {self.dispenser.pill_remain}")

                    
                    # 准备分发下一种药品
                    self.current_medicine_index += 1
                    # 等待一小段时间再分发下一种药品
                    self.dispense_next_medicine()
                    
                else:
                    error_msg = f"{medicine_name} 分药错误，错误码: {self.dispenser.err_code}"
                    print(f"[错误] {error_msg}")
                    self.error_occurred.emit(error_msg)

                    # 目前分药失败后仍继续分发下一种药品
                    # 准备分发下一种药品
                    self.current_medicine_index += 1
                    # 等待一小段时间再分发下一种药品
                    QTimer.singleShot(1000, self.dispense_next_medicine)
                    
            elif self.dispenser.machine_state == 2:  # 暂停状态
                print("[分药] 分药已暂停")
                
            elif self.dispenser.machine_state == 1:  # 分药中
                print("[分药] 正在分药...")
                
        except Exception as e:
            error_msg = f"监控分药状态异常: {str(e)}"
            print(f"[错误] {error_msg}")
            self.error_occurred.emit(error_msg)

    def complete_dispensing(self):
        """完成分药流程"""
        try:
            self.is_dispensing = False
            self.monitor_timer.stop()
            
            # 关闭药盘
            print("[分药] 关闭药盘...")
            if self.dispenser.close_plate() != 0:
                print("[警告] 关闭药盘失败")
            
            print("[分药] 所有药品分发完成")
            self.dispensing_completed.emit()
            
        except Exception as e:
            error_msg = f"完成分药异常: {str(e)}"
            print(f"[错误] {error_msg}")
            self.error_occurred.emit(error_msg)

    # def pause_dispensing(self):
    #     """暂停分药"""
    #     try:
    #         if self.dispenser and self.is_dispensing:
    #             result = self.dispenser.pause()
    #             if result == 0:
    #                 print("[分药] 分药已暂停")
    #             else:
    #                 self.error_occurred.emit("暂停分药失败")
    #     except Exception as e:
    #         self.error_occurred.emit(f"暂停分药异常: {str(e)}")

    # def stop_dispensing(self):
    #     """停止分药"""
    #     try:
    #         self.is_dispensing = False
    #         self.monitor_timer.stop()
            
    #         if self.dispenser:
    #             self.dispenser.pause()
                
    #         print("[分药] 分药已停止")
            
    #     except Exception as e:
    #         self.error_occurred.emit(f"停止分药异常: {str(e)}")


class MainWindow(QMainWindow):
    def __init__(self):
        super(MainWindow, self).__init__()
        self.ui = Ui_MainWindow()
        self.ui.setupUi(self) # 设置UI界面

        self.controller = DispenserController()  # 实例化控制器
        self.controller.initialize_hardware()  # 初始化硬件
        self.controller_worker_thread = QThread()  # 创建工作线程
        self.controller.moveToThread(self.controller_worker_thread)
        # self.controller_worker_thread.started.connect(self.controller.create_monitor_timer)  # 在工作线程中创建监控定时器
        self.controller_worker_thread.start() 

        self.ui.rignt_stackedWidget.setCurrentIndex(0)  # Set the initial page

    def connection(self):
        self.ui.start_dispense_button.clicked.connect(self.prepare_for_dispensing)
        self.ui.exit_button.clicked.connect(self.finsh_dispensing)

        self.controller.rfid_detected.connect(self.show_rfid_message)
        self.controller.pills_dispensing_list_loaded.connect(self.show_prescription_message)
        self.controller.current_medicine_info.connect(self.update_prescription_info)
        self.controller.dispensing_completed.connect(self.move_to_finish_page)
        self.controller.dispensing_progress.connect(self.set_pressbar_value)

    def move_to_start_page(self):
        """切换到开始页面"""
        self.ui.rignt_stackedWidget.setCurrentIndex(0)
    
    def move_to_put_pan_in_page(self):
        """切换到放入药盘页面"""
        self.ui.RFID_msg.hide()  # Hide the RFID message initially
        self.ui.prescription_msg.hide()  # Hide the prescription message initially
        self.ui.rignt_stackedWidget.setCurrentIndex(1)

    def move_to_dispensing_page(self):
        """切换到分药页面"""
        self.ui.rignt_stackedWidget.setCurrentIndex(2)

    def move_to_finish_page(self):
        """切换到完成页面"""
        self.ui.rignt_stackedWidget.setCurrentIndex(3)

    @Slot(str)
    def prepare_for_dispensing(self):
        self.move_to_put_pan_in_page()
        # 使用QMetaObject.invokeMethod在工作线程中调用方法
        from PySide6.QtCore import QMetaObject, Qt
        QMetaObject.invokeMethod(self.controller, "prepare_for_rfid_detection", Qt.QueuedConnection)
        # 延迟1秒后执行start_rfid_detection
        QTimer.singleShot(1000, lambda: QMetaObject.invokeMethod(self.controller, "start_rfid_detection", Qt.QueuedConnection))
    
    def start_rfid_detection(self):
        """开始RFID检测"""
        if self.controller.prepare_for_rfid_detection():
            self.controller.start_rfid_detection()

    @Slot(str)
    def show_rfid_message(self, rfid):
        """显示RFID检测结果"""
        self.ui.RFID_msg.setText(f"检测到RFID: {rfid}")
        self.ui.RFID_msg.show()

    @Slot(dict)
    def show_prescription_message(self, pills_dispensing_list):
        """显示处方信息"""
        self.ui.prescription_msg.show()
        self.ui.patient_name.setText(f"患者姓名: {pills_dispensing_list['name']}")
        self.ui.current_drug.setText(f"当前药品: {pills_dispensing_list['medicines'][0]['medicine_name']}")
        self.move_to_dispensing_page()  # 切换到分药页面
        from PySide6.QtCore import QMetaObject, Qt
        QMetaObject.invokeMethod(self.controller, "start_dispensing", Qt.QueuedConnection)

    @Slot(str, int, int, int)
    def update_prescription_info(self, current_medicine_name, total_pills_num, current_medicine_index, total_medicines_num):
        """更新处方信息显示"""
        self.ui.current_drug.setText(current_medicine_name)
        self.ui.guide_msg.setText(f"共需要{total_pills_num}片")

    @Slot(int)
    def set_pressbar_value(self, value):
        """设置进度条的值"""
        self.ui.dispense_progressBar.setValue(value)
        self.ui.progressBar_percentage.setText(f"{value}%")

    @Slot()
    def finsh_dispensing(self):
        """完成分药流程"""
        self.move_to_start_page()
        from PySide6.QtCore import QMetaObject, Qt
        QMetaObject.invokeMethod(self.controller, "open_plate", Qt.QueuedConnection)





def control_test():
    # 测试控制器
    app = QApplication(sys.argv)
    
    print("=== 分药机控制器测试 ===")
    controller = DispenserController()
    
    # 连接错误信号
    controller.error_occurred.connect(lambda msg: print(f"[UI错误] {msg}"))
    controller.dispensing_started.connect(lambda: print(f"[UI] 分药开始"))
    controller.current_medicine_info.connect(lambda name, cur, total: print(f"[UI] 分药进度: {name} ({cur}/{total})"))
    controller.dispensing_completed.connect(lambda: print(f"[UI] 分药完成"))
    
    if controller.initialize_hardware():
        print("硬件初始化成功，开始测试流程...")
        
        # 手动测试流程
        if controller.prepare_for_rfid_detection():
            print("药盘打开成功，开始RFID检测...")
            controller.start_rfid_detection()
            
            # 如果检测成功，会自动加载处方和分药矩阵
            # 然后可以开始分药
            if controller.current_medicines:
                print("开始分药...")
                controller.start_dispensing()
            else:
                print("没有找到药品，无法分药")
        else:
            print("药盘打开失败")
    else:
        print("硬件初始化失败")
    
    # 清理资源
    def cleanup():
        controller.cleanup_hardware()
        app.quit()
    
    # 设置5分钟后自动退出（防止程序挂死）
    QTimer.singleShot(300000, cleanup)
    
    sys.exit(app.exec())

if __name__ == "__main__":
    app = QApplication(sys.argv)
    window = MainWindow()
    window.connection()
    window.show()

    sys.exit(app.exec())

