import sys
from PyQt5 import QtCore, QtGui, QtWidgets
import RPi.GPIO as GPIO
import time

def load_stylesheet(app):
    #载入样式表
    try:
        with open('style.qss', 'r') as file:
            app.setStyleSheet(file.read())
    except FileNotFoundError:
        print("样式表文件未找到。")

def init_font():
    #载入字体
    font_files = [
        "font/HarmonyOS_Sans_SC_Regular.ttf",
        "font/HarmonyOS_Sans_SC_Bold.ttf",
        "font/HarmonyOS_Sans_SC_Light.ttf",
        "font/HarmonyOS_Sans_SC_Black.ttf",
        "font/HarmonyOS_Sans_SC_Thin.ttf"
    ]
    for font_file in font_files:
        font_id = QtGui.QFontDatabase.addApplicationFont(font_file)
        if font_id == -1:
            print(f"字体文件 {font_file} 加载失败")
        else:
            font_family = QtGui.QFontDatabase.applicationFontFamilies(font_id)
            print(f"字体文件 {font_file} 已加载，字体家族：{font_family}")

def rounded_corners_image(image_path, radius):
    # 自动为图像添加圆角

    # 加载图像
    image = QtGui.QImage(image_path)
    size = image.size()
    
    # 创建圆角的QPixmap
    rounded_pixmap = QtGui.QPixmap(size)
    rounded_pixmap.fill(QtCore.Qt.transparent)
    
    # 使用QPainter绘制圆角
    painter = QtGui.QPainter(rounded_pixmap)
    painter.setRenderHint(QtGui.QPainter.Antialiasing)
    
    path = QtGui.QPainterPath()
    path.addRoundedRect(0, 0, size.width(), size.height(), radius, radius)
    
    painter.setClipPath(path)
    painter.drawPixmap(0, 0, QtGui.QPixmap.fromImage(image))
    painter.end()
    
    return rounded_pixmap

def quit_app():
    app = QtWidgets.QApplication.instance()
    app.quit()

class Ui_Form1(QtCore.QObject):
    ctrl_ready = QtCore.pyqtSignal() #关闭舱门并开始读取RFID的信号

    def __init__(self):
        super().__init__()

    # 初始界面
    def setupUi(self, Form):
        self.Form = Form
        Form.setObjectName("Form")
        Form.setEnabled(True)
        Form.setGeometry(0,0,800,480)
        Form.setWindowFlag(QtCore.Qt.FramelessWindowHint)
        
        sizePolicy = QtWidgets.QSizePolicy(QtWidgets.QSizePolicy.Preferred, QtWidgets.QSizePolicy.Preferred)
        sizePolicy.setHorizontalStretch(0)
        sizePolicy.setVerticalStretch(0)
        sizePolicy.setHeightForWidth(Form.sizePolicy().hasHeightForWidth())
        Form.setSizePolicy(sizePolicy)
        Form.setAcceptDrops(False)

        self.slogan = QtWidgets.QLabel(Form)
        self.slogan.setGeometry(QtCore.QRect(123, 184, 233, 96))
        self.slogan.setMouseTracking(False)
        self.slogan.setObjectName("slogan")


        self.labelRfid = QtWidgets.QLabel(Form)
        self.labelRfid.setGeometry(QtCore.QRect(171, 280, 200, 42))
        self.labelRfid.setMouseTracking(False)
        self.labelRfid.setTextFormat(QtCore.Qt.AutoText)
        self.labelRfid.setAlignment(QtCore.Qt.AlignCenter)
        self.labelRfid.setWordWrap(False)
        self.labelRfid.setObjectName("labelRfid")

        self.pushButton = QtWidgets.QPushButton(Form)
        self.pushButton.setGeometry(QtCore.QRect(501, 182, 156, 156))
        self.pushButton.setObjectName("pushButton")

        self.exitButton = QtWidgets.QPushButton(Form)
        self.exitButton.setGeometry(QtCore.QRect(450, 10, 20, 10))
        self.exitButton.setObjectName("exitButton")

        self.retranslateUi(Form)
        QtCore.QMetaObject.connectSlotsByName(Form)

        self.pushButton.clicked.connect(self.onclick)
        
        self.exitButton.clicked.connect(quit_app)

    def retranslateUi(self, Form):
        _translate = QtCore.QCoreApplication.translate
        Form.setWindowTitle(_translate("Form", "智能分药盒"))
        self.slogan.setText(_translate("Form", "EZ dose"))
        self.labelRfid.setText(_translate("Form", "请放入药盘"))
        self.pushButton.setText(_translate("Form", "关闭药舱"))
    
    def onclick(self):
        self.pushButton.setText("请稍等")
        self.pushButton.setEnabled(False)
        self.pushButton.setStyleSheet("background-color: #D9D9D9;")
        self.pushButton.repaint()
        self.labelRfid.setText("正在获取处方...")
        self.labelRfid.repaint()

        self.ctrl_ready.emit() # 发送信号，通知舱门关闭并开始读取RFID

    def init_btn(self):
        self.pushButton.setText("关闭药舱")
        self.pushButton.setEnabled(True)
        self.pushButton.setStyleSheet("background-color: #6135c6;")
        self.labelRfid.setText("请放入药盘")


