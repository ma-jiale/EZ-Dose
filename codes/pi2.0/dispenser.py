import serial
import numpy as np
import threading
import time
import struct

class Dispenser:
    def __init__(self, port):
        """
        初始化分药机实例, 使用pyserial库打开指定的串口, 并设置分药机的相关参数。
        :param port: 串口名称，例如 'COM6' 或 '/dev/ttyUSB0'
        """
        # open serial port
        self.port = port
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
            self.ser = None
            print(f"[错误] 串口连接失败: {e}")
            print(f"[提示] 请检查设备是否连接到 {self.port} 或端口是否被占用")
            return
        except Exception as e:
            self.ser = None
            print(f"[错误] 初始化串口时发生未知错误: {e}")
            return

        # 设置分药机相关参数
        self.timeout = 5 * 60 # 分药超时时间(s)
        self.is_plate_opened = False
        self.is_receiver_thread_running = False # 接收串口信息线程是否在运行
        self.machine_state = 0 # 机器状态 0：空闲; 1: 正在工作; 2: 暂停工作; 3.完成工作;
        self.is_send_package_start = False # 是否正在发送数据包
        self.err_code = 0 # 错误信息 0：没有问题；1：超时异常；2：分药计数异常; 
        self.pill_remain = -1 #剩余药片数量
        self.total_pill = 0 # 一个药片矩阵总药片数量
        self.ACK = False
        self.DONE = False
        self.repeat = 5 # 重发指令次数

    def start_dispenser_feedback_handler(self):
        """
        Start the serial receiver thread to handle feedback from the dispenser.
        """
        if self.is_receiver_thread_running:
            print("[警告] 串口信息接收线程已经在运行中")
            return
        self.is_receiver_thread_running = True
        # 修改：使用 args 传递位置参数
        self.serial_thread = threading.Thread(target=handle_dispenser_feedback, args=(self,))
        self.serial_thread.daemon = True
        self.serial_thread.start()
        print("[启动] 串口信息接收线程已启动，现在可以看到所有串口消息")

    def stop_dispenser_feedback_handler(self):
        """
        Stop the serial receiver thread and close the serial connection.
        This method ensures clean shutdown of all communication resources.
        """
        # First stop the receiving thread by setting the flag to False
        self.is_receiver_thread_running = False

        # # Close the serial connection if it exists
        # if self.ser is not None:
        #     try:
        #         # Flush any remaining data before closing
        #         self.ser.flush()
        #         self.ser.close()
        #         print("[关闭] 串口连接已关闭")
        #     except Exception as e:
        #         print(f"[错误] 关闭串口连接时发生异常: {e}")

        # Wait for the thread to actually terminate if it exists
        if hasattr(self, 'serial_thread') and self.serial_thread.is_alive():
            try:
                self.serial_thread.join(timeout=2.0)  # 增加超时时间到2秒
                if self.serial_thread.is_alive():
                    print("[警告] 串口信息接收线程无法正常结束，可能存在阻塞")
                    print("[提示] 建议检查串口读取是否存在死锁")
                else:
                    print("[成功] 串口信息接收线程已正常结束")
            except Exception as e:
                print(f"[错误] 停止接收线程时发生异常: {e}")
        elif hasattr(self, 'serial_thread'):
            print("[信息] 串口接收线程已经停止")
        else:
            print("[信息] 串口接收线程尚未创建")
                  
        # Reset state variables
        self.ACK = False
        self.DONE = False
        self.is_send_package_start = False
    

    def wait_ACK(self, timeout):
        """
        等待分药机的ACK确认信号。
        :param timeout: 等待ACK的超时时间（秒）
        :return: 如果在超时时间内收到ACK，返回True，否则返回False。
        """
        t0 = time.time()
        while(time.time() - t0 < timeout and not self.ACK):
            pass
        return self.ACK
    
    def wait_DONE(self, timeout):
        """
        等待分药机的DONE信号，表示操作完成。
        :param timeout: 等待DONE的超时时间（秒）
        :return: 如果在超时时间内收到DONE，返回True，否则返回False。
        """
        t0 = time.time()
        while(time.time() - t0 < timeout and not self.DONE):
            pass
        return self.DONE
    
    def send_package(self, data, repeat):
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
        if self.is_send_package_start:
            print("[警告] 上一个发送尚未完成")
            return False
        
        # 参数验证
        if not isinstance(data, (bytes, bytearray)):
            print("[错误] 数据必须是字节类型")
            return False
        
        if repeat < 0:
            print("[错误] 重发次数不能为负数")
            return False
        
        self.is_send_package_start = True
        
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
            # 清除ACK状态
            self.ACK = False
            
            # 首次发送
            try:
                self.ser.write(package)
                print(f"[发送] {package.hex()}")
            except Exception as e:
                print(f"[错误] 发送数据失败: {e}")
                return False
            
            # 等待首次ACK
            if self.wait_ACK(0.2):
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
                
                if self.wait_ACK(0.2):
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
            self.is_send_package_start = False
            
