import sys
from PySide6.QtWidgets import QApplication, QMainWindow
from PySide6.QtCore import QObject, Signal, Slot, QTimer, QThread
from main_window_ui import Ui_MainWindow
from dispenser import Dispenser
from rfid_reader import RFIDReader
from prescription_database import PrescriptionDatabase
import numpy as np

class DispenserController(QObject):
    """分药机控制器，运行在单独的线程中"""
    # 信号定义
    hardware_initialized_signal = Signal() # 硬件初始化成功信号
    prescription_database_initialized_signal = Signal()  # 处方数据库初始化成功信号
    rfid_detected_signal = Signal(str)  # 信号：检测到RFID
    pills_dispensing_list_loaded_signal = Signal(dict)  # 处方加载成功信号
    error_occurred_signal = Signal(str)  # 信号：发生错误
    dispensing_started_signal = Signal()  # 分药流程开始信号
    current_medicine_info_signal = Signal(str, int, int, int)  # 当前分发药品的信息 (药品名称, 药片总数，当前索引, 总数)
    dispensing_progress_signal = Signal(int)  # 分药进度信号 (百分比)
    dispensing_completed_signal = Signal()  # 分药流程完成信号
    plate_opened_signal = Signal()  # 药盘打开信号
    plate_closed_signal = Signal()  # 药盘关闭信号
    

    def __init__(self, dispenser_port="COM6", rfid_port="COM9"):
        super().__init__()

        # 初始化分药机和RFID读卡器的端口
        self.dispenser_port = dispenser_port
        self.rfid_port = rfid_port
        
        # 初始化组件
        self.dispenser = None
        self.rfid_reader = None
        self.database = None

        # 初始化当前状态
        self.hardware_initialized = False  # 硬件是否已初始化
        self.prescription_database_initialized = False  # 处方数据库是否已初始化
        self.current_rfid = None
        self.current_pills_dispensing_list = {} # 当前患者分药清单
        self.current_medicines = [] # 当前患者的药品列表
        self.current_medicine_index = 0 # 当前分发的药品索引
        self.pill_remain = 0 # 当前分发的药品剩余需要的药片数量
        self.total_pill = 0 # 当前分发的药品的总药片数量
        self.is_dispensing = False # 是否正在分药

    @Slot()
    def initialize_hardware(self):
        """初始化硬件设备"""
        try:
            # 初始化分药机
            print("[初始化] 连接分药机...")
            self.dispenser = Dispenser(self.dispenser_port)
            if self.dispenser.ser is None:
                raise Exception("分药机连接失败")
            
            # 启动分药机反馈处理
            self.dispenser.start_dispenser_feedback_handler() # 
            
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
            self.hardware_initialized = True
            self.hardware_initialized_signal.emit() #  发射硬件初始化成功信号
            return True
            
        except Exception as e:
            print(f"[错误] 硬件初始化失败: {str(e)}")
            self.error_occurred_signal.emit(f"硬件初始化失败: {str(e)}")
            return False
        
    @Slot()
    def initialize_prescription_database(self):
        """初始化处方数据库"""
        try:
            self.database = PrescriptionDatabase("demo_prescriptions.csv")
            self.prescription_database_initialized = True
            self.prescription_database_initialized_signal.emit()  # 发射处方数据库初始化成功信号
            print("[成功] 处方数据库初始化完成")
        except Exception as e:
            print(f"[错误] 处方数据库初始化失败: {str(e)}")
            self.error_occurred_signal.emit(f"处方数据库初始化失败: {str(e)}")
        
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
                self.error_occurred_signal.emit("分药机未初始化")
                return False
            
            else:
                print("[分药机] 正在打开药盘...")
                open_result = self.dispenser.open_plate()
                
                if open_result != 0:
                    self.error_occurred_signal.emit(f"打开药盘失败，错误码: {open_result}")
                    return False
                
                print("[分药机] 药盘打开成功")
            self.plate_opened_signal.emit()  # 发射药盘打开信号
            return True

        except Exception as e:
            self.error_occurred_signal.emit(f"打开药盘失败: {str(e)}")
            return False
        
    @Slot()
    def close_plate(self):
        """关闭药盘"""
        try:
            if not self.dispenser:
                self.error_occurred_signal.emit("分药机未初始化")
                return False
            else:
                print("[分药机] 正在关闭药盘...")
                close_result = self.dispenser.close_plate()
                
                if close_result != 0:
                    self.error_occurred_signal.emit(f"关闭药盘失败，错误码: {close_result}")
                    return False
                
                print("[分药机] 药盘关闭成功")
            self.plate_closed_signal.emit()  # 发射药盘关闭信号
            return True

        except Exception as e:
            self.error_occurred_signal.emit(f"关闭药盘失败: {str(e)}")
            return False
        
    @Slot()
    def prepare_for_rfid_detection(self):
        """为RFID检测做准备, 打开药盘"""
        try:
            if not self.dispenser:
                self.error_occurred_signal.emit("分药机未初始化")
                return False
            
            print("[分药机] 正在打开药盘...")
            open_result = self.dispenser.open_plate()
            
            if open_result != 0:
                self.error_occurred_signal.emit(f"打开药盘失败，错误码: {open_result}")
                return False
            
            print("[分药机] 药盘打开成功，准备检测RFID")
            return True

        except Exception as e:
            self.error_occurred_signal.emit(f"准备RFID检测失败: {str(e)}")
            return False   
        
    @Slot()
    def start_rfid_detection(self):
        """开始RFID检测, 如果成功则从处方库加载分药清单"""
        try:            
            if not self.rfid_reader:
                self.error_occurred_signal.emit("RFID读卡器未初始化")
                return
            
            self.current_rfid = None  # 重置当前RFID
            print("[RFID] 开始检测RFID...")
            # 读取RFID
            result = self.rfid_reader.read_single(timeout=20.0)
            
            if result["error_code"] == 0 and result["epc"]:
                epc = result["epc"]
                print(f"[RFID] 检测到卡片: {epc}")
                
                # 将EPC转换为RFID（这里可能需要根据实际情况调整）
                rfid = epc
                
                self.current_rfid = rfid
                self.rfid_detected_signal.emit(rfid)
                
                # 自动加载药品矩阵
                self.load_pills_disensing_list(rfid)
                
            else:
                error_msg = result.get('error', '未知错误')
                print(f"[RFID] 读取失败: {error_msg}")
                self.error_occurred_signal.emit(f"RFID读取失败: {error_msg}")
                
        except Exception as e:
            print(f"[错误] RFID检测异常: {str(e)}")
            self.error_occurred_signal.emit(f"RFID检测异常: {str(e)}")

    def load_pills_disensing_list(self, rfid):
        """加载处方信息并生成分药清单"""
        try:
            # 清空当前状态
            self.current_pills_dispensing_list = {}
            self.current_medicines = []
            self.current_medicine_index = 0

            # 从数据库获取处方信息和分药矩阵
            success, pills_disensing_list, error = self.database.generate_pills_disensing_list(rfid)
            
            if not success:
                self.error_occurred_signal.emit(f"生成分药矩阵失败: {error}")
                return

            print(pills_disensing_list)
            
            # 保存处方信息和分药矩阵
            self.current_pills_dispensing_list = pills_disensing_list
            self.current_medicines = pills_disensing_list["medicines"]
            self.current_medicine_index = 0
            
            # 发送信号，包含处方信息和分药矩阵
            enhanced_prescription_data = pills_disensing_list.copy()  
            self.pills_dispensing_list_loaded_signal.emit(enhanced_prescription_data)
        
        except Exception as e:
            print(f"[错误] 加载分药清单异常: {str(e)}")
            self.error_occurred_signal.emit(f"加载分药清单异常: {str(e)}")


    ########
    # 分药 #
    ########
    @Slot()
    def start_dispensing(self):
        """开始分药流程"""

        self.monitor_timer = QTimer()
        self.monitor_timer.timeout.connect(self.monitor_dispensing_status)
        try:
            if not self.current_medicines:
                print("[错误] 没有药品需要分发")
                self.error_occurred_signal.emit("没有药品需要分发")
                return
            
            if not self.dispenser:
                print("[错误] 分药机未初始化")
                self.error_occurred_signal.emit("分药机未初始化")
                return
            
            self.is_dispensing = True
            self.current_medicine_index = 0
            
            print("[分药] 开始分药流程")
            self.dispensing_started_signal.emit()
            
            # 开始分发第一种药品
            self.dispense_next_medicine()
            
        except Exception as e:
            print(f"[错误] 启动分药异常: {str(e)}")
            self.error_occurred_signal.emit(f"启动分药异常: {str(e)}")

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
            self.current_medicine_info_signal.emit(
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
                self.error_occurred_signal.emit(error_msg)
                return
            
            print(f"[分药] 数据发送成功，等待分药完成...")
            
            # 启动监控定时器
            self.monitor_timer.start(1000)  # 每秒检查一次

        except Exception as e:
            error_msg = f"分药异常: {str(e)}"
            print(f"[错误] {error_msg}")
            self.error_occurred_signal.emit(error_msg)

    def monitor_dispensing_status(self):
        """监控分药状态"""
        try:
            # 如果没有分药机或未处于分药状态，停止监控
            if not self.dispenser or not self.is_dispensing:
                self.monitor_timer.stop()
                return
            
            self.pill_remain = self.dispenser.pill_remain if hasattr(self.dispenser, 'pill_remain') else 0
            self.dispensing_progress_signal.emit((self.total_pill - self.pill_remain) / self.total_pill * 100)
            
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
                    self.error_occurred_signal.emit(error_msg)

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
            self.error_occurred_signal.emit(error_msg)

    def complete_dispensing(self):
        """完成分药流程"""
        try:
            self.is_dispensing = False
            self.monitor_timer.stop()
            print("[分药] 所有药品分发完成")
            self.dispensing_completed_signal.emit()
            
            # 开启药盘
            print("[分药] 开启药盘...")
            if self.open_plate():
                print("[警告] 开启药盘失败")
            
        except Exception as e:
            error_msg = f"完成分药异常: {str(e)}"
            print(f"[错误] {error_msg}")
            self.error_occurred_signal.emit(error_msg)


class MainWindow(QMainWindow):
    def __init__(self):
        super(MainWindow, self).__init__()
        self.ui = Ui_MainWindow()
        self.ui.setupUi(self) # 设置UI界面

        self.controller = DispenserController()  # 实例化控制器
        # self.controller.initialize_hardware()  # 初始化硬件
        # self.controller.initialize_prescription_database()  # 初始化处方数据库
        self.controller_worker_thread = QThread()  # 创建工作线程
        self.controller.moveToThread(self.controller_worker_thread)
        self.controller_worker_thread.start() 

        self.ui.rignt_stackedWidget.setCurrentIndex(0)  # Set the initial page

    def connection(self):
        """连接UI信号和控制器槽函数"""
        self.ui.start_dispense_button.clicked.connect(self.prepare_for_dispensing)
        self.ui.next_patient_button.clicked.connect(self.prepare_for_dispensing)
        self.ui.send_plate_in_button.clicked.connect(self.close_plate)
        self.ui.refresh_rfid_button.clicked.connect(self.refresh_rdid)

        self.controller.pills_dispensing_list_loaded_signal.connect(self.show_get_prescription_message)
        self.controller.current_medicine_info_signal.connect(self.update_prescription_info)
        self.controller.dispensing_completed_signal.connect(self.move_to_finish_page)
        self.controller.dispensing_progress_signal.connect(self.set_dispense_progress_bar_value)
        self.controller.hardware_initialized_signal.connect(self.update_hardware_status_label)
        self.controller.prescription_database_initialized_signal.connect(self.update_database_status_label)
        self.controller.rfid_detected_signal.connect(self.update_rfid_status_label)
        self.controller.pills_dispensing_list_loaded_signal.connect(self.update_pills_dispensing_list_label)
        self.controller.dispensing_completed_signal.connect(self.update_dispensing_finished_label)
        self.controller.plate_opened_signal.connect(self.update_plate_opened_label)
        self.controller.plate_closed_signal.connect(self.update_plate_closed_label)
        self.controller.error_occurred_signal.connect(self.update_error_msg)


    ############
    # 页面切换 #
    ############
    def move_to_start_page(self):
        """切换到开始页面"""
        self.ui.rignt_stackedWidget.setCurrentIndex(0)

    @Slot()
    def move_to_put_pan_in_page(self):
        """切换到放入药盘页面"""
        self.reset_labels()
        self.ui.pan_img.show()  # Show the pan image
        self.ui.green_arrow.show()  # Show the green arrow
        self.ui.guide_msg_2.setText("将药盘放入机器托盘中")
        self.ui.check_mark_2.hide()  # Hide the check mark initially
        self.ui.get_prescription_msg.hide()  # Hide the prescription message initially
        self.ui.send_plate_in_button.hide()  # Hide the button initially
        self.ui.rignt_stackedWidget.setCurrentIndex(1)

    def move_to_dispensing_page(self):
        """切换到分药页面"""
        self.ui.rignt_stackedWidget.setCurrentIndex(2)

    def move_to_finish_page(self):
        """切换到完成页面"""
        self.ui.rignt_stackedWidget.setCurrentIndex(3)

    ##############
    # 更新GUI信息 #
    ##############
    @Slot(dict)
    def show_get_prescription_message(self, pills_dispensing_list):
        """显示处方信息"""

        self.ui.get_prescription_msg.setText(f"成功获取到处方,当前患者：{pills_dispensing_list['name']}")
        self.ui.check_mark_2.show()
        self.ui.get_prescription_msg.show()
        self.ui.send_plate_in_button.show()  # 显示放入药盘按钮
        self.ui.patient_name.setText(pills_dispensing_list['name'])


    @Slot(str, int, int, int)
    def update_prescription_info(self, current_medicine_name, total_pills_num, current_medicine_index, total_medicines_num):
        """更新处方信息显示"""
        self.ui.current_drug.setText(current_medicine_name)
        self.ui.pills_num_msg_2.setText(str(total_pills_num))

    @Slot(int)
    def set_dispense_progress_bar_value(self, value):
        """设置分药进度条的值"""
        self.ui.dispense_progressBar.setValue(value)
        self.ui.progressBar_percentage.setText(f"{value}%")

    @Slot()
    def update_error_msg(self, error_msg):
        """更新错误信息显示"""
        if "RFID读取失败" in error_msg:
            self.ui.get_prescription_msg.setText("处方读取失败，请正确放入药盘后点击刷新按钮重新读取")
            self.ui.get_prescription_msg.show()
            self.ui.check_mark_2.hide()

    ###########################
    # 更新GUI分药流程进度栏信息 #
    ###########################
    @Slot()
    def update_hardware_status_label(self):
        """更新硬件连接状态标签显示"""
        if hasattr(self.ui, 'hardware_status_label'):
            self.ui.hardware_status_label.setStyleSheet("color: hsla(165, 33%, 62%, 1);")
        if hasattr(self.ui, 'task_progressBar'):
            self.ui.task_progressBar.setValue(1)

    @Slot()
    def update_database_status_label(self):
        """更新处方连接状态标签显示"""
        if hasattr(self.ui, 'database_status_label'):
            self.ui.database_status_label.setStyleSheet("color: hsla(165, 33%, 62%, 1);")
        if hasattr(self.ui, 'task_progressBar'):
            self.ui.task_progressBar.setValue(2)
    
    @Slot()
    def update_rfid_status_label(self):
        """更新RFID连接状态标签显示"""
        self.ui.pan_img.hide()  # 隐藏药盘图片
        self.ui.green_arrow.hide()  # 隐藏绿色箭头
        self.ui.guide_msg_2.setText("送入药盘开始分药")

        if hasattr(self.ui, 'rfid_status_label'):
            self.ui.rfid_status_label.setStyleSheet("color: hsla(165, 33%, 62%, 1);")
        if hasattr(self.ui, 'task_progressBar'):
            self.ui.task_progressBar.setValue(3)
    
    @Slot()
    def update_pills_dispensing_list_label(self):
        """更新处方信息状态标签显示"""
        if hasattr(self.ui, 'pills_dispensing_list_label'):
            self.ui.pills_dispensing_list_label.setStyleSheet("color: hsla(165, 33%, 62%, 1);")
        if hasattr(self.ui, 'task_progressBar'):
            self.ui.task_progressBar.setValue(4)

    @Slot()
    def update_plate_closed_label(self):
        """更新药盘状态标签显示"""
        if hasattr(self.ui, 'plate_closed_label'):
            self.ui.plate_closed_label.setStyleSheet("color: hsla(165, 33%, 62%, 1);")
        if hasattr(self.ui, 'task_progressBar'):
            self.ui.task_progressBar.setValue(5)

    @Slot()
    def update_dispensing_finished_label(self):
        """更新分药状态标签显示"""
        if hasattr(self.ui, 'dispensing_finished_label'):
            self.ui.dispensing_finished_label.setStyleSheet("color: hsla(165, 33%, 62%, 1);")
        if hasattr(self.ui, 'task_progressBar'):
            self.ui.task_progressBar.setValue(6)

    @Slot()
    def update_plate_opened_label(self):
        """更新药盘状态标签显示"""
        if hasattr(self.ui, 'plate_opened_label'):
            self.ui.plate_opened_label.setStyleSheet("color: hsla(165, 33%, 62%, 1);")
        if hasattr(self.ui, 'task_progressBar'):
            self.ui.task_progressBar.setValue(7)

    def reset_labels(self):
        """重置所有状态标签的样式"""
        if hasattr(self.ui, 'rfid_status_label'):
            self.ui.rfid_status_label.setStyleSheet("color:  rgb(136, 136, 136);")
        if hasattr(self.ui, 'pills_dispensing_list_label'):
            self.ui.pills_dispensing_list_label.setStyleSheet("color:  rgb(136, 136, 136);")
        if hasattr(self.ui, 'plate_closed_label'):
            self.ui.plate_closed_label.setStyleSheet("color:  rgb(136, 136, 136);")
        if hasattr(self.ui, 'plate_opened_label'):
            self.ui.plate_opened_label.setStyleSheet("color:  rgb(136, 136, 136);")
        if hasattr(self.ui, 'dispensing_finished_label'):
            self.ui.dispensing_finished_label.setStyleSheet("color:  rgb(136, 136, 136);")

    ###############################
    # 调用controller线程控制分药机 #
    ###############################
    @Slot(str)
    def prepare_for_dispensing(self):
        # 重置标签
        self.move_to_put_pan_in_page()

        if not self.controller.hardware_initialized:
            from PySide6.QtCore import QMetaObject, Qt
            QMetaObject.invokeMethod(self.controller, "initialize_hardware", Qt.QueuedConnection)
        if not self.controller.prescription_database_initialized:
            from PySide6.QtCore import QMetaObject, Qt
            QMetaObject.invokeMethod(self.controller, "initialize_prescription_database", Qt.QueuedConnection)
        # 使用QMetaObject.invokeMethod在工作线程中调用方法
        from PySide6.QtCore import QMetaObject, Qt
        QMetaObject.invokeMethod(self.controller, "prepare_for_rfid_detection", Qt.QueuedConnection)
        # 延迟1秒后执行start_rfid_detection
        QTimer.singleShot(1000, lambda: QMetaObject.invokeMethod(self.controller, "start_rfid_detection", Qt.QueuedConnection))
    
    def close_plate(self):
        """关闭药盘"""
        from PySide6.QtCore import QMetaObject, Qt
        QMetaObject.invokeMethod(self.controller, "close_plate", Qt.QueuedConnection)

        self.move_to_dispensing_page()  # 切换到分药页面
        from PySide6.QtCore import QMetaObject, Qt
        QMetaObject.invokeMethod(self.controller, "start_dispensing", Qt.QueuedConnection)

    @Slot()
    def refresh_rdid(self):
        """刷新RFID"""
        self.ui.get_prescription_msg.setText("正在重新读取处方信息...")
        self.ui.check_mark_2.hide()  # 隐藏勾选标记
        if not self.controller.rfid_reader:
            print("[错误] RFID读卡器未初始化")
            self.controller.error_occurred_signal.emit("RFID读卡器未初始化")
            return
        
        # 重新开始RFID检测
        from PySide6.QtCore import QMetaObject, Qt
        QMetaObject.invokeMethod(self.controller, "start_rfid_detection", Qt.QueuedConnection)





############################
# 无GUI controller 测试函数 #
############################
def control_test():
    # 测试控制器
    app = QApplication(sys.argv)
    
    print("=== 分药机控制器测试 ===")
    controller = DispenserController()
    
    # 连接信号
    controller.error_occurred_signal.connect(lambda msg: print(f"[UI错误] {msg}"))
    controller.dispensing_started_signal.connect(lambda: print(f"[UI] 分药开始"))
    controller.current_medicine_info_signal.connect(lambda name, cur, total: print(f"[UI] 分药进度: {name} ({cur}/{total})"))
    controller.dispensing_completed_signal.connect(lambda: print(f"[UI] 分药完成"))
    
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

