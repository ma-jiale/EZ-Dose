import serial
import serial.tools.list_ports
import numpy as np
import threading
import time
import struct

class DispenserController:
    # Dispenser device identification information
    DISPENSER_VID_PID_PATTERNS = [
        r'USB VID:PID=1A86:7523',  # Common VID:PID for CH340 chip
        r'CH340',                   # Device description containing CH340
        r'USB-SERIAL CH340'         # Complete device description
    ]
    DISPENSER_MANUFACTURER = 'wch.cn'

    def __init__(self, port=None):
        """
        Initialize dispenser controller instance without establishing connection.
        :param port: Serial port name, e.g., 'COM6' or '/dev/ttyUSB0'
        """
        # Store port for later initialization
        self.port = port
        self.ser = None
        
        # Set dispenser related parameters
        self.is_tray_opened = False
        self.is_receiver_thread_running = False # Whether serial receive thread is running
        self.machine_state = 0 # Machine state 0: idle; 1: working; 2: paused; 3: completed;
        self.is_sending_package = False # Whether sending data package
        self.err_code = 0 # Error code 0: no problem; 1: timeout exception; 2: pill counting exception; 
        self.pill_remain = -1 # Remaining pill count
        self.total_pill = 0 # Total pill count in a pill matrix
        self.ACK = False
        self.DONE = False
        self.repeat = 5 # Command retransmission count

    def initialize_dispenser(self):
        """
        Initialize dispenser by establishing serial connection and starting feedback handler.
        This method should be called after creating the DispenserController instance.
        :return: True if initialization successful, False otherwise
        """
        try:
            print("[Initialize] Starting dispenser initialization...")
            
            # If no port specified, automatically search for dispenser
            if self.port is None:
                self.port = self._find_dispenser_port()
                if self.port is None:
                    print("[Error] Dispenser device not found, please check device connection")
                    return False
            
            # Connect serial port
            self._connect_serial()
            
            # Start feedback handler thread
            self.start_dispenser_feedback_handler()
            # self.reset_dispenser()
            
            print("[Success] Dispenser initialization completed successfully")
            return True
            
        except ConnectionError as e:
            print(f"[Error] Connection failed during initialization: {e}")
            return False
        except Exception as e:
            print(f"[Error] Unexpected error during initialization: {e}")
            return False