class Card(QtWidgets.QWidget):
    # 药物卡片
    def __init__(self, pill_name_cn = "unknown", pill_name_en = "unknown", pill_number = 0, pill_picture = "pic/pill_pic/default.png", parent=None):
        super(Card, self).__init__(parent)
        self.status = 0 #>3:已完成，2:正在分药，1:正在摆药，0:未开始
        self.pill_name_cn = pill_name_cn
        self.pill_name_en = pill_name_en
        self.pill_picture = pill_picture
        self.pill_number = pill_number
        self.current_pill_number = 0
        self.init_ui()

    def init_ui(self):
        self.setFixedSize(750, 63)
        self.setObjectName("card_widget")
        self.card = QtWidgets.QWidget(self)
        self.card.setObjectName("card")

        self.name_en = QtWidgets.QLabel(f'{self.pill_name_en}', self)
        self.name_cn = QtWidgets.QLabel(f'{self.pill_name_cn}', self)
        self.name_en.setObjectName("name_en")
        self.name_cn.setObjectName("name_cn")

        self.pill_number_label = QtWidgets.QLabel(f'{self.pill_number}', self)
        self.pill_number_label.setGeometry(650, 13, 80, 36)
        self.pill_number_label.setObjectName("pill_number_label")

        self.pill_number_geo_progress = QtWidgets.QLabel(self)
        self.pill_number_geo_progress.setObjectName("pill_number_geo_progress")
        self.pill_number_geo_progress.setGeometry(self.rect())
        self.pill_number_geo_progress.raise_()

        self.pill_number_text_progress = QtWidgets.QLabel(f'{self.current_pill_number}/{self.pill_number}', self)
        self.pill_number_text_progress.setGeometry(151, 121, 249, 91)
        self.pill_number_text_progress.setObjectName("pill_number_text_progress")
        text_shadow = QtWidgets.QGraphicsDropShadowEffect()
        text_shadow.setColor(QtGui.QColor(0, 0, 0, 64)) 
        text_shadow.setBlurRadius(14.8) 
        text_shadow.setOffset(0, 0) 
        text_shadow.setXOffset(0)
        text_shadow.setYOffset(0)
        self.pill_number_text_progress.setGraphicsEffect(text_shadow)
        self.pill_number_text_progress.hide()
         
        self.pill_picture_label = QtWidgets.QLabel(self)
        self.pill_picture_label.setGeometry(16, 102, 130, 130)
        #如果pill_picture不存在，则显示默认图片
        if not self.pill_picture or QtGui.QImage(self.pill_picture).isNull():
            self.pill_picture = "pic/pill_pic/default.png"
        self.pill_picture_label.setPixmap(QtGui.QPixmap(rounded_corners_image(self.pill_picture, 14)))
        self.pill_picture_label.setScaledContents(True)
        self.pill_picture_label.setObjectName("pill_picture_label")
        self.pill_picture_label.hide()

        self.update_status(self.status)

    def update_status(self, status):
        # 更新药物卡片状态, 0:未开始，1:正在摆药，2:正在分药，3:已完成
        self.status = status

        card_shadow = QtWidgets.QGraphicsDropShadowEffect()
        card_shadow.setColor(QtGui.QColor(0, 0, 0, 25)) 
        card_shadow.setBlurRadius(20)
        card_shadow.setOffset(0, 0) 
        card_shadow.setXOffset(0)
        card_shadow.setYOffset(0)
        
        if self.status >= 3:
            self.pill_number_geo_progress.setFixedWidth(self.width())
            self.card.setObjectName("card")
            self.transform_card(True)
            self.pill_number_label.hide()
            self.pill_number_geo_progress.show()
            self.pill_number_text_progress.hide()
            self.pill_number_geo_progress.show()
         
        elif self.status == 2:
            self.current_pill_number = 0
            self.card.setGraphicsEffect(None)
            self.card.setObjectName("card")
            self.transform_card(True)
            self.pill_number_label.hide()
            self.pill_number_text_progress.hide()
            self.pill_number_geo_progress.show()

        elif self.status == 1:
            self.current_pill_number = 0
            self.card.setGraphicsEffect(card_shadow)
            self.card.setObjectName("card_p_doing")
            self.transform_card(False)
            self.pill_number_label.hide()
            self.pill_number_text_progress.show()
            self.pill_number_geo_progress.hide()

        elif self.status == 0:
            self.current_pill_number = 0
            self.card.setObjectName("card")
            self.transform_card(True)
            self.pill_number_label.show()
            self.pill_number_text_progress.hide()
            self.pill_number_geo_progress.hide()

        self.progress_update(self.current_pill_number)
        self.update() 

    def progress_update(self, current_pill_number):
        # 若药物卡片为状态1和2，则根据各自的current_pill_number属性更新进度条（或者文字）
        if self.status in [1, 2]:
            if current_pill_number <= self.pill_number:
                self.current_pill_number = current_pill_number
                self.pill_number_text_progress.setText(f'{self.current_pill_number}/{self.pill_number}')
            else:
                self.current_pill_number = self.pill_number
                self.pill_number_text_progress.setText(f'{current_pill_number}/{self.pill_number}')
                
            if self.current_pill_number < self.pill_number * 0.1:
                    self.pill_number_geo_progress.setFixedWidth(int(self.width() * 0.1))
            else:
                self.pill_number_geo_progress.setFixedWidth(int(self.width() * self.current_pill_number / self.pill_number))
            self.update()
        self.repaint()
    
    def transform_card(self, to_small = True):
        # 调整药物卡片大小
        if to_small:
            self.setFixedSize(750, 63)
            self.card.setFixedSize(750, 75)
            self.card.setStyleSheet("border: 2px solid #FFFFFF; background-color: rgba(255, 255, 255, 0.5); border-radius: 30px;")
            self.name_en.setGeometry(31, 7, 371, 21)
            self.name_en.setStyleSheet("font-size: 14px; font-weight: 100; color: #6135C6;")
            self.name_cn.setGeometry(31, 20, 371, 36)
            self.name_cn.setStyleSheet("font-size: 24px; font-weight: regular; color: #6135C6;")
            self.pill_picture_label.hide()
        else:
            self.setFixedSize(750, 248)
            self.card.setFixedSize(750, 248)
            self.card.setStyleSheet("border: 5px solid #FFFFFF; background-color: #6135C6; border-radius: 30px;")
            self.name_en.setGeometry(31, 13, 371, 21)
            self.name_en.setStyleSheet("font-size: 14px; font-weight: 100; color: #FFFFFF;")
            self.name_cn.setGeometry(31, 30, 371, 36)
            self.name_cn.setStyleSheet("font-size: 28px; font-weight: bold; color: #FFFFFF;")
            self.pill_picture_label.show()

