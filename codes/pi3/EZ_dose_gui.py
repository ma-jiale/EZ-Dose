import sys
import cv2
from PySide6.QtWidgets import QApplication, QMainWindow, QWidget, QPushButton, QVBoxLayout, QScrollArea, QDialog, QMessageBox
from PySide6.QtCore import QObject, Signal, Slot, QTimer, QThread, Qt
from PySide6.QtGui import QImage, QPixmap

from main_window_ui import Ui_MainWindow
from today_patient_ui import Ui_today_patient
from main_controller import MainController
from cam_controller import CamController, CamMode
from startup_screen_ui import Ui_StartupScreen


class StartupScreen(QDialog):
    def __init__(self):
        super().__init__()
        self.ui = Ui_StartupScreen()
        self.ui.setupUi(self)
        
        # 设置窗口属性
        self.setModal(True)
        self.setWindowFlags(Qt.FramelessWindowHint)  # 无边框窗口
        self.setFixedSize(480, 480)
        
        # 居中显示
        self.center_on_screen()
        
    def center_on_screen(self):
        """将窗口居中显示在屏幕上"""
        screen = QApplication.primaryScreen().geometry()
        size = self.geometry()
        self.move(
            (screen.width() - size.width()) // 2,
            (screen.height() - size.height()) // 2
        )
    
    def update_status(self, message):
        """更新状态信息"""
        self.ui.txt_process_info.setText(message)
        QApplication.processEvents()  # 强制刷新UI

class MainWindow(QMainWindow):
    def __init__(self, manager):
        super().__init__()
        self.ui = Ui_MainWindow()
        self.ui.setupUi(self)
        self.manager = manager
        
        # Define page indices as constants
        self.PAGES = {
            'start': 0,
            'scan_qrcode': 1,
            'scan_rfid': 2,
            'rx': 3,
            'dispensing': 4,
            'finish': 5
        }
        self.connect_signals()
        # Set initial page
        self.go_to_page('start')



    def go_to_page(self, page_name):
        """Navigate to specified page by name"""
        if page_name in self.PAGES:
            self.ui.stackedWidget.setCurrentIndex(self.PAGES[page_name])
            print(f"Navigated to {page_name} page")
        else:
            print(f"Warning: Page '{page_name}' not found")
    
    def connect_signals(self):
        self.ui.btn_start_find_patient.clicked.connect(self.manager.show_today_patients)
        self.ui.btn_start_dispensing.clicked.connect(self.manager.start_dispensing)
        self.ui.btn_continue_dispensing.clicked.connect(self.manager.show_today_patients)
        self.ui.btn_finish_dispensing.clicked.connect(self.manager.finish_dispensing)
        self.ui.btn_refresh_database.clicked.connect(self.manager.refresh_database)

    @Slot(object)
    def update_prescription_info(self, pills_dispensing_list):
        """更新处方信息显示"""
        try:
            patient_info = pills_dispensing_list.get("patient_info", {})
            patient_name = patient_info.get('patient_name', 'Unknown')
            patient_id = patient_info.get('patient_id', 0)
            
            # 从patient_id解析楼层和床位号
            # patient_id的第一位代表楼层，后面的位数代表床位号
            if patient_id:
                patient_id_str = str(patient_id)
                floor = patient_id_str[0]  # 第一位是楼层
                
                # 格式化显示信息
                prescription_info = f"成功获取{floor}楼{patient_id_str}床{patient_name}的处方信息"
                patient_info_display = f"{patient_name}    {floor}楼{patient_id_str}床"
            else:
                prescription_info = f"成功获取{patient_name}的处方信息"
                patient_info_display = f"{patient_name}"
            
            # 更新处方信息显示
            if hasattr(self.ui, 'txt_load_prescription_info'):
                self.ui.txt_load_prescription_info.setText(prescription_info)
            
            # 更新患者信息显示
            if hasattr(self.ui, 'txt_patient_info'):
                self.ui.txt_patient_info.setText(patient_info_display)
            
            print(f"[UI] 处方信息已更新: {prescription_info}")
            print(f"[UI] 患者信息已更新: {patient_info_display}")
            
        except Exception as e:
            print(f"[错误] 更新处方信息异常: {str(e)}")
            if hasattr(self.ui, 'txt_load_prescription_info'):
                self.ui.txt_load_prescription_info.setText("获取处方信息失败")
            if hasattr(self.ui, 'txt_patient_info'):
                self.ui.txt_patient_info.setText("患者信息获取失败")

    @Slot(str, int)
    def update_current_medicine_info(self, medicine_name, total_pill):
        self.ui.txt_current_medicine.setText(medicine_name)
        self.ui.txt_pills_num_needed.setText(str(total_pill))

    
    @Slot(int)
    def update_dispense_progress_bar_value(self, value):
        """设置分药进度条的值"""
        self.ui.progressBar_current_medicine.setValue(value)
        self.ui.txt_current_medicine_percentage.setText(f"{value}%")

    @Slot()
    def go_to_finish_page(self):
        self.go_to_page("finish")
        


