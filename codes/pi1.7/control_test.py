import numpy as np
from PyQt5 import QtCore
from control import *
from rfid_reader import get_rfid

control = Control("/dev/ttyUSB0")
control.repeat = 0


class Control_thread(QtCore.QObject):
    rfid_ready = QtCore.pyqtSignal(str) # rfid读取完成的信号
    distributed = QtCore.pyqtSignal() # 分发完成的信号
    progress_distributed = QtCore.pyqtSignal(int) # 更新已分发进度的信号

    def __init__(self, port):
        super().__init__()
        print(port)
        self.current_progress = 0 # 当前分发药物


    @QtCore.pyqtSlot()
    def on_thread(self):
        self.timer = QtCore.QTimer(self)
        self.timer.timeout.connect(self.update_progress_distributed)
        control.start()
        control.init()
        self.open_plate()
        
  

    @QtCore.pyqtSlot(object)
    def distribute(self, single_pill_matrix):
        
        print("开始分药")
        a = control.send_data(single_pill_matrix)
        print(a)

        print(single_pill_matrix)
        if not self.timer.isActive():
            self.timer.start(500)
        control.state = 1
        print(control.state)

        #if control.state == 3:
        #    self.end_distribute()
        #control.stop()
        # 延迟5s结束分发
        #QtCore.QTimer.singleShot(5000, lambda: self.end_distribute()) # 发送分发完成信号

    def end_distribute(self):
        self.distributed.emit() # 发送分发完成信号
        self.timer.stop() # 停止计时器
        self.current_progress = 0 # 重置已分发进度

    @QtCore.pyqtSlot()
    def open_plate(self):
        control.open_plate()
        print("药舱已打开")

    @QtCore.pyqtSlot()
    def close_plate(self):
        control.close_plate()
        print("药舱已关闭")

    @QtCore.pyqtSlot()
    def get_rfid(self):
        print("开始读取RFID")
        print(f'get_rfid: current thread: {id(QtCore.QThread.currentThread())}')

        try:
            # 调用外部库读取 RFID 数据
            result = get_rfid()
            print(f"get_rfid 返回值: {result}")

            epc_hex = result.get("epc", None)
            if result.get("error_code", -1) == 0 and epc_hex:
            #epc_int = int(epc_hex, 16)  # 转换为整数
                epc_str = str(epc_hex)  # 将整数转换为字符串
                print(f"EPC 转换为HEX: {epc_hex}")

            else:
                epc_str = "-1"  # 错误情况下返回 -1
                print("读取失败或无有效 EPC")
        except (ValueError, KeyError) as e:
            print(f"读取 RFID 失败: {e}")
            epc_str = "-1"

        # 发送信号，将 EPC 字符串传递给主线程
        self.rfid_ready.emit(epc_str)

        # 延迟1s发送信号
        # QtCore.QTimer.singleShot(1000, lambda: self.rfid_ready.emit('1234567890')) # 发送RFID读取完成信号
    
    def update_progress_distributed(self):
        '''每隔一定时间更新已分发药物数量，现在是模拟定时增加'''
        #increment = np.random.randint(1, 2)
        #self.current_progress += increment
        self.progress_distributed.emit(self.current_progress)
        print(f'已分发药物数量: {self.current_progress}')
        print(f'机器状态: {control.state}')
        if control.state == 3:
            self.end_distribute()