class Ui_Form2(QtCore.QObject):
    cap_off = QtCore.pyqtSignal() # 关闭摄像头的信号
    start_distribute = QtCore.pyqtSignal(object) # 开始分药的信号
    open_dialog = QtCore.pyqtSignal(int, str) # 打开提醒界面的信号
    return_to_main = QtCore.pyqtSignal() # 返回主界面的信号
    get_rfid = QtCore.pyqtSignal() # 开始读取RFID的信号

    # 第二层级界面，药物列表界面
    def __init__(self):
        super().__init__()
        
        self.Perception_number = ""
        self.Patient_name = ""
        self.Patient_portrait = "pic/potrait.png"
        self.Pill_list = [] #药品信息的列表，药品信息为字典：{"name_cn":xxx,"name_en":xxx, pill_number:xxx, "pic":xxx}
        self.pill_matrix = []
        self.Pill_list_length = 0

        self.pill_status = []

        self.step = 0
        self.distrubuted_or_not = True
        self.distributing_or_not = False

        self.pause_status = False # 指示暂停状态

    def update_parameters(self, perception_number, patient_name, patient_portrait, pill_list, pill_matrix):
        '''
        更新基本参数
        args:
            perception_number: 处方号
            patient_name: 病人姓名
            patient_portrait: 病人头像
            pill_list: 药品信息的列表
            pill_matrix: 每个药品的分药矩阵的列表
        '''
        self.Perception_number = perception_number
        self.Patient_name = patient_name
        self.Patient_portrait = patient_portrait
        self.Pill_list = pill_list
        self.pill_matrix = pill_matrix
        self.Pill_list_length = len(pill_list)

    def init_status(self):
        '''
        在基本参数确定之后，初始化分药状态，包括当前摆药进度、上一个药的分药情况、药片状态列表。
        '''
        self.step = 0
        self.distrubuted_or_not = True
        for i in range(self.Pill_list_length):
            self.pill_status.append(0)
        if self.pill_status:
            self.pill_status[0] = 1

    def setupUi(self, Form):
        # 界面布局
        Form.setObjectName("Form2")
        Form.setGeometry(0, 0, 800, 480)
        Form.setWindowFlag(QtCore.Qt.FramelessWindowHint)

        self.patient_info = QtWidgets.QFrame(Form)
        self.patient_info.setGeometry(QtCore.QRect(0, 0, 800, 77))
        self.patient_info.setObjectName("patient_info")

        self.exitButton = QtWidgets.QPushButton(self.patient_info)
        self.exitButton.setGeometry(QtCore.QRect(450, 10, 20, 10))
        self.exitButton.setObjectName("exitButton")
        self.exitButton.clicked.connect(quit_app)

        self.patient_number = QtWidgets.QLabel(self.patient_info)
        self.patient_number.setGeometry(QtCore.QRect(265, 30, 92, 18))
        self.patient_number.setObjectName("patient_number")
        
        self.patient_name = QtWidgets.QLabel(self.patient_info)
        self.patient_name.setGeometry(QtCore.QRect(101, 25, 150, 27))
        self.patient_name.setObjectName("patient_name")

        self.patient_graphics = QtWidgets.QLabel(self.patient_info)
        self.patient_graphics.setGeometry(QtCore.QRect(25, 14, 50, 50))
        self.patient_graphics.setScaledContents(True)
        self.patient_graphics.setObjectName("patient_graphics")
        self.patient_graphics.setPixmap(QtGui.QPixmap(self.Patient_portrait))

        self.camera_status = QtWidgets.QLabel(Form)
        self.camera_status.setGeometry(QtCore.QRect(721, 28, 18, 16))
        self.camera_status.setObjectName("camera_status")
        self.camera_status.setPixmap(QtGui.QPixmap("pic/camera_on.png"))
        self.camera_status.setScaledContents(True)

        self.scrollArea = QtWidgets.QScrollArea(Form)
        self.scrollArea.setGeometry(QtCore.QRect(0, 77, 800, 403))
        self.scrollArea.setWidgetResizable(True)
        self.scrollArea.setAlignment(QtCore.Qt.AlignCenter)
        self.scrollArea.setObjectName("scrollArea")
        self.scrollArea.setVerticalScrollBarPolicy(QtCore.Qt.ScrollBarAlwaysOff)
        self.scrollArea.setHorizontalScrollBarPolicy(QtCore.Qt.ScrollBarAlwaysOff)

        self.scrollAreaWidgetContents = QtWidgets.QWidget()
        self.scrollArea.setWidget(self.scrollAreaWidgetContents)
        self.scrollAreaWidgetContents.setObjectName("scrollAreaWidgetContents")

        self.layout = QtWidgets.QVBoxLayout(self.scrollAreaWidgetContents)
        self.layout.setContentsMargins(23, 0, 23, 0)
        self.layout.setAlignment(QtCore.Qt.AlignTop)
        self.layout.setSpacing(12)
        
        self.pause_button = QtWidgets.QPushButton(Form)
        self.pause_button.setGeometry(QtCore.QRect(385, 705, 75, 75))
        self.pause_button.setObjectName("pause_button")
        self.pause_button.setIcon(QtGui.QIcon("pic/pause.png"))
        self.pause_button.setIconSize(QtCore.QSize(71, 71))
        self.pause_button.clicked.connect(self.pause_button_update)
        self.pause_button.hide()
        
        self.no_pill_label = QtWidgets.QLabel(self.scrollAreaWidgetContents)
        self.no_pill_label.setFixedSize(QtCore.QSize(400, 42))
        self.no_pill_label.setObjectName("no_pill_label")
        self.no_pill_label.setText("未找到处方")
        self.no_pill_label.hide()
        
        self.retry_button = QtWidgets.QPushButton(self.scrollAreaWidgetContents)
        self.retry_button.setFixedSize(QtCore.QSize(120, 45))
        self.retry_button.setObjectName("retry_button")
        self.retry_button.clicked.connect(self.get_rfid.emit)
        self.retry_button.setText("重试")
        self.retry_button.hide()

        self.pushButton = QtWidgets.QPushButton(Form)
        self.pushButton.setGeometry(QtCore.QRect(164, 555, 156, 156))
        self.pushButton.setObjectName("pushButton")
        self.pushButton.clicked.connect(self.return_to_main.emit)
        self.pushButton.setText("打开药舱")
        self.pushButton.hide()

        if self.Pill_list:
            for i in range(len(self.Pill_list)):
                self.add_card(self.Pill_list[i])
            self.pause_button.show()
        else:
            self.layout.addWidget(self.no_pill_label)
            self.layout.addWidget(self.retry_button)
            self.layout.setAlignment(self.retry_button, QtCore.Qt.AlignHCenter)
            self.layout.setAlignment(self.no_pill_label, QtCore.Qt.AlignHCenter)
            self.pushButton.show()
            self.no_pill_label.show()
            self.retry_button.show()

        self.retranslateUi(Form)

    def retranslateUi(self, Form):
        _translate = QtCore.QCoreApplication.translate
        Form.setWindowTitle(_translate("Form", "智能分药盒"))
        self.patient_number.setText(_translate("Form", f"处方号：{self.Perception_number}"))
        self.patient_number.adjustSize()
        self.patient_name.setText(_translate("Form", f"<b>{self.Patient_name}</b> 的药品清单"))
        self.patient_name.adjustSize()

    @QtCore.pyqtSlot()
    def distributed(self):
        '''
        分药完成时调用的槽函数, 更新界面的distrubuted_or_not参数状态
        '''
        print('分药完成')
        self.distrubuted_or_not = True

    # 首先添加servo_map函数到Ui_Form2类中
    def servo_map(self, angle):
        """将角度转换为PWM占空比"""
        return angle / 18 + 2.5

    @QtCore.pyqtSlot(int)
    def update_status(self, pill_count):
        # 根据摄像头检测到的已摆放药片数量更新分药进度
        '''
        根据逻辑, 更新药物列表的进度
        args:
            pill_count: 摄像头检测到的已摆放药物数量。
        '''
        print("判定中")
        print(self.step)
        if self.step == self.Pill_list_length and self.Pill_list_length > 0: # 若为最后一种药品 
            if self.distrubuted_or_not: # 若完成分药，进入下一步，关闭摄像头，弹出成功弹窗
                self.next_step()
                self.layout.itemAt(self.Pill_list_length - 1).widget().update_status(self.pill_status[self.Pill_list_length - 1]) # 更新最后一张药物卡片的状态
                self.cap_off.emit()
                self.distrubuted_or_not = False
                self.open_dialog.emit(1, "成功")

        elif self.step < self.Pill_list_length: # 若非最后一种药品
            # 初始化GPIO
            GPIO.setmode(GPIO.BOARD)
            servo_SIG = 32
            servo_freq = 50
            GPIO.setup(servo_SIG, GPIO.OUT)
            servo = GPIO.PWM(servo_SIG, servo_freq)  # 50Hz
            servo.start(0)  # 初始占空比为 0
            servo.ChangeDutyCycle(self.servo_map(0))
        
            self.progress_update(1, pill_count)
        
            # 当药片数量大于或等于需求量且上一个药分完时
            if pill_count >= self.Pill_list[self.step]['number'] and self.distrubuted_or_not:
                self.start_distribute.emit(self.pill_matrix[self.step]) # 发送信号，开始分发药物
                print("开始分药信号已发送")
                self.distrubuted_or_not = False
                self.distributing_or_not = True
            
                # 转到 180 度
                servo.ChangeDutyCycle(self.servo_map(180))
                time.sleep(0.5)
            
                # 小幅度振动
                for _ in range(30):
                    servo.ChangeDutyCycle(self.servo_map(150))
                    time.sleep(0.07)
                    servo.ChangeDutyCycle(self.servo_map(180))
                    time.sleep(0.07)
                time.sleep(1)
                print("num", pill_count)
                
                if pill_count == 0:
                    # 药片归零后，舵机回到初始位置
                    servo.ChangeDutyCycle(self.servo_map(0))
                    self.next_step()
                    self.distributing_or_not = False
                    print("药片已全部分发，舵机已复位")
            
            elif pill_count > 0 and pill_count < self.Pill_list[self.step]['number'] and not self.distributing_or_not:
                # 药片数量不足时，小幅度振动提醒
                for _ in range(10):
                    servo.ChangeDutyCycle(self.servo_map(10))
                    time.sleep(0.07)
                    servo.ChangeDutyCycle(self.servo_map(0))
                    time.sleep(0.07)
            
                # 复位到初始位置
                #servo.ChangeDutyCycle(self.servo_map(0))
                time.sleep(1)
                print("药片数量不足，舵机振动")
            
            elif pill_count > 0 and pill_count < self.Pill_list[self.step]['number'] and self.distributing_or_not:
                # 小幅度振动
                for _ in range(30):
                    servo.ChangeDutyCycle(self.servo_map(150))
                    time.sleep(0.07)
                    servo.ChangeDutyCycle(self.servo_map(180))
                    time.sleep(0.07)
                time.sleep(1)
                print("num", pill_count)
                
            elif pill_count == 0 and self.distributing_or_not :
                    # 药片归零后，舵机回到初始位置
                    
                    servo.ChangeDutyCycle(self.servo_map(0))
                    time.sleep(0.5)
                    
                    self.next_step()
                    self.distributing_or_not = False
                    print("药片已全部分发，舵机已复位")
        
            # 停止PWM并清理GPIO
            servo.ChangeDutyCycle(0)
        
        else:
            return

    def on_click(self):
        self.return_to_main.emit()

    def add_card(self, pill_list):
        '''
        添加药物卡片到界面上

        args:
            pill_list: 药物信息的字典。
        '''
        card = Card(pill_list["name_cn"], pill_list["name_en"], pill_list["number"], pill_list["pic"], self.scrollAreaWidgetContents)
        self.layout.addWidget(card)

    def update_camera_status(self, camera_status):
        '''更新界面上的摄像头图标的状态'''

        if camera_status:
            self.camera_status.setPixmap(QtGui.QPixmap("pic/camera_on.png"))
        else:
            self.camera_status.setPixmap(QtGui.QPixmap("pic/camera_off.png"))

    def pause_button_update(self):
        '''更新界面上的暂停图标的状态'''

        if self.pause_status:
            self.pause_status = False
            self.pause_button.setIcon(QtGui.QIcon("pic/pause.png"))
        else:
            self.pause_status = True
            self.pause_button.setIcon(QtGui.QIcon("pic/resume.png"))

    def next_step(self):
        '''进入下一步'''
        for i in range(self.step + 2):
            if i < self.Pill_list_length:
                self.pill_status[i] += 1
                self.layout.itemAt(i).widget().update_status(self.pill_status[i])
        self.step += 1 
    
    @QtCore.pyqtSlot(int, int)
    def progress_update(self, pill_status, current_pill_number):
        '''
        更新对应药物状态的卡片的进度（包括文字和进度条）

        args:
            pill_status: 目标的药物状态。1表示正在摆放的药物,对应文字进度。2表示正在分发的药物,对应图像进度条。
            current_pill_number: 要更新的目标数量。
        '''
        indices = [i for i, x in enumerate(self.pill_status) if x == pill_status]
        for index in indices:
            self.layout.itemAt(index).widget().progress_update(current_pill_number)

    def update_card(self):
        '''根据当前pill_status列表更新界面上药物卡片的状态'''
        if self.Pill_list_length != 0:
            for i in range(self.Pill_list_length):
                self.layout.itemAt(i).widget().update_status(self.pill_status[i])

