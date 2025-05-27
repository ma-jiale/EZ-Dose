import numpy as np
from PyQt5 import QtCore
import time

class DataFetcher(QtCore.QObject):
    data_ready = QtCore.pyqtSignal(dict, list, list)
    pill_count_detected = QtCore.pyqtSignal(int)
   
    def __init__(self):
        super().__init__()
        self.patient_info = {
            "patient_portrait": "pic/potrait.png",
            "patient_name": "无信息",
            "perception_number": "无信息"
        }
        self.pill_list = []
        self.pill_matrix = []
        self.is_moving_servo = False
        self.first_move_done = False
        
    def on_thread(self):
        # 移除重复的Timer初始化，因为已经在__init__中完成
        pass
    
    @QtCore.pyqtSlot(str)
    def fetch_info(self, rfid):
        print(f'datafetcher: current thread ID: {id(self.thread())}')
        print(f'fetch_info: Current thread ID: {id(QtCore.QThread.currentThread())}')
        # 从服务器获取患者信息和药物列表
        if rfid == "-1":
            self.set_data_default()
        else:
            # 在这里从服务器获取信息
            # 现在使用默认信息
            default = []
            default.append([])
            
            default.append([{"name_cn": "阿司匹林", "name_en": "Aspirin", "number": 7, "pic": "pic/pill_pic/aspirin.png"},
                {"name_cn": "六味地黄丸", "name_en": "Liuwei Dihuang pills", "number": 7, "pic": "pic/pill_pic/test1.png"}])
            #default.append([
                #{"name_cn": "阿司匹林", "name_en": "Aspirin", "number": 10, "pic": "pic/pill_pic/aspirin.png"},
                #{"name_cn": "六味地黄丸", "name_en": "Liuwei Dihuang pills", "number": 15, "pic": "pic/pill_pic/test1.png"},
                #{"name_cn": "抗生素", "name_en": "Antibiotic", "number": 20, "pic": "pic/pill_pic/test2.png"},
                #{"name_cn": "氯芬黄敏片", "name_en": "Chlorfenac Huangmin tablet", "number": 11, "pic": ""}
            #])

            self.patient_info = {
                "patient_portrait": "pic/potrait.png",
                "patient_name": "测试者",
                "perception_number": rfid
            }
            if rfid == "1111222277774028C8262982":
                self.pill_list = default[1]  # 包含 2 种药品的列表           
            # elif rfid == "6666888800004028C8268982": 
            #     self.pill_list = default[2]  # 包含 4 种药品的列表           
            else: 
                self.pill_list = default[0] # default分配
            # 这里判断从服务器传回的数据是否正常，正常将继续处理数据
            if not self.patient_info or not self.pill_list:
                self.set_data_default()
            else:
                self.pill_list_formatter()
        # 发射信号，传递数据
          
        self.data_ready.emit(self.patient_info, self.pill_list, self.pill_matrix)
        print("数据准备")
        # 若pill_list不为空，则开启摄像头
        # if self.pill_list:
        #     self.cap_on()

    def pill_list_formatter(self):
        # 根据self.pill_list生成为self.pill_matrix
        
        # 这里暂时使用默认格式
        for i in range(self.pill_list.__len__()):
            matrix = np.zeros((4, 7), dtype=np.byte)
            for r in range(4):
                for c in range(7):
                    if r == 1 :
                        matrix[r][c] = 1
            self.pill_matrix.append(matrix)
    
    def set_data_default(self):
        self.patient_info = {
            "patient_portrait": "pic/potrait.png",
            "patient_name": "无信息",
            "perception_number": "无信息"
        }
        self.pill_list = []

    def cap_on(self):
        # 启动摄像头检测
        # if not hasattr(self, 'cap') or not self.cap.isOpened():
        #      self.cap = cv2.VideoCapture(0)
        
        print("摄像头开启")
        print(f'cap_on: Current thread ID: {id(QtCore.QThread.currentThread())}')
        # print(f'QTimer in thread: {id(self.cap_timer.thread())}')
        # self.cap_timer.start(200)  # 更新为更高的帧率
        print("定时器启动")
        
    @QtCore.pyqtSlot()
    def cap_off(self):
        # 停止摄像头检测
        # if hasattr(self, 'cap') and self.cap.isOpened():
        #     self.cap.release()
        # cv2.destroyAllWindows()
        print("摄像头关闭")
        # print(f"Stopping QTimer in thread: {id(self.cap_timer.thread())}")
        # self.cap_timer.stop()