###############################################################
# Auto select serial port to connect dispenser lower computer #
###############################################################
    def _find_dispenser_port(self):
        """
        Automatically search for dispenser serial port device
        :return: Found serial port name, returns None if not found
        """
        print("[Search] Searching for dispenser device...")
        
        # Get all available serial ports
        available_ports = serial.tools.list_ports.comports()
        
        if not available_ports:
            print("[Warning] No serial port devices found")
            return None
        
        print(f"[Info] Found {len(available_ports)} serial port devices:")
        for port in available_ports:
            print(f"  - {port.device}: {port.description}")
            if hasattr(port, 'manufacturer') and port.manufacturer:
                print(f"    Manufacturer: {port.manufacturer}")
            if hasattr(port, 'vid') and hasattr(port, 'pid') and port.vid and port.pid:
                print(f"    VID:PID: {port.vid:04X}:{port.pid:04X}")

        # Search for matching devices
        potential_ports = []
        
        for port in available_ports:
            score = self._score_port_match(port)
            if score > 0:
                potential_ports.append((port, score))
                print(f"[Match] Found potential dispenser device: {port.device} (match score: {score})")
        
        if not potential_ports:
            print("[Error] No matching dispenser device found")
            self._print_search_hints()
            return None
        
        # Sort by match score, select best match
        potential_ports.sort(key=lambda x: x[1], reverse=True)
        best_port = potential_ports[0][0]
        
        print(f"[Select] Auto-selected device: {best_port.device}")
        print(f"  Description: {best_port.description}")
        if hasattr(best_port, 'manufacturer') and best_port.manufacturer:
            print(f"  Manufacturer: {best_port.manufacturer}")
        
        return best_port.device
    
    def _score_port_match(self, port):
        """
        Score serial port device to determine if it's a dispenser device
        :param port: Serial port device information
        :return: Match score, higher score means better match, 0 means no match
        """
        score = 0
        
        # Check description information
        description = port.description.upper() if port.description else ""
        
        # CH340 chip detection (high priority)
        if 'CH340' in description:
            score += 50
            print(f"    [Match] CH340 chip detected: +50")
        
        # USB-SERIAL detection
        if 'USB-SERIAL' in description:
            score += 30
            print(f"    [Match] USB-SERIAL detected: +30")
        
        # Manufacturer detection
        if hasattr(port, 'manufacturer') and port.manufacturer:
            manufacturer = port.manufacturer.lower()
            if 'wch.cn' in manufacturer or 'wch' in manufacturer:
                score += 40
                print(f"    [Match] Manufacturer detected: +40")

        # VID:PID detection
        if hasattr(port, 'vid') and hasattr(port, 'pid') and port.vid and port.pid:
            # CH340 common VID:PID is 1A86:7523
            if port.vid == 0x1A86 and port.pid == 0x7523:
                score += 60
                print(f"    [Match] VID:PID detected(1A86:7523): +60")
        
        # Port name detection (Windows system)
        if port.device.startswith('COM') and hasattr(port, 'location') and port.location:
            # Check if it's USB device
            if 'USB' in port.hwid.upper():
                score += 10
                print(f"    [Match] USB device detected: +10")
        
        return score
    
    def _print_search_hints(self):
        """Print search hint information"""
        print("\n[Hint] Dispenser search failed, please check:")
        print("  1. Is the dispenser connected to the computer")
        print("  2. Are device drivers properly installed")
        print("  3. Is the device being used by other programs")
        print("  4. Is the USB cable working properly")
        print("\n[Expected device characteristics]:")
        print("  - Description contains: USB-SERIAL CH340")
        print("  - Manufacturer: wch.cn")
        print("  - VID:PID: 1A86:7523")

    def _connect_serial(self):
        """Establish serial connection"""
        try:
            self.ser = serial.Serial(
                port=self.port, 
                baudrate=115200,
                timeout=1.0,  # Set read timeout
                write_timeout=1.0  # Set write timeout
            )
            # Clear input buffer
            self.ser.reset_input_buffer()
            self.ser.reset_output_buffer()
            print(f"[Success] Serial port {self.port} connected successfully")
        except serial.SerialException as e:
            print(f"[Error] Serial connection failed: {e}")
            print(f"[Hint] Please check if device is connected to {self.port} or if port is occupied")
            raise
        except Exception as e:
            print(f"[Error] Unknown error occurred during serial initialization: {e}")
            raise

    def reconnect(self):
        """Reconnect to dispenser"""
        print("[Reconnect] Attempting to reconnect to dispenser...")
        
        # Close existing connection
        if self.ser and self.ser.is_open:
            self.ser.close()
        
        # Re-search for device
        new_port = self._find_dispenser_port()
        if new_port is None:
            raise ConnectionError("Reconnection failed: dispenser device not found")
        
        self.port = new_port
        self._connect_serial()
        print(f"[Success] Reconnected to {self.port}")

    @staticmethod
    def list_dispenser_devices():
        """
        List all possible dispenser devices
        :return: List of possible dispenser devices
        """
        print("[Scan] Scanning all possible dispenser devices...")
        controller = DispenserController.__new__(DispenserController)  # Create instance without calling __init__
        
        available_ports = serial.tools.list_ports.comports()
        potential_devices = []
        
        for port in available_ports:
            score = controller._score_port_match(port)
            if score > 0:
                potential_devices.append({
                    'port': port.device,
                    'description': port.description,
                    'manufacturer': getattr(port, 'manufacturer', 'Unknown'),
                    'vid_pid': f"{port.vid:04X}:{port.pid:04X}" if hasattr(port, 'vid') and port.vid else 'Unknown',
                    'score': score
                })
        
        # Sort by score
        potential_devices.sort(key=lambda x: x['score'], reverse=True)
        
        if potential_devices:
            print(f"[Result] Found {len(potential_devices)} possible dispenser devices:")
            for i, device in enumerate(potential_devices, 1):
                print(f"  {i}. {device['port']}")
                print(f"     Description: {device['description']}")
                print(f"     Manufacturer: {device['manufacturer']}")
                print(f"     VID:PID: {device['vid_pid']}")
                print(f"     Match score: {device['score']}")
        else:
            print("[Result] No possible dispenser devices found")
        
        return potential_devices

