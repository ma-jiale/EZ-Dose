import serial
import serial.tools.list_ports
import numpy as np
import threading
import time
import struct

class DispenserController:
    # 分药机设备识别信息
    DISPENSER_VID_PID_PATTERNS = [
        r'USB VID:PID=1A86:7523',  # CH340芯片常见的VID:PID
        r'CH340',                   # 设备描述中包含CH340
        r'USB-SERIAL CH340'         # 完整的设备描述
    ]
    DISPENSER_MANUFACTURER = 'wch.cn'

    def __init__(self, port=None):
        """
        初始化分药机控制器实例, 使用pyserial库打开指定的串口, 并设置分药机参数。
        :param port: 串口名称，例如 'COM6' 或 '/dev/ttyUSB0'
        """
        # 如果没有指定端口，自动搜索分药机
        if port is None:
            port = self._find_dispenser_port()
            if port is None:
                raise ConnectionError("未找到分药机设备，请检查设备连接")

        # Connect serial port
        self.port = port
        self.ser = None
        self._connect_serial()

        # 设置分药机相关参数
        self.is_tray_opened = False
        self.is_receiver_thread_running = False # 接收串口信息线程是否在运行
        self.machine_state = 0 # 机器状态 0：空闲; 1: 正在工作; 2: 暂停工作; 3.完成工作;
        self.is_sending_package = False # 是否正在发送数据包
        self.err_code = 0 # 错误信息 0：没有问题；1：超时异常；2：分药计数异常; 
        self.pill_remain = -1 # 剩余药片数量
        self.total_pill = 0 # 一个药片矩阵总药片数量
        self.ACK = False
        self.DONE = False
        self.repeat = 5 # 重发指令次数

        # 自动启动接收线程
        self.start_dispenser_feedback_handler()

