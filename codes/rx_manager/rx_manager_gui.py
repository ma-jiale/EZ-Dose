import sys
import os
import pandas as pd
from datetime import datetime, timedelta
from PySide6.QtWidgets import (QApplication, QMainWindow, QWidget, QPushButton, 
                               QMessageBox, QGridLayout, QSizePolicy, QDialog,
                               QVBoxLayout, QHBoxLayout, QLabel, QLineEdit, 
                               QDialogButtonBox)
from PySide6.QtCore import Qt, Signal, Slot, QDate
from PySide6.QtGui import QFont, QPixmap
from main_window_ui import Ui_MainWindow
from medicine_setting_ui import Ui_medicine_setting
from patient_prescription_manager import PatientPrescriptionManager
from patient_info_manager import PatientInfoManager

class AddPatientDialog(QDialog):
    """新增患者对话框"""
    
    def __init__(self, parent=None):
        super().__init__(parent)
        self.setWindowTitle("新增患者")
        self.setModal(True)
        self.setFixedSize(400, 200)
        self.setup_ui()
    
    def setup_ui(self):
        """设置对话框界面"""
        layout = QVBoxLayout(self)
        
        # 标题
        title_label = QLabel("新增患者信息")
        title_label.setFont(QFont("Microsoft YaHei", 12, QFont.Weight.Bold))
        title_label.setAlignment(Qt.AlignmentFlag.AlignCenter)
        layout.addWidget(title_label)
        
        # 输入区域
        form_layout = QVBoxLayout()
        
        # 患者姓名
        name_layout = QHBoxLayout()
        name_label = QLabel("患者姓名:")
        name_label.setMinimumWidth(80)
        name_label.setFont(QFont("Microsoft YaHei", 10))
        self.name_input = QLineEdit()
        self.name_input.setPlaceholderText("请输入患者姓名")
        self.name_input.setFont(QFont("Microsoft YaHei", 10))
        name_layout.addWidget(name_label)
        name_layout.addWidget(self.name_input)
        form_layout.addLayout(name_layout)
        
        # 患者ID
        id_layout = QHBoxLayout()
        id_label = QLabel("患者ID:")
        id_label.setMinimumWidth(80)
        id_label.setFont(QFont("Microsoft YaHei", 10))
        self.id_input = QLineEdit()
        self.id_input.setPlaceholderText("请输入患者ID（数字）")
        self.id_input.setFont(QFont("Microsoft YaHei", 10))
        id_layout.addWidget(id_label)
        id_layout.addWidget(self.id_input)
        form_layout.addLayout(id_layout)
        
        layout.addLayout(form_layout)
        
        # 按钮区域
        button_box = QDialogButtonBox(
            QDialogButtonBox.StandardButton.Ok | QDialogButtonBox.StandardButton.Cancel
        )
        button_box.accepted.connect(self.accept)
        button_box.rejected.connect(self.reject)
        
        # 设置按钮文本为中文
        button_box.button(QDialogButtonBox.StandardButton.Ok).setText("确定")
        button_box.button(QDialogButtonBox.StandardButton.Cancel).setText("取消")
        
        layout.addWidget(button_box)
        
        # 设置样式
        self.setStyleSheet("""
            QDialog {
                background-color: white;
            }
            QLineEdit {
                border: 2px solid #cccccc;
                border-radius: 6px;
                padding: 8px;
                margin: 5px 0;
            }
            QLineEdit:focus {
                border: 2px solid #0078d4;
            }
            QPushButton {
                background-color: #0078d4;
                color: white;
                border: none;
                padding: 8px 16px;
                border-radius: 4px;
                font-weight: bold;
            }
            QPushButton:hover {
                background-color: #106ebe;
            }
            QPushButton:pressed {
                background-color: #005a9e;
            }
        """)
    
    def get_patient_data(self):
        """获取输入的患者数据"""
        return {
            'patientName': self.name_input.text().strip(),
            'patientId': self.id_input.text().strip()
        }
    
    def validate_input(self):
        """验证输入数据"""
        patient_data = self.get_patient_data()
        
        if not patient_data['patientName']:
            QMessageBox.warning(self, "警告", "请输入患者姓名")
            return False
        
        if not patient_data['patientId']:
            QMessageBox.warning(self, "警告", "请输入患者ID")
            return False
        
        # 验证ID是否为数字
        try:
            int(patient_data['patientId'])
        except ValueError:
            QMessageBox.warning(self, "警告", "患者ID必须是数字")
            return False
        
        return True
    
    def accept(self):
        """确定按钮事件"""
        if self.validate_input():
            super().accept()

