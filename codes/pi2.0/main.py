import sys
import os
from PySide6.QtWidgets import QApplication, QMainWindow
from PySide6.QtCore import QObject, Signal, Slot, QTimer, QThread, Qt
from PySide6.QtGui import QPixmap, QIcon
from main_window_ui import Ui_MainWindow
from dispenser import Dispenser
from rfid_reader import RFIDReader
from prescription_database import PrescriptionDatabase
import numpy as np
import serial.tools.list_ports

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
    medicine_transition_signal = Signal(str, str)  # 当前药品完成，下一药品信息 (current_medicine, next_medicine)
    

    def __init__(self, dispenser_port=None, rfid_port=None):
        super().__init__()

        # 自动检测串口
        detected_dispenser_port, detected_rfid_port = self.auto_detect_ports()
        
        # 使用检测到的端口，如果检测失败则使用传入的参数或默认值
        self.dispenser_port = detected_dispenser_port or dispenser_port or "COM6"
        self.rfid_port = detected_rfid_port or rfid_port or "COM9"
        
        print(f"[初始化] 使用端口 - 分药机: {self.dispenser_port}, RFID: {self.rfid_port}")
        
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
            
    @Slot()
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
    def close_plate_when_exit(self):
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
            # self.plate_closed_signal.emit()  # 发射药盘关闭信号
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

            # 发射药品转换信号
            if self.dispenser.pill_remain == 0 and self.current_medicine_index < len(self.current_medicines):
                current_medicine_name = self.current_medicines[self.current_medicine_index]['medicine_name']
                next_medicine_index = self.current_medicine_index + 1
                if next_medicine_index < len(self.current_medicines):
                    next_medicine_name = self.current_medicines[next_medicine_index]['medicine_name']
                    self.medicine_transition_signal.emit(current_medicine_name, next_medicine_name)
                else:
                    # 最后一个药品分发完成
                    self.medicine_transition_signal.emit(current_medicine_name, "")

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
                    self.dispense_next_medicine()
                    
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

    def auto_detect_ports(self):
        """自动检测分药机和RFID设备的串口"""
        try:
            # 获取所有可用串口
            ports = serial.tools.list_ports.comports()
            
            dispenser_port = None
            rfid_port = None
            
            print("[检测] 正在扫描可用串口...")
            
            for port in ports:
                port_name = port.device
                description = port.description
                manufacturer = (port.manufacturer or "")
                
                print(f"[检测] 发现串口: {port_name}")
                print(f"  描述: {description}")
                print(f"  制造商: {manufacturer}")
                print("---")
                
                # 根据你提供的设备描述进行匹配
                # 分药机识别：USB-SERIAL CH340
                if "USB-SERIAL CH340" in description or "CH340" in description:
                    if not dispenser_port:
                        dispenser_port = port_name
                        print(f"[检测] 识别为分药机: {port_name} (CH340)")
                
                # RFID识别：Silicon Labs CP210x USB to UART Bridge
                elif "Silicon Labs CP210x USB to UART Bridge" in description or \
                     ("Silicon Labs" in description and "CP210" in description):
                    if not rfid_port:
                        rfid_port = port_name
                        print(f"[检测] 识别为RFID读卡器: {port_name} (CP210x)")
            
            # 如果无法通过精确描述识别，尝试备用关键词
            if not dispenser_port or not rfid_port:
                print("[检测] 精确匹配失败，尝试关键词匹配...")
                
                for port in ports:
                    port_name = port.device
                    description = port.description.lower()
                    
                    # 分药机备用关键词
                    if not dispenser_port and any(keyword in description for keyword in ['ch340', 'ch341']):
                        dispenser_port = port_name
                        print(f"[检测] 通过关键词识别为分药机: {port_name}")
                    
                    # RFID备用关键词
                    elif not rfid_port and any(keyword in description for keyword in ['cp210', 'silicon labs']):
                        rfid_port = port_name
                        print(f"[检测] 通过关键词识别为RFID: {port_name}")
            
            # 如果仍然无法识别，按端口顺序分配
            if not dispenser_port or not rfid_port:
                available_ports = [port.device for port in ports]
                available_ports.sort()  # 按端口号排序
                
                print(f"[检测] 关键词匹配失败，可用端口: {available_ports}")
                
                if not dispenser_port and available_ports:
                    dispenser_port = available_ports[0]
                    print(f"[检测] 默认分配分药机端口: {dispenser_port}")
                
                if not rfid_port and len(available_ports) > 1:
                    # 找一个不是分药机的端口
                    for port in available_ports:
                        if port != dispenser_port:
                            rfid_port = port
                            break
                    print(f"[检测] 默认分配RFID端口: {rfid_port}")
                elif not rfid_port and len(available_ports) == 1:
                    # 如果只有一个端口，可能两个设备共用
                    rfid_port = available_ports[0]
                    print(f"[检测] 使用共用端口: {rfid_port}")
            
            if dispenser_port and rfid_port:
                print(f"[成功] 自动检测完成 - 分药机: {dispenser_port}, RFID: {rfid_port}")
            else:
                print(f"[警告] 自动检测不完整 - 分药机: {dispenser_port}, RFID: {rfid_port}")
            
            return dispenser_port, rfid_port
            
        except Exception as e:
            print(f"[错误] 自动检测串口失败: {e}")
            return None, None

    def show_all_ports_info(self):
        """显示所有串口详细信息（调试用）"""
        try:
            ports = serial.tools.list_ports.comports()
            print("=== 所有可用串口信息 ===")
            for i, port in enumerate(ports):
                print(f"串口 {i+1}: {port.device}")
                print(f"  描述: {port.description}")
                print(f"  制造商: {port.manufacturer}")
                print(f"  VID:PID: {port.vid}:{port.pid}")
                print(f"  序列号: {port.serial_number}")
                print("---")
        except Exception as e:
            print(f"获取串口信息失败: {e}")

    def test_port_connection(self, port, device_type):
        """测试端口连接是否正常"""
        try:
            if not port:
                return False
                
            print(f"[测试] 正在测试{device_type}端口: {port}")
            
            # 尝试打开串口
            test_serial = serial.Serial(
                port=port,
                baudrate=115200,
                timeout=1.0
            )
            test_serial.close()
            print(f"[测试] {device_type}端口{port}连接正常")
            return True
            
        except Exception as e:
            print(f"[测试] {device_type}端口{port}连接失败: {e}")
            return False