#################
# Handle dispenser feedback #
#################
    def start_dispenser_feedback_handler(self):
        """
        Start the serial receiver thread to handle feedback from the dispenser.
        """
        if self.is_receiver_thread_running:
            print("[Warning] Serial message receiver thread is already running")
            return
            
        self.is_receiver_thread_running = True
        self.receiver_thread = threading.Thread(
            target=self._handle_dispenser_feedback,
            name="DispenserReceiver"  # Name thread for easier debugging
        )
        self.receiver_thread.daemon = True
        self.receiver_thread.start()
        print("[Start] Serial message receiver thread started, now you can see all serial messages")

    def set_reset_callback(self, callback):
        """Set callback function to be called when reset is detected"""
        self.reset_callback = callback
        
    def _handle_dispenser_feedback(self):
        """
        Receive messages from dispenser lower computer via serial port and update dispenser instance attributes
        """
        while self.is_receiver_thread_running:
            if self.ser == None:
                time.sleep(0.01)  # Avoid empty loop consuming CPU
                continue
            try:
                org = self.ser.readline().decode('utf-8', errors='ignore').strip()
                if not org:
                    continue
                    
                print(f"Serial received >>> {org}")
                name = None
                val = None
                have_received_data = False
                attr_list = org.split(":")  # Split string to get attribute name and value
                if len(attr_list) >= 1:
                    name = attr_list[0]
                if len(attr_list) >= 2:
                    val = attr_list[1]

                if name == "machine init":
                    print("[Status Update] Physical reset detected")
                    self.machine_state = 0  # Reset to idle state
                    self.pill_remain = -1
                    self.total_pill = 0
                    # Call reset callback if set
                    if self.reset_callback:
                        self.reset_callback()
                elif name == "machine_state":
                    if val == 'FINISH':
                        self.machine_state = 3
                        self.err_code = 0
                        print("[Status Update] Dispensing completed")
                    elif val == 'CNT_ERR':
                        self.machine_state = 3
                        self.err_code = 2
                        print("[Status Update] Counting error")
                elif name == "pills out":
                    self.pill_remain = self.total_pill - int(val)
                    print(f"[Pills Remaining] Remaining undispensed pills: {self.pill_remain}")
                    have_received_data = True
                elif name == "ACK":
                    self.ACK = True
                    print("[Communication Confirmation] ACK received")
                elif name == "DONE":
                    self.DONE = True
                    print("[Operation Complete] DONE received")
                # Only send ACK back after receiving data
                if have_received_data:
                    self._send_package(b'\x0A', 0)
            except serial.SerialException as e:
                print(f"[Error] Serial communication exception: {e}")
                # Consider reconnection mechanism
                break
            except UnicodeDecodeError as e:
                print(f"[Error] Data decoding exception: {e}")
                continue
            except ValueError as e:
                print(f"[Error] Data parsing exception: {e}")
                continue
            except Exception as e:
                print(f"[Error] Unknown exception: {e}")
                continue

    def stop_dispenser_feedback_handler(self):
        """
        Stop the serial receiver thread and close the serial connection.
        This method ensures clean shutdown of all communication resources.
        """
        # Stop the receiving thread by setting the flag to False
        self.is_receiver_thread_running = False
        # Wait for the thread to actually terminate if it exists
        if hasattr(self, 'receiver_thread') and self.receiver_thread.is_alive():
            try:
                self.receiver_thread.join(timeout=2.0)
                if self.receiver_thread.is_alive():
                    print("[Warning] Serial message receiver thread cannot terminate normally, possible blocking")
                    print("[Hint] Suggest checking if serial read has deadlock")
                else:
                    print("[Success] Serial message receiver thread terminated normally")
            except Exception as e:
                print(f"[Error] Exception occurred while stopping receiver thread: {e}")
        elif hasattr(self, 'receiver_thread'):
            print("[Info] Serial receiver thread already stopped")
        else:
            print("[Info] Serial receiver thread not yet created")
        # Reset state variables about dispenser's feedback
        self.ACK = False
        self.DONE = False

    def __enter__(self):
        """Support context manager"""
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        """Automatically clean up resources"""
        self.stop_dispenser_feedback_handler()
        if self.ser and self.ser.is_open:
            self.ser.close()
            print("[Cleanup] Serial port closed")

    def close(self):
        """Manually close connection"""
        self.stop_dispenser_feedback_handler()
        if self.ser and self.ser.is_open:
            self.ser.close()
            print("[Cleanup] Serial port closed")
    
    def _wait_ACK(self, timeout=0.2):
        """
        Wait for ACK confirmation signal from dispenser.
        :param timeout: Timeout for waiting ACK (seconds)
        :return: Returns True if ACK received within timeout, otherwise False.
        """
        t0 = time.time()
        while(time.time() - t0 < timeout and not self.ACK):
            pass
        return self.ACK
    
    def _wait_DONE(self, timeout):
        """
        Wait for DONE signal from dispenser, indicating operation completion.
        :param timeout: Timeout for waiting DONE (seconds)
        :return: Returns True if DONE received within timeout, otherwise False.
        """
        t0 = time.time()
        while(time.time() - t0 < timeout and not self.DONE):
            pass
        return self.DONE
    
    def _send_package(self, data, repeat=5):
        """
        Send data package to dispenser and wait for ACK confirmation.
        :param data: Data package to send, must be bytes type.
        :param repeat: Retransmission count if sending fails.
        :return: Returns True if ACK received successfully, otherwise False.
        """
        # Check serial port status
        if self.ser is None:
            print("[Error] Serial port not connected, cannot send data")
            return False
        # Check sending status
        if self.is_sending_package:
            print("[Warning] Previous send not yet completed")
            return False
        # Parameter validation
        if not isinstance(data, (bytes, bytearray)):
            print("[Error] Data must be bytes type")
            return False
        # Make sure repeat times is not negative
        if repeat < 0:
            print("[Error] Retransmission count cannot be negative")
            return False
        self.is_sending_package = True
        
        try:
            # Calculate CRC checksum
            crc = sum(data) & 0xFFFF  # Use bitwise operation to ensure within 16-bit range
            # Build data package
            package = b'\xaa\xbb' + data + crc.to_bytes(2, 'little')
            # Check if it's ACK command (0x0A)
            is_ack_command = len(data) >= 1 and data[0] == 0x0A
            if is_ack_command:
                # ACK command sent directly, no need to wait for reply
                try:
                    self.ser.write(package)
                    print(f"[Send ACK to dispenser] {package.hex()}")
                    return True  # ACK command returns success immediately after sending
                except Exception as e:
                    print(f"[Error] ACK send failed: {e}")
                    return False
                
            # Normal processing flow for non-ACK commands
            # Make sure ACK state is initialized
            self.ACK = False
            # First send
            try:
                self.ser.write(package)
                print(f"[Send] {package.hex()}")
            except Exception as e:
                print(f"[Error] Data send failed: {e}")
                return False
            
            # Wait for first ACK
            if self._wait_ACK(0.2):
                print("[Success] Confirmation received")
                return True
            
            # Retransmission mechanism
            for attempt in range(repeat):
                print(f"[Retransmit {attempt + 1}/{repeat}] No confirmation received, resending")
                try:
                    self.ser.write(package)
                    print(f"[Retransmit] {package.hex()}")
                except Exception as e:
                    print(f"[Error] Retransmit data failed: {e}")
                    continue
                
                if self._wait_ACK(0.2):
                    print("[Success] Confirmation received after retransmission")
                    return True
            # All retransmissions failed
            print(f"[Failure] No confirmation received after {repeat} retransmissions")
            return False  
        except Exception as e:
            print(f"[Error] Exception occurred during sending: {e}")
            return False
        finally:
            # Ensure sending status is reset under any circumstances
            self.is_sending_package = False
            