class TodayPatientDialog(QDialog):  # Now inherits from QDialog
    def __init__(self, manager):
        super().__init__()  # QDialog initialization
        self.ui = Ui_today_patient()
        self.ui.setupUi(self)
        self.manager = manager
        
        # Set dialog properties
        self.setModal(True)  # Make it modal
        self.setWindowTitle(" ")
        self.resize(600, 400)  # Set appropriate size
        
        # Create scroll area for patient buttons
        self.scroll_area = QScrollArea()
        self.scroll_widget = QWidget()
        self.scroll_layout = QVBoxLayout(self.scroll_widget)
        self.scroll_area.setWidget(self.scroll_widget)
        self.scroll_area.setWidgetResizable(True)
        
        # Add scroll area to main layout
        if hasattr(self.ui, 'patient_container'):
            container_layout = QVBoxLayout(self.ui.patient_container)
            container_layout.addWidget(self.scroll_area)
        
    
    def generate_today_patient_buttons(self, success, today_patients_data):
        """Generate buttons for today's patients who need medication"""
        # Clear existing buttons
        for i in reversed(range(self.scroll_layout.count())):
            child = self.scroll_layout.itemAt(i).widget()
            if child:
                child.setParent(None)
    
        # 获取真实的今日患者数据

    
        if not success or not today_patients_data:
            # Show "No patients today" message
            no_patient_btn = QPushButton("今天没有需要分药的患者")
            no_patient_btn.setEnabled(False)
            self.scroll_layout.addWidget(no_patient_btn)
            return
    
        # Create button for each patient
        for patient_data in today_patients_data:
            # 创建患者按钮显示文本
            patient_name = patient_data['patient_name']
            patient_id = patient_data['patient_id']
            medicines_count = len(patient_data['medicines_expiring'])
            
            # 构建显示文本
            button_text = f"{patient_name} (ID: {patient_id})\n{medicines_count}种药物需要分配"
            
            patient_btn = QPushButton(button_text)
            patient_btn.setMinimumHeight(80)  # 增加高度以显示更多信息
            patient_btn.setStyleSheet("""
                QPushButton {
                    text-align: left;
                    padding: 10px;
                    font-size: 14px;
                    border: 2px solid #3498db;
                    border-radius: 5px;
                    background-color: #ecf0f1;
                }
                QPushButton:hover {
                    background-color: #3498db;
                    color: white;
                }
                QPushButton:pressed {
                    background-color: #2980b9;
                }
            """)
            
            # Connect button to dispense medication for this patient
            patient_btn.clicked.connect(
                lambda checked, patient_id=patient_data['patient_id']: self.manager.check_plate(patient_id)
            )
            
            self.scroll_layout.addWidget(patient_btn)
    