class MainWindow(QMainWindow):
    def __init__(self):
        super(MainWindow, self).__init__()
        self.ui = Ui_MainWindow()
        self.ui.setupUi(self)

        # 固定窗口大小为960x540，并禁止最大化和拉伸
        self.setFixedSize(960, 540)
        self.setWindowFlags(self.windowFlags() & ~Qt.WindowMaximizeButtonHint)
        
        # 设置药品图片目录路径
        self.medicine_images_dir = "imgs/medicines"  # 根据你的实际目录调整
        self.placeholder_image = "imgs/medicines/place_holder.png"  # 占位图片路径

        # 使用自动检测的控制器（不传入端口参数，让它自动检测）
        self.controller = DispenserController()
        
        self.controller_worker_thread = QThread()
        self.controller.moveToThread(self.controller_worker_thread)
        self.controller_worker_thread.start()

        self.ui.rignt_stackedWidget.setCurrentIndex(0)
        
        # 初始化check_marks（隐藏所有）
        self.initialize_check_marks()

    def connection(self):
        """连接UI信号和控制器槽函数"""
        self.ui.start_dispense_button.clicked.connect(self.prepare_for_dispensing)
        self.ui.next_patient_button.clicked.connect(self.go_to_next_patient)
        self.ui.send_plate_in_button.clicked.connect(self.close_plate)
        self.ui.refresh_rfid_button.clicked.connect(self.refresh_rfid)
        self.ui.finish_dispensing_button.clicked.connect(self.finish_dispensing)

        self.controller.pills_dispensing_list_loaded_signal.connect(self.update_prescription_info)
        self.controller.pills_dispensing_list_loaded_signal.connect(self.update_to_plate_closed_label)
        self.controller.current_medicine_info_signal.connect(self.update_current_medicine_info)
        self.controller.dispensing_completed_signal.connect(self.move_to_finish_page)
        self.controller.dispensing_completed_signal.connect(self.update_to_plate_opened_label)
        self.controller.dispensing_progress_signal.connect(self.set_dispense_progress_bar_value)
        self.controller.hardware_initialized_signal.connect(self.update_to_database_status_label)
        self.controller.prescription_database_initialized_signal.connect(self.update_to_rfid_status_label)
        self.controller.rfid_detected_signal.connect(self.update_to_pills_dispensing_list_label)
        self.controller.plate_closed_signal.connect(self.update_to_dispensin_finished_label)
        self.controller.error_occurred_signal.connect(self.update_error_msg)
        self.controller.medicine_transition_signal.connect(self.update_medicine_transition_status)


    ############
    # 页面切换 #
    ############
    def move_to_start_page(self):
        """切换到开始页面"""
        self.ui.rignt_stackedWidget.setCurrentIndex(0)

    @Slot()
    def move_to_put_pan_in_page(self):
        """切换到放入药盘页面"""
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
    def update_prescription_info(self, pills_dispensing_list):
        """更新获取处方信息的消息和显示放入药盘按钮"""
        self.ui.get_prescription_msg.setText(f"成功获取到处方,当前患者：{pills_dispensing_list['name']}")
        self.ui.check_mark_2.show()
        self.ui.get_prescription_msg.show()
        self.ui.send_plate_in_button.show()  # 显示放入药盘按钮
        self.ui.patient_name.setText(pills_dispensing_list['name'])


    def load_medicine_image(self, medicine_name):
        """
        根据药品名加载对应的图片
        :param medicine_name: 药品名称
        :return: 是否成功加载图片
        """
        try:
            # 常见的图片格式
            image_extensions = ['.png', '.jpg', '.jpeg', '.bmp', '.gif']
            
            # 清理药品名称（移除特殊字符，替换空格为下划线）
            clean_name = medicine_name.replace(' ', '_').replace('/', '_').replace('\\', '_')
            
            # 尝试不同的文件名格式和扩展名
            possible_names = [
                clean_name,
                medicine_name,
                clean_name.lower(),
                medicine_name.lower()
            ]
            
            for name in possible_names:
                for ext in image_extensions:
                    image_path = os.path.join(self.medicine_images_dir, f"{name}{ext}")
                    if os.path.exists(image_path):
                        pixmap = QPixmap(image_path)
                        if not pixmap.isNull():
                            # 缩放到固定尺寸 300x200，保持纵横比
                            scaled_pixmap = pixmap.scaled(
                                300, 200,
                                Qt.KeepAspectRatio,
                                Qt.SmoothTransformation
                            )
                            self.ui.current_drug_img.setPixmap(scaled_pixmap)
                            print(f"[图片] 成功加载药品图片: {image_path}")
                            return True
        
            # 如果没有找到对应图片，使用占位图片
            self.load_placeholder_image()
            print(f"[图片] 未找到药品 '{medicine_name}' 的图片，使用占位图片")
            return False
        
        except Exception as e:
            print(f"[错误] 加载药品图片失败: {e}")
            self.load_placeholder_image()
            return False

    def load_placeholder_image(self):
        """加载占位图片"""
        try:
            if os.path.exists(self.placeholder_image):
                pixmap = QPixmap(self.placeholder_image)
                if not pixmap.isNull():
                    # 缩放到固定尺寸 300x200
                    scaled_pixmap = pixmap.scaled(
                        300, 200,
                        Qt.KeepAspectRatio,
                        Qt.SmoothTransformation
                    )
                    self.ui.current_drug_img.setPixmap(scaled_pixmap)
                    print(f"[图片] 加载占位图片: {self.placeholder_image}")
                else:
                    self.ui.current_drug_img.setText("暂无图片")
            else:
                self.ui.current_drug_img.setText("暂无图片")
                print(f"[警告] 占位图片不存在: {self.placeholder_image}")
        except Exception as e:
            print(f"[错误] 加载占位图片失败: {e}")
            self.ui.current_drug_img.setText("暂无图片")

    @Slot(str, int, int, int)
    def update_current_medicine_info(self, current_medicine_name, total_pills_num, current_medicine_index, total_medicines_num):
        """更新当前药品信息显示"""
        self.ui.is_dispensing_msg.setText("正在分发药品...")
        self.ui.put_pills_in_msg.setText("请投入药品到分药机中")
        self.ui.current_drug.setText(current_medicine_name)
        self.ui.pills_num_msg_2.setText(str(total_pills_num))
        
        # 加载药品图片
        self.load_medicine_image(current_medicine_name)

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

    @Slot(str, str)
    def update_medicine_transition_status(self, current_medicine, next_medicine):
        """更新药品转换状态标签"""
        if next_medicine:
            status_text_1 = f"{current_medicine}分发完成"
            status_text_2 = f"即将分发{next_medicine}，请准备"
        else:
            status_text_1 = f"{current_medicine}分发完成"
            status_text_2 = "所有药品分发完成，即将退出药盘"
        
        if hasattr(self.ui, 'is_dispensing_msg'):
            self.ui.is_dispensing_msg.setText(status_text_1)
        if hasattr(self.ui, 'put_pills_in_msg'):
            self.ui.put_pills_in_msg.setText(status_text_2)

    ###########################
    # 更新GUI分药流程进度栏信息 #
    ###########################
    def set_label_active_style(self, label):
        """设置标签为当前激活状态的样式（绿色背景+白色文字）"""
        label.setStyleSheet("""
            color: white;
            font-family: "Source Han Sans SC";
            background-color: hsla(165, 33%, 62%, 1);
            padding: 5px;
            border-radius: 4px;
            font-weight: normal;
        """)
        
        # 隐藏对应的check_mark（当前激活状态不显示完成标记）
        self.hide_check_mark_for_label(label)

    def set_label_completed_style(self, label):
        """设置标签为已完成状态的样式（绿色文字+无背景）"""
        label.setStyleSheet("""
            color: hsla(165, 33%, 62%, 1);
            font-family: "Source Han Sans SC";
            background-color: transparent;
            padding: 5px;
            border-radius: 4px;
            font-weight: normal;
        """)
        
        # 显示对应的check_mark
        self.show_check_mark_for_label(label)

    def show_check_mark_for_label(self, label):
        """根据标签显示对应的check_mark"""
        # 定义标签与check_mark的对应关系
        label_to_check_mark = {
            'hardware_status_label': 'green_check_mark_1',
            'database_status_label': 'green_check_mark_2', 
            'rfid_status_label': 'green_check_mark_3',
            'pills_dispensing_list_label': 'green_check_mark_4',
            'plate_closed_label': 'green_check_mark_5',
            'dispensing_finished_label': 'green_check_mark_6',
            'plate_opened_label': 'green_check_mark_7'
        }
        
        # 获取标签的对象名
        label_name = label.objectName()
        
        # 如果找到对应的check_mark，则显示它
        if label_name in label_to_check_mark:
            check_mark_name = label_to_check_mark[label_name]
            if hasattr(self.ui, check_mark_name):
                check_mark = getattr(self.ui, check_mark_name)
                check_mark.show()
                print(f"[UI] 显示完成标记: {check_mark_name}")

    def hide_check_mark_for_label(self, label):
        """隐藏对应标签的check_mark"""
        # 定义标签与check_mark的对应关系
        label_to_check_mark = {
            'hardware_status_label': 'green_check_mark_1',
            'database_status_label': 'green_check_mark_2',
            'rfid_status_label': 'green_check_mark_3',
            'pills_dispensing_list_label': 'green_check_mark_4',
            'plate_closed_label': 'green_check_mark_5',
            'dispensing_finished_label': 'green_check_mark_6',
            'plate_opened_label': 'green_check_mark_7'
        }
        
        # 获取标签的对象名
        label_name = label.objectName()
        
        # 如果找到对应的check_mark，则隐藏它
        if label_name in label_to_check_mark:
            check_mark_name = label_to_check_mark[label_name]
            if hasattr(self.ui, check_mark_name):
                check_mark = getattr(self.ui, check_mark_name)
                check_mark.hide()

    def initialize_check_marks(self):
        """初始化时隐藏所有check_mark"""
        check_marks = [
            'green_check_mark_1',
            'green_check_mark_2', 
            'green_check_mark_3',
            'green_check_mark_4',
            'green_check_mark_5',
            'green_check_mark_6',
            'green_check_mark_7'
        ]
        
        for check_mark_name in check_marks:
            if hasattr(self.ui, check_mark_name):
                check_mark = getattr(self.ui, check_mark_name)
                check_mark.hide()
                print(f"[初始化] 隐藏check_mark: {check_mark_name}")

    def set_label_pending_style(self, label):
        """设置标签为待完成状态的样式（灰色文字+无背景）"""
        label.setStyleSheet("""
            color: rgb(136, 136, 136);
            background-color: transparent;
            font-family: "Source Han Sans SC";
            padding: 5px;
            border-radius: 4px;
            font-weight: normal;
        """)
    
        # 隐藏对应的check_mark（待完成状态不显示完成标记）
        self.hide_check_mark_for_label(label)

    def update_all_labels_status(self, current_step):
        """
        根据当前步骤更新所有标签的状态
        :param current_step: 当前步骤（1-7）
        """
        # 定义所有标签的顺序
        label_sequence = [
            'hardware_status_label',          # 步骤1
            'database_status_label',          # 步骤2  
            'rfid_status_label',              # 步骤3
            'pills_dispensing_list_label',    # 步骤4
            'plate_closed_label',             # 步骤5
            'dispensing_finished_label',      # 步骤6
            'plate_opened_label'              # 步骤7
        ]
        
        for i, label_name in enumerate(label_sequence):
            if hasattr(self.ui, label_name):
                label = getattr(self.ui, label_name)
                step_number = i + 1
                
                if step_number < current_step:
                    # 已完成的步骤：绿色文字，无背景
                    self.set_label_completed_style(label)
                elif step_number == current_step:
                    # 当前步骤：绿色背景，白色文字
                    self.set_label_active_style(label)
                else:
                    # 未完成的步骤：灰色文字，无背景
                    self.set_label_pending_style(label)

    @Slot()
    def update_to_hardware_status_label(self):
        """更新硬件连接状态标签显示"""
        self.update_all_labels_status(1)
        if hasattr(self.ui, 'task_progressBar'):
            self.ui.task_progressBar.setValue(1)

    @Slot()
    def update_to_database_status_label(self):
        """更新处方连接状态标签显示"""
        self.update_all_labels_status(2)
        if hasattr(self.ui, 'task_progressBar'):
            self.ui.task_progressBar.setValue(2)

    @Slot()
    def update_to_rfid_status_label(self):
        """更新RFID连接状态标签显示"""
        
        self.update_all_labels_status(3)
        if hasattr(self.ui, 'task_progressBar'):
            self.ui.task_progressBar.setValue(3)

    @Slot()
    def update_to_pills_dispensing_list_label(self):
        """更新处方信息状态标签显示"""
        self.ui.pan_img.hide()  # 隐藏药盘图片
        self.ui.green_arrow.hide()  # 隐藏绿色箭头
        self.ui.guide_msg_2.setText("送入药盘开始分药")
        self.update_all_labels_status(4)
        if hasattr(self.ui, 'task_progressBar'):
            self.ui.task_progressBar.setValue(4)

    @Slot()
    def update_to_plate_closed_label(self):
        """更新药盘状态标签显示"""
        self.update_all_labels_status(5)
        if hasattr(self.ui, 'task_progressBar'):
            self.ui.task_progressBar.setValue(5)

    @Slot()
    def update_to_dispensin_finished_label(self):
        """更新分药状态标签显示"""
        self.update_all_labels_status(6)
        if hasattr(self.ui, 'task_progressBar'):
            self.ui.task_progressBar.setValue(6)

    @Slot()
    def update_to_plate_opened_label(self):
        """更新药盘状态标签显示"""
        self.update_all_labels_status(7)
        if hasattr(self.ui, 'task_progressBar'):
            self.ui.task_progressBar.setValue(7)

    def reset_labels(self, text_color="rgb(136, 136, 136)", background_color="transparent"):
        """
        重置所有状态标签的样式
        :param text_color: 文字颜色，默认为灰色 rgb(136, 136, 136)
        :param background_color: 背景颜色，默认为透明 transparent
        """
        # 构建样式字符串
        style = f"color: {text_color}; background-color: {background_color}; padding: 5px; border-radius: 4px; font-weight: normal;"
        
        label_list = [
            'hardware_status_label',
            'database_status_label', 
            'rfid_status_label',
            'pills_dispensing_list_label',
            'plate_closed_label',
            'dispensing_finished_label',
            'plate_opened_label'
        ]
        
        for label_name in label_list:
            if hasattr(self.ui, label_name):
                getattr(self.ui, label_name).setStyleSheet(style)


    ###############################
    # 调用controller线程控制分药机 #
    ###############################
    @Slot(str)
    def prepare_for_dispensing(self):
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
    def refresh_rfid(self):
        """刷新RFID"""
        self.ui.get_prescription_msg.setText("正在重新读取处方信息中，请稍后...")
        if not self.controller.rfid_reader:
            print("[错误] RFID读卡器未初始化")
            self.controller.error_occurred_signal.emit("RFID读卡器未初始化")
            return
        
        # 重置所有状态标签
        self.hide_check_mark_for_label(self.ui.rfid_status_label)
        self.hide_check_mark_for_label(self.ui.pills_dispensing_list_label)
        self.update_to_rfid_status_label()

        # 重新开始RFID检测
        from PySide6.QtCore import QMetaObject, Qt
        QMetaObject.invokeMethod(self.controller, "start_rfid_detection", Qt.QueuedConnection)

    @Slot()
    def finish_dispensing(self):
        """完成分药流程"""
        # 关闭药盘
        from PySide6.QtCore import QMetaObject, Qt
        QMetaObject.invokeMethod(self.controller, "close_plate_when_exit", Qt.QueuedConnection)

        # 切换到完成页面
        self.update_to_rfid_status_label()
        self.move_to_start_page()

    @Slot()
    def go_to_next_patient(self):
        """切换到下一个患者"""
        self.update_to_rfid_status_label()
        self.prepare_for_dispensing()






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
    
    # 设置应用程序图标
    app.setWindowIcon(QIcon("imgs/icon.png"))  # 替换为你的图标路径
    
    window = MainWindow()
    window.connection()
    window.show()

    sys.exit(app.exec())