##################
# Pill dispensing control commands #
##################
    def send_pill_matrix(self, pill_matrix):
        """
        Send dispensing command and pill matrix to dispenser
        """
        assert isinstance(pill_matrix, np.ndarray), "pill_matrix must be a numpy array"
        self.total_pill = np.sum(pill_matrix) # Update remaining pill count
        self.pill_remain = self.total_pill # Initialize remaining pill count
        cmd = b'\x05'
        data = pill_matrix.tobytes()
        ack = self._send_package(cmd + data, self.repeat)
        if(ack):
            self.machine_state = 1 # Dispenser is working    
            return True
        else:
            print("[error] Send pill matrix fail, Dispenser doesn't respond")
            return False
    
    def open_tray(self):
        """
        Send open tray command to dispenser
        Blocking operation
        err_code:
            0: normal
            1: timeout no response
        """
        cmd = b'\x03'
        if(not self._send_package(cmd, self.repeat)):
            return 1
        self.is_tray_opened = True
        return 0
        
    def close_tray(self):
        """
        Send close tray command to dispenser
        Blocking operation
        err_code:
            0: normal
            1: timeout no response
        """
        cmd = b'\x04'
        if(not self._send_package(cmd, self.repeat)):
            return 1
        self.is_tray_opened = False
        return 0
    
    def pause_dispenser(self):
        """
        Send pause command to dispenser
        Blocking operation
        err_code:
            0: normal
            1: timeout no response
        """
        cmd = b'\x01'
        if(not self._send_package(cmd, self.repeat)):
            return 1
        self.machine_state = 2
        return 0
    
    def reset_dispenser(self):
        """
        Machine initialization, clean pill tray
        Blocking operation
        err_code:
            0: normal
            1: timeout no response
        """
        cmd = b'\x00'
        if(not self._send_package(cmd, self.repeat)):
            return 1
        DONE = self._wait_DONE(10)
        time.sleep(6)  # Additional 6 second wait to ensure physical operation completion
        print("[Operation] Dispenser has been reset")
        if(DONE):
            self.DONE = False
            self.machine_state = 0
            return 0
        return 1
    