class Manager(QObject):
    # 定义主控制信号
    init_dispenser_signal = Signal()
    initialize_database_signal = Signal()
    generate_pills_dispensing_list_signal = Signal(object)
    open_tray_signal = Signal()
    close_tray_signal = Signal()
    start_dispensing_signal = Signal()
    get_today_patients_signal = Signal(int)
    refresh_database_signal = Signal()
    #定义相机控制器信号
    init_camera_signal = Signal()
    start_camera_signal = Signal()
    stop_camera_signal = Signal()
    pause_camera_signal = Signal()
    set_mode_signal = Signal(object)  # 用于传递 CamMode

    qr_mismatch_signal = Signal()
    
    def __init__(self):
        super().__init__()

        # the variable to track selected patient
        self.selected_patient_id = None
        # the flag to prevent multiple message boxes
        self.is_showing_mismatch_message = False
        
        # 创建并显示启动界面
        self.startup_screen = StartupScreen()
        self.startup_screen.show()
        
        # 主窗口先不显示
        self.main_window = MainWindow(self)
        self.today_patient_dialog = None

        # 新建主控制器实例
        self.main_controller = MainController()
        self.main_controller_thread = QThread()
        self.main_controller.moveToThread(self.main_controller_thread)
        self.main_controller_thread.start()
        
        # 新建相机控制器实例
        self.cam_controller = CamController()
        self.cam_controller_thread = QThread()
        self.cam_controller.moveToThread(self.cam_controller_thread)
        self.cam_controller_thread.start()
        self.setup_camera_callbacks()
        
        self.connect_signals()

        # add timer for periodically update the camera feed display in the GUI.
        self.current_display_label = "img_cam_frame"
        self.display_timer = QTimer()
        self.display_timer.timeout.connect(self.display_current_cam_frame)

        # 初始化状态跟踪
        self.init_states = {
            'dispenser': False,
            'database': False, 
            'camera': False
        }
        # 开始初始化流程
        self.start_initialization()

    def connect_signals(self):
        self.qr_mismatch_signal.connect(self.show_qr_mismatch_message)
        # 连接信号到相机控制器的槽
        self.init_camera_signal.connect(self.cam_controller.initialize_camera)
        self.start_camera_signal.connect(self.cam_controller.start_camera)
        self.stop_camera_signal.connect(self.cam_controller.stop_camera)
        self.pause_camera_signal.connect(self.cam_controller.pause_camera)
        self.set_mode_signal.connect(self.cam_controller.set_mode)

        # 连接信号到主控制器的槽
        self.init_dispenser_signal.connect(self.main_controller.initialize_hardware)
        self.initialize_database_signal.connect(self.main_controller.initialize_database)
        self.generate_pills_dispensing_list_signal.connect(self.main_controller.generate_pills_dispensing_list)
        self.open_tray_signal.connect(self.main_controller.open_tray)
        self.close_tray_signal.connect(self.main_controller.close_tray)
        self.start_dispensing_signal.connect(self.main_controller.start_dispensing)
        self.get_today_patients_signal.connect(self.main_controller.get_today_patients)
        self.refresh_database_signal.connect(self.main_controller.initialize_database)

        # 连接信号到主页面的槽
        self.main_controller.prescription_loaded_signal.connect(self.main_window.update_prescription_info)
        self.main_controller.dispensing_progress_signal.connect(self.main_window.update_dispense_progress_bar_value)
        self.main_controller.current_medicine_info_signal.connect(self.main_window.update_current_medicine_info)
        self.main_controller.dispensing_completed_signal.connect(self.main_window.go_to_finish_page)
        self.main_controller.today_patients_ready_signal.connect(self.on_today_patients_ready)

        # 连接初始化完成信号（修正信号名称）
        self.cam_controller.camera_initialized_signal.connect(self.on_camera_initialized)
        self.main_controller.dispenser_initialized_signal.connect(self.on_dispenser_initialized) 
        self.main_controller.database_connected_signal.connect(self.on_database_connected)

