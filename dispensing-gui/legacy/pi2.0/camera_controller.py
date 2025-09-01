import cv2
import numpy as np
import time
import contextlib
import sys
from PySide6.QtCore import QObject, Signal, QTimer, QThread, Slot
from PySide6.QtGui import QImage, QPixmap

# 添加兼容性处理
try:
    from ultralytics import YOLO
    YOLO_AVAILABLE = True
except ImportError as e:
    print(f"[警告] YOLO导入失败: {e}")
    YOLO_AVAILABLE = False
    YOLO = None

class PillCounterResult:
    '''
    药片计数结果类，用于存储识别结果和相关信息
    '''
    def __init__(self):
        self.counts = {'roundPill': 0, 'longPill': 0, 'capsule': 0}
        self.last_frame = None  # 可选：存储最近一帧的图像
        self.timestamp = None   # 可选：存储最近一次更新时间

    def update(self, counts, last_frame=None, timestamp=None):
        self.counts = counts.copy()
        if last_frame is not None:
            self.last_frame = last_frame
        if timestamp is not None:
            self.timestamp = timestamp

    def get_count(self, pill_type):
        return self.counts.get(pill_type, 0)

    def get_total_count(self):
        return sum(self.counts.values())

    def get_last_frame(self):
        return self.last_frame

    def get_timestamp(self):
        return self.timestamp

    def write_to_txt(self, file_path='result.txt'):
        with open(file_path, 'a', encoding='utf-8') as f:
            f.write(f"roundPill: {self.counts['roundPill']} longPill: {self.counts['longPill']} capsule: {self.counts['capsule']} 总数: {self.get_total_count()} 时间戳: {self.timestamp}\n")

class DummyFile:
    """用于抑制YOLO输出的辅助类"""
    def write(self, x): pass
    def flush(self): pass