#############################
# 自动选择串口连接分药机下位机 #
#############################
    def _find_dispenser_port(self):
        """
        自动搜索分药机串口设备
        :return: 找到的串口名称，如果未找到返回None
        """
        print("[搜索] 正在搜索分药机设备...")
        
        # 获取所有可用串口
        available_ports = serial.tools.list_ports.comports()
        
        if not available_ports:
            print("[警告] 未发现任何串口设备")
            return None
        
        print(f"[信息] 发现 {len(available_ports)} 个串口设备:")
        for port in available_ports:
            print(f"  - {port.device}: {port.description}")
            if hasattr(port, 'manufacturer') and port.manufacturer:
                print(f"    制造商: {port.manufacturer}")
            if hasattr(port, 'vid') and hasattr(port, 'pid') and port.vid and port.pid:
                print(f"    VID:PID: {port.vid:04X}:{port.pid:04X}")

        # 搜索匹配的设备
        potential_ports = []
        
        for port in available_ports:
            score = self._score_port_match(port)
            if score > 0:
                potential_ports.append((port, score))
                print(f"[匹配] 发现潜在分药机设备: {port.device} (匹配度: {score})")
        
        if not potential_ports:
            print("[错误] 未找到匹配的分药机设备")
            self._print_search_hints()
            return None
        
        # 按匹配度排序，选择最佳匹配
        potential_ports.sort(key=lambda x: x[1], reverse=True)
        best_port = potential_ports[0][0]
        
        print(f"[选择] 自动选择设备: {best_port.device}")
        print(f"  描述: {best_port.description}")
        if hasattr(best_port, 'manufacturer') and best_port.manufacturer:
            print(f"  制造商: {best_port.manufacturer}")
        
        return best_port.device
    
    def _score_port_match(self, port):
        """
        为串口设备打分，判断是否为分药机设备
        :param port: 串口设备信息
        :return: 匹配分数，分数越高匹配度越高，0表示不匹配
        """
        score = 0
        
        # 检查描述信息
        description = port.description.upper() if port.description else ""
        
        # CH340芯片检测（高优先级）
        if 'CH340' in description:
            score += 50
            print(f"    [匹配] CH340芯片检测: +50")
        
        # USB-SERIAL检测
        if 'USB-SERIAL' in description:
            score += 30
            print(f"    [匹配] USB-SERIAL检测: +30")
        
        # 制造商检测
        if hasattr(port, 'manufacturer') and port.manufacturer:
            manufacturer = port.manufacturer.lower()
            if 'wch.cn' in manufacturer or 'wch' in manufacturer:
                score += 40
                print(f"    [匹配] 制造商检测: +40")

        # VID:PID检测
        if hasattr(port, 'vid') and hasattr(port, 'pid') and port.vid and port.pid:
            # CH340常见的VID:PID是1A86:7523
            if port.vid == 0x1A86 and port.pid == 0x7523:
                score += 60
                print(f"    [匹配] VID:PID检测(1A86:7523): +60")
        
        # 端口名称检测（Windows系统）
        if port.device.startswith('COM') and hasattr(port, 'location') and port.location:
            # 检查是否为USB设备
            if 'USB' in port.hwid.upper():
                score += 10
                print(f"    [匹配] USB设备检测: +10")
        
        return score
    
    def _print_search_hints(self):
        """打印搜索提示信息"""
        print("\n[提示] 分药机搜索失败，请检查:")
        print("  1. 分药机是否已连接到计算机")
        print("  2. 设备驱动是否正确安装")
        print("  3. 设备是否被其他程序占用")
        print("  4. USB线缆是否正常")
        print("\n[期望设备特征]:")
        print("  - 描述包含: USB-SERIAL CH340")
        print("  - 制造商: wch.cn")
        print("  - VID:PID: 1A86:7523")

    def _connect_serial(self):
        """建立串口连接"""
        try:
            self.ser = serial.Serial(
                port=self.port, 
                baudrate=115200,
                timeout=1.0,  # 设置读取超时
                write_timeout=1.0  # 设置写入超时
            )
            # 清空输入缓冲区
            self.ser.reset_input_buffer()
            self.ser.reset_output_buffer()
            print(f"[成功] 串口 {self.port} 连接成功")
        except serial.SerialException as e:
            print(f"[错误] 串口连接失败: {e}")
            print(f"[提示] 请检查设备是否连接到 {self.port} 或端口是否被占用")
            raise
        except Exception as e:
            print(f"[错误] 初始化串口时发生未知错误: {e}")
            raise

    def reconnect(self):
        """重新连接分药机"""
        print("[重连] 尝试重新连接分药机...")
        
        # 关闭现有连接
        if self.ser and self.ser.is_open:
            self.ser.close()
        
        # 重新搜索设备
        new_port = self._find_dispenser_port()
        if new_port is None:
            raise ConnectionError("重连失败：未找到分药机设备")
        
        self.port = new_port
        self._connect_serial()
        print(f"[成功] 重连到 {self.port}")

    @staticmethod
    def list_dispenser_devices():
        """
        列出所有可能的分药机设备
        :return: 可能的分药机设备列表
        """
        print("[扫描] 扫描所有可能的分药机设备...")
        controller = DispenserController.__new__(DispenserController)  # 创建实例但不调用__init__
        
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
        
        # 按分数排序
        potential_devices.sort(key=lambda x: x['score'], reverse=True)
        
        if potential_devices:
            print(f"[结果] 找到 {len(potential_devices)} 个可能的分药机设备:")
            for i, device in enumerate(potential_devices, 1):
                print(f"  {i}. {device['port']}")
                print(f"     描述: {device['description']}")
                print(f"     制造商: {device['manufacturer']}")
                print(f"     VID:PID: {device['vid_pid']}")
                print(f"     匹配度: {device['score']}")
        else:
            print("[结果] 未找到可能的分药机设备")
        
        return potential_devices

