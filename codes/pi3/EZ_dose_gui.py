import sys
import cv2
from PySide6.QtWidgets import QApplication, QMainWindow, QWidget, QPushButton, QVBoxLayout, QScrollArea, QDialog
from PySide6.QtCore import QObject, Signal, Slot, QTimer, QThread, Qt
from PySide6.QtGui import QImage, QPixmap

from main_window_ui import Ui_MainWindow
from today_patient_ui import Ui_today_patient
from main_controller import MainController
from cam_controller import CamController, CamMode


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
        
        self.generate_today_patient_buttons()
    
    def generate_today_patient_buttons(self):
        """Generate buttons for today's patients who need medication"""
        # Clear existing buttons
        for i in reversed(range(self.scroll_layout.count())):
            child = self.scroll_layout.itemAt(i).widget()
            if child:
                child.setParent(None)
        
        # FAKE DEMO DATA - Replace with real data later
        today_patients = [
            {
                'id': 1,
                'name': '张三',
                'medication': '阿司匹林',
                'dosage': '100mg',
                'time': '08:00',
                'status': 'pending'
            },
            {
                'id': 2,
                'name': '李四',
                'medication': '维生素D',
                'dosage': '400IU',
                'time': '12:00',
                'status': 'pending'
            },
            {
                'id': 3,
                'name': '王五',
                'medication': '降压药',
                'dosage': '5mg',
                'time': '09:30',
                'status': 'pending'
            },
            {
                'id': 4,
                'name': '赵六',
                'medication': '胰岛素',
                'dosage': '10单位',
                'time': '07:00',
                'status': 'pending'
            },
            {
                'id': 5,
                'name': '钱七',
                'medication': '钙片',
                'dosage': '500mg',
                'time': '20:00',
                'status': 'pending'
            }
        ]
        
        # Uncomment this when you have real data:
        # today_patients = self.manager.controller.rx_manager.get_patients_for_date(today)
        
        if not today_patients:
            # Show "No patients today" message
            no_patient_btn = QPushButton("今天没有需要分药的患者")
            no_patient_btn.setEnabled(False)
            self.scroll_layout.addWidget(no_patient_btn)
            return
        
        # Create button for each patient
        for patient in today_patients:
            patient_btn = QPushButton(f"{patient['name']}")
            patient_btn.setMinimumHeight(60)
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
                lambda checked, p=patient: self.manager.check_plate(p)
            )
            
            self.scroll_layout.addWidget(patient_btn)
    
    def refresh_patient_list(self):
        """Refresh the patient button list"""
        self.generate_today_patient_buttons()

class Manager(QObject):
    # 定义主控制信号
    init_dispenser_signal = Signal()
    connect_database_signal = Signal()
    generate_pills_dispensing_list_signal = Signal(object)
    open_tray_signal = Signal()
    close_tray_signal = Signal()
    start_dispensing_signal = Signal()
    #定义相机控制器信号
    init_camera_signal = Signal()
    start_camera_signal = Signal()
    stop_camera_signal = Signal()
    pause_camera_signal = Signal()
    set_mode_signal = Signal(object)  # 用于传递 CamMode
    
    def __init__(self):
        super().__init__() 
        self.main_window = None
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

        # 显示定时器
        # 添加当前显示label的变量
        self.current_display_label = "img_cam_frame"
        self.display_timer = QTimer()
        self.display_timer.timeout.connect(self.display_current_cam_frame)


        # 初始化相机
        self.init_camera_signal.emit()
        # 初始化分药机
        self.init_dispenser_signal.emit()
        # 加载数据库
        self.connect_database_signal.emit()
        

    def connect_signals(self):
        # 连接信号到相机控制器的槽
        self.init_camera_signal.connect(self.cam_controller.initialize_camera)
        self.start_camera_signal.connect(self.cam_controller.start_camera)
        self.stop_camera_signal.connect(self.cam_controller.stop_camera)
        self.pause_camera_signal.connect(self.cam_controller.pause_camera)
        self.set_mode_signal.connect(self.cam_controller.set_mode)

        # 连接信号到主控制器的槽
        self.init_dispenser_signal.connect(self.main_controller.initialize_hardware)
        self.connect_database_signal.connect(self.main_controller.connect_database)
        self.generate_pills_dispensing_list_signal.connect(self.main_controller.generate_pills_dispensing_list)
        self.open_tray_signal.connect(self.main_controller.open_tray)
        self.close_tray_signal.connect(self.main_controller.close_tray)
        self.start_dispensing_signal.connect(self.main_controller.start_dispensing)


    def show_main(self):
        if self.main_window is None:
            self.main_window = MainWindow(self)
            self.main_window.connect_signals()
        self.main_window.show()
    
    def show_today_patients(self):
        """Show today's patient dialog"""
        self.today_patient_dialog = TodayPatientDialog(self)
        self.today_patient_dialog.exec()  # Use exec() for modal dialog


####################
# Dispensing logic #
####################

    @Slot()
    def check_plate(self, patient):
        self.today_patient_dialog.accept()
        self.set_display_label("img_cam_frame")
        self.start_camera()
        self.set_qr_scan_mode()
        self.main_window.go_to_page("scan_qrcode")

    @Slot()
    def show_prescriptions(self, id):
        self.main_window.go_to_page("rx")
        # 请求生成配药列表
        self.generate_pills_dispensing_list_signal.emit(id)
        self.open_tray_signal.emit()

    @Slot()
    def start_dispensing(self):
        # 分药逻辑
        self.main_window.go_to_page("dispensing")
        print("开始分药")
        self.close_tray_signal.emit()
        self.start_dispensing_signal.emit()


        #数药逻辑
        self.set_display_label("img_pillcount_frame") 
        self.start_camera()
        self.set_pills_count_mode()
        
        





#######
# Cam #
#######
    def setup_camera_callbacks(self):
        """Set up callbacks for camera events"""
        self.cam_controller.set_qr_callback(self.qc_scan_done_callbcak)
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

    def qc_scan_done_callbcak(self, qr_results):
        """Handle QR scan completion"""
        print("QR scan completed")
        if qr_results and self.main_window:
            # Set camera to idle mode
            self.set_idle_mode()
            self.pause_camera()
            
            # Extract QR data from results
            qr_data_list = []
            for qr in qr_results:
                qr_data_list.append(qr['data'])
            
            # Check if all QR codes are the same
            if len(set(qr_data_list)) == 1:
                # All QR codes are identical
                qr_id = qr_data_list[0]  
                print(f"All QR codes are identical: {qr_id}")
                
                # Call show_prescriptions with the QR code as ID
                self.show_prescriptions(qr_id)
            else:
                # QR codes are different
                qr_display_list = []
                for qr in qr_results:
                    qr_display_list.append(f"Type: {qr['type']}, Data: {qr['data']}")
                
                qr_data_str = "\n".join(qr_display_list)
                
                # Display error message in UI
                if hasattr(self.main_window.ui, 'qrdata'):
                    self.main_window.ui.qrdata.setText(f"Multiple different QR codes detected:\n{qr_data_str}")
                
                print(f"Multiple different QR codes detected: {qr_data_str}")

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
    manager.show_main()
    sys.exit(app.exec())