##############
# initialize #
##############
    def start_initialization(self):
        """开始初始化流程"""
        self.startup_screen.update_status("正在初始化分药机...")
        self.init_dispenser_signal.emit()

    @Slot(bool)
    def on_dispenser_initialized(self, success):
        """分药机初始化完成回调"""
        self.init_states['dispenser'] = success
        
        if success:
            print("[Manager] 分药机初始化成功")
            # 分药机完成后，开始数据库初始化
            self.startup_screen.update_status("正在加载数据库...")
            self.initialize_database_signal.emit()
        else:
            print("[Manager] 分药机初始化失败")
            self.startup_screen.update_status("分药机初始化失败!")
            self.handle_initialization_failure()

    @Slot(bool)
    def on_database_connected(self, success):
        """数据库连接完成回调"""
        self.init_states['database'] = success
        
        if success:
            print("[Manager] 数据库连接成功")
            # 数据库完成后，开始相机初始化
            self.startup_screen.update_status("正在初始化摄像头...")
            self.init_camera_signal.emit()
        else:
            print("[Manager] 数据库连接失败")
            self.startup_screen.update_status("数据库连接失败!")
            self.handle_initialization_failure()

    @Slot(bool)
    def on_camera_initialized(self, success):
        """相机初始化完成回调"""
        self.init_states['camera'] = success
        
        if success:
            print("[Manager] 相机初始化成功")
            # 所有初始化完成
            self.on_all_initialization_complete()
        else:
            print("[Manager] 相机初始化失败")
            self.startup_screen.update_status("相机初始化失败!")
            self.handle_initialization_failure()

    def on_all_initialization_complete(self):
        """所有初始化完成"""
        self.startup_screen.update_status("初始化完成!")
        
        # 显示完成状态1秒后关闭启动界面
        QTimer.singleShot(1000, self.close_startup_and_show_main)

    def close_startup_and_show_main(self):
        """关闭启动界面并显示主界面"""
        self.startup_screen.accept()
        self.show_main()

    def handle_initialization_failure(self):
        """处理初始化失败"""
        # 显示错误状态3秒后关闭程序或重试
        QTimer.singleShot(3000, self.on_initialization_failed)

    def on_initialization_failed(self):
        """初始化失败处理"""
        # 可以选择重试或退出程序
        reply = QMessageBox.question(
            self.startup_screen, 
            "初始化失败", 
            "设备初始化失败，是否重试？",
            QMessageBox.Yes | QMessageBox.No,
            QMessageBox.Yes
        )
        
        if reply == QMessageBox.Yes:
            # 重置状态并重试
            self.init_states = {
                'dispenser': False,
                'database': False,
                'camera': False
            }
            self.start_initialization()
        else:
            # 退出程序
            QApplication.quit()

#############
# show page #
#############

    def show_main(self):
        self.main_window.show()
    
    def show_today_patients(self):
        """Show today's patient dialog"""
        self.today_patient_dialog = TodayPatientDialog(self)
        self.get_today_patients_signal.emit(2)
        self.today_patient_dialog.exec()  # Use exec() for modal dialog

####################
# Dispensing logic #
####################
    @Slot(bool, list)
    def on_today_patients_ready(self, success, patients_data):
        """处理今日患者数据返回"""
        if hasattr(self, 'today_patient_dialog'):
            self.today_patient_dialog.generate_today_patient_buttons(success, patients_data)

    @Slot()
    def check_plate(self, patient_id):
        # Store the selected patient ID for QR code validation
        self.selected_patient_id = patient_id
        print(f"Selected patient ID: {patient_id}")
        
        self.today_patient_dialog.accept()
        self.set_display_label("img_cam_frame")
        self.start_camera()
        self.set_qr_scan_mode()
        self.main_window.go_to_page("scan_qrcode")

    @Slot()
    def show_prescriptions(self, id):
        self.main_window.go_to_page("rx")
        # 请求生成分药矩阵
        self.generate_pills_dispensing_list_signal.emit(id)
        self.open_tray_signal.emit()

    @Slot()
    def start_dispensing(self):
        # 分药逻辑
        self.main_window.go_to_page("dispensing")
        print("开始分药")
        self.start_dispensing_signal.emit()

        #数药逻辑
        self.set_display_label("img_pillcount_frame") 
        self.start_camera()
        self.set_pills_count_mode()

    @Slot()
    def finish_dispensing(self):
        self.main_window.go_to_page("start")
        self.set_idle_mode()
        self.pause_camera()
        self.close_tray_signal.emit()