#################
# 处理分药机反馈 #
#################
    def start_dispenser_feedback_handler(self):
        """
        Start the serial receiver thread to handle feedback from the dispenser.
        """
        if self.is_receiver_thread_running:
            print("[警告] 串口信息接收线程已经在运行中")
            return
            
        self.is_receiver_thread_running = True
        self.receiver_thread = threading.Thread(
            target=self._handle_dispenser_feedback,
            name="DispenserReceiver"  # 给线程命名便于调试
        )
        self.receiver_thread.daemon = True
        self.receiver_thread.start()
        print("[启动] 串口信息接收线程已启动，现在可以看到所有串口消息")

    def _handle_dispenser_feedback(self):
        """
        接收分药机下位机通过串口传输的消息并对分药机实例属性进行更新
        """
        while self.is_receiver_thread_running:
            if self.ser == None:
                time.sleep(0.01)  # 避免空循环占用CPU
                continue
            try:
                org = self.ser.readline().decode('utf-8', errors='ignore').strip()
                if not org:
                    continue
                    
                print(f"串口接收 >>> {org}")
                name = None
                val = None
                have_received_data = False
                attr_list = org.split(":")  # 分割字符串，获取属性名和属性值
                if len(attr_list) >= 1:
                    name = attr_list[0]
                if len(attr_list) >= 2:
                    val = attr_list[1]

                if name == "machine init":
                    pass
                elif name == "machine_state":
                    if val == 'FINISH':
                        self.machine_state = 3
                        self.err_code = 0
                        print("[状态更新] 分药完成")
                    elif val == 'CNT_ERR':
                        self.machine_state = 3
                        self.err_code = 2
                        print("[状态更新] 计数错误")
                elif name == "pills out":
                    self.pill_remain = self.total_pill - int(val)
                    print(f"[药片剩余] 剩余未发药片数量: {self.pill_remain}")
                    have_received_data = True
                elif name == "ACK":
                    self.ACK = True
                    print("[通信确认] 收到ACK")
                elif name == "DONE":
                    self.DONE = True
                    print("[操作完成] 收到DONE")
                # 只有接收到数据后才需回发ACK
                if have_received_data:
                    self._send_package(b'\x0A', 0)
            except serial.SerialException as e:
                print(f"[错误] 串口通信异常: {e}")
                # 可以考虑重连机制
                break
            except UnicodeDecodeError as e:
                print(f"[错误] 数据解码异常: {e}")
                continue
            except ValueError as e:
                print(f"[错误] 数据解析异常: {e}")
                continue
            except Exception as e:
                print(f"[错误] 未知异常: {e}")
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
                    print("[警告] 串口信息接收线程无法正常结束，可能存在阻塞")
                    print("[提示] 建议检查串口读取是否存在死锁")
                else:
                    print("[成功] 串口信息接收线程已正常结束")
            except Exception as e:
                print(f"[错误] 停止接收线程时发生异常: {e}")
        elif hasattr(self, 'receiver_thread'):
            print("[信息] 串口接收线程已经停止")
        else:
            print("[信息] 串口接收线程尚未创建")
        # Reset state variables about dispenser's feedback
        self.ACK = False
        self.DONE = False

    def __enter__(self):
        """支持上下文管理器"""
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        """自动清理资源"""
        self.stop_dispenser_feedback_handler()
        if self.ser and self.ser.is_open:
            self.ser.close()
            print("[清理] 串口已关闭")

    def close(self):
        """手动关闭连接"""
        self.stop_dispenser_feedback_handler()
        if self.ser and self.ser.is_open:
            self.ser.close()
            print("[清理] 串口已关闭")
    
    def _wait_ACK(self, timeout=0.2):
        """
        等待分药机的ACK确认信号。
        :param timeout: 等待ACK的超时时间（秒）
        :return: 如果在超时时间内收到ACK，返回True，否则返回False。
        """
        t0 = time.time()
        while(time.time() - t0 < timeout and not self.ACK):
            pass
        return self.ACK
    
    def _wait_DONE(self, timeout):
        """
        等待分药机的DONE信号，表示操作完成。
        :param timeout: 等待DONE的超时时间（秒）
        :return: 如果在超时时间内收到DONE，返回True，否则返回False。
        """
        t0 = time.time()
        while(time.time() - t0 < timeout and not self.DONE):
            pass
        return self.DONE
    
    def _send_package(self, data, repeat=5):
        """
        发送数据包到分药机，并等待ACK确认。
        :param data: 要发送的数据包，必须是字节类型。
        :param repeat: 重发次数，如果发送失败则重发。
        :return: 如果成功收到ACK，返回True，否则返回False。
        """
        # 检查串口状态
        if self.ser is None:
            print("[错误] 串口未连接，无法发送数据")
            return False
        # 检查发送状态
        if self.is_sending_package:
            print("[警告] 上一个发送尚未完成")
            return False
        # 参数验证
        if not isinstance(data, (bytes, bytearray)):
            print("[错误] 数据必须是字节类型")
            return False
        # Make sure repeat times is not negative
        if repeat < 0:
            print("[错误] 重发次数不能为负数")
            return False
        self.is_sending_package = True
        
        try:
            # 计算CRC校验和
            crc = sum(data) & 0xFFFF  # 使用位运算确保在16位范围内
            # 构建数据包
            package = b'\xaa\xbb' + data + crc.to_bytes(2, 'little')
            # 检查是否是ACK命令（0x0A）
            is_ack_command = len(data) >= 1 and data[0] == 0x0A
            if is_ack_command:
                # ACK命令直接发送，无需等待回复
                try:
                    self.ser.write(package)
                    print(f"[发送ACK给分药机] {package.hex()}")
                    return True  # ACK命令发送成功即返回
                except Exception as e:
                    print(f"[错误] ACK发送失败: {e}")
                    return False
                
            # 非ACK命令的正常处理流程
            # Make sure ACK state is initialized
            self.ACK = False
            # 首次发送
            try:
                self.ser.write(package)
                print(f"[发送] {package.hex()}")
            except Exception as e:
                print(f"[错误] 发送数据失败: {e}")
                return False
            
            # 等待首次ACK
            if self._wait_ACK(0.2):
                print("[成功] 收到确认")
                return True
            
            # 重发机制
            for attempt in range(repeat):
                print(f"[重发 {attempt + 1}/{repeat}] 未收到确认，重新发送")
                try:
                    self.ser.write(package)
                    print(f"[重发] {package.hex()}")
                except Exception as e:
                    print(f"[错误] 重发数据失败: {e}")
                    continue
                
                if self._wait_ACK(0.2):
                    print("[成功] 重发后收到确认")
                    return True
            # 所有重发都失败
            print(f"[失败] 经过 {repeat} 次重发仍未收到确认")
            return False  
        except Exception as e:
            print(f"[错误] 发送过程中发生异常: {e}")
            return False
        finally:
            # 确保在任何情况下都重置发送状态
            self.is_sending_package = False
            
