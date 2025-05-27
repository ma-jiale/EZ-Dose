import cv2
import numpy as np
import random
from PyQt5 import QtCore

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

    def on_thread(self):
        self.cap_timer = QtCore.QTimer(self)
        self.cap_timer.timeout.connect(self.detect_pill_count)

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
            default.append([{"name_cn": "阿司匹林", "name_en": "Aspirin", "number": 10, "pic": "pic/pill_pic/aspirin.png"},
                {"name_cn": "六味地黄丸", "name_en": "Liuwei Dihuang pills", "number": 15, "pic": "pic/pill_pic/test1.png"}])
            default.append([
                {"name_cn": "阿司匹林", "name_en": "Aspirin", "number": 10, "pic": "pic/pill_pic/aspirin.png"},
                {"name_cn": "六味地黄丸", "name_en": "Liuwei Dihuang pills", "number": 15, "pic": "pic/pill_pic/test1.png"},
                {"name_cn": "抗生素", "name_en": "Antibiotic", "number": 20, "pic": "pic/pill_pic/test2.png"},
                {"name_cn": "氯芬黄敏片", "name_en": "Chlorfenac Huangmin tablet", "number": 11, "pic": ""}
            ])

            self.patient_info = {
                "patient_portrait": "pic/potrait.png",
                "patient_name": "测试者",
                "perception_number": rfid
            }
            self.pill_list = random.choice(default)
            # 这里判断从服务器传回的数据是否正常，正常将继续处理数据
            if not self.patient_info or not self.pill_list:
                self.set_data_default()
            else:
                self.pill_list_formatter()
        # 发射信号，传递数据
        self.data_ready.emit(self.patient_info, self.pill_list, self.pill_matrix)

        # 若pill_list不为空，则开启摄像头
        if self.pill_list:
            self.cap_on()

    def pill_list_formatter(self):
        # 根据self.pill_list生成为self.pill_matrix
        
        # 这里暂时使用默认格式
        for i in range(self.pill_list.__len__()):
            matrix = np.zeros((4, 7), dtype=int)
            for r in range(4):
                for c in range(7):
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
        #     self.cap = cv2.VideoCapture(0)
        print("摄像头开启")
        print(f'cap_on: Current thread ID: {id(QtCore.QThread.currentThread())}')
        print(f'QTimer in thread: {id(self.cap_timer.thread())}')
        self.cap_timer.start(1000)
    
    def detect_pill_count(self):
        # 检测当前帧的药片数量
        # ret, frame = self.cap.read()
        # if ret:
        #     gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        #     gray = cv2.GaussianBlur(gray, (5, 5), 0)
        #     thresh = cv2.threshold(gray, 127, 255, cv2.THRESH_BINARY)[1]
        #     contours, _ = cv2.findContours(thresh, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        #     pill_count = len(contours)
            
        #     # 发射信号更新药片数量
        #     self.pill_count_detected.emit(pill_count)
        num = random.randint(0, 99)
        print(f'摄像头检测到当前药片数量：{num}')
        self.pill_count_detected.emit(num)
    
    @QtCore.pyqtSlot()
    def cap_off(self):
        # 停止摄像头检测
        if hasattr(self, 'cap') and self.cap.isOpened():
            self.cap.release()
        cv2.destroyAllWindows()
        print("摄像头关闭")
        print(f"Stopping QTimer in thread: {id(self.cap_timer.thread())}")
        self.cap_timer.stop()

