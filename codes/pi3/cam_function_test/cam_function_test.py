import sys
import cv2
import os
from PySide6.QtWidgets import QApplication, QWidget
from PySide6.QtCore import QTimer, Qt
from PySide6.QtGui import QImage, QPixmap
from test_window_ui import Ui_Form

# 获取当前文件的绝对路径
current_dir = os.path.dirname(os.path.abspath(__file__))
# 获取上一级目录
parent_dir = os.path.dirname(current_dir)
# 添加到 sys.path
sys.path.append(parent_dir)
from cam_controller import CamController, CamMode

class CameraTestWindow(QWidget):
    def __init__(self):
        super().__init__()
        self.ui = Ui_Form()
        self.ui.setupUi(self)

        self.cam = CamController()
        self.cam.initialize_camera()
        self.cam.set_qr_callback(self.display_qc_scan_done)
        self.cam.set_pills_count_callback(self.display_pills_count) 

        # Timer for updating frame display
        self.display_timer = QTimer()
        self.display_timer.timeout.connect(self.display_cam_frame)
        
        # Set window title
        self.setWindowTitle("Camera Test Window")
        
        self.connect_signals()

    def connect_signals(self):
        self.ui.start_camera.clicked.connect(self.start_camera)
        self.ui.stop_camera.clicked.connect(self.stop_camera)
        self.ui.pause_camera.clicked.connect(self.pause_camera)
        self.ui.scan_qrcode_mode.clicked.connect(self.set_qr_scan_mode)
        self.ui.count_pill_mode.clicked.connect(self.set_pills_count_mode)
        self.ui.save_medicine_img.clicked.connect(self.save_medicine_img)

    def start_camera(self):
        if self.cam.start_camera():
            self.display_timer.start(33)  # ~30 FPS
            print("Camera started successfully")
        else:
            print("Failed to start camera")

    def stop_camera(self):
        self.display_timer.stop()
        self.cam.stop_camera()
        self.ui.cam_frame.clear()
        self.ui.cam_frame.setText("Camera Stopped")

    def pause_camera(self):
        self.display_timer.stop()
        self.cam.pause_camera()
        self.ui.cam_frame.clear()
        self.ui.cam_frame.setText("Camera Paused")

    def set_qr_scan_mode(self):
        self.cam.set_mode(CamMode.QR_SCAN)
        print("QR Scan mode activated")
    
    def display_qc_scan_done(self, qr_results):
        print("QR scan completed")
        if qr_results:
            # Display the QR code data
            self.cam.set_mode(CamMode.IDLE)
            qr_data = str(qr_results)  # Convert to string if needed
            self.ui.qrdata.setText(qr_data)
            print(f"QR Code detected: {qr_data}")
        else:
            self.ui.qrdata.setText("No QR code detected")

    def set_pills_count_mode(self):
        self.cam.set_mode(CamMode.PILLS_COUNT)

    def display_pills_count(self, pills_count_result):
        self.ui.pills_number.setText(str(pills_count_result["total_count"]))

    def save_medicine_img(self):
        """according to lineEdit input to set img's name and save it"""
        medicine_name = self.ui.edit_medicine_name.text().strip()
        
        if not medicine_name:
            print("Please enter a medicine name")
            return
        
        # Get current frame from camera
        frame = self.cam.get_current_frame()
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
                self.ui.edit_medicine_name.clear()
            else:
                print("Failed to encode image")
        except Exception as e:
            print(f"Failed to save image: {e}")

    def display_cam_frame(self):
        """Get frame from camera and display it in Qt label"""
        frame = self.cam.get_current_frame()
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
            
            # Display the frame in the label
            self.ui.cam_frame.setPixmap(scaled_pixmap)

def main():
    app = QApplication(sys.argv)
    
    # Create and show the window
    window = CameraTestWindow()
    window.show()
    
    # Start the application
    sys.exit(app.exec())

if __name__ == "__main__":
    main()