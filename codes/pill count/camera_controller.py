import cv2
import numpy as np
import sys
from pyzbar import pyzbar

class CameraController:
    
    def __init__(self, camera_index=1, target_width=1280, target_height=720, target_fps=30):
        """Initialize camera controller with parameters"""
        self.camera_index = camera_index
        self.target_width = target_width
        self.target_height = target_height
        self.target_fps = target_fps
        
        self.cap = None
        self.is_running = False
        
        # Camera calibration parameters
        self.camera_matrix = None
        self.dist_coeffs = None
        self.map1 = None
        self.map2 = None
        self.roi = None
        
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
        
        return True
    
    def setup_distortion_correction(self):
        """Setup distortion correction parameters for wide-angle lens"""
        # Get actual resolution
        width = int(self.cap.get(cv2.CAP_PROP_FRAME_WIDTH))
        height = int(self.cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
        
        # Camera matrix estimation for wide-angle lens
        # These are approximate values - calibrated for your specific camera
        self.camera_matrix = np.array([[width * 0.8, 0, width / 2],
                                      [0, width * 0.8, height / 2],
                                      [0, 0, 1]], dtype=np.float32)
        
        # Distortion coefficients [k1, k2, p1, p2, k3] for barrel distortion
        # Adjusted parameters for your wide-angle lens
        self.dist_coeffs = np.array([-0.25, 0.1, 0, 0, 0], dtype=np.float32)
        
        # Create optimal new camera matrix
        new_camera_matrix, self.roi = cv2.getOptimalNewCameraMatrix(
            self.camera_matrix, self.dist_coeffs, (width, height), 1, (width, height))
        
        # Create undistortion maps for real-time correction
        self.map1, self.map2 = cv2.initUndistortRectifyMap(
            self.camera_matrix, self.dist_coeffs, None, new_camera_matrix, 
            (width, height), cv2.CV_16SC2)
        
        print("Distortion correction maps created")
    
    def detect_qr_codes(self, frame):
        """Detect and decode QR codes in the frame"""
        # Convert to grayscale for better QR detection
        gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        
        # Detect QR codes
        qr_codes = pyzbar.decode(gray)
        
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
                
                # Display QR code information
                x, y = points[0].x, points[0].y
                cv2.putText(frame, f"Type: {qr_type}", (x, y - 40), 
                           cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 0), 2)
                cv2.putText(frame, f"Data: {qr_data}", (x, y - 10), 
                           cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 0), 2)
                
                # Print to console
                print(f"QR Code detected - Type: {qr_type}, Data: {qr_data}")
        
        return frame
    
    def get_camera_info(self):
        """Get current camera properties"""
        if not self.cap:
            return None
        
        return {
            'width': int(self.cap.get(cv2.CAP_PROP_FRAME_WIDTH)),
            'height': int(self.cap.get(cv2.CAP_PROP_FRAME_HEIGHT)),
            'fps': self.cap.get(cv2.CAP_PROP_FPS)
        }
    
    def capture_and_correct_frame(self):
        """Capture frame and apply distortion correction"""
        if not self.cap:
            return None, False
        
        # Capture frame
        ret, frame = self.cap.read()
        if not ret:
            return None, False
        
        # Apply distortion correction
        corrected_frame = cv2.remap(frame, self.map1, self.map2, cv2.INTER_LINEAR)
        
        # Crop to remove black borders
        x, y, w, h = self.roi
        if w > 0 and h > 0:
            corrected_frame = corrected_frame[y:y+h, x:x+w]
        
        return corrected_frame, True
    
    def display_camera_info(self):
        """Display camera information"""
        info = self.get_camera_info()
        print("=" * 60)
        print("USB Camera - Wide Angle Lens with QR Code Detection")
        print("=" * 60)
        print(f"Resolution: {info['width']}x{info['height']}")
        print(f"Frame Rate: {info['fps']} FPS")
        print("Distortion Correction: ENABLED")
        print("QR Code Detection: ENABLED")
        print("-" * 60)
        print("Controls:")
        print("  Press 'q' or 'Q' to exit")
        print("=" * 60)
    
    def start_preview(self):
        """Start real-time camera preview with distortion correction"""
        try:
            # Initialize camera
            if not self.initialize_camera():
                return False
            
            # Setup distortion correction
            self.setup_distortion_correction()
            
            # Display info
            self.display_camera_info()
            
            # Start preview loop
            self.is_running = True
            self._preview_loop()
            
        except Exception as e:
            print(f"Error during camera preview: {e}")
            return False
        finally:
            self._cleanup()
        
        return True
    
    def _preview_loop(self):
        """Main preview loop"""
        while self.is_running:
            # Capture and correct frame
            frame, ret = self.capture_and_correct_frame()
            
            if not ret:
                print("Error: Cannot receive frame from camera")
                break
            
            # Detect QR codes and draw overlays
            frame_with_qr = self.detect_qr_codes(frame)
            
            # Display frame
            cv2.imshow('USB Camera - QR Code Detection', frame_with_qr)
            
            # Handle user input
            if self._handle_keypress():
                break
    
    def _handle_keypress(self):
        """Handle keyboard input, allowing exit on 'q' or 'Q'"""
        key = cv2.waitKey(1) & 0xFF
        if key == ord('q') or key == ord('Q'):
            print("Exit requested by user")
            return True
        return False
    
    def _cleanup(self):
        """Clean up resources"""
        print("Cleaning up resources...")
        
        if self.cap:
            self.cap.release()
        
        cv2.destroyAllWindows()
        self.is_running = False
        print("Camera preview stopped")

def main():
    """Main function"""
    print("Starting USB Camera Application...")
    
    try:
        # Create camera controller with default settings
        camera = CameraController(
            camera_index=1,      # USB camera index
            target_width=1280,   # Target resolution width
            target_height=720,   # Target resolution height
            target_fps=60        # Target frame rate
        )
        
        # Start camera preview
        success = camera.start_preview()
        
        if not success:
            print("Failed to start camera preview")
            return 1
        
    except KeyboardInterrupt:
        print("\nProgram interrupted by user (Ctrl+C)")
    except Exception as e:
        print(f"Unexpected error: {e}")
        return 1
    finally:
        print("Program exited")
    
    return 0

if __name__ == "__main__":
    sys.exit(main())