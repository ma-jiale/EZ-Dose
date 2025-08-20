from PySide6.QtCore import Signal, Slot, QTimer, QObject
from dispenser_controller import DispenserController
from patient_prescription_manager import PatientPrescriptionManager
import numpy as np

class MainController(QObject):
    # Signal definitions
    dispenser_initialized_signal = Signal(bool)  # Dispenser initialization complete signal
    database_connected_signal = Signal(bool)     # Database connection complete signal
    prescription_loaded_signal = Signal(object)
    dispensing_completed_signal = Signal()
    current_medicine_info_signal = Signal(str, int)
    dispensing_progress_signal = Signal(int)
    today_patients_ready_signal = Signal(bool, list)  # (success, patients_data)
    
    ###############
    # Initialization
    ###############
    def __init__(self):
        super().__init__()
        self.dispenser_controller = DispenserController()
        # self.rx_manager = PatientPrescriptionManager(server_url="https://ixd.sjtu.edu.cn/flask/packer")
        self.rx_manager = PatientPrescriptionManager()

        # Current dispensing state variables
        self.current_pills_dispensing_list = {}
        self.current_medicines = []
        self.current_medicine_index = 0
        self.is_dispensing = False
        self.max_days = 7

    @Slot()
    def initialize_hardware(self):
        """Initialize dispenser hardware"""
        try:
            self.dispenser_controller.start_dispenser_feedback_handler()
            self.dispenser_controller.initialize_dispenser()
            self.dispenser_initialized_signal.emit(True)
            print("[Init] Dispenser initialization completed")
        except Exception as e:
            print(f"[Error] Dispenser initialization failed: {e}")
            self.dispenser_initialized_signal.emit(False)
        
    @Slot()
    def initialize_database(self):
        """Connect to database and load prescriptions"""
        try:
            self.rx_manager.load_prescriptions()
            self.database_connected_signal.emit(True)
            print("[Init] Database loading completed")
        except Exception as e:
            print(f"[Error] Database loading failed: {e}")
            self.database_connected_signal.emit(False)

    ###########################
    # Prescription Management #
    ###########################
    @Slot(object)
    def generate_pills_dispensing_list(self, id):
        """Generate pill dispensing list for a patient"""
        try:
            # Reset current dispensing state
            self.current_pills_dispensing_list = {}
            self.current_medicines = []
            self.current_medicine_index = 0

            success, pills_dispensing_list = self.rx_manager.generate_pills_dispensing_list(id, self.max_days)
            
            if not success:
                print(f"[Error] Failed to generate pill dispensing matrix")
                return
            
            self.prescription_loaded_signal.emit(pills_dispensing_list)

            # Save prescription information and dispensing matrix
            self.current_pills_dispensing_list = pills_dispensing_list
            self.current_medicines = pills_dispensing_list["medicines_1"]
            self.current_medicine_index = 0
        except Exception as e:
            print(f"[Error] Exception loading pill dispensing list: {str(e)}")

    @Slot(int)
    def get_today_patients(self, days_threshold):
        """Get today's patient data (thread-safe)"""
        try:
            success, patients_data = self.rx_manager.get_patients_for_today(days_threshold)
            self.today_patients_ready_signal.emit(success, patients_data)
        except Exception as e:
            print(f"[Error] Failed to get today patients: {e}")
            self.today_patients_ready_signal.emit(False, [])



    #####################
    # Dispenser Control #
    #####################
    @Slot()
    def open_tray(self):
        """Open the pill tray"""
        self.dispenser_controller.open_tray()

    @Slot()
    def close_tray(self):
        """Close the pill tray"""
        self.dispenser_controller.close_tray()

    ####################
    # Dispensing Logic #
    ####################
    @Slot()
    def start_dispensing(self):
        """Start the dispensing process"""
        self.monitor_timer = QTimer()
        self.monitor_timer.timeout.connect(self.monitor_dispensing_status)
        try:
            if not self.current_medicines:
                print("[Error] No medicines to dispense")
                return
            
            if not self.dispenser_controller:
                print("[Error] Dispenser not initialized")
                return
            
            self.is_dispensing = True
            self.current_medicine_index = 0
        
            # Start dispensing the first medicine
            self.dispense_next_medicine()
        except Exception as e:
            print(f"[Error] Exception starting dispensing: {str(e)}")

    def configure_dispenser_for_pill_size(self, pill_size="M"):
        """Configure motor speed and servo angle based on pill size"""
        try:
            # Define settings for different pill sizes
            pill_settings = {
                "S": {"turn_motor_speed": 0.3, "servo_angle": 0.8},  # Small pills: slower speed, smaller angle
                "M": {"turn_motor_speed": 0.5, "servo_angle": 0.5},   # Medium pills: medium speed, medium angle
                "L": {"turn_motor_speed": 0.8, "servo_angle": 0.2}   # Large pills: faster speed, larger angle
            }
            
            if pill_size not in pill_settings:
                print(f"[Error] Invalid pill size: {pill_size}. Using default settings for medium pills.")
                pill_size = "M"
            
            settings = pill_settings[pill_size]
            turn_motor_speed = settings["turn_motor_speed"]
            servo_angle = settings["servo_angle"]
            
            # Set motor speed
            print(f"[Dispensing] Setting turn motor speed for {pill_size} pills: {turn_motor_speed}")
            self.dispenser_controller.set_turnMotor_speed(turn_motor_speed)
            
            # Set servo angle
            print(f"[Dispensing] Setting servo angle for {pill_size} pills: {servo_angle}")
            self.dispenser_controller.set_servo_angle(servo_angle)
            
            return True
            
        except Exception as e:
            print(f"[Error] Failed to configure dispenser for pill size {pill_size}: {str(e)}")
            return False

    def dispense_next_medicine(self):
        """Dispense the next medicine in the queue"""
        try:
            if self.current_medicine_index >= len(self.current_medicines):
                # All medicines have been dispensed
                print("[Dispensing] All medicines dispensed successfully")
                self.complete_dispensing()
                return
            
            current_medicine = self.current_medicines[self.current_medicine_index]
            medicine_name = current_medicine['medicine_name']

            # Update medicine expiry date in prescription database
            self.rx_manager.update_medicine_expiry_date(medicine_name)
            
            print(f"[Dispensing] Starting to dispense medicine {self.current_medicine_index + 1}/{len(self.current_medicines)}: {medicine_name}")
            pill_matrix = current_medicine["pill_matrix"]

            # Print current pill matrix information
            print(f"[Dispensing] {medicine_name} pill matrix:")
            time_labels = ["Morning", "Noon", "Evening", "Night"]
            for i, label in enumerate(time_labels):
                print(f"  {label}: {' '.join(f'{x:2}' for x in pill_matrix[i])}")

            # Configure dispenser based on pill size
            if not self.configure_dispenser_for_pill_size(current_medicine["pill_size"]):
                print(f"[Error] Failed to configure dispenser for {medicine_name}")
                return

            # Send dispensing progress signal
            self.total_pill = np.sum(pill_matrix)
            self.current_medicine_info_signal.emit(
                medicine_name,
                self.total_pill
            )
            
            # Send pill matrix data to dispenser
            print(f"[Dispensing] Sending pill matrix data to dispenser...")
            ack = self.dispenser_controller.send_pill_matrix(pill_matrix)
            
            if not ack:
                error_msg = f"Failed to send pill matrix data: {medicine_name}"
                print(f"[Error] {error_msg}")
                return
            
            print(f"[Dispensing] Data sent successfully, waiting for dispensing completion...")
            
            # Start monitoring timer
            self.monitor_timer.start(1000)  # Check every second
        except Exception as e:
            error_msg = f"Dispensing exception: {str(e)}"
            print(f"[Error] {error_msg}")

    def monitor_dispensing_status(self):
        """Monitor the dispensing status"""
        try:
            # If no dispenser or not in dispensing state, stop monitoring
            if not self.dispenser_controller or not self.is_dispensing:
                self.monitor_timer.stop()
                return
            
            self.pill_remain = self.dispenser_controller.pill_remain if hasattr(self.dispenser_controller, 'pill_remain') else 0
            self.dispensing_progress_signal.emit((self.total_pill - self.pill_remain) / self.total_pill * 100)

            # # # 发射药品转换信号
            # if self.dispenser_controller.pill_remain == 0 and self.current_medicine_index < len(self.current_medicines):
            #     current_medicine_name = self.current_medicines[self.current_medicine_index]['medicine_name']
            #     next_medicine_index = self.current_medicine_index + 1
            #     if next_medicine_index < len(self.current_medicines):
            #         next_medicine_name = self.current_medicines[next_medicine_index]['medicine_name']
            #         self.medicine_transition_signal.emit(current_medicine_name, next_medicine_name)
            #     else:
            #         # 最后一个药品分发完成
            #         self.medicine_transition_signal.emit(current_medicine_name, "")

            # Check dispenser status
            print(f"[Monitor] Dispenser state: {self.dispenser_controller.machine_state}, Error code: {self.dispenser_controller.err_code}, Pills remaining: {self.dispenser_controller.pill_remain}")
            
            if self.dispenser_controller.machine_state == 3:  # Dispensing completed
                self.monitor_timer.stop()                
                current_medicine = self.current_medicines[self.current_medicine_index]
                medicine_name = current_medicine['medicine_name']
                
                if self.dispenser_controller.err_code == 0:
                    print(f"[Dispensing] {medicine_name} dispensing completed")
                    if hasattr(self.dispenser_controller, 'pill_remain'):
                        print(f"[Dispensing] Pills remaining: {self.dispenser_controller.pill_remain}")

                    # Prepare to dispense next medicine
                    self.current_medicine_index += 1
                    # Wait briefly before dispensing next medicine
                    self.dispense_next_medicine()
                    
                else:
                    error_msg = f"{medicine_name} dispensing error, error code: {self.dispenser_controller.err_code}"
                    print(f"[Error] {error_msg}")

                    # Currently continue dispensing next medicine even after failure
                    # Prepare to dispense next medicine
                    self.current_medicine_index += 1
                    # Wait briefly before dispensing next medicine
                    self.dispense_next_medicine()
                    
            elif self.dispenser_controller.machine_state == 2:  # Paused state
                print("[Dispensing] Dispensing paused")
                
            elif self.dispenser_controller.machine_state == 1:  # Dispensing in progress
                print("[Dispensing] Dispensing in progress...")
                
        except Exception as e:
            error_msg = f"Exception monitoring dispensing status: {str(e)}"
            print(f"[Error] {error_msg}")

    def complete_dispensing(self):
        """Complete the dispensing process"""
        try:
            self.rx_manager.upload_prescriptions_to_server()
            
            self.is_dispensing = False
            self.monitor_timer.stop()
            print("[Dispensing] All medicines dispensed successfully")
            self.dispensing_completed_signal.emit()

            # Open the pill tray
            print("[Dispensing] Opening pill tray...")
            if self.open_tray():
                print("[Warning] Failed to open pill tray")
            
        except Exception as e:
            error_msg = f"Exception completing dispensing: {str(e)}"
            print(f"[Error] {error_msg}")

















