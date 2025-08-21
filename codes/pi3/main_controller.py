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
    dispenser_reset_signal = Signal()  # Signal for physical reset
    plate_transition_signal = Signal(int) # Signal when need to switch plates (plate_number)
    dispensing_error_signal = Signal(str, int)

##################
# Initialization #
##################
    def __init__(self):
        super().__init__()
        self.dispenser_controller = DispenserController()
        # self.rx_manager = PatientPrescriptionManager(server_url="https://ixd.sjtu.edu.cn/flask/packer")
        self.rx_manager = PatientPrescriptionManager()


        # Maximum number of days for which pills will be dispensed in advance
        # Controls how many days' worth of medication to prepare for each patient
        self.max_days = 7
        
        # Threshold in days before medicine expiry to trigger dispensing 
        # Medicines expiring within this many days will be flagged for dispensing
        self.expiry_days_threshold = 2

        # Current dispensing state variables
        self.current_pills_dispensing_list = {}
        self.current_medicines = []
        self.current_medicine_index = 0
        self.current_plate_number = 1  # Track current plate (1 or 2)
        self.is_dispensing = False

        # Set up reset callback
        self.dispenser_controller.set_reset_callback(self.on_dispenser_reset)

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
            self.current_plate_number = 1  # Start with plate 1

            success, pills_dispensing_list = self.rx_manager.generate_pills_dispensing_list(id, self.max_days)
            
            if not success:
                print(f"[Error] Failed to generate pill dispensing matrix")
                return
            
            self.prescription_loaded_signal.emit(pills_dispensing_list)

            # Save prescription information and dispensing matrix
            self.current_pills_dispensing_list = pills_dispensing_list
            self.current_medicines = pills_dispensing_list.get("medicines_1", [])
            self.current_medicine_index = 0
        except Exception as e:
            print(f"[Error] Exception loading pill dispensing list: {str(e)}")

    @Slot()
    def get_today_patients(self):
        """Get today's patient data (thread-safe)"""
        try:
            success, patients_data = self.rx_manager.get_patients_for_today(self.expiry_days_threshold)
            self.today_patients_ready_signal.emit(success, patients_data)
        except Exception as e:
            print(f"[Error] Failed to get today patients: {e}")
            self.today_patients_ready_signal.emit(False, [])

###########################
# Basic Dispenser Control #
###########################
    @Slot()
    def open_tray(self):
        """Open the pill tray"""
        self.dispenser_controller.open_tray()

    @Slot()
    def close_tray(self):
        """Close the pill tray"""
        self.dispenser_controller.close_tray()

    @Slot()
    def pause_dispenser(self):
        self.dispenser_controller.pause_dispenser()

