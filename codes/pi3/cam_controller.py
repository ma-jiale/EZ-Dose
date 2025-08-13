import cv2
import numpy as np
from pyzbar import pyzbar
from enum import Enum
import threading
import time
from PySide6.QtCore import QObject, Signal, Slot
from pill_counter import PillCounter

class CamMode(Enum):
    IDLE = "idle"
    QR_SCAN = "qr_scan"
    PILLS_COUNT = "pills_count"
    # Add more modes as needed

class CamController(QObject):
    camera_initialized_signal = Signal(bool)  # 相机初始化完成信号
    # 添加信号用于状态通知
    camera_initialized = Signal(bool)
    camera_started = Signal(bool)
    camera_stopped = Signal()
    mode_changed = Signal(object)
    
    def __init__(self, camera_index=1, target_width=1280, target_height=960, target_fps=30):
        """Initialize camera controller with parameters"""
        super().__init__()
        self.camera_index = camera_index
        self.target_width = target_width
        self.target_height = target_height
        self.target_fps = target_fps
        
        self.cap = None
        self.is_running = False
        self.is_cam_initialized =False
        self.correcting_img_distortion = True
        self.current_mode = CamMode.IDLE
        
        # Threading
        self.capture_thread = None
        self.thread_lock = threading.Lock()
        
        # Camera calibration parameters
        self.camera_matrix = None
        self.dist_coeffs = None
        self.map1 = None
        self.map2 = None
        self.roi = None
        
        # Frame storage
        self.current_frame = None
        self.frame_ready = False
        
        # QR Code detection results
        self.qr_codes_detected = []
        self.qr_callback = None

        # Pills Count results
        self.pills_count_detected = 0
        self.pills_count_callback = None
        
        # 初始化 PillCounter 实例
        self.pill_counter = None