class MedicineSettingDialog(QDialog):
    """药品设置对话框"""
    
    # 添加信号，用于通知主窗口刷新数据
    medicine_updated = Signal()
    medicine_added = Signal()  # 新增药品成功信号
    
    def __init__(self, medicine_info, current_patient_info, rx_manager, is_new_medicine=False, parent=None):
        super().__init__(parent)
        self.medicine_info = medicine_info
        self.current_patient_info = current_patient_info  # 当前患者信息
        self.rx_manager = rx_manager  # 处方管理器实例
        self.is_new_medicine = is_new_medicine  # 是否为新增药品
        self.original_start_date = None  # 保存原始开始日期，用于检测是否修改
        
        if is_new_medicine:
            self.setWindowTitle("新增药品")
        else:
            self.setWindowTitle("药品详情设置")
        
        self.setModal(True)
        self.resize(570, 380)
        
        # 使用UI文件
        self.ui = Ui_medicine_setting()
        self.ui.setupUi(self)
        
        # 初始化界面
        self.setup_interface()
        
        # 连接信号
        self.connect_signals()
        
        # 加载药品数据
        if not is_new_medicine:
            self.load_medicine_data()
        else:
            self.setup_new_medicine_defaults()
    
    def setup_interface(self):
        """设置界面"""
        # 设置默认图片显示
        self.ui.img_cam_frame.setText("暂无图片")
        self.ui.img_cam_frame.setAlignment(Qt.AlignmentFlag.AlignCenter)
        
        # 设置SpinBox的范围（如果这些控件存在的话）
        if hasattr(self.ui, 'spinBox_morning_dosage'):
            self.ui.spinBox_morning_dosage.setRange(0, 20)
        if hasattr(self.ui, 'spinBox_noon_dosage'):
            self.ui.spinBox_noon_dosage.setRange(0, 20)
        if hasattr(self.ui, 'spinBox_evening_dosage'):
            self.ui.spinBox_evening_dosage.setRange(0, 20)
        if hasattr(self.ui, 'spinBox_duration_days'):
            self.ui.spinBox_duration_days.setRange(1, 365)
        
        # 设置日期编辑器
        if hasattr(self.ui, 'dateEdit_start_date'):
            self.ui.dateEdit_start_date.setCalendarPopup(True)
            self.ui.dateEdit_start_date.setDisplayFormat("yyyy-MM-dd")
        
        # 不设置任何样式，保持原始UI样式
    
    def setup_new_medicine_defaults(self):
        """为新药品设置默认值"""
        # 清空药品名称
        if hasattr(self.ui, 'lineEdit_medicine_name'):
            self.ui.lineEdit_medicine_name.clear()
        
        # 设置默认剂量为0
        if hasattr(self.ui, 'spinBox_morning_dosage'):
            self.ui.spinBox_morning_dosage.setValue(0)
        if hasattr(self.ui, 'spinBox_noon_dosage'):
            self.ui.spinBox_noon_dosage.setValue(0)
        if hasattr(self.ui, 'spinBox_evening_dosage'):
            self.ui.spinBox_evening_dosage.setValue(0)
        
        # 设置默认服药时间为饭前服用
        if hasattr(self.ui, 'comboBox_dosage_time'):
            self.ui.comboBox_dosage_time.setCurrentIndex(0)
        
        # 设置默认持续天数为7天
        if hasattr(self.ui, 'spinBox_duration_days'):
            self.ui.spinBox_duration_days.setValue(7)
        
        # 设置默认药片尺寸为M
        if hasattr(self.ui, 'comboBox_size'):
            self.ui.comboBox_size.setCurrentIndex(1)  # M
        
        # 设置默认为启用状态
        if hasattr(self.ui, 'comboBox_isActive'):
            self.ui.comboBox_isActive.setCurrentIndex(0)  # 启用
        
        # 设置默认开始日期为今天
        if hasattr(self.ui, 'dateEdit_start_date'):
            today = QDate.currentDate()
            self.ui.dateEdit_start_date.setDate(today)
            self.original_start_date = today.toString("yyyy-MM-dd")
    
    def connect_signals(self):
        """连接信号"""
        if hasattr(self.ui, 'btn_take_photo'):
            self.ui.btn_take_photo.clicked.connect(self.take_photo)
        if hasattr(self.ui, 'btb_delete_photo'):
            self.ui.btb_delete_photo.clicked.connect(self.delete_photo)
        if hasattr(self.ui, 'btn_save_medicine'):
            self.ui.btn_save_medicine.clicked.connect(self.save_medicine)
    
    def load_medicine_data(self):
        """加载药品数据到界面"""
        if not self.medicine_info:
            return
        
        # 设置药品名称
        medicine_name = self.medicine_info.get('medicine_name', '')
        if hasattr(self.ui, 'lineEdit_medicine_name'):
            self.ui.lineEdit_medicine_name.setText(medicine_name)
        
        # 设置剂量
        if hasattr(self.ui, 'spinBox_morning_dosage'):
            self.ui.spinBox_morning_dosage.setValue(int(self.medicine_info.get('morning_dosage', 0)))
        if hasattr(self.ui, 'spinBox_noon_dosage'):
            self.ui.spinBox_noon_dosage.setValue(int(self.medicine_info.get('noon_dosage', 0)))
        if hasattr(self.ui, 'spinBox_evening_dosage'):
            self.ui.spinBox_evening_dosage.setValue(int(self.medicine_info.get('evening_dosage', 0)))
        
        # 设置服药时间 - 修正映射关系
        meal_timing = self.medicine_info.get('meal_timing', 'before')
        if hasattr(self.ui, 'comboBox_dosage_time'):
            # 映射关系：
            # 'before' -> index 0 (饭前服用)
            # 'after' -> index 1 (饭后服用) 
            # 'anytime' -> index 2 (任意时段)
            if meal_timing == 'before':
                self.ui.comboBox_dosage_time.setCurrentIndex(0)
            elif meal_timing == 'after':
                self.ui.comboBox_dosage_time.setCurrentIndex(1)
            elif meal_timing == 'anytime':
                self.ui.comboBox_dosage_time.setCurrentIndex(2)
            else:
                # 如果是中文或其他格式，尝试匹配
                if '饭前' in meal_timing:
                    self.ui.comboBox_dosage_time.setCurrentIndex(0)
                elif '饭后' in meal_timing:
                    self.ui.comboBox_dosage_time.setCurrentIndex(1)
                elif '任意' in meal_timing:
                    self.ui.comboBox_dosage_time.setCurrentIndex(2)
                else:
                    self.ui.comboBox_dosage_time.setCurrentIndex(0)  # 默认饭前服用
        
        # 设置持续天数
        duration_days = int(self.medicine_info.get('duration_days', 1))
        if hasattr(self.ui, 'spinBox_duration_days'):
            self.ui.spinBox_duration_days.setValue(duration_days)
        
        # 设置药片尺寸
        pill_size = self.medicine_info.get('pill_size', 'M')
        if hasattr(self.ui, 'comboBox_size'):
            if pill_size == 'S':
                self.ui.comboBox_size.setCurrentIndex(0)
            elif pill_size == 'M':
                self.ui.comboBox_size.setCurrentIndex(1)
            else:
                self.ui.comboBox_size.setCurrentIndex(2)
        
        # 设置是否启用
        is_active = int(self.medicine_info.get('is_active', 1))

        if hasattr(self.ui, 'comboBox_isActive'):
            self.ui.comboBox_isActive.setCurrentIndex(0 if is_active else 1)
        
        # 设置开始日期
        start_date = self.medicine_info.get('start_date', '')
        if hasattr(self.ui, 'dateEdit_start_date') and start_date:
            try:
                # 尝试解析日期字符串
                if start_date:
                    # 支持多种日期格式
                    date_formats = ["%Y-%m-%d", "%Y/%m/%d", "%m/%d/%Y", "%d/%m/%Y"]
                    parsed_date = None
                    
                    for fmt in date_formats:
                        try:
                            parsed_date = datetime.strptime(start_date, fmt)
                            break
                        except ValueError:
                            continue
                    
                    if parsed_date:
                        qdate = QDate(parsed_date.year, parsed_date.month, parsed_date.day)
                        self.ui.dateEdit_start_date.setDate(qdate)
                        self.original_start_date = qdate.toString("yyyy-MM-dd")
                    else:
                        # 如果解析失败，使用今天的日期
                        today = QDate.currentDate()
                        self.ui.dateEdit_start_date.setDate(today)
                        self.original_start_date = today.toString("yyyy-MM-dd")
                else:
                    # 如果没有开始日期，使用今天的日期
                    today = QDate.currentDate()
                    self.ui.dateEdit_start_date.setDate(today)
                    self.original_start_date = today.toString("yyyy-MM-dd")
                    
            except Exception as e:
                print(f"解析开始日期时出错: {e}")
                # 出错时使用今天的日期
                today = QDate.currentDate()
                self.ui.dateEdit_start_date.setDate(today)
                self.original_start_date = today.toString("yyyy-MM-dd")
        elif hasattr(self.ui, 'dateEdit_start_date'):
            # 如果没有日期控件或没有日期数据，使用今天的日期
            today = QDate.currentDate()
            self.ui.dateEdit_start_date.setDate(today)
            self.original_start_date = today.toString("yyyy-MM-dd")
    
    @Slot()
    def take_photo(self):
        """拍照功能"""
        QMessageBox.information(self, "提示", "拍照功能暂未实现")
    
    @Slot()
    def delete_photo(self):
        """清除照片"""
        self.ui.img_cam_frame.setText("暂无图片")
        if hasattr(self.ui.img_cam_frame, 'setPixmap'):
            self.ui.img_cam_frame.setPixmap(QPixmap())
    
    @Slot()
    def save_medicine(self):
        """保存药品信息到CSV文件"""
        try:
            # 获取界面数据
            medicine_data = self.get_medicine_data()
            
            # 验证数据
            if not self.validate_medicine_data(medicine_data):
                return
            
            # 检查是否有必要的信息
            if not self.current_patient_info:
                QMessageBox.warning(self, "错误", "缺少患者信息，无法保存")
                return
            
            # 准备处方数据
            prescription_data = {
                'patient_info': {
                    'patient_name': self.current_patient_info['patient_name'],
                    'patient_id': self.current_patient_info['patient_id'],
                    'rfid': self.current_patient_info.get('rfid', '')  # 处理可能缺少rfid的情况
                },
                'medicines': [medicine_data]
            }
            
            # 根据是否为新药品选择不同的保存方法
            if self.is_new_medicine:
                # 检查药品是否已存在
                if self.check_medicine_exists(medicine_data['medicine_name']):
                    QMessageBox.warning(self, "警告", f"药品 '{medicine_data['medicine_name']}' 已存在，请使用其他名称")
                    return
                
                # 添加新药品
                success, result = self.rx_manager.update_patient_prescription(prescription_data)
                
                if success:
                    # 立即上传到服务器以保持同步
                    upload_success = self.rx_manager.upload_prescriptions_to_server()
                    if upload_success:
                        print("[Info] Successfully synced new medicine to server")
                    else:
                        print("[Warning] Failed to sync to server, but saved locally")
                    
                    QMessageBox.information(self, "成功", f"药品 '{medicine_data['medicine_name']}' 添加成功")
                    
                    # 发出新增药品成功信号
                    self.medicine_added.emit()
                    
                    # 关闭对话框
                    self.accept()
                else:
                    QMessageBox.critical(self, "添加失败", f"添加药品失败：{result['error']}")
            else:
                # 更新现有药品
                success, result = self.rx_manager.update_patient_prescription(prescription_data)
                
                if success:
                    # 立即上传到服务器以保持同步
                    upload_success = self.rx_manager.upload_prescriptions_to_server()
                    if upload_success:
                        print("[Info] Successfully synced updated medicine to server")
                    else:
                        print("[Warning] Failed to sync to server, but saved locally")
                    
                    QMessageBox.information(self, "成功", f"药品信息已保存：{result['message']}")
                    
                    # 发出信号通知主窗口刷新数据
                    self.medicine_updated.emit()
                    
                    # 关闭对话框
                    self.accept()
                else:
                    QMessageBox.critical(self, "保存失败", f"保存药品信息时出错：{result['error']}")
        except Exception as e:
            QMessageBox.critical(self, "错误", f"保存药品信息时发生异常：{str(e)}")
    
    def check_medicine_exists(self, medicine_name):
        """检查药品是否已存在"""
        if not self.current_patient_info:
            return False
        
        # 从数据库中检查
        if self.rx_manager.df is not None and not self.rx_manager.df.empty:
            patient_id = self.current_patient_info['patient_id']
            existing_mask = (
                (self.rx_manager.df['patientId'].astype(str) == str(patient_id)) & 
                (self.rx_manager.df['medicine_name'] == medicine_name)
            )
            return not self.rx_manager.df[existing_mask].empty
        
        return False
    
    def calculate_last_dispensed_expiry_date(self, start_date_str):
        """计算last_dispensed_expiry_date为start_date的前一天"""
        try:
            # 解析开始日期
            start_date = datetime.strptime(start_date_str, "%Y-%m-%d")
            # 计算前一天
            previous_day = start_date - timedelta(days=1)
            return previous_day.strftime("%Y-%m-%d")
        except Exception as e:
            print(f"计算last_dispensed_expiry_date时出错: {e}")
            # 出错时返回空字符串
            return ""
    
    def get_medicine_data(self):
        """获取界面中的药品数据"""
        data = {}
        
        if hasattr(self.ui, 'lineEdit_medicine_name'):
            data['medicine_name'] = self.ui.lineEdit_medicine_name.text().strip()
        else:
            data['medicine_name'] = self.medicine_info.get('medicine_name', '') if self.medicine_info else ''
        
        if hasattr(self.ui, 'spinBox_morning_dosage'):
            data['morning_dosage'] = self.ui.spinBox_morning_dosage.value()
        else:
            data['morning_dosage'] = self.medicine_info.get('morning_dosage', 0) if self.medicine_info else 0
        
        if hasattr(self.ui, 'spinBox_noon_dosage'):
            data['noon_dosage'] = self.ui.spinBox_noon_dosage.value()
        else:
            data['noon_dosage'] = self.medicine_info.get('noon_dosage', 0) if self.medicine_info else 0
        
        if hasattr(self.ui, 'spinBox_evening_dosage'):
            data['evening_dosage'] = self.ui.spinBox_evening_dosage.value()
        else:
            data['evening_dosage'] = self.medicine_info.get('evening_dosage', 0) if self.medicine_info else 0
        
        # 设置服药时间 - 将界面选择转换为对应的英文值
        if hasattr(self.ui, 'comboBox_dosage_time'):
            selected_index = self.ui.comboBox_dosage_time.currentIndex()
            # 映射关系：
            # index 0 (饭前服用) -> 'before'
            # index 1 (饭后服用) -> 'after'
            # index 2 (任意时段) -> 'anytime'
            if selected_index == 0:
                data['meal_timing'] = 'before'
            elif selected_index == 1:
                data['meal_timing'] = 'after'
            elif selected_index == 2:
                data['meal_timing'] = 'anytime'
            else:
                data['meal_timing'] = 'before'  # 默认值
        else:
            data['meal_timing'] = self.medicine_info.get('meal_timing', 'before') if self.medicine_info else 'before'
        
        if hasattr(self.ui, 'spinBox_duration_days'):
            data['duration_days'] = self.ui.spinBox_duration_days.value()
        else:
            data['duration_days'] = self.medicine_info.get('duration_days', 7) if self.medicine_info else 7
        
        if hasattr(self.ui, 'comboBox_size'):
            data['pill_size'] = self.ui.comboBox_size.currentText()
        else:
            data['pill_size'] = self.medicine_info.get('pill_size', 'M') if self.medicine_info else 'M'
        
        if hasattr(self.ui, 'comboBox_isActive'):
            data['is_active'] = 1 if self.ui.comboBox_isActive.currentIndex() == 0 else 0
        else:
            data['is_active'] = self.medicine_info.get('is_active', 1) if self.medicine_info else 1
        
        # 处理开始日期
        if hasattr(self.ui, 'dateEdit_start_date'):
            current_start_date = self.ui.dateEdit_start_date.date().toString("yyyy-MM-dd")
            data['start_date'] = current_start_date
            
            # 判断是否需要更新last_dispensed_expiry_date
            if self.is_new_medicine:
                # 新药品：设置为start_date的前一天
                data['last_dispensed_expiry_date'] = self.calculate_last_dispensed_expiry_date(current_start_date)
            else:
                # 现有药品：检查是否修改了开始日期
                if current_start_date != self.original_start_date:
                    # 开始日期被修改了，更新last_dispensed_expiry_date为新开始日期的前一天
                    data['last_dispensed_expiry_date'] = self.calculate_last_dispensed_expiry_date(current_start_date)
                    print(f"开始日期已修改：{self.original_start_date} -> {current_start_date}")
                    print(f"更新last_dispensed_expiry_date为：{data['last_dispensed_expiry_date']}")
                else:
                    # 开始日期未修改，保持原有的last_dispensed_expiry_date
                    data['last_dispensed_expiry_date'] = self.medicine_info.get('last_dispensed_expiry_date', '') if self.medicine_info else ''
        else:
            # 如果没有日期控件，使用默认逻辑
            if self.is_new_medicine:
                # 新药品使用当前日期作为开始日期
                data['start_date'] = datetime.now().strftime('%Y-%m-%d')
                data['last_dispensed_expiry_date'] = self.calculate_last_dispensed_expiry_date(data['start_date'])
            else:
                # 保持原有的开始日期和最后配药到期日期
                data['start_date'] = self.medicine_info.get('start_date', '')
                data['last_dispensed_expiry_date'] = self.medicine_info.get('last_dispensed_expiry_date', '')
        
        return data
    
    def validate_medicine_data(self, data):
        """验证药品数据"""
        if not data['medicine_name']:
            QMessageBox.warning(self, "警告", "请输入药品名称")
            return False
        
        total_dosage = data['morning_dosage'] + data['noon_dosage'] + data['evening_dosage']
        if total_dosage == 0:
            QMessageBox.warning(self, "警告", "请至少设置一个时段的服药剂量")
            return False
        
        return True