#################
# Set dispenser parameters #
#################

    # Turntable motor speed setting speed(float32)
    def set_turnMotor_speed(self, speed): 
        cmd = b'\x08'
        id = b'\x00'
        if(not self._send_package(cmd + id + struct.pack('<f',speed), self.repeat)):
            return 1
        return 0
    
    # Servo angle setting speed(float32)
    def set_servo_angle(self, angle): 
        cmd = b'\x08'
        id = b'\x01'    
        if(not self._send_package(cmd + id + struct.pack('<f',angle), self.repeat)):
            return 1
        return 0
    
    # Upper optocoupler threshold setting thresh(float32)
    def set_upperOptocoupler_thresh(self, thresh): 
        cmd = b'\x06'
        id = b'\x00'
        if(not self._send_package(cmd + id + struct.pack('<f',thresh), self.repeat)):
            return 1
        return 0
    
    # Lower optocoupler threshold setting thresh(float32)
    def set_lowerOptocoupler_thresh(self, thresh): 
        cmd = b'\x06'
        id = b'\x01'
        if(not self._send_package(cmd + id + struct.pack('<f',thresh), self.repeat)):
            return 1
        return 0
    
    # Upper optocoupler pulse refractory period setting noresp(uint32)
    def set_upperOptocoupler_noresp(self, noresp): 
        cmd = b'\x07'
        id = b'\x00'
        if(not self._send_package(cmd + id + noresp.to_bytes(4, 'little'), self.repeat)):
            return 1
        return 0
    
    # Lower optocoupler pulse refractory period setting noresp(uint32)
    def set_lowerOptocoupler_noresp(self, noresp): 
        cmd = b'\x07'
        id = b'\x01'
        if(not self._send_package(cmd + id + noresp.to_bytes(4, 'little'), self.repeat)):
            return 1
        return 0
    
    # Turntable motor brake delay setting delay_stop(uint32) ms
    def set_turnMotor_delay_stop(self, delay_stop): 
        cmd = b'\x09'
        id = b'\x00'
        if(not self._send_package(cmd + id + delay_stop.to_bytes(4, 'little'), self.repeat)):
            return 1
        return 0

    # Clean speed setting clean_speed(float32)
    def set_clean_speed(self, speed): 
        cmd = b'\x0B'
        if(not self._send_package(cmd + struct.pack('<f',speed), self.repeat)):
            return 1
        return 0
    
    # Clean time setting clean_delay(uint32) ms
    def set_clean_delay(self, delay): 
        cmd = b'\x0C'
        if(not self._send_package(cmd + delay.to_bytes(4, 'little'), self.repeat)):
            return 1
        return 0