###########
# Setting #
###########
    @Slot()
    def initialize_camera(self):
        """初始化相机"""
        try:
            # Open camera
            self.cap = cv2.VideoCapture(self.camera_index)
            
            if not self.cap.isOpened():
                print(f"Error: Cannot open camera at index {self.camera_index}")
                return False
            
            # Set camera properties
            self.cap.set(cv2.CAP_PROP_FRAME_WIDTH, self.target_width)
            self.cap.set(cv2.CAP_PROP_FRAME_HEIGHT, self.target_height)
            self.cap.set(cv2.CAP_PROP_FPS, self.target_fps)
            
            self.is_cam_initialized = True
            print("Camera is initialized")
            
            # 初始化成功后发射信号
            self.camera_initialized_signal.emit(True)
            print("[Init] 相机初始化完成")
        except Exception as e:
            print(f"[Error] 相机初始化失败: {e}")
            self.camera_initialized_signal.emit(False)

    def setup_distortion_correction(self):
        """Setup distortion correction parameters for wide-angle lens"""
        # Get actual resolution
        width = int(self.cap.get(cv2.CAP_PROP_FRAME_WIDTH))
        height = int(self.cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
        
        # Camera matrix estimation for wide-angle lens
        self.camera_matrix = np.array([[width * 0.8, 0, width / 2],
                                      [0, width * 0.8, height / 2],
                                      [0, 0, 1]], dtype=np.float32)
        
        # Distortion coefficients [k1, k2, p1, p2, k3] for barrel distortion
        self.dist_coeffs = np.array([-0.25, 0.1, 0, 0, 0], dtype=np.float32)
        
        # Create optimal new camera matrix
        new_camera_matrix, self.roi = cv2.getOptimalNewCameraMatrix(
            self.camera_matrix, self.dist_coeffs, (width, height), 1, (width, height))
        
        # Create undistortion maps for real-time correction
        self.map1, self.map2 = cv2.initUndistortRectifyMap(
            self.camera_matrix, self.dist_coeffs, None, new_camera_matrix, 
            (width, height), cv2.CV_16SC2)
        
        print("Distortion correction maps created")

    @Slot(object)
    def set_mode(self, mode):
        """Set camera operation mode"""
        if isinstance(mode, CamMode):
            with self.thread_lock:
                self.current_mode = mode
                print(f"Camera mode set to: {mode.value}")
                
                # Clear previous results when switching modes
                if mode == CamMode.QR_SCAN:
                    self.qr_codes_detected = []
                elif mode == CamMode.PILLS_COUNT:
                    # 初始化 pill counter（不使用摄像头，我们会传入帧）
                    if self.pill_counter is None:
                        self.pill_counter = PillCounter(camera_id=None)  # 不初始化摄像头
                    # 重置 pill counter 状态
                    self.pill_counter.background_captured = False
                    self.pill_counter.stable_count = 0
                    self.pill_counter.recent_edge_counts.clear()
                    self.pills_count_detected = 0
                
                # 发出模式改变信号
                self.mode_changed.emit(mode)
        else:
            print(f"Invalid mode type: {type(mode)}")

########################
# Capture camera frame #
########################

    def _capture_frame(self):
        """Capture a single frame from the camera"""
        if not self.cap:
            return None, False
        
        ret, frame = self.cap.read()
        if not ret:
            return None, False
        
        return frame, True
    
    def _capture_and_correct_frame(self):
        """Capture frame and apply distortion correction"""
        frame, ret = self._capture_frame()
        if not ret:
            return None, False
        
        # Apply distortion correction
        corrected_frame = cv2.remap(frame, self.map1, self.map2, cv2.INTER_LINEAR)
        
        # Crop to remove black borders
        x, y, w, h = self.roi
        if w > 0 and h > 0:
            corrected_frame = corrected_frame[y:y+h, x:x+w]
        
        return corrected_frame, True
    
    @Slot()
    def start_camera(self):
        """Start the camera in background thread"""
        if self.is_running:
            print("Camera is already running")
            return True
            
        try:
            # Initialize camera
            if not self.is_cam_initialized:
                if not self.initialize_camera():
                    return False
            
            # Setup distortion correction
            self.setup_distortion_correction()
            
            print("Starting camera...")
            self.is_running = True
            
            # Start capture thread
            self.capture_thread = threading.Thread(target=self._capture_loop)
            self.capture_thread.daemon = True
            self.capture_thread.start()
            
            return True
            
        except Exception as e:
            print(f"Error starting camera: {e}")
            return False
        
    @Slot()     
    def pause_camera(self):
        """Pause the camera without releasing the camera resource"""
        print("Pausing camera...")
        self.is_running = False
        self.current_mode = CamMode.IDLE
        
        if self.capture_thread:
            self.capture_thread.join(timeout=2.0)
        
        with self.thread_lock:
            self.current_frame = None
            self.frame_ready = False
            self.qr_codes_detected = []
        
        print("Camera paused (resource still held)")

    @Slot()
    def stop_camera(self):
        """Stop the camera and release all resources"""
        print("Stopping camera...")
        self.is_running = False
        self.current_mode = CamMode.IDLE
        
        if self.capture_thread:
            self.capture_thread.join(timeout=2.0)
        
        with self.thread_lock:
            self.current_frame = None
            self.frame_ready = False
            self.qr_codes_detected = []
        
        # Release camera resource
        self._cleanup()
        self.is_cam_initialized = False
        print("Camera stopped and resources released")
    
    @Slot()
    def get_current_frame(self):
        """Get the current frame for display in Qt app"""
        with self.thread_lock:
            if self.frame_ready and self.current_frame is not None:
                return self.current_frame.copy()
        return None
    
    def _capture_loop(self):
        """Main capture loop running in background thread"""
        while self.is_running:
            try:
                # Capture frame
                if self.correcting_img_distortion:
                    frame, ret = self._capture_and_correct_frame()
                else:
                    frame, ret = self._capture_frame()

                if not ret:
                    print("Error: Cannot receive frame from camera")
                    break
                
                # Process frame based on current mode
                processed_frame = self._process_frame(frame)
                
                # Store current frame
                with self.thread_lock:
                    self.current_frame = processed_frame
                    self.frame_ready = True
                
                # Small delay to prevent excessive CPU usage
                time.sleep(0.1)  # ~10 FPS
                
            except Exception as e:
                print(f"Error in capture loop: {e}")
                break
    
    def _process_frame(self, frame):
        """Process frame based on current mode"""
        with self.thread_lock:
            mode = self.current_mode
        
        if mode == CamMode.QR_SCAN:
            return self._process_qr_scan(frame)
        elif mode == CamMode.PILLS_COUNT:
            return self._process_pills_count(frame)
        else:
            # IDLE mode - just return the frame
            return frame

###########
# QR_scan #
###########

    def set_qr_callback(self, callback):
        """Set callback function for QR code detection results"""
        self.qr_callback = callback

    def _process_qr_scan(self, frame):
        """Process frame for QR code detection"""
        # Convert to grayscale for better QR detection
        gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        
        # Detect QR codes
        qr_codes = pyzbar.decode(gray)
        
        # Process detected QR codes
        qr_results = []
        
        for qr_code in qr_codes:
            # Get QR code data
            qr_data = qr_code.data.decode('utf-8')
            qr_type = qr_code.type
            
            # Get bounding box coordinates
            points = qr_code.polygon
            if len(points) == 4:
                # Draw green bounding box around QR code
                pts = np.array([[point.x, point.y] for point in points], dtype=np.int32)
                cv2.polylines(frame, [pts], True, (0, 255, 0), 3)
                
                # Display QR code information on frame
                x, y = points[0].x, points[0].y
                cv2.putText(frame, f"Type: {qr_type}", (x, y - 40), 
                           cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 0), 2)
                cv2.putText(frame, f"Data: {qr_data}", (x, y - 10), 
                           cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 0), 2)
                
                # Store QR code result
                qr_result = {
                    'type': qr_type,
                    'data': qr_data,
                    'points': [(point.x, point.y) for point in points],
                    'timestamp': time.time()
                }
                qr_results.append(qr_result)
                
                print(f"QR Code detected - Type: {qr_type}, Data: {qr_data}")
        
        # Update QR codes detected
        with self.thread_lock:
            self.qr_codes_detected = qr_results
        
        # Call callback if set
        if qr_results and self.qr_callback:
            self.qr_callback(qr_results)
        
        return frame
    
    def get_qr_results(self):
        """Get latest QR code detection results"""
        with self.thread_lock:
            return self.qr_codes_detected.copy()
        
###############
# pills count #
###############

    def set_pills_count_callback(self, callback):
        """Set callback function for QR code detection results"""
        self.pills_count_callback = callback

    def get_pills_count_results(self):
        """Get latest pills count detection results"""
        with self.thread_lock:
            return self.pills_count_detected

    def _process_pills_count(self, frame):
        """
        Count pills using PillCounter class methods
        Args:
            frame: Captured frame by Cam
        Returns:
            processed_frame: Frame with pill detection results
        """
        if self.pill_counter is None:
            # 如果 pill_counter 未初始化，创建一个（不使用摄像头）
            self.pill_counter = PillCounter(camera_id=None)
        
        # 检测边缘以确定场景稳定性
        edge_count, edges = self.pill_counter.detect_edges(frame)
        
        # 如果还没有背景，尝试捕捉
        if not self.pill_counter.background_captured:
            if self.pill_counter.is_scene_stable(edge_count):
                self.pill_counter.capture_background(frame)
                cv2.putText(frame, 'Background captured!', (10, 30), 
                           cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
            else:
                # 显示等待背景的状态
                h, w = frame.shape[:2]
                cv2.rectangle(frame, 
                             (self.pill_counter.crop_margin, self.pill_counter.crop_margin), 
                             (w-self.pill_counter.crop_margin, h-self.pill_counter.crop_margin), 
                             (0, 255, 255), 2)
                cv2.putText(frame, 'Waiting for stable background...', (10, 30), 
                           cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 255), 2)
                cv2.putText(frame, f'Edge count: {edge_count}', (10, 70), 
                           cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 0, 255), 2)
                cv2.putText(frame, f'Stable: {self.pill_counter.stable_count}/{self.pill_counter.stable_frames_needed}', 
                           (10, 110), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255, 165, 0), 2)
        else:
            # 进行药片计数
            pill_count, result_frame = self.pill_counter.count_pills(frame)
            
            # 更新检测到的药片数量
            with self.thread_lock:
                self.pills_count_detected = pill_count
            
            # 调用回调函数（如果设置了）
            if self.pills_count_callback:
                self.pills_count_callback({
                    'total_count': pill_count,
                    'timestamp': time.time(),
                    'method': 'pill_counter'
                })
            
            return result_frame
        
        return frame

    def _cleanup(self):
        """Clean up resources"""
        print("Cleaning up camera resources...")
        
        if self.cap:
            self.cap.release()
            self.cap = None
        
        print("Camera cleanup completed")