class AddMedicineButton(QPushButton):
    """添加药品按钮"""
    add_medicine_clicked = Signal()  # 添加药品按钮点击信号
    
    def __init__(self):
        super().__init__()
        self.setup_button()
        self.clicked.connect(self.on_button_clicked)
    
    def setup_button(self):
        """设置按钮显示"""
        button_text = "＋ 添加药品"
        self.setText(button_text)
        
        # 设置按钮样式
        self.setFont(QFont("Microsoft YaHei", 12, QFont.Weight.Bold))
        self.setMinimumHeight(80)
        self.setMaximumHeight(100)
        self.setSizePolicy(QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Fixed)
        
        # 设置添加按钮的样式
        self.setStyleSheet("""
            QPushButton {
                background-color: #f0f8ff;
                border: 2px dashed #4CAF50;
                border-radius: 8px;
                padding: 10px;
                text-align: center;
                color: #4CAF50;
            }
            QPushButton:hover {
                background-color: #e8f5e8;
                border: 2px dashed #28a745;
                color: #28a745;
            }
            QPushButton:pressed {
                background-color: #d4edda;
                border: 2px solid #28a745;
            }
        """)
    
    def on_button_clicked(self):
        """按钮点击事件"""
        self.add_medicine_clicked.emit()

class MedicineButton(QPushButton):
    """自定义药品按钮"""
    medicine_clicked = Signal(dict)  # 发送药品信息的信号
    
    def __init__(self, medicine_info):
        super().__init__()
        self.medicine_info = medicine_info
        self.setup_button()
        self.clicked.connect(self.on_button_clicked)
    
    def setup_button(self):
        """设置按钮显示"""
        medicine_name = self.medicine_info.get('medicine_name', '未知药品')
        start_date = self.medicine_info.get('start_date', '未知日期')
        
        # 按钮文本：药品名称 + 创建日期
        button_text = f"{medicine_name}\n创建日期: {start_date}"
        self.setText(button_text)
        
        # 设置按钮样式
        self.setFont(QFont("Microsoft YaHei", 10))
        self.setMinimumHeight(80)
        self.setMaximumHeight(100)
        self.setSizePolicy(QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Fixed)
        
        # 根据药品激活状态设置不同颜色
        is_active = int( self.medicine_info.get('is_active', 1))
        if is_active:
            self.setStyleSheet("""
                QPushButton {
                    background-color: #e8f5e8;
                    border: 2px solid #4CAF50;
                    border-radius: 8px;
                    padding: 10px;
                    text-align: center;
                }
                QPushButton:hover {
                    background-color: #d4edda;
                    border-color: #28a745;
                }
                QPushButton:pressed {
                    background-color: #c3e6cb;
                }
            """)
        else:
            self.setStyleSheet("""
                QPushButton {
                    background-color: #f8f9fa;
                    border: 2px solid #6c757d;
                    border-radius: 8px;
                    padding: 10px;
                    text-align: center;
                    color: #6c757d;
                }
                QPushButton:hover {
                    background-color: #e9ecef;
                }
                QPushButton:pressed {
                    background-color: #dee2e6;
                }
            """)
    
    def on_button_clicked(self):
        """按钮点击事件"""
        self.medicine_clicked.emit(self.medicine_info)