############
# database #
############
    @Slot()
    def refresh_database(self):
        self.refresh_database_signal.emit()
        
#######
# Cam #
#######
    def setup_camera_callbacks(self):
        """Set up callbacks for camera events"""
        self.cam_controller.set_qr_callback(self.qr_scan_done_callbcak)
        self.cam_controller.set_pills_count_callback(self.pills_count_done_callback)

    def start_camera(self):
        """Start the camera"""
        self.start_camera_signal.emit()
        # Start the display timer for ~30 FPS
        self.display_timer.start(33)
        print("Camera start requested")

    def stop_camera(self):
        """Stop the camera and clear display"""
        self.display_timer.stop()
        self.stop_camera_signal.emit()
        
        # Clear the camera frame display
        if self.main_window and hasattr(self.main_window.ui, 'img_cam_frame'):
            self.main_window.ui.img_cam_frame.clear()
            self.main_window.ui.img_cam_frame.setText("Camera Stopped")

    def pause_camera(self):
        """Pause the camera and clear display"""
        self.display_timer.stop()
        self.pause_camera_signal.emit()
        
        # Clear the camera frame display
        if self.main_window and hasattr(self.main_window.ui, 'img_cam_frame'):
            self.main_window.ui.img_cam_frame.clear()
            self.main_window.ui.img_cam_frame.setText("Camera Paused")
    
    def set_qr_scan_mode(self):
        """Set camera to QR scan mode"""
        self.set_mode_signal.emit(CamMode.QR_SCAN)
        print("QR Scan mode activated")
    
    def set_pills_count_mode(self):
        """Set camera to pills counting mode"""
        self.set_mode_signal.emit(CamMode.PILLS_COUNT)
        print("Pills count mode activated")

    def set_idle_mode(self):
        """Set camera to idle mode"""
        self.set_mode_signal.emit(CamMode.IDLE)
        print("Camera idle mode activated")

    def qr_scan_done_callbcak(self, qr_results):
        """Handle QR scan completion"""
        print("QR scan completed")
        if qr_results and self.main_window:
            # Extract QR data from results
            qr_data_list = []
            for qr in qr_results:
                qr_data_list.append(qr['data'])
            
            # Check if all QR codes are the same
            if len(set(qr_data_list)) == 1:
                # All QR codes are identical
                qr_id = qr_data_list[0]  
                print(f"All QR codes are identical: {qr_id}")
                
                # Check if QR code matches selected patient ID
                if self.selected_patient_id is not None:
                    # Convert both to string for comparison to handle different data types
                    if str(qr_id) == str(self.selected_patient_id):
                        # QR code matches patient ID - proceed with dispensing
                        print(f"QR code matches patient ID: {self.selected_patient_id}")
                        self.set_idle_mode()
                        self.pause_camera()
                        self.show_prescriptions(qr_id)
                    else:
                        # QR code doesn't match patient ID - show error and continue scanning
                        print(f"QR code mismatch: scanned {qr_id}, expected {self.selected_patient_id}")
                        self.qr_mismatch_signal.emit()
                        # Keep camera in QR scan mode and continue scanning
                        # Don't pause camera, just continue scanning
                else:
                    # No patient selected (shouldn't happen in normal flow)
                    print("No patient ID selected")
                    self.set_idle_mode()
                    self.pause_camera()
                    self.show_prescriptions(qr_id)
            else:
                # QR codes are different
                qr_display_list = []
                for qr in qr_results:
                    qr_display_list.append(f"Type: {qr['type']}, Data: {qr['data']}")
                
                qr_data_str = "\n".join(qr_display_list)
                print(f"Multiple different QR codes detected: {qr_data_str}")
                # Keep scanning for consistent QR codes

    def show_qr_mismatch_message(self):
        """Show message when QR code doesn't match patient ID"""
        # Prevent multiple message boxes
        if self.is_showing_mismatch_message:
            return
            
        if self.main_window:
            self.is_showing_mismatch_message = True
            
            # Temporarily pause QR scanning to prevent multiple detections
            self.set_idle_mode()
            
            # Show message box
            msg_box = QMessageBox(self.main_window)
            msg_box.setWindowTitle("药盒验证失败")
            msg_box.setText("当前药盒并不属于该患者，请检查后重新扫描")
            msg_box.setIcon(QMessageBox.Warning)
            msg_box.setStandardButtons(QMessageBox.Ok)
            msg_box.exec()
            
            # Reset flag and resume QR scanning after user closes the message
            self.is_showing_mismatch_message = False
            self.set_qr_scan_mode()  # Resume QR scanning
            
            print("QR code mismatch message displayed")

    def pills_count_done_callback(self, pills_count_result):
        """Handle pills count results"""
        if self.main_window and hasattr(self.main_window.ui, 'txt_pillcount_num'):
            total_count = pills_count_result.get("total_count", 0)
            self.main_window.ui.txt_pillcount_num.setText(str(total_count))
            print(f"Pills count updated: {total_count}")

    def save_medicine_img(self):
        """Save current camera frame as medicine image"""
        if not self.main_window or not hasattr(self.main_window.ui, 'edit_medicine_name'):
            print("UI not available")
            return
            
        medicine_name = self.main_window.ui.edit_medicine_name.text().strip()
        
        if not medicine_name:
            print("Please enter a medicine name")
            return
        
        # Get current frame from camera
        frame = self.cam_controller.get_current_frame()
        if frame is None:
            print("No frame available to save")
            return
        
        # Create filename with medicine name (support Chinese characters)
        filename = f"{medicine_name}.jpg"
        
        # Save the image using imencode to support Chinese filenames
        try:
            # Encode image to memory buffer
            success, buffer = cv2.imencode('.jpg', frame)
            
            if success:
                # Write buffer to file with proper encoding
                with open(filename, 'wb') as f:
                    f.write(buffer)
                print(f"Medicine image saved as: {filename}")
                # Clear the line edit after successful save
                self.main_window.ui.edit_medicine_name.clear()
            else:
                print("Failed to encode image")
        except Exception as e:
            print(f"Failed to save image: {e}")

    # 选择label显示摄像头画面
    def _display_cam_frame(self, label_name="img_cam_frame"):
        """Get frame from camera and display it in specified Qt label
        
        Args:
            label_name (str): Name of the label widget to display the frame in
        """    
        frame = self.cam_controller.get_current_frame()
        if frame is not None:
            # Convert BGR to RGB (OpenCV uses BGR, Qt uses RGB)
            rgb_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            
            # Get frame dimensions
            h, w, ch = rgb_frame.shape
            bytes_per_line = ch * w
            
            # Create QImage from frame data
            qt_image = QImage(rgb_frame.data, w, h, bytes_per_line, QImage.Format_RGB888)
            
            # Convert QImage to QPixmap
            scaled_pixmap = QPixmap.fromImage(qt_image)
            
            # Check if main window and specified label exist
            if self.main_window and hasattr(self.main_window.ui, label_name):
                target_label = getattr(self.main_window.ui, label_name)
                
                # Scale pixmap to fit the label while maintaining aspect ratio
                label_size = target_label.size()
                scaled_pixmap = scaled_pixmap.scaled(
                    label_size, 
                    Qt.KeepAspectRatio, 
                    Qt.SmoothTransformation
                )
                
                # Display the frame in the specified label
                target_label.setPixmap(scaled_pixmap)
            else:
                print(f"Warning: Label '{label_name}' not found in UI")

    def display_current_cam_frame(self):
        """Display camera frame to current active label"""
        self._display_cam_frame(self.current_display_label)

    def set_display_label(self, label_name):
        """Set which label should display the camera feed"""
        self.current_display_label = label_name
        print(f"Camera display switched to: {label_name}")





if __name__ == "__main__":
    app = QApplication(sys.argv)
    manager = Manager()
    sys.exit(app.exec())