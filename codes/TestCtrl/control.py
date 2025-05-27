import serial
import numpy as np
import threading
from subprocess import run
# from get_http import Client
import time
import struct

class Dispenser:
    def __init__(self, port):
        self.port = port
        # 打开串口
        try:
            self.ser = serial.Serial(self.port, 115200)
            self.ser.close()
            self.ser.open()
            self.ser.flushInput()
        except Exception:
            self.ser = None
            print("[错误] 无法打开串口，请检查连接")
            return

        #分药机相关参数
        self.timeout = 5 * 60 # 分药超时时间(s)
        self.rfid = -1 # RFID信息, -1代表无
        self.cam_state = 0 # 摄像头状态 0 关 1 开
        self.state = 0 # 机器状态 0：空闲; 1: 正在工作; 2: 暂停; 3.分药结束;
        self.err_code = 0 # 错误信息 0：没有问题；1：超时异常；2：分药计数异常; 
        self.plate_state = 0 #药盘状态 0 关 1 开
        self.pill_remain = -1 #剩余药片数量
        self.pillList = None
        self.recv_flag = False # 接收线程标志位
        self.send_start = False
        self.ACK = False
        self.DONE = False
        self.repeat = 5

    def start_dispenser_feedback_handler(self):
        """
        Start the serial receiver thread to handle feedback from the dispenser.
        """
        if self.recv_flag:
            print("[警告] 接收线程已经在运行中")
            return
        self.recv_flag = True
        self.serial_thread = threading.Thread(target=handle_dispenser_feedback, kwargs={"dispenser": self})
        self.serial_thread.daemon = True  # 设置为守护线程，主线程结束时自动结束
        self.serial_thread.start()
        print("[启动] 串口接收线程已启动，现在可以看到所有串口消息")

    def stop_dispenser_feedback_handler(self):
        """
        Stop the serial receiver thread and close the serial connection.
        This method ensures clean shutdown of all communication resources.
        """
        # First stop the receiving thread by setting the flag to False
        self.recv_flag = False

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
                self.serial_thread.join(timeout=1.0)  # Wait up to 1 second for thread to end
                if self.serial_thread.is_alive():
                    print("[警告] 接收线程无法正常结束")
            except Exception as e:
                print(f"[错误] 停止接收线程时发生异常: {e}")
                  
        # Reset state variables
        self.ACK = False
        self.DONE = False
        self.send_start = False
        
        print("[关闭] 分药机通信已停止")
    

    def wait_ACK(self, timeout):
        t0 = time.time()
        while(time.time() - t0 < timeout and not self.ACK):
            pass
        return self.ACK
    
    def wait_DONE(self, timeout):
        t0 = time.time()
        while(time.time() - t0 < timeout and not self.DONE):
            pass
        return self.DONE
    
    def send_package(self, data, repeat): # data: bytes
        if(self.send_start):
            print("[警告] 上一个发送尚未完成")
            return False
        self.send_start = True
        crc = 0
        for i in range(len(data)):
            crc = crc + data[i]
        self.ACK = False
        package = b'\xaa\xbb' + data + crc.to_bytes(2, 'little')
        self.ser.write(package)
        print(f"[发送] {package}")
        # 超时重发
        for i in range(repeat):
            if(not self.wait_ACK(0.2)):
                self.ser.write(package)
                print(f"[重发 {i+1}/{repeat}] {package}")
            else:
                print("[成功] 收到确认")
                break

        self.send_start = False
        return self.ACK
        
    def send_data(self, pillList): # pillList: array[4][7] dtype=np.byte
        if(self.ser == None):
            return 0, 0
        
        cmd = b'\x05'
        data = pillList.tobytes()
        ack = self.send_package(cmd + data, self.repeat)
        if(ack):
            self.state = 1     
        return ack
    