###########
# 控制命令 #
###########
    def send_pill_matrix(self, pill_matrix):
        """
        发送分药命令和药品矩阵到分药机
        """
        assert isinstance(pill_matrix, np.ndarray), "pill_matrix must be a numpy array"
        self.total_pill = np.sum(pill_matrix) # 更新剩余药片数量
        self.pill_remain = self.total_pill # 初始化剩余药片数量
        cmd = b'\x05'
        data = pill_matrix.tobytes()
        ack = self.send_package(cmd + data, self.repeat)
        if(ack):
            self.machine_state = 1 # 分药机正在工作    
        return ack
    
    def open_plate(self):
        """
        发送打开药盘命令到分药机
        阻塞式
        err_code:
            0: 正常
            1：超时未响应
        """
        cmd = b'\x03'
        if(not self.send_package(cmd, self.repeat)):
            return 1
        self.is_plate_opened = True
        return 0
        
    def close_plate(self):
        """
        发送关闭药盘命令到分药机
        阻塞式
        err_code:
            0: 正常
            1：超时未响应
        """
        cmd = b'\x04'
        if(not self.send_package(cmd, self.repeat)):
            return 1
        self.is_plate_opened = False
        return 0
    
    def pause(self):
        """
        发送暂停分药命令到分药机
        阻塞式
        err_code:
            0: 正常
            1：超时未响应
        """
        cmd = b'\x01'
        if(not self.send_package(cmd, self.repeat)):
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
        if(not self.send_package(cmd, self.repeat)):
            return 1
        DONE = self.wait_DONE(10)
        time.sleep(9)  # 额外等待3秒确保物理操作完成
        print("[操作] 分药机已重置")
        if(DONE):
            self.DONE = False
            self.machine_state = 0
            return 0
        return 1

    def resume(self): #重新分药，就是按照原来的分药方案
        """
        发送恢复分药命令到分药机
        阻塞式
        """
        self.reset_dispenser()
        # return self.send_pillList(self.pillList)
    
#################
# 设置分药机参数 #
#################

    #转盘电机速度设置 speed(float32)
    def set_turnMotor_speed(self, speed): 
        cmd = b'\x08'
        id = b'\x00'
        if(not self.send_package(cmd + id + struct.pack('<f',speed), self.repeat)):
            return 1
        return 0
    
    #传送带电机速度设置 speed(float32)
    def set_conveyorMotor_speed(self, speed): 
        cmd = b'\x08'
        id = b'\x01'    
        if(not self.send_package(cmd + id + struct.pack('<f',speed), self.repeat)):
            return 1
        return 0
    
    #上光耦阈值设置 thresh(float32)
    def set_upperOptocoupler_thresh(self, thresh): 
        cmd = b'\x06'
        id = b'\x00'
        if(not self.send_package(cmd + id + struct.pack('<f',thresh), self.repeat)):
            return 1
        return 0
    
    #下光耦阈值设置 thresh(float32)
    def set_lowerOptocoupler_thresh(self, thresh): 
        cmd = b'\x06'
        id = b'\x01'
        if(not self.send_package(cmd + id + struct.pack('<f',thresh), self.repeat)):
            return 1
        return 0
    
    #上光耦脉冲不应期设置 noresp(uint32)
    def set_upperOptocoupler_noresp(self, noresp): 
        cmd = b'\x07'
        id = b'\x00'
        if(not self.send_package(cmd + id + noresp.to_bytes(4, 'little'), self.repeat)):
            return 1
        return 0
    
    #下光耦脉冲不应期设置 noresp(uint32)
    def set_lowerOptocoupler_noresp(self, noresp): 
        cmd = b'\x07'
        id = b'\x01'
        if(not self.send_package(cmd + id + noresp.to_bytes(4, 'little'), self.repeat)):
            return 1
        return 0
    
    #转盘电机刹车延时设置 delay_stop(uint32) ms
    def set_turnMotor_delay_stop(self, delay_stop): 
        cmd = b'\x09'
        id = b'\x00'
        if(not self.send_package(cmd + id + delay_stop.to_bytes(4, 'little'), self.repeat)):
            return 1
        return 0

    #清零速度设置 clean_speed(float32)
    def set_clean_speed(self, speed): 
        cmd = b'\x0B'
        if(not self.send_package(cmd + struct.pack('<f',speed), self.repeat)):
            return 1
        return 0
    
    #清零时间设置 clean_delay(uint32) ms
    def set_clean_delay(self, delay): 
        cmd = b'\x0C'
        if(not self.send_package(cmd + delay.to_bytes(4, 'little'), self.repeat)):
            return 1
        return 0