class PatientPrescriptionMainWindow(QMainWindow):
    """患者处方管理主窗口"""
    
    def __init__(self):
        super().__init__()
        # 使用UI文件
        self.ui = Ui_MainWindow()
        self.ui.setupUi(self)
        
        # 初始化数据管理器
        # self.rx_manager = PatientPrescriptionManager(server_url="https://ixd.sjtu.edu.cn/flask/packer")
        self.rx_manager = PatientPrescriptionManager()
        self.rx_manager.load_prescriptions()
        
        # 初始化患者信息管理器
        # self.patient_manager = PatientInfoManager(server_url="https://ixd.sjtu.edu.cn/flask/packer")
        self.patient_manager = PatientInfoManager()
        
        # 初始化状态变量
        self.current_patient_info = None
        self.medicine_buttons = []
        self.add_medicine_button = None  # 添加药品按钮
        
        # 设置药品容器的网格布局
        self.medicine_grid_layout = QGridLayout(self.ui.medicine_container)
        
        # 连接信号
        self.connect_signals()
        
        # 设置样式
        self.setup_styles()
        
        # 初始状态
        self.setup_initial_state()
        
        # 添加新增患者按钮
        self.add_patient_button()
    
    def add_patient_button(self):
        """添加新增患者按钮到搜索组"""
        # 创建新增患者按钮
        self.add_patient_btn = QPushButton("新增患者")
        self.add_patient_btn.setObjectName("add_patient_button")
        self.add_patient_btn.setFont(QFont("Microsoft YaHei", 10, QFont.Weight.Bold))
        self.add_patient_btn.clicked.connect(self.show_add_patient_dialog)
        
        # 添加到搜索布局
        self.ui.search_layout.addWidget(self.add_patient_btn)
    
    @Slot()
    def show_add_patient_dialog(self):
        """显示新增患者对话框"""
        dialog = AddPatientDialog(self)
        
        if dialog.exec() == QDialog.DialogCode.Accepted:
            patient_data = dialog.get_patient_data()
            
            # 使用PatientInfoManager验证和保存患者信息
            is_valid, error_msg = self.patient_manager.validate_patient_data(patient_data)
            if not is_valid:
                QMessageBox.warning(self, "警告", error_msg)
                return
            
            # 检查患者是否已存在
            if self.patient_manager.check_patient_exists(patient_data['patientName'], patient_data['patientId']):
                QMessageBox.warning(self, "警告", "该患者姓名或ID已存在，请检查后重新输入")
                return
            
            # 保存患者信息
            if self.patient_manager.write_local_patient_list(patient_data):
                QMessageBox.information(self, "成功", f"患者 '{patient_data['patientName']}' 添加成功")
                
                # 同步到服务器
                self.patient_manager.save_patient_list()
                
                # 自动搜索刚添加的患者
                self.ui.search_input.setText(patient_data['patientName'])
                self.search_patient()
            else:
                QMessageBox.critical(self, "错误", "保存患者信息失败")
    
    @Slot()
    def show_add_medicine_dialog(self):
        """显示新增药品对话框"""
        if not self.current_patient_info:
            QMessageBox.warning(self, "警告", "请先搜索并选择一个患者，然后再添加药品")
            return
        
        # 创建新增药品对话框，传入空的药品信息和新增标志
        dialog = MedicineSettingDialog(None, self.current_patient_info, self.rx_manager, is_new_medicine=True, parent=self)
        dialog.medicine_added.connect(self.refresh_current_patient)
        dialog.exec()
    
    @Slot()
    def refresh_current_patient(self):
        """刷新当前患者的数据显示"""
        if self.current_patient_info:
            # 重新加载处方数据
            self.rx_manager.load_prescriptions()
            
            # 重新搜索当前患者
            patient_name = self.current_patient_info['patient_name']
            self.ui.search_input.setText(patient_name)
            self.search_patient()
        
    def connect_signals(self):
        """连接信号和槽"""
        # 搜索相关信号
        self.ui.search_input.returnPressed.connect(self.search_patient)
        self.ui.search_button.clicked.connect(self.search_patient)
        self.ui.clear_button.clicked.connect(self.clear_search)
        
        # 菜单信号
        self.ui.refresh_action.triggered.connect(self.refresh_data)
        self.ui.exit_action.triggered.connect(self.close)
        self.ui.about_action.triggered.connect(self.show_about)
    
    def setup_styles(self):
        """设置样式"""
        self.setStyleSheet("""
            QPushButton#search_button, QPushButton#clear_button, QPushButton#add_patient_button {
                background-color: #0078d4;
                color: white;
                border: none;
                padding: 10px 20px;
                border-radius: 6px;
                font-weight: bold;
            }
            QPushButton#search_button:hover, QPushButton#clear_button:hover, QPushButton#add_patient_button:hover {
                background-color: #106ebe;
            }
            QPushButton#search_button:pressed, QPushButton#clear_button:pressed, QPushButton#add_patient_button:pressed {
                background-color: #005a9e;
            }
            QLineEdit {
                border: 2px solid #cccccc;
                border-radius: 6px;
                padding: 10px;
                font-size: 11px;
            }
            QLineEdit:focus {
                border: 2px solid #0078d4;
            }
        """)
        
        # 设置按钮对象名称用于样式
        self.ui.search_button.setObjectName("search_button")
        self.ui.clear_button.setObjectName("clear_button")
    
    def setup_initial_state(self):
        """设置初始状态"""
        # 初始隐藏滚动区域
        self.ui.scroll_area.hide()
        self.ui.no_medicine_label.show()
    
    @Slot()
    def search_patient(self):
        """搜索患者"""
        patient_name = self.ui.search_input.text().strip()
        if not patient_name:
            QMessageBox.warning(self, "警告", "请输入患者姓名")
            return
        
        try:
            # 使用PatientInfoManager查找患者
            patient_csv_info = self.patient_manager.find_patient_by_name(patient_name)
            if not patient_csv_info:
                QMessageBox.information(self, "信息", f"患者 '{patient_name}' 不在患者列表中")
                self.clear_patient_display()
                return
            
            # 在患者列表中找到了，再从处方数据中查找药品信息
            patient_data = self.get_patient_prescription_data(patient_name)
            
            if not patient_data:
                # 显示患者基本信息但无药品
                self.display_patient_info_without_prescription(patient_csv_info)
                return
            
            # 合并CSV中的基本信息和处方数据
            patient_data = self.merge_patient_info(patient_csv_info, patient_data)
            
            # 显示患者信息和药品
            self.display_patient_info(patient_data)
            self.display_patient_medicines(patient_data)
            
        except Exception as e:
            QMessageBox.critical(self, "错误", f"搜索患者时出错: {str(e)}")
    
    def get_patient_prescription_data(self, patient_name):
        """从处方管理器中获取患者的处方数据"""
        if self.rx_manager.df is None or self.rx_manager.df.empty:
            return None
        
        try:
            # 确保patient_name列是字符串类型
            if 'patient_name' in self.rx_manager.df.columns:
                self.rx_manager.df['patient_name'] = self.rx_manager.df['patient_name'].astype(str)
            
            # 在处方数据中查找患者
            matching_patients = self.rx_manager.df[
                self.rx_manager.df['patient_name'].str.contains(patient_name, na=False, case=False)
            ]
            
            if matching_patients.empty:
                return None
            
            # 获取第一个匹配的患者ID的所有记录
            patient_id = matching_patients.iloc[0]['patientId']
            patient_medicines = self.rx_manager.df[self.rx_manager.df['patientId'] == patient_id]
            
            # 获取患者基本信息
            first_record = patient_medicines.iloc[0]
            
            patient_info = {
                'patient_id': str(first_record['patientId']),
                'patient_name': first_record['patient_name'],
                'medicines': []
            }
            
            # 获取所有药品信息
            for _, medicine_row in patient_medicines.iterrows():
                medicine_info = {
                    'medicine_name': medicine_row['medicine_name'],
                    'morning_dosage': medicine_row['morning_dosage'],
                    'noon_dosage': medicine_row['noon_dosage'],
                    'evening_dosage': medicine_row['evening_dosage'],
                    'meal_timing': medicine_row['meal_timing'],
                    'start_date': medicine_row['start_date'],
                    'duration_days': medicine_row['duration_days'],
                    'last_dispensed_expiry_date': medicine_row['last_dispensed_expiry_date'],
                    'is_active': medicine_row['is_active'],
                    'pill_size': medicine_row.get('pill_size', 'M')
                }
                patient_info['medicines'].append(medicine_info)
            
            return patient_info
            
        except Exception as e:
            print(f"获取处方数据时出错: {str(e)}")
            return None
    
    def merge_patient_info(self, csv_info, prescription_info):
        """合并CSV中的患者信息和处方信息"""
        # 以CSV中的信息为准，如果处方中有更详细的信息则使用处方中的
        merged_info = prescription_info.copy()
        
        # 优先使用CSV中的基本信息
        merged_info['patient_id'] = csv_info['patient_id']
        merged_info['patient_name'] = csv_info['patient_name']
        
        return merged_info
    
    def display_patient_info_without_prescription(self, patient_csv_info):
        """显示没有处方数据的患者信息（来自CSV）"""
        self.current_patient_info = {
            'patient_name': patient_csv_info['patient_name'],
            'patient_id': patient_csv_info['patient_id'],
            'medicines': []
        }
        
        info_text = (f"患者姓名: {patient_csv_info['patient_name']} | "
                    f"患者ID: {patient_csv_info['patient_id']} | "
                    f"药品数量: 0")
        
        self.ui.patient_info_label.setText(info_text)
        self.ui.patient_info_label.setStyleSheet("color: orange; padding: 10px; font-weight: bold;")
        
        # 显示添加药品按钮
        self.ui.no_medicine_label.hide()
        self.ui.scroll_area.show()
        self.display_add_medicine_button_only()
    
    def display_patient_info(self, patient):
        """显示患者基本信息"""
        self.current_patient_info = patient
        
        info_text = (f"患者姓名: {patient['patient_name']} | "
                    f"患者ID: {patient['patient_id']} | "
                    f"药品数量: {len(patient['medicines'])}")
        
        self.ui.patient_info_label.setText(info_text)
        self.ui.patient_info_label.setStyleSheet("color: black; padding: 10px; font-weight: bold;")
    
    def display_add_medicine_button_only(self):
        """只显示添加药品按钮"""
        # 清除现有按钮
        self.clear_medicine_buttons()
        
        # 创建并添加"添加药品"按钮
        self.add_medicine_button = AddMedicineButton()
        self.add_medicine_button.add_medicine_clicked.connect(self.show_add_medicine_dialog)
        
        # 添加到网格布局的第一个位置
        self.medicine_grid_layout.addWidget(self.add_medicine_button, 0, 0)
    
    def display_patient_medicines(self, patient):
        """显示患者药品按钮"""
        # 清除现有按钮
        self.clear_medicine_buttons()
        
        medicines = patient['medicines']
        
        # 隐藏提示标签，显示滚动区域
        self.ui.no_medicine_label.hide()
        self.ui.scroll_area.show()
        
        # 创建并添加"添加药品"按钮
        self.add_medicine_button = AddMedicineButton()
        self.add_medicine_button.add_medicine_clicked.connect(self.show_add_medicine_dialog)
        self.medicine_grid_layout.addWidget(self.add_medicine_button, 0, 0)
        
        if not medicines:
            return
        
        # 创建药品按钮 (每行3个按钮，第一个位置已被添加药品按钮占用)
        columns = 3
        for i, medicine in enumerate(medicines):
            # 计算位置，跳过第一个位置
            position = i + 1  # 因为第一个位置是添加药品按钮
            row = position // columns
            col = position % columns
            
            # 创建药品按钮
            medicine_button = MedicineButton(medicine)
            medicine_button.medicine_clicked.connect(self.handle_medicine_clicked)
            
            # 添加到网格布局
            self.medicine_grid_layout.addWidget(medicine_button, row, col)
            self.medicine_buttons.append(medicine_button)
    
    def clear_medicine_buttons(self):
        """清除所有药品按钮"""
        for button in self.medicine_buttons:
            button.deleteLater()
        self.medicine_buttons.clear()
        
        # 清除添加药品按钮
        if self.add_medicine_button:
            self.add_medicine_button.deleteLater()
            self.add_medicine_button = None
        
        # 清除网格布局中的所有项目
        while self.medicine_grid_layout.count():
            child = self.medicine_grid_layout.takeAt(0)
            if child.widget():
                child.widget().deleteLater()
    
    def clear_patient_display(self):
        """清除患者显示信息"""
        self.current_patient_info = None
        self.ui.patient_info_label.setText("请搜索患者以显示信息")
        self.ui.patient_info_label.setStyleSheet("color: gray; padding: 10px;")
        
        self.clear_medicine_buttons()
        self.ui.scroll_area.hide()
        self.ui.no_medicine_label.setText("请先搜索患者")
        self.ui.no_medicine_label.show()
    
    @Slot()
    def clear_search(self):
        """清空搜索"""
        self.ui.search_input.clear()
        self.ui.search_input.setFocus()
        self.clear_patient_display()
    
    @Slot(dict)
    def handle_medicine_clicked(self, medicine_info):
        """药品按钮点击事件处理器 - 打开药品设置对话框"""
        dialog = MedicineSettingDialog(medicine_info, self.current_patient_info, self.rx_manager, is_new_medicine=False, parent=self)
        dialog.medicine_updated.connect(self.refresh_current_patient)
        dialog.exec()
    
    @Slot()
    def refresh_data(self):
        """刷新数据"""
        try:
            # 使用PatientInfoManager刷新患者列表
            self.patient_manager.refresh_data()
            self.rx_manager.load_prescriptions()
            QMessageBox.information(self, "成功", "数据已刷新")
            
            # 如果当前有显示的患者，重新搜索显示
            if self.current_patient_info:
                patient_name = self.current_patient_info['patient_name']
                self.ui.search_input.setText(patient_name)
                self.search_patient()
                
        except Exception as e:
            QMessageBox.critical(self, "错误", f"刷新数据时出错: {str(e)}")
    
    @Slot()
    def show_about(self):
        """显示关于信息"""
        patient_count = self.patient_manager.get_patient_count()
        about_text = (f"患者处方管理系统 v1.0\n\n"
                     f"用于管理患者处方信息的应用程序\n"
                     f"当前患者列表中有 {patient_count} 个患者")
        QMessageBox.about(self, "关于", about_text)

def main():
    """主函数"""
    app = QApplication(sys.argv)
    
    # 设置应用程序属性
    app.setApplicationName("患者处方管理系统")
    app.setApplicationVersion("1.0")
    
    # 创建主窗口
    window = PatientPrescriptionMainWindow()
    window.show()
    
    # 运行应用程序
    sys.exit(app.exec())

if __name__ == "__main__":
    main()