###########
# 控制命令 #
###########

    #发处方
    # 该函数是阻塞式的，在分药结束后会返回err_code，多余的药量
    # err_code：
    #   0: 正常分药结束
    #   1：超时未响应
    # 每次发一种药的分药方案，示例：第0行的药
    def send_pillList(self, df_line):
        self.pillList = df_line
        if(self.send_data(df2np(df_line))):
            self.state = 1
            t0 = time.time()
            while(time.time() - t0 < self.timeout and (self.state != 3 or self.pill_remain == -1)):
                pass
            if(self.state != 3 or self.pill_remain == -1): #分药超时未回复
                return 1, 0
            return self.err_code, self.pill_remain
            

    #获取rfid
    #返回RFID和err_code
    #RFID=-1则没检测到RFID
    # err_code：
    #   0: 正常分药结束
    #   1：超时未响应
    def get_rfid(self):
        cmd = b'\x02'
        if(not self.send_package(cmd, self.repeat)):
            return -1
        t0 = time.time()
        while(time.time() - t0 < 1 and not self.DONE):
            pass
        err_code = 0
        if(not self.DONE):
            err_code = 1
        self.DONE = False
        return self.rfid, err_code
    
    #开药盘
    # 阻塞式
    # err_code:
    #   0: 正常
    #   1：超时未响应
    def open_plate(self):
        cmd = b'\x03'
        if(not self.send_package(cmd, self.repeat)):
            return 1
        self.plate_state = 1
        return 0
        
    #关药盘
    # err_code:
    #   0: 正常
    #   1：超时未响应
    def close_plate(self):
        cmd = b'\x04'
        if(not self.send_package(cmd, self.repeat)):
            return 1
        self.plate_state = 0
        return 0
    
    #暂停
    # 阻塞式
    # err_code:
    #   0: 正常
    #   1：超时未响应
    def pause(self): #停止所有的电机
        cmd = b'\x01'
        if(not self.send_package(cmd, self.repeat)):
            return 1
        self.state = 2
        return 0
    
    #机器重新初始化，清理药盘
    # 阻塞式
    # err_code:
    #   0: 正常
    #   1：超时未响应
    def init(self):
        cmd = b'\x00'
        if(not self.send_package(cmd, self.repeat)):
            return 1
        DONE = self.wait_DONE(10)
        if(DONE):
            self.DONE = False
            self.state = 0
            return 0
        return 1

    #重新分药
    # 阻塞式, 返回err_code，多余的药量
    # err_code:
    #   0: 正常
    #   1：超时未响应
    def resume(self): #重新分药，就是按照原来的分药方案
        self.init()
        return self.send_pillList(self.pillList)
    
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
    while(dispenser.recv_flag):
        if(dispenser.ser == None):
            continue
        try:
            org = dispenser.ser.readline().decode().strip() # 读取一行数据
            if org:
                print(f"串口接收 >>> {org}")
            name = None
            val = None
            read_flag = False
            attr_list = org.split(":")  # 分割字符串，获取属性名和属性值
            if(len(attr_list) >= 1):
                name = attr_list[0]
            if(len(attr_list) >= 2):
                val = attr_list[1]

            if(name == "rfid"):
                dispenser.rfid = int(val)
                dispenser.DONE = True
                read_flag = True
                print(f"[RFID读取] 当前RFID: {dispenser.rfid}")
            elif(name == "machine_state"):
                if(val == 'FINISH'):
                    dispenser.state = 3
                    dispenser.err_code = 0
                    print("[状态更新] 分药完成")
                elif(val == 'CNT_ERR'):
                    dispenser.state = 3
                    dispenser.err_code = 2
                    print("[状态更新] 计数错误")
            elif(name == "pill_remain"):
                dispenser.pill_remain = int(val)
                print(f"[药片剩余] 当前剩余药片数量: {dispenser.pill_remain}")
                read_flag = True
            elif(name == "ACK"):
                dispenser.ACK = True
                print("[通信确认] 收到ACK")
            elif(name == "DONE"):
                dispenser.DONE = True
                print("[操作完成] 收到DONE")
            #接收数据后回发ACK
            if(read_flag):
                dispenser.send_package(b'\x0A', 0)
        except Exception as e:
            print(f"[错误] 串口接收处理异常: {e}")
   
# #开屏幕
# def open_display():
#     run('vcgencmd display_power 1', shell=True)
# #关屏幕
# def close_display():
#     run('vcgencmd display_power 0', shell=True)


# def dfbyRFID(df, rfid):
#     return df.loc[df.patient==rfid]

# def df2np(data): #pandas df单行转4x7矩阵
#     pillList = np.zeros([4,7], dtype=np.byte)
#     if(data['morning_dosage']):
#         pillList[0,:] = 1
#     if(data['noon_dosage']):
#         pillList[1,:] = 1
#     if(data['evening_dosage']):
#         pillList[2,:] = 1
#     if(data['night_dosage']):
#         pillList[3,:] = 1
#     return pillList



if __name__ == '__main__':
    # upload_url = 'http://202.120.61.207/upload'
    # api_endpoint = 'http://202.120.61.207/UserList'
    # username = 'wkk'#根据不同的用户进行修改
    # image_file_path = '/home/wkk/wkk/django/MedicineServer/training_process_plot.png' #修改图片路径
    # client = Client(username)
    # df_total = client.get_data(api_endpoint)
    # print(df_total)
    # df = dfbyRFID(df_total, 1) #根据RFID查找数据
    # print(df)
    # print(df.shape[0])

    my_dispenser = Dispenser("COM6") # 根据实际串口修改
    my_dispenser.start_dispenser_feedback_handler()
    my_dispenser.init()

    # 创建测试分药矩阵
    pillList = np.zeros([4, 7], dtype=np.byte)
    for r in range(4):
        if r % 2 != 0:
            for c in range(7):
                if c % 2 == 0:
                    pillList[r][c] = 1
        if r % 2 == 0:
            for c in range(7):
                if c % 2 != 0:
                    pillList[r][c] = 1
    # print(pillList)
    return_msg = my_dispenser.send_data(pillList)
    print("开始分药")
    my_dispenser.state = 1
    t0 = time.time()
    last_pill_remain = -1
    while(time.time() - t0 < my_dispenser.timeout and (my_dispenser.state != 3 or my_dispenser.pill_remain == -1)):
        # 每当pill_remain发生变化时打印
        if my_dispenser.pill_remain != last_pill_remain and my_dispenser.pill_remain != -1:
            print(f"[实时监控] 剩余药片数量: {my_dispenser.pill_remain}")
            last_pill_remain = my_dispenser.pill_remain
        time.sleep(0.1)  # 短暂休眠，减少CPU占用
    if(my_dispenser.state != 3 or my_dispenser.pill_remain == -1): #分药超时未回复
        print("[ERR]:分药超时未回复")
    else:
        print(f"分药结束，err_code:{my_dispenser.err_code} pill_remain:{my_dispenser.pill_remain}")
    # control.send_pillList(df.loc[2])
    my_dispenser.stop_dispenser_feedback_handler()