##################
# 分药相关控制命令 #
##################
    def send_pill_matrix(self, pill_matrix):
        """
        发送分药命令和药片矩阵到分药机
        """
        assert isinstance(pill_matrix, np.ndarray), "pill_matrix must be a numpy array"
        self.total_pill = np.sum(pill_matrix) # 更新剩余药片数量
        self.pill_remain = self.total_pill # 初始化剩余药片数量
        cmd = b'\x05'
        data = pill_matrix.tobytes()
        ack = self._send_package(cmd + data, self.repeat)
        if(ack):
            self.machine_state = 1 # 分药机正在工作    
            return True
        else:
            print("[error] Send pill matrix fail, Dispenser doesn't respond")
            return False
    
    def open_tray(self):
        """
        发送打开舱门命令到分药机
        阻塞式
        err_code:
            0: 正常
            1：超时未响应
        """
        cmd = b'\x03'
        if(not self._send_package(cmd, self.repeat)):
            return 1
        self.is_tray_opened = True
        return 0
        
    def close_tray(self):
        """
        发送关闭舱门命令到分药机
        阻塞式
        err_code:
            0: 正常
            1：超时未响应
        """
        cmd = b'\x04'
        if(not self._send_package(cmd, self.repeat)):
            return 1
        self.is_tray_opened = False
        return 0
    
    def pause_dispenser(self):
        """
        发送暂停命令到分药机
        阻塞式
        err_code:
            0: 正常
            1：超时未响应
        """
        cmd = b'\x01'
        if(not self._send_package(cmd, self.repeat)):
            return 1
        self.machine_state = 2
        return 0
    
    def reset_dispenser(self):
        """
        机器初始化，清理药盘
        阻塞式
        err_code:
            0: 正常
            1：超时未响应
        """
        cmd = b'\x00'
        if(not self._send_package(cmd, self.repeat)):
            return 1
        DONE = self._wait_DONE(10)
        time.sleep(6)  # 额外等待6秒确保物理操作完成
        print("[操作] 分药机已重置")
        if(DONE):
            self.DONE = False
            self.machine_state = 0
            return 0
        return 1
    
