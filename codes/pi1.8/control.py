import serial
import numpy as np
import threading
from subprocess import run
import time
import struct
from PyQt5 import QtCore, QtGui, QtWidgets


def serial_handle(control): #接收线程
    #global control

    while(control.recv_flag):
        if(control.ser == None):
            continue
        #读
        org = control.ser.readline().decode().strip()
        print(org)
        attr_list = org.split(":")
        name = None
        val = None
        if(len(attr_list) >= 1):
            name = attr_list[0]
        if(len(attr_list) >= 2):
            val = attr_list[1]

        read_flag = False
        if(name == "rfid"):
            control.rfid = int(val)
            control.DONE = True
            read_flag = True
        elif(name == "machine_state"):
            if(val == 'FINISH'):
                control.state = 3
                control.err_code = 0
            elif(val == 'CNT_ERR'):
                control.state = 3
                control.err_code = 2
        elif(name == "pill_remain"):
            control.pill_remain = int(val)
            read_flag = True
        elif(name == "ACK"):
            control.ACK = True
        elif(name == "DONE"):
            control.DONE = True


        #接收数据要回发ACK
        if(read_flag):
            control.send_package(b'\x0A', 0)

class Control:
    rfid_ready = QtCore.pyqtSignal(str) # 信号
    distributed = QtCore.pyqtSignal() # 信号

    @QtCore.pyqtSlot(np.ndarray)
    def start_distribute(self, pillList):
        print("开始分药流程...")
        self.pillList = pillList  # 保存药片矩阵
        ack = self.send_data(pillList)  # 发送分药指令
        if ack:
            print("分药开始")
            self.state = 1
            t0 = time.time()
            while time.time() - t0 < self.timeout and (self.state != 3 or self.pill_remain == -1):
                pass
            if self.state != 3 or self.pill_remain == -1:
                print("[ERR]: 分药超时未回复")
            else:
                print(f"分药结束，err_code: {self.err_code} pill_remain: {self.pill_remain}")
        else:
            print("[ERR]: send timeout")

    def __init__(self, port) -> None:
        self.port = port
        try:
            self.ser = serial.Serial(port, 115200)
            self.ser.close()
            self.ser.open()
            self.ser.flushInput()
        except Exception:
            self.ser = None
            print("serial open fail")

        #机器参数
        #分药超时时间(s)
        self.timeout = 5 * 60

        #RFID信息
        self.rfid = -1 #-1代表无
        #唤醒开关 0 关 1 开
        self.cam_state = 0
        #机器状态 0：空闲; 1: 正在工作; 2: 暂停; 3.分药结束;
        self.state = 0
        #错误信息 0：没有问题；1：超时异常；2：分药计数异常; 
        self.err_code = 0
        #药盘状态 0 关 1 开
        self.plate_state = 0
        #剩余药片
        self.pill_remain = -1

        self.pillList = None

        self.recv_flag = False
        self.send_start = False
        self.ACK = False
        self.DONE = False
        self.repeat = 5

    def start(self): #程序开始
        self.recv_flag = True
        #接收线程启动
        self.serial_thread = threading.Thread(target=serial_handle, kwargs={"control": self})
        self.serial_thread.start()

    def stop(self): #程序终止
        if(self.ser != None):
            self.ser.close()
        self.recv_flag = False
    

    def wait_ACK(self, timeout):
        t0 = time.time()
        while(time.time() - t0 < timeout and not self.ACK):
            pass
        return self.ACK
    
    def wait_DONE(self, timeout):
        t0 = time.time()
        while(time.time() - t0 < timeout and not self.DONE):
            pass
        return self.ACK
    def send_package(self, data, repeat): #data: bytes
        
        if(self.send_start):
            return False
        self.send_start = True
        crc = 0
        for i in range(len(data)):
            crc = crc + data[i]

        self.ACK = False
        package = b'\xaa\xbb' + data + crc.to_bytes(2, 'little')
        self.ser.write(package)
        print(package)

        for i in range(repeat): #超时重发
            if(not self.wait_ACK(0.2)):
                self.ser.write(package)
                print(package)
            else:
                break

        self.send_start = False
        return self.ACK
        
    def send_data(self, pillList): #pillList: array[4][7] dtype=np.byte
        if(self.ser == None):
            return 0, 0

        cmd = b'\x05'
        data = pillList.tobytes()
        ack = self.send_package(cmd + data, self.repeat)
        if(ack):
            self.state = 1     
        return ack

    ######################### 命令系列 ##########################
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
    
    ############################# 设置参数系列 #############################

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
       
    
#开屏幕
def open_display():
    run('vcgencmd display_power 1', shell=True)
#关屏幕
def close_display():
    run('vcgencmd display_power 0', shell=True)


def dfbyRFID(df, rfid):
    return df.loc[df.patient==rfid]

def df2np(data): #pandas df单行转4x7矩阵
    pillList = np.zeros([4,7], dtype=np.byte)
    if(data['morning_dosage']):
        pillList[0,:] = 1
    if(data['noon_dosage']):
        pillList[1,:] = 1
    if(data['evening_dosage']):
        pillList[2,:] = 1
    if(data['night_dosage']):
        pillList[3,:] = 1
    return pillList



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

    control = Control("COM2")
    control.start()

    control.init()

    #默认初始化完毕
    #测试分药矩阵
    pillList = np.zeros([4, 7], dtype=np.byte)
    for r in range(4):
        for c in range(7):
            pillList[r][c] = 1
    if(control.send_data(pillList)):
        print("开始分药")
        control.state = 1
        t0 = time.time()
        while(time.time() - t0 < control.timeout and (control.state != 3 or control.pill_remain == -1)):
            pass
        if(control.state != 3 or control.pill_remain == -1): #分药超时未回复
            print("[ERR]:分药超时未回复")
        else:
            print(f"分药结束，err_code:{control.err_code} pill_remain:{control.pill_remain}")
    else:
        print("[ERR]:send timeout")
    #control.send_pillList(df.loc[2])
    control.stop()
                                        