class Ui_Dialog(QtCore.QObject):
    return_to_main = QtCore.pyqtSignal()
    open_plate = QtCore.pyqtSignal()
    close_plate = QtCore.pyqtSignal()

    def __init__(self, kind = 0, error_text = '未知错误'):
        super().__init__()
        self.kind = kind # 0表示错误，1表示成功
        self.error_text = error_text # 错误信息
        self.dialog = None

    def update_parameters(self, kind, error_text):
        self.kind = kind
        self.error_text = error_text

    def setupUi(self, Dialog):
        Dialog.setObjectName("dialog")
        Dialog.setGeometry(0, 77, 800, 403)
        Dialog.setWindowFlag(QtCore.Qt.FramelessWindowHint)
        self.dialog = Dialog

        card_shadow = QtWidgets.QGraphicsDropShadowEffect()
        card_shadow.setColor(QtGui.QColor(0, 0, 0, 25)) 
        card_shadow.setBlurRadius(20)
        card_shadow.setOffset(0, 0) 
        card_shadow.setXOffset(0)
        card_shadow.setYOffset(0)

        self.box = QtWidgets.QLabel(Dialog)
        self.box.setGeometry(QtCore.QRect(25, 0, 750, 378))
        self.box.setGraphicsEffect(card_shadow)

        self.text = QtWidgets.QLabel(Dialog)
        self.text.setGeometry(QtCore.QRect(115, 162, 320, 42))
        self.text.setAlignment(QtCore.Qt.AlignCenter)

        if self.kind == 0:
            self.text.setText(self.error_text)
            self.box.setObjectName("red_box")
            self.text.setObjectName("red_text")
        else:
            self.text.setText("所有药物摆放完成")
            self.box.setObjectName("blue_box")
            self.text.setObjectName("blue_text")

        Dialog.setAttribute(QtCore.Qt.WA_TranslucentBackground)

        self.pushButton = QtWidgets.QPushButton(Dialog)
        self.pushButton.setGeometry(QtCore.QRect(476, 105, 156, 156))
        self.pushButton.setObjectName("pushButton")
        self.pushButton.clicked.connect(self.on_click)
        self.pushButton.setText("打开药舱")

    def on_click(self):
        if self.kind == 1:
            self.return_to_main.emit()
            self.dialog.close()
        else:
            self.open_plate.emit()
            self.pushButton.setText("关闭药舱")
            self.pushButton.clicked.disconnect()
            self.pushButton.clicked.connect(self.on_click_close)

    def on_click_close(self):
        self.close_plate.emit()
        self.dialog.close()
