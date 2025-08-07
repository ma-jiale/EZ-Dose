import cv2
import numpy as np
from pyzbar import pyzbar
from enum import Enum
import threading
import time

class CamMode(Enum):
    IDLE = "idle"
    QR_SCAN = "qr_scan"
    PILLS_COUNT = "pills_count"
    # Add more modes as needed
    # FACE_DETECTION = "face_detection"
    # MOTION_DETECTION = "motion_detection"

class CamController:
    
    def __init__(self, camera_index=1, target_width=1280, target_height=720, target_fps=30):
        """Initialize camera controller with parameters"""
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
    
    def capture_frame(self):
        """Capture a single frame from the camera"""
        if not self.cap:
            return None, False
        
        ret, frame = self.cap.read()
        if not ret:
            return None, False
        
        return frame, True
    
    def capture_and_correct_frame(self):
        """Capture frame and apply distortion correction"""
        frame, ret = self.capture_frame()
        if not ret:
            return None, False
        
        # Apply distortion correction
        corrected_frame = cv2.remap(frame, self.map1, self.map2, cv2.INTER_LINEAR)
        
        # Crop to remove black borders
        x, y, w, h = self.roi
        if w > 0 and h > 0:
            corrected_frame = corrected_frame[y:y+h, x:x+w]
        
        return corrected_frame, True
    
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
    
    def set_mode(self, mode: CamMode):
        """Set camera operation mode"""
        with self.thread_lock:
            self.current_mode = mode
            print(f"Camera mode set to: {mode.value}")
            
            # Clear previous results when switching modes
            if mode == CamMode.QR_SCAN:
                self.qr_codes_detected = []
    
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
                    frame, ret = self.capture_and_correct_frame()
                else:
                    frame, ret = self.capture_frame()

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
                time.sleep(0.03)  # ~30 FPS
                
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

    def _process_pills_count(self, frame):
        """Count pills using improved multi-method approach"""
        
        # Set ROI region (adjust coordinates based on your camera setup)
        roi_top_left = (20, 20)
        roi_bottom_right = (min(1260, frame.shape[1]), min(700, frame.shape[0]))
        
        # Extract ROI region
        roi = frame[roi_top_left[1]:roi_bottom_right[1], roi_top_left[0]:roi_bottom_right[0]]
        
        # Stabilization processing
        roi = cv2.bilateralFilter(roi, 9, 75, 75)
        
        # Multi-method preprocessing
        gray = cv2.cvtColor(roi, cv2.COLOR_BGR2GRAY)
        
        # Method 1: Adaptive threshold
        blur1 = cv2.GaussianBlur(gray, (5, 5), 0)
        thresh1 = cv2.adaptiveThreshold(blur1, 255, cv2.ADAPTIVE_THRESH_GAUSSIAN_C, 
                                        cv2.THRESH_BINARY_INV, 11, 2)
        
        # Method 2: Otsu threshold
        blur2 = cv2.GaussianBlur(gray, (3, 3), 0)
        _, thresh2 = cv2.threshold(blur2, 0, 255, cv2.THRESH_BINARY_INV + cv2.THRESH_OTSU)
        
        # Method 3: Canny edge detection with filling
        edges = cv2.Canny(blur1, 50, 150)
        kernel = np.ones((3,3), np.uint8)
        thresh3 = cv2.morphologyEx(edges, cv2.MORPH_CLOSE, kernel, iterations=2)
        
        # Combine multiple method results
        combined = cv2.bitwise_or(thresh1, thresh2)
        combined = cv2.bitwise_or(combined, thresh3)
        
        # Morphological operations optimization
        kernel_close = np.ones((4,4), np.uint8)
        kernel_open = np.ones((2,2), np.uint8)
        
        # Closing operation to connect broken contours
        combined = cv2.morphologyEx(combined, cv2.MORPH_CLOSE, kernel_close, iterations=2)
        # Opening operation to remove small noise
        combined = cv2.morphologyEx(combined, cv2.MORPH_OPEN, kernel_open, iterations=1)
        
        # Find contours
        contours, _ = cv2.findContours(combined, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        
        medicine_count = 0
        valid_contours = []
        
        for contour in contours:
            area = cv2.contourArea(contour)
            
            # Dynamic area threshold (based on image size)
            min_area = roi.shape[0] * roi.shape[1] * 0.0005  # 0.05% of image area
            max_area = roi.shape[0] * roi.shape[1] * 0.1     # 10% of image area
            
            if area < min_area or area > max_area:
                continue
            
            # Calculate contour features
            x, y, w, h = cv2.boundingRect(contour)
            aspect_ratio = w / h if h > 0 else 0
            
            # Calculate contour convexity
            hull = cv2.convexHull(contour)
            hull_area = cv2.contourArea(hull)
            if hull_area > 0:
                solidity = area / hull_area
            else:
                continue
                
            # Calculate contour circularity
            perimeter = cv2.arcLength(contour, True)
            if perimeter > 0:
                circularity = 4 * np.pi * area / (perimeter * perimeter)
            else:
                continue
            
            # Relaxed medicine recognition conditions (adapt to various shapes)
            # Including: round pills, oval capsules, square pills, irregular pills, etc.
            is_medicine = (
                (0.2 < circularity < 1.2) and           # Reasonable shape
                (0.3 < aspect_ratio < 3.0) and          # Reasonable aspect ratio
                (solidity > 0.6) and                    # Reasonable convexity (exclude overly irregular shapes)
                (w > 10 and h > 10)                     # Minimum size limit
            )
            
            if is_medicine:
                medicine_count += 1
                valid_contours.append(contour)
                
                # Adjust contour coordinates to original frame
                adjusted_x = x + roi_top_left[0]
                adjusted_y = y + roi_top_left[1]
                
                # Draw detection box
                cv2.rectangle(frame, (adjusted_x, adjusted_y), (adjusted_x + w, adjusted_y + h), (0, 255, 0), 2)
                cv2.putText(frame, f'M{medicine_count}', (adjusted_x, adjusted_y-5), 
                           cv2.FONT_HERSHEY_SIMPLEX, 0.5, (0, 255, 0), 1)
        
        # Update pills count
        with self.thread_lock:
            self.pills_count_detected = medicine_count
        
        # Display count on frame
        cv2.putText(frame, f'Medicine Count: {medicine_count}', (10, 30), 
                   cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 255), 2)
        
        # Call callback if set
        if self.pills_count_callback:
            self.pills_count_callback({
                'total_count': medicine_count,
                'valid_contours': len(valid_contours),
                'timestamp': time.time()
            })
        
        print(f"Pills detected - Total: {medicine_count}")
        
        return frame

    def get_pills_count_results(self):
        """Get latest pills count detection results"""
        with self.thread_lock:
            return self.pills_count_detected
    
    def _cleanup(self):
        """Clean up resources"""
        print("Cleaning up camera resources...")
        
        if self.cap:
            self.cap.release()
            self.cap = None
        
        print("Camera cleanup completed")
