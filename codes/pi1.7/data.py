import cv2
import numpy as np
from PyQt5 import QtCore
import time

def mse(imageA, imageB):
    # 计算均方误差 (MSE)
    err = np.sum((imageA.astype("float") - imageB.astype("float")) ** 2)
    err /= float(imageA.shape[0] * imageA.shape[1])
    return err

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
        
        # 初始化摄像头相关变量
        self.cap_timer = QtCore.QTimer(self)
        self.cap_timer.timeout.connect(self.detect_pill_count)
        
        # 药片识别相关变量
        self.background = None
        self.prev_frame = None
        self.stable_count = 0
        self.threshold_stable = 3  # 连续3帧保持稳定
        self.last_stable_time = 0
        self.pill_count = 0
        
        # 设置ROI区域
        self.roi_top_left = (20, 20)
        self.roi_bottom_right = (1260, 700)
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
        if self.pill_list:
            self.cap_on()

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
        if not hasattr(self, 'cap') or not self.cap.isOpened():
             self.cap = cv2.VideoCapture(0)
             
             # 设置图像尺寸
             self.cap.set(cv2.CAP_PROP_FRAME_WIDTH, 1280)
             self.cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 720)
        
        print("摄像头开启")
        print(f'cap_on: Current thread ID: {id(QtCore.QThread.currentThread())}')
        print(f'QTimer in thread: {id(self.cap_timer.thread())}')
        self.cap_timer.start(200)  # 更新为更高的帧率
        print("定时器启动")
        
    def detect_pill_count(self):
        # 读取摄像头图像
        ret, frame = self.cap.read()
        if not ret:
            return

        # 获取当前时间
        current_time = time.time()
        
        # 提取ROI区域
        roi = frame[self.roi_top_left[1]:self.roi_bottom_right[1], 
                   self.roi_top_left[0]:self.roi_bottom_right[0]]

        # 1. 图像稳定性检查
        gray = cv2.cvtColor(roi, cv2.COLOR_BGR2GRAY)
        if self.prev_frame is not None:
            # 计算 MSE
            MES_score = mse(gray, self.prev_frame)

            # 如果差异较小，认为图像稳定
            if MES_score < 20:  # 这个阈值可以根据实际情况调整
                self.stable_count += 1
            else:
                self.stable_count = 0

        # 2. 判断图像是否稳定
        if self.stable_count >= self.threshold_stable:
            # 应用canny边缘检测
            edges = cv2.Canny(gray, 50, 150)
            
            # 3. 如果ROI中没有边缘，将当前图像加入背景
            if np.count_nonzero(edges) == 0:
                self.background = gray
                print("No edges detected, updating background...")
                self.last_stable_time = current_time
                pill_count = 0
                print(f"药片数量: {pill_count}")
                self.pill_count_detected.emit(pill_count)
                    
            else:
                # 4. 如果有边缘且有背景图，执行图像相减操作
                if self.background is not None:
                    subtracted_img = cv2.subtract(self.background, gray)

                    # 5. 去噪和阈值化
                    burred = cv2.GaussianBlur(subtracted_img, (5, 5), 0)

                    # 6. 转换为灰度图像并进行二值化
                    _, thresh = cv2.threshold(burred, 20, 255, cv2.THRESH_BINARY)

                    # 7. 腐蚀和膨胀处理（去噪）
                    kernel = np.ones((5, 5), np.uint8)
                    erosion = cv2.erode(thresh, kernel, iterations=2)
                    dilation = cv2.dilate(erosion, kernel, iterations=2)

                    # 8. 查找轮廓
                    contours, _ = cv2.findContours(dilation, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)

                    min_area = 100  # 设置最小面积阈值，过滤掉小的噪声轮廓

                    # 9. 轮廓特征提取和计数
                    areas = []
                    normal_contours = 0  # 用于统计没有凹凸缺陷的轮廓数量
                    abnormal_contours = 0  # 用于统计异常药片数
                    abnormal_area_threshold = 1.5  # 用于判断异常药片的面积比例
                    area_mode = 0  # 计数依据
                    sum_area = 0

                    # 过滤出没有凹凸缺陷的轮廓
                    for cnt in contours:
                        area = cv2.contourArea(cnt)
                        perimeter = cv2.arcLength(cnt, True)

                        # 使用多边形逼近
                        epsilon = 0.02 * perimeter  # 逼近精度
                        approx = cv2.approxPolyDP(cnt, epsilon, True)  # 简化轮廓为多边形

                        # 如果轮廓面积小于最小阈值，跳过该轮廓
                        if area < min_area:
                            continue  # 跳过当前轮廓

                        convex_hull = cv2.isContourConvex(approx)
                        areas.append(area)
            
                        if convex_hull:  # 如果轮廓没有凹凸缺陷
                            sum_area += area  # 计算面积总和
                            normal_contours += 1
                            if normal_contours > 0:
                                area_mode = sum_area / normal_contours  # 使用平均数作为计数依据

                        else:  # 如果轮廓有凹凸缺陷
                            # 判断面积是否小于计数依据的1.5倍
                            if area_mode > 0 and area < area_mode * abnormal_area_threshold:
                                abnormal_contours += 1

                            else:
                                # 如果面积大于计数依据的1.5倍，逐步递增除数
                                divisor = 2
                                max_divisor = 10  # 最大除数值，避免无限递增
                                while area_mode > 0 and abs((area / divisor) - area_mode) > area_mode * 0.25 and divisor < max_divisor:
                                    divisor += 1
                                # 此时的除数代表一个新的计数依据
                                normal_contours += divisor  # 计数药片数
                    
                    # 发送药片数量信号
                    pill_count = normal_contours
                    print(f"药片数量: {pill_count}")
                    self.pill_count_detected.emit(pill_count)

        # 10. 更新上一帧图像
        self.prev_frame = gray

    @QtCore.pyqtSlot()
    def cap_off(self):
        # 停止摄像头检测
        if hasattr(self, 'cap') and self.cap.isOpened():
            self.cap.release()
        cv2.destroyAllWindows()
        print("摄像头关闭")
        print(f"Stopping QTimer in thread: {id(self.cap_timer.thread())}")
        self.cap_timer.stop()
        
        # 重置药片识别相关变量
        self.background = None
        self.prev_frame = None
        self.stable_count = 0
        