#################
# 设置分药机参数 #
#################

    #转盘电机速度设置 speed(float32)
    def set_turnMotor_speed(self, speed): 
        cmd = b'\x08'
        id = b'\x00'
        if(not self._send_package(cmd + id + struct.pack('<f',speed), self.repeat)):
            return 1
        return 0
    
    #传送带电机速度设置 speed(float32)
    def set_servo_angle(self, angle): 
        cmd = b'\x08'
        id = b'\x01'    
        if(not self._send_package(cmd + id + struct.pack('<f',angle), self.repeat)):
            return 1
        return 0
    
    #上光耦阈值设置 thresh(float32)
    def set_upperOptocoupler_thresh(self, thresh): 
        cmd = b'\x06'
        id = b'\x00'
        if(not self._send_package(cmd + id + struct.pack('<f',thresh), self.repeat)):
            return 1
        return 0
    
    #下光耦阈值设置 thresh(float32)
    def set_lowerOptocoupler_thresh(self, thresh): 
        cmd = b'\x06'
        id = b'\x01'
        if(not self._send_package(cmd + id + struct.pack('<f',thresh), self.repeat)):
            return 1
        return 0
    
    #上光耦脉冲不应期设置 noresp(uint32)
    def set_upperOptocoupler_noresp(self, noresp): 
        cmd = b'\x07'
        id = b'\x00'
        if(not self._send_package(cmd + id + noresp.to_bytes(4, 'little'), self.repeat)):
            return 1
        return 0
    
    #下光耦脉冲不应期设置 noresp(uint32)
    def set_lowerOptocoupler_noresp(self, noresp): 
        cmd = b'\x07'
        id = b'\x01'
        if(not self._send_package(cmd + id + noresp.to_bytes(4, 'little'), self.repeat)):
            return 1
        return 0
    
    #转盘电机刹车延时设置 delay_stop(uint32) ms
    def set_turnMotor_delay_stop(self, delay_stop): 
        cmd = b'\x09'
        id = b'\x00'
        if(not self._send_package(cmd + id + delay_stop.to_bytes(4, 'little'), self.repeat)):
            return 1
        return 0

    #清零速度设置 clean_speed(float32)
    def set_clean_speed(self, speed): 
        cmd = b'\x0B'
        if(not self._send_package(cmd + struct.pack('<f',speed), self.repeat)):
            return 1
        return 0
    
    #清零时间设置 clean_delay(uint32) ms
    def set_clean_delay(self, delay): 
        cmd = b'\x0C'
        if(not self._send_package(cmd + delay.to_bytes(4, 'little'), self.repeat)):
            return 1
        return 0