def handle_dispenser_feedback(dispenser):
    """
    这个函数用来接收分药机下位机通过串口传输的消息并对分药机实例属性进行更新
    """
    while(dispenser.is_receiver_thread_running):
        if(dispenser.ser == None):
            continue
        try:
            org = dispenser.ser.readline().decode().strip() # 读取一行数据
            if org:
                print(f"串口接收 >>> {org}")
            name = None
            val = None
            is_receive_msg = False
            attr_list = org.split(":")  # 分割字符串，获取属性名和属性值
            if(len(attr_list) >= 1):
                name = attr_list[0]
            if(len(attr_list) >= 2):
                val = attr_list[1]

            if(name == "rfid"):
                dispenser.rfid = int(val)
                dispenser.DONE = True
                is_receive_msg = True
                print(f"[RFID读取] 当前RFID: {dispenser.rfid}")
            elif(name == "machine_state"):
                if(val == 'FINISH'):
                    dispenser.machine_state = 3
                    dispenser.err_code = 0
                    print("[状态更新] 分药完成")
                elif(val == 'CNT_ERR'):
                    dispenser.machine_state = 3
                    dispenser.err_code = 2
                    print("[状态更新] 计数错误")
            elif(name == "pills out"):
                dispenser.pill_remain = dispenser.total_pill- int(val)
            # elif(name == "pill_remain"):
            #     dispenser.pill_remain = int(val)
                print(f"[药片剩余] 当前剩余药片数量: {dispenser.pill_remain}")
                is_receive_msg = True
            elif(name == "ACK"):
                dispenser.ACK = True
                print("[通信确认] 收到ACK")
            elif(name == "DONE"):
                dispenser.DONE = True
                print("[操作完成] 收到DONE")
            #接收数据后回发ACK
            if(is_receive_msg):
                dispenser.send_package(b'\x0A', 0)
        except Exception as e:
            print(f"[错误] 串口接收处理异常: {e}")

if __name__ == '__main__':
    my_dispenser = Dispenser("COM6")  # 根据实际串口修改
    
    # 检查串口连接是否成功
    if my_dispenser.ser is None:
        print("[错误] 串口连接失败，程序退出")
        exit(1)
    
    # 启动接收线程
    my_dispenser.start_dispenser_feedback_handler()
    
    # 等待一段时间确保线程启动
    time.sleep(1)
    
    # 重置分药机
    print("[操作] 开始重置分药机...")
    reset_result = my_dispenser.reset_dispenser()
    print(f"[结果] 重置分药机结果: {reset_result}")
    
    # 等待重置完成
    time.sleep(2)
    
    # 创建测试分药矩阵
    pill_matrix = np.zeros([4, 7], dtype=np.byte)
    for r in range(4):
        if r % 2 != 0:
            for c in range(7):
                if c % 2 == 0:
                    pill_matrix[r][c] = 1
        if r % 2 == 0:
            for c in range(7):
                if c % 2 != 0:
                    pill_matrix[r][c] = 1
    
    print("[操作] 发送分药矩阵...")
    print(f"分药矩阵:\n{pill_matrix}")
    
    # 发送分药命令
    return_msg = my_dispenser.send_pill_matrix(pill_matrix)
    print(f"[结果] 发送分药命令结果: {return_msg}")
    
    # 等待分药过程完成或超时
    print("[等待] 分药过程进行中，等待完成...")
    timeout = 120  # 60秒超时
    start_time = time.time()
    
    while (time.time() - start_time < timeout and 
           my_dispenser.machine_state in [0, 1, 2]):  # 等待直到完成(3)或出错
        time.sleep(0.5)  # 每0.5秒检查一次状态
        print(f"[状态] 当前机器状态: {my_dispenser.machine_state}, 剩余药片: {my_dispenser.pill_remain}")
    
    # 输出最终结果
    print(f"[完成] 最终机器状态: {my_dispenser.machine_state}")
    print(f"[完成] 错误代码: {my_dispenser.err_code}")
    print(f"[完成] 剩余药片数: {my_dispenser.pill_remain}")
    
    # 等待一段时间查看更多消息
    print("[等待] 继续监听消息 5 秒...")
    time.sleep(5)
    
    # 停止接收线程
    print("[关闭] 停止消息接收线程...")
    my_dispenser.stop_dispenser_feedback_handler()