####################
# Dispensing Logic #
####################
    @Slot()
    def start_dispensing(self):
        """Start the dispensing process"""
        self.monitor_timer = QTimer()
        self.monitor_timer.timeout.connect(self._monitor_dispensing_status)
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

    @Slot()
    def start_second_plate_dispensing(self):
        """Start dispensing second plate after plate change"""
        try:
            print("[Dispensing] Starting second plate dispensing")
            
            # Switch to second plate medicines
            self.current_medicines = self.current_pills_dispensing_list.get("medicines_2", [])
            self.current_medicine_index = 0
            
            if not self.current_medicines:
                print("[Error] No medicines in second plate")
                self.complete_dispensing()
                return
            
            # Close tray and start dispensing
            print(f"[Dispensing] Second plate has {len(self.current_medicines)} medicines")
            self.dispense_next_medicine()
            
        except Exception as e:
            print(f"[Error] Exception starting second plate dispensing: {str(e)}")

    def _configure_dispenser_for_pill_size(self, pill_size="M"):
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
            # Check if current plate is finished
            if self.current_medicine_index >= len(self.current_medicines):
                if self.current_plate_number == 1:
                    # First plate finished, check if there's a second plate
                    second_plate_medicines = self.current_pills_dispensing_list.get("medicines_2", [])
                    if second_plate_medicines:
                        # Need to switch to second plate
                        print("[Dispensing] First plate completed, requesting plate change")
                        self.current_plate_number = 2
                        self.plate_transition_signal.emit(2)  # Signal GUI to scan for plate 2
                        return
                    else:
                        # No second plate, dispensing complete
                        print("[Dispensing] All medicines dispensed successfully (single plate)")
                        self.complete_dispensing()
                        return
                else:
                    # Second plate also finished
                    print("[Dispensing] All medicines dispensed successfully (both plates)")
                    self.complete_dispensing()
                    return
            
            current_medicine = self.current_medicines[self.current_medicine_index]
            medicine_name = current_medicine['medicine_name']

            # Update medicine expiry date in prescription database
            self.rx_manager.update_medicine_expiry_date(medicine_name)
            
            plate_info = f"Plate {self.current_plate_number}"
            print(f"[Dispensing] {plate_info} - Starting to dispense medicine {self.current_medicine_index + 1}/{len(self.current_medicines)}: {medicine_name}")
            
            pill_matrix = current_medicine["pill_matrix"]
            # Print current pill matrix information
            print(f"[Dispensing] {medicine_name} pill matrix:")
            time_labels = ["Morning", "Noon", "Evening", "Night"]
            for i, label in enumerate(time_labels):
                print(f"  {label}: {' '.join(f'{x:2}' for x in pill_matrix[i])}")

            # Configure dispenser based on pill size
            if not self._configure_dispenser_for_pill_size(current_medicine["pill_size"]):
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

    def _monitor_dispensing_status(self):
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

                    # Open tray for manual correction when dispensing error occurs
                    print("[Error] Opening tray for manual correction...")
                    self.open_tray()
                    
                    # Emit signal to show error message to user
                    self.dispensing_error_signal.emit(medicine_name, self.dispenser_controller.err_code)
                    
                    # Don't automatically continue - wait for user confirmation
                    # The dispensing will continue when user confirms manual correction is done
                    
            elif self.dispenser_controller.machine_state == 2:  # Paused state
                print("[Dispensing] Dispensing paused")
                
            elif self.dispenser_controller.machine_state == 1:  # Dispensing in progress
                print("[Dispensing] Dispensing in progress...")
                
        except Exception as e:
            error_msg = f"Exception monitoring dispensing status: {str(e)}"
            print(f"[Error] {error_msg}")

    @Slot()
    def continue_after_manual_correction(self):
        """Continue dispensing after manual correction is completed"""
        try:
            print("[Dispensing] Manual correction completed, closing tray and continuing...")
            self.close_tray()
            
            # Move to next medicine
            self.current_medicine_index += 1
            # Continue with next medicine
            self.dispense_next_medicine()
            
        except Exception as e:
            print(f"[Error] Exception continuing after manual correction: {str(e)}")

    def complete_dispensing(self):
        """Complete the dispensing process"""
        try:
            self.rx_manager.upload_prescriptions_to_server()
            self.is_dispensing = False
            self.monitor_timer.stop()
            print("[Dispensing] All medicines dispensed successfully")
            self.dispensing_completed_signal.emit()
            
            # Pause the turnmotor of dispenser
            self.pause_dispenser()
            # Open the pill tray
            print("[Dispensing] Opening pill tray...")
            if self.open_tray():
                print("[Warning] Failed to open pill tray")
            
        except Exception as e:
            error_msg = f"Exception completing dispensing: {str(e)}"
            print(f"[Error] {error_msg}")

#########################
# Handle physical reset #
#########################
    def on_dispenser_reset(self):
        # Emit reset signal to GUI
        if self.is_dispensing:
            self.dispenser_reset_signal.emit()

    @Slot()
    def stop_dispensing(self):
        """Handle physical reset from dispenser"""
        print("[Reset] Physical reset detected, stopping dispensing process")
        # Reset dispensing state
        self.is_dispensing = False
        if hasattr(self, 'monitor_timer'):
            self.monitor_timer.stop()
        
        # Clear current dispensing data
        self.current_pills_dispensing_list = {}
        self.current_medicines = []
        self.current_medicine_index = 0
        


