class CameraController(QObject):
    """摄像头控制器，运行在单独线程中，集成YOLO药片识别功能"""
    
    # 信号定义
    frame_ready_signal = Signal(QPixmap)  # 新帧准备好的信号
    camera_error_signal = Signal(str)     # 摄像头错误信号
    camera_connected_signal = Signal()    # 摄像头连接成功信号
    camera_disconnected_signal = Signal() # 摄像头断开连接信号
    
    # 新增YOLO相关信号
    pill_recognition_signal = Signal(dict)  # 药片识别结果信号 {type: count}
    recognition_error_signal = Signal(str)  # 识别错误信号
    annotated_frame_signal = Signal(QPixmap)  # 标注后的画面信号
    
    def __init__(self, camera_index=0, model_path='model/best0606.pt', conf_thres=0.65):
        super().__init__()
        self.camera_index = camera_index
        self.model_path = model_path
        self.conf_thres = conf_thres
        
        # 摄像头相关
        self.camera = None
        self.timer = None
        self.is_running = False
        self.frame_width = 640
        self.frame_height = 480
        
        # YOLO识别相关
        self.model = None
        self.recognition_enabled = True
        self.last_recognition_time = 0
        self.recognition_interval = 0.5  # 识别间隔(秒)
        self.current_frame = None
        self.annotated_frame = None
        self.recognition_result = PillCounterResult()
        self.consecutive_failures = 0
        self.max_consecutive_failures = 5
        
        # 药片类型定义
        self.pill_types = ['roundPill', 'longPill', 'capsule']
        
        # 检查YOLO是否可用
        self.yolo_available = YOLO_AVAILABLE

    @Slot()
    def initialize_camera(self):
        """初始化摄像头"""
        try:
            print(f"[摄像头] 尝试连接摄像头索引: {self.camera_index}")
            
            # 创建VideoCapture对象
            self.camera = cv2.VideoCapture(self.camera_index)
            
            if not self.camera.isOpened():
                # 尝试其他索引
                for i in range(3):
                    if i != self.camera_index:
                        print(f"[摄像头] 尝试备用索引: {i}")
                        self.camera = cv2.VideoCapture(i)
                        if self.camera.isOpened():
                            self.camera_index = i
                            break
                
                if not self.camera.isOpened():
                    raise Exception("无法找到可用的摄像头")
            
            # 设置摄像头参数
            self.camera.set(cv2.CAP_PROP_FRAME_WIDTH, self.frame_width)
            self.camera.set(cv2.CAP_PROP_FRAME_HEIGHT, self.frame_height)
            self.camera.set(cv2.CAP_PROP_FPS, 30)
            
            # 测试读取一帧
            ret, frame = self.camera.read()
            if not ret:
                raise Exception("摄像头无法读取画面")
            
            print(f"[摄像头] 摄像头初始化成功，索引: {self.camera_index}")
            print(f"[摄像头] 分辨率: {self.frame_width}x{self.frame_height}")
            
            # 尝试初始化YOLO模型
            if self.yolo_available:
                self.initialize_yolo_model()
            else:
                print("[警告] YOLO不可用，将只提供摄像头功能")
            
            self.camera_connected_signal.emit()
            return True
            
        except Exception as e:
            error_msg = f"摄像头初始化失败: {str(e)}"
            print(f"[错误] {error_msg}")
            self.camera_error_signal.emit(error_msg)
            return False

    def initialize_yolo_model(self):
        """初始化YOLO模型"""
        if not self.yolo_available:
            self.recognition_error_signal.emit("YOLO库不可用")
            return
            
        try:
            print(f"[YOLO] 正在加载模型: {self.model_path}")
            print(f"[YOLO] Python版本: {sys.version}")
            
            # 检查模型文件是否存在
            import os
            if not os.path.exists(self.model_path):
                raise FileNotFoundError(f"模型文件不存在: {self.model_path}")
            
            # 尝试加载模型
            self.model = YOLO(self.model_path)
            print("[YOLO] 模型加载完成")
            
            # 预热模型
            dummy_image = np.zeros((640, 640, 3), dtype=np.uint8)
            with contextlib.redirect_stdout(DummyFile()):
                try:
                    self.model(dummy_image)
                    print("[YOLO] 模型预热完成")
                except Exception as warmup_error:
                    print(f"[警告] 模型预热失败: {warmup_error}")
            
        except Exception as e:
            error_msg = f"YOLO模型初始化失败: {str(e)}"
            print(f"[错误] {error_msg}")
            self.recognition_error_signal.emit(error_msg)
            self.model = None
    
    @Slot()
    def start_streaming(self):
        """开始视频流"""
        if not self.camera or not self.camera.isOpened():
            self.camera_error_signal.emit("摄像头未初始化")
            return
        
        if self.is_running:
            print("[摄像头] 视频流已在运行")
            return
        
        print("[摄像头] 开始视频流")
        self.is_running = True
        
        # 创建定时器用于定期捕获帧
        self.timer = QTimer()
        self.timer.timeout.connect(self.capture_frame)
        self.timer.start(33)  # 约30fps
    
    @Slot()
    def stop_streaming(self):
        """停止视频流"""
        if self.timer:
            self.timer.stop()
            self.timer = None
        
        self.is_running = False
        print("[摄像头] 视频流已停止")
    
    @Slot(bool)
    def enable_recognition(self, enabled):
        """启用/禁用药片识别"""
        if not self.yolo_available or self.model is None:
            if enabled:
                self.recognition_error_signal.emit("YOLO模型未加载，无法启用识别")
            return
            
        self.recognition_enabled = enabled
        print(f"[识别] 药片识别: {'启用' if enabled else '禁用'}")
    
    @Slot(float)
    def set_confidence_threshold(self, threshold):
        """设置识别置信度阈值"""
        self.conf_thres = max(0.1, min(1.0, threshold))
        print(f"[识别] 置信度阈值设置为: {self.conf_thres}")
    
    @Slot(float)
    def set_recognition_interval(self, interval):
        """设置识别间隔"""
        self.recognition_interval = max(0.1, interval)
        print(f"[识别] 识别间隔设置为: {self.recognition_interval}秒")
    
    def capture_frame(self):
        """捕获一帧画面"""
        if not self.camera or not self.camera.isOpened():
            return
        
        try:
            ret, frame = self.camera.read()
            if not ret:
                self.consecutive_failures += 1
                if self.consecutive_failures <= self.max_consecutive_failures:
                    if self.consecutive_failures == 1:
                        print("[警告] 无法读取摄像头画面，尝试恢复中...")
                        self.camera_error_signal.emit("读取摄像头画面失败")
                    elif self.consecutive_failures == self.max_consecutive_failures:
                        print("[警告] 摄像头连续读取失败，可能存在问题")
                return
            else:
                # 成功读取帧后重置失败计数
                if self.consecutive_failures > 0:
                    print(f"[信息] 摄像头已恢复正常，连续失败{self.consecutive_failures}次后恢复")
                    self.consecutive_failures = 0
            
            self.current_frame = frame
            
            # 如果启用了识别功能且模型可用，进行药片识别
            if self.recognition_enabled and self.model is not None and self.yolo_available:
                self.perform_recognition(frame)
            else:
                # 不进行识别，直接显示原始画面
                self.display_frame(frame)
                
        except Exception as e:
            error_msg = f"处理摄像头画面时出错: {str(e)}"
            print(f"[错误] {error_msg}")
            self.camera_error_signal.emit(error_msg)
    
    def perform_recognition(self, frame):
        """执行药片识别"""
        try:
            # 控制识别频率，避免过高CPU使用率
            current_time = time.time()
            if current_time - self.last_recognition_time >= self.recognition_interval:
                self.last_recognition_time = current_time
                
                # 调用YOLO药片识别算法
                with contextlib.redirect_stdout(DummyFile()):
                    results = self.model(frame)
                
                # 统计识别结果
                counts = {k: 0 for k in self.pill_types}
                
                # 检查results是否有效
                if results and len(results) > 0:
                    for r in results:
                        if r.boxes is not None:
                            for box, cls, conf in zip(r.boxes.xyxy, r.boxes.cls, r.boxes.conf):
                                if conf < self.conf_thres:
                                    continue
                                label = self.model.names[int(cls)]
                                if label in self.pill_types:
                                    counts[label] += 1
                
                total_count = sum(counts.values())
                info = f"Total: {total_count}"
                
                # 绘制标注后的画面
                if results and len(results) > 0:
                    annotated_frame = results[0].plot()
                else:
                    annotated_frame = frame.copy()
                
                cv2.putText(annotated_frame, info, (10, 30), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 0, 255), 2)
                self.annotated_frame = annotated_frame
                
                # 更新识别结果
                self.recognition_result.update(
                    counts, 
                    last_frame=annotated_frame, 
                    timestamp=time.strftime('%Y-%m-%d %H:%M:%S')
                )
                
                # 发送识别结果信号
                self.pill_recognition_signal.emit(counts)
                
                # 显示标注后的画面
                self.display_frame(annotated_frame)
                
                # 发送标注后的画面信号
                pixmap = self.frame_to_pixmap(annotated_frame)
                self.annotated_frame_signal.emit(pixmap)
                
            else:
                # 未到识别时间，显示原始画面
                self.display_frame(frame)
                
        except Exception as e:
            error_msg = f"药片识别过程发生异常: {str(e)}"
            print(f"[错误] {error_msg}")
            self.recognition_error_signal.emit(error_msg)
            # 出错时显示原始画面
            self.display_frame(frame)
    
    def display_frame(self, frame):
        """显示画面"""
        try:
            pixmap = self.frame_to_pixmap(frame)
            self.frame_ready_signal.emit(pixmap)
        except Exception as e:
            print(f"[错误] 显示画面时出错: {str(e)}")
    
    def frame_to_pixmap(self, frame):
        """将OpenCV帧转换为QPixmap"""
        # 转换为QImage
        rgb_image = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        h, w, ch = rgb_image.shape
        bytes_per_line = ch * w
        
        qt_image = QImage(rgb_image.data, w, h, bytes_per_line, QImage.Format_RGB888)
        
        # 转换为QPixmap
        pixmap = QPixmap.fromImage(qt_image)
        return pixmap
    
    @Slot()
    def get_latest_result(self):
        """获取最新识别结果"""
        return self.recognition_result
    
    @Slot(str)
    def save_result_to_file(self, file_path='result.txt'):
        """保存识别结果到文件"""
        try:
            self.recognition_result.write_to_txt(file_path)
            print(f"[信息] 识别结果已保存到: {file_path}")
        except Exception as e:
            error_msg = f"保存识别结果失败: {str(e)}"
            print(f"[错误] {error_msg}")
            self.recognition_error_signal.emit(error_msg)
    
    @Slot()
    def cleanup(self):
        """清理摄像头资源"""
        try:
            self.stop_streaming()
            
            if self.camera:
                self.camera.release()
                self.camera = None
            
            # 清理YOLO模型
            self.model = None
            
            print("[摄像头] 资源已清理")
            self.camera_disconnected_signal.emit()
            
        except Exception as e:
            print(f"[错误] 清理摄像头资源时出错: {e}")