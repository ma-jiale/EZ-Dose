import cv2
import numpy as np
from pyzbar import pyzbar
from enum import Enum
import threading
import time
from PySide6.QtCore import QObject, Signal, Slot

class CamMode(Enum):
    IDLE = "idle"
    QR_SCAN = "qr_scan"
    PILLS_COUNT = "pills_count"
    # Add more modes as needed

class CamController(QObject):
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
        self.background = None
        self.prev_frame = None
        self.threshold_stable = 3
        self.last_stable_time = 0

###########
# Setting #
###########

    @Slot()
    def initialize_camera(self):
        """Initialize camera and set properties"""
        print("Initializing camera...")
        
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
        return True
    
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
    
    def _mse(self, imageA, imageB):
        """Calculate Mean Squared Error (MSE) between two images"""
        err = np.sum((imageA.astype("float") - imageB.astype("float")) ** 2)
        err /= float(imageA.shape[0] * imageA.shape[1])
        return err

    def _process_pills_count(self, frame):
        """
        Count pills using background subtraction and stability detection
        Based on the provided algorithm with MSE stability checking
        Arg: Captured frame by Cam
        Return: processed_frame
        """
        current_time = time.time()
        
        # Set ROI region (adjust coordinates based on your camera setup)
        roi_top_left = (20, 20)
        roi_bottom_right = (min(1260, frame.shape[1]), min(700, frame.shape[0]))
        
        # Extract ROI region
        roi = frame[roi_top_left[1]:roi_bottom_right[1], roi_top_left[0]:roi_bottom_right[0]]
        
        # Convert to grayscale
        gray = cv2.cvtColor(roi, cv2.COLOR_BGR2GRAY)
        
        # 1. Image stability check
        if self.prev_frame is not None:
            # Calculate MSE
            mse_score = self._mse(gray, self.prev_frame)
            print(f"MSE: {mse_score}")
            
            # If difference is small, consider image stable
            if mse_score < 10:  # Threshold can be adjusted
                self.pills_count_detected += 1
            else:
                self.pills_count_detected = 0
        
        # 2. Check if image is stable
        if self.pills_count_detected >= self.threshold_stable:
            # Apply Canny edge detection
            edges = cv2.Canny(gray, 50, 150)
            
            # 3. If no edges in ROI, update background
            if np.count_nonzero(edges) == 0:
                self.background = gray.copy()
                print("No edges detected, updating background...")
                self.last_stable_time = current_time
                
                # Display status on frame
                cv2.putText(frame, 'Background Updated', (10, 120), 
                           cv2.FONT_HERSHEY_SIMPLEX, 1, (255, 255, 0), 2)
            
            elif self.background is not None:
                # 4. If edges detected, perform background subtraction
                subtracted_img = cv2.subtract(self.background, gray)
                
                # 5. Noise reduction and thresholding
                blurred = cv2.GaussianBlur(subtracted_img, (5, 5), 0)
                
                # 6. Convert to binary image
                _, thresh = cv2.threshold(blurred, 20, 255, cv2.THRESH_BINARY)
                
                # 7. Erosion and dilation (noise removal)
                kernel = np.ones((5, 5), np.uint8)
                erosion = cv2.erode(thresh, kernel, iterations=2)
                dilation = cv2.dilate(erosion, kernel, iterations=2)
                
                # 8. Find contours
                contours, _ = cv2.findContours(dilation, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
                
                min_area = 100  # Minimum area threshold to filter small noise
                
                # 9. Contour feature extraction and counting
                areas = []
                normal_contours = 0
                abnormal_contours = 0
                abnormal_area_threshold = 1.5
                area_mode = 0
                sum_area = 0
                
                # Filter contours without convexity defects
                for cnt in contours:
                    area = cv2.contourArea(cnt)
                    perimeter = cv2.arcLength(cnt, True)
                    
                    # Skip small contours
                    if area < min_area:
                        continue
                    
                    # Polygon approximation
                    epsilon = 0.02 * perimeter
                    approx = cv2.approxPolyDP(cnt, epsilon, True)
                    
                    convex_hull = cv2.isContourConvex(approx)
                    areas.append(area)
                    
                    # Adjust contour coordinates to original frame
                    x, y, w, h = cv2.boundingRect(cnt)
                    adjusted_x = x + roi_top_left[0]
                    adjusted_y = y + roi_top_left[1]
                    adjusted_contour = cnt + np.array([roi_top_left[0], roi_top_left[1]])
                    
                    if convex_hull:  # No convexity defects
                        sum_area += area
                        normal_contours += 1
                        cv2.drawContours(frame, [adjusted_contour], -1, (0, 255, 0), 2)  # Green
                        if normal_contours > 0:
                            area_mode = sum_area / normal_contours
                    else:  # Has convexity defects
                        if area_mode > 0 and area < area_mode * abnormal_area_threshold:
                            abnormal_contours += 1
                            cv2.drawContours(frame, [adjusted_contour], -1, (0, 0, 255), 2)  # Red
                        else:
                            # Large area handling
                            divisor = 2
                            max_divisor = 10
                            while (area_mode > 0 and 
                                   abs((area / divisor) - area_mode) > area_mode * 0.25 and 
                                   divisor < max_divisor):
                                divisor += 1
                            normal_contours += divisor
                            cv2.drawContours(frame, [adjusted_contour], -1, (0, 255, 255), 2)  # Yellow
                
                total_count = normal_contours + abnormal_contours
                
                # Update pills count
                with self.thread_lock:
                    self.pills_count_detected = total_count
                
                # Display counts on frame
                cv2.putText(frame, f'Normal Count: {normal_contours}', (10, 60), 
                           cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)
                cv2.putText(frame, f'Abnormal Count: {abnormal_contours}', (10, 90), 
                           cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 255), 2)
                cv2.putText(frame, f'Total Pills: {total_count}', (10, 30), 
                           cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 255), 2)
                
                # Call callback if set
                if self.pills_count_callback:
                    self.pills_count_callback({
                        'total_count': total_count,
                        'normal_count': normal_contours,
                        'abnormal_count': abnormal_contours,
                        'areas': areas,
                        'area_mode': area_mode,
                        'timestamp': time.time(),
                        'method': 'background_subtraction'
                    })
                
                print(f"Pills detected (Method 2) - Normal: {normal_contours}, Abnormal: {abnormal_contours}, Total: {total_count}")
            
            else:
                # No background yet
                cv2.putText(frame, 'Waiting for stable background...', (10, 120), 
                           cv2.FONT_HERSHEY_SIMPLEX, 1, (255, 0, 0), 2)
        
        else:
            # Image not stable yet
            cv2.putText(frame, f'Stabilizing... ({self.pills_count_detected}/{self.threshold_stable})', 
                       (10, 120), cv2.FONT_HERSHEY_SIMPLEX, 1, (255, 165, 0), 2)
        
        # 10. Update previous frame
        self.prev_frame = gray.copy()
        
        return frame

    def _cleanup(self):
        """Clean up resources"""
        print("Cleaning up camera resources...")
        
        if self.cap:
            self.cap.release()
            self.cap = None
        
        print("Camera cleanup completed")
