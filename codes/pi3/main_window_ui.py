# -*- coding: utf-8 -*-

################################################################################
## Form generated from reading UI file 'main_window.ui'
##
## Created by: Qt User Interface Compiler version 6.9.0
##
## WARNING! All changes made in this file will be lost when recompiling UI file!
################################################################################

from PySide6.QtCore import (QCoreApplication, QDate, QDateTime, QLocale,
    QMetaObject, QObject, QPoint, QRect,
    QSize, QTime, QUrl, Qt)
from PySide6.QtGui import (QBrush, QColor, QConicalGradient, QCursor,
    QFont, QFontDatabase, QGradient, QIcon,
    QImage, QKeySequence, QLinearGradient, QPainter,
    QPalette, QPixmap, QRadialGradient, QTransform)
from PySide6.QtWidgets import (QApplication, QHBoxLayout, QLabel, QMainWindow,
    QProgressBar, QPushButton, QSizePolicy, QSpacerItem,
    QStackedWidget, QVBoxLayout, QWidget)
import icons_rc
import images_rc

class Ui_MainWindow(object):
    def setupUi(self, MainWindow):
        if not MainWindow.objectName():
            MainWindow.setObjectName(u"MainWindow")
        MainWindow.resize(960, 600)
        MainWindow.setStyleSheet(u"QPushButton {\n"
"    min-width: 213px;\n"
"    min-height: 61px;\n"
"    max-width: 213px;\n"
"    max-height: 61px;\n"
"}\n"
"")
        self.centralwidget = QWidget(MainWindow)
        self.centralwidget.setObjectName(u"centralwidget")
        self.horizontalLayout = QHBoxLayout(self.centralwidget)
        self.horizontalLayout.setSpacing(0)
        self.horizontalLayout.setObjectName(u"horizontalLayout")
        self.horizontalLayout.setContentsMargins(0, 0, -1, 0)
        self.sideBar = QWidget(self.centralwidget)
        self.sideBar.setObjectName(u"sideBar")
        sizePolicy = QSizePolicy(QSizePolicy.Policy.Preferred, QSizePolicy.Policy.Preferred)
        sizePolicy.setHorizontalStretch(0)
        sizePolicy.setVerticalStretch(0)
        sizePolicy.setHeightForWidth(self.sideBar.sizePolicy().hasHeightForWidth())
        self.sideBar.setSizePolicy(sizePolicy)
        self.sideBar.setMinimumSize(QSize(162, 0))
        self.sideBar.setMaximumSize(QSize(162, 16777215))
        self.sideBar.setBaseSize(QSize(160, 160))
        self.sideBar.setStyleSheet(u"QWidget {\n"
"    border-right: 2px solid #ECECEC;\n"
"    background-color: #F9F9F9;\n"
"    min-width: 160px;\n"
"    max-width: 160px;\n"
"}\n"
"")
        self.verticalLayout = QVBoxLayout(self.sideBar)
        self.verticalLayout.setObjectName(u"verticalLayout")
        self.verticalLayout.setContentsMargins(-1, 0, -1, -1)
        self.profile_info = QWidget(self.sideBar)
        self.profile_info.setObjectName(u"profile_info")
        self.profile_info.setStyleSheet(u"QWidget\n"
"{\n"
"    min-width: 147px;\n"
"    max-width: 147px;\n"
"    min-height: 60px;\n"
"    max-height: 60px;\n"
"	border-bottom: 2px solid #ECECEC;\n"
"	border-right: 0\n"
"}")
        self.img_profile_photo = QLabel(self.profile_info)
        self.img_profile_photo.setObjectName(u"img_profile_photo")
        self.img_profile_photo.setGeometry(QRect(0, 10, 40, 42))
        self.img_profile_photo.setMinimumSize(QSize(40, 42))
        self.img_profile_photo.setMaximumSize(QSize(40, 42))
        self.img_profile_photo.setStyleSheet(u"QLabel\n"
"{\n"
"    min-width: 40px;\n"
"    max-width: 40px;\n"
"    min-height: 40px;\n"
"    max-height: 40px;\n"
"}")
        self.img_profile_photo.setPixmap(QPixmap(u":/images/profile_photo.png"))
        self.img_profile_photo.setScaledContents(True)

        self.verticalLayout.addWidget(self.profile_info)

        self.btn_dispense_page = QPushButton(self.sideBar)
        self.btn_dispense_page.setObjectName(u"btn_dispense_page")
        self.btn_dispense_page.setStyleSheet(u"QPushButton {\n"
"    /* Fixed size */\n"
"    min-width: 147px;\n"
"    max-width: 147px;\n"
"    min-height: 34px;\n"
"    max-height: 34px;\n"
"\n"
"    /* Text style */\n"
"    color: #3A3A3A;\n"
"    font-family: \"Microsoft YaHei\", \"Inter\";\n"
"    font-size: 16px;\n"
"    font-style: normal;\n"
"    font-weight: 500;\n"
"\n"
"    /* Idle background */\n"
"    border-radius: 8px;\n"
"    background-color: #F9F9F9;\n"
"    border: none;\n"
"}\n"
"\n"
"QPushButton:hover {\n"
"    background-color: #ECECEC;  /* lighter than pressed */\n"
"}\n"
"\n"
"QPushButton:pressed {\n"
"    background-color: #E2E2E2;  /* darker when pressed */\n"
"}\n"
"\n"
"")
        icon = QIcon()
        icon.addFile(u":/icons/icon_machine.png", QSize(), QIcon.Mode.Normal, QIcon.State.Off)
        self.btn_dispense_page.setIcon(icon)
        self.btn_dispense_page.setIconSize(QSize(28, 28))

        self.verticalLayout.addWidget(self.btn_dispense_page)

        self.btn_setting_page = QPushButton(self.sideBar)
        self.btn_setting_page.setObjectName(u"btn_setting_page")
        self.btn_setting_page.setStyleSheet(u"QPushButton {\n"
"    /* Fixed size */\n"
"    min-width: 147px;\n"
"    max-width: 147px;\n"
"    min-height: 34px;\n"
"    max-height: 34px;\n"
"\n"
"    /* Text style */\n"
"    color: #3A3A3A;\n"
"    font-family: \"Microsoft YaHei\", \"Inter\";\n"
"    font-size: 16px;\n"
"    font-style: normal;\n"
"    font-weight: 500;\n"
"\n"
"    /* Idle background */\n"
"    border-radius: 8px;\n"
"    background-color: #F9F9F9;\n"
"    border: none;\n"
"}\n"
"\n"
"QPushButton:hover {\n"
"    background-color: #ECECEC;  /* lighter than pressed */\n"
"}\n"
"\n"
"QPushButton:pressed {\n"
"    background-color: #E2E2E2;  /* darker when pressed */\n"
"}\n"
"\n"
"")
        icon1 = QIcon()
        icon1.addFile(u":/icons/icon_setting.png", QSize(), QIcon.Mode.Normal, QIcon.State.Off)
        self.btn_setting_page.setIcon(icon1)
        self.btn_setting_page.setIconSize(QSize(28, 28))

        self.verticalLayout.addWidget(self.btn_setting_page)

        self.verticalSpacer = QSpacerItem(20, 40, QSizePolicy.Policy.Minimum, QSizePolicy.Policy.Expanding)

        self.verticalLayout.addItem(self.verticalSpacer)

        self.btn_refresh_database = QPushButton(self.sideBar)
        self.btn_refresh_database.setObjectName(u"btn_refresh_database")
        self.btn_refresh_database.setStyleSheet(u"QPushButton {\n"
"    /* Fixed size */\n"
"    min-width: 147px;\n"
"    max-width: 147px;\n"
"    min-height: 34px;\n"
"    max-height: 34px;\n"
"\n"
"    /* Text style */\n"
"    color: #3A3A3A;\n"
"    font-family: \"Microsoft YaHei\", \"Inter\";\n"
"    font-size: 16px;\n"
"    font-style: normal;\n"
"    font-weight: 500;\n"
"\n"
"    /* Idle background */\n"
"    border-radius: 8px;\n"
"    background-color: #F9F9F9;\n"
"    border: none;\n"
"}\n"
"\n"
"QPushButton:hover {\n"
"    background-color: #ECECEC;  /* lighter than pressed */\n"
"}\n"
"\n"
"QPushButton:pressed {\n"
"    background-color: #E2E2E2;  /* darker when pressed */\n"
"}\n"
"\n"
"")
        icon2 = QIcon()
        icon2.addFile(u":/icons/icon_refresh.png", QSize(), QIcon.Mode.Normal, QIcon.State.Off)
        self.btn_refresh_database.setIcon(icon2)
        self.btn_refresh_database.setIconSize(QSize(28, 28))

        self.verticalLayout.addWidget(self.btn_refresh_database)

        self.btn_contact_us_page = QPushButton(self.sideBar)
        self.btn_contact_us_page.setObjectName(u"btn_contact_us_page")
        self.btn_contact_us_page.setStyleSheet(u"QPushButton {\n"
"    /* Fixed size */\n"
"    min-width: 147px;\n"
"    max-width: 147px;\n"
"    min-height: 34px;\n"
"    max-height: 34px;\n"
"\n"
"    /* Text style */\n"
"    color: #3A3A3A;\n"
"    font-family: \"Microsoft YaHei\", \"Inter\";\n"
"    font-size: 16px;\n"
"    font-style: normal;\n"
"    font-weight: 500;\n"
"\n"
"    /* Idle background */\n"
"    border-radius: 8px;\n"
"    background-color: #F9F9F9;\n"
"    border: none;\n"
"}\n"
"\n"
"QPushButton:hover {\n"
"    background-color: #ECECEC;  /* lighter than pressed */\n"
"}\n"
"\n"
"QPushButton:pressed {\n"
"    background-color: #E2E2E2;  /* darker when pressed */\n"
"}\n"
"\n"
"")
        icon3 = QIcon()
        icon3.addFile(u":/icons/icon_help.png", QSize(), QIcon.Mode.Normal, QIcon.State.Off)
        self.btn_contact_us_page.setIcon(icon3)
        self.btn_contact_us_page.setIconSize(QSize(28, 28))

        self.verticalLayout.addWidget(self.btn_contact_us_page)


        self.horizontalLayout.addWidget(self.sideBar)

        self.stackedWidget = QStackedWidget(self.centralwidget)
        self.stackedWidget.setObjectName(u"stackedWidget")
        self.start_page = QWidget()
        self.start_page.setObjectName(u"start_page")
        self.btn_start_find_patient = QPushButton(self.start_page)
        self.btn_start_find_patient.setObjectName(u"btn_start_find_patient")
        self.btn_start_find_patient.setGeometry(QRect(290, 300, 260, 60))
        self.btn_start_find_patient.setStyleSheet(u"QPushButton {\n"
"    min-width: 260px;\n"
"    min-height: 60px;\n"
"    max-width: 260px;\n"
"    max-height: 60px;\n"
"\n"
"    background-color: #8BD2C0;      /* \u80cc\u666f\u8272 */\n"
"    border-radius: 30px;            /* \u5706\u89d2 */\n"
"\n"
"    color: #000;                    /* \u6587\u5b57\u989c\u8272 */\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 20px;\n"
"    font-style: normal;\n"
"    font-weight: 500;\n"
"}\n"
"\n"
"/* \u9f20\u6807\u60ac\u505c */\n"
"QPushButton:hover {\n"
"    background-color: #7CC9B3;  /* \u7a0d\u6df1\u4e00\u70b9\u7684\u7eff\u8272 */\n"
"}\n"
"\n"
"/* \u9f20\u6807\u6309\u4e0b */\n"
"QPushButton:pressed {\n"
"    background-color: #6BBFA5;  /* \u66f4\u6df1\u4e00\u70b9\u7684\u7eff\u8272 */\n"
"}\n"
"\n"
"\n"
"")
        self.label = QLabel(self.start_page)
        self.label.setObjectName(u"label")
        self.label.setGeometry(QRect(380, 190, 76, 76))
        self.label.setPixmap(QPixmap(u":/icons/capsule.png"))
        self.label.setScaledContents(True)
        self.stackedWidget.addWidget(self.start_page)
        self.scan_qrcode_page = QWidget()
        self.scan_qrcode_page.setObjectName(u"scan_qrcode_page")
        self.img_cam_frame = QLabel(self.scan_qrcode_page)
        self.img_cam_frame.setObjectName(u"img_cam_frame")
        self.img_cam_frame.setGeometry(QRect(350, 160, 400, 300))
        self.img_cam_frame.setStyleSheet(u"QLabel\n"
"{\n"
"    min-width: 400px;\n"
"    max-width: 400px;\n"
"    min-height: 300px;\n"
"    max-height: 300px;\n"
"}")
        self.img_cam_frame.setScaledContents(True)
        self.txt_ = QLabel(self.scan_qrcode_page)
        self.txt_.setObjectName(u"txt_")
        self.txt_.setGeometry(QRect(20, 50, 611, 41))
        self.txt_.setStyleSheet(u"QLabel {\n"
"    color: #737373;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 22px;\n"
"    font-style: normal;\n"
"    font-weight: 400;   /* Normal */\n"
"}\n"
"")
        self.txt_1 = QLabel(self.scan_qrcode_page)
        self.txt_1.setObjectName(u"txt_1")
        self.txt_1.setGeometry(QRect(20, 20, 181, 31))
        self.txt_1.setStyleSheet(u"QLabel {\n"
"    color: #000;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 24px;\n"
"    font-style: normal;\n"
"    font-weight: 700;   /* Bold */\n"
"}\n"
"")
        self.label_2 = QLabel(self.scan_qrcode_page)
        self.label_2.setObjectName(u"label_2")
        self.label_2.setGeometry(QRect(100, 200, 210, 250))
        self.label_2.setStyleSheet(u"QLabel\n"
"{\n"
"    min-width: 210px;\n"
"    max-width: 210px;\n"
"    min-height: 250px;\n"
"    max-height: 250px;\n"
"\n"
"}")
        self.label_2.setPixmap(QPixmap(u":/images/img_scanQR_guide.png"))
        self.label_2.setScaledContents(True)
        self.stackedWidget.addWidget(self.scan_qrcode_page)
        self.scan_rfid_page = QWidget()
        self.scan_rfid_page.setObjectName(u"scan_rfid_page")
        self.stackedWidget.addWidget(self.scan_rfid_page)
        self.rx_page = QWidget()
        self.rx_page.setObjectName(u"rx_page")
        self.btn_start_dispensing = QPushButton(self.rx_page)
        self.btn_start_dispensing.setObjectName(u"btn_start_dispensing")
        self.btn_start_dispensing.setGeometry(QRect(270, 420, 260, 60))
        self.btn_start_dispensing.setStyleSheet(u"QPushButton {\n"
"    min-width: 260px;\n"
"    min-height: 60px;\n"
"    max-width: 260px;\n"
"    max-height: 60px;\n"
"\n"
"    background-color: #8BD2C0;      /* \u80cc\u666f\u8272 */\n"
"    border-radius: 30px;            /* \u5706\u89d2 */\n"
"\n"
"    color: #000;                    /* \u6587\u5b57\u989c\u8272 */\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 20px;\n"
"    font-style: normal;\n"
"    font-weight: 500;\n"
"}\n"
"\n"
"/* \u9f20\u6807\u60ac\u505c */\n"
"QPushButton:hover {\n"
"    background-color: #7CC9B3;  /* \u7a0d\u6df1\u4e00\u70b9\u7684\u7eff\u8272 */\n"
"}\n"
"\n"
"/* \u9f20\u6807\u6309\u4e0b */\n"
"QPushButton:pressed {\n"
"    background-color: #6BBFA5;  /* \u66f4\u6df1\u4e00\u70b9\u7684\u7eff\u8272 */\n"
"}\n"
"")
        self.txt_load_prescription_info = QLabel(self.rx_page)
        self.txt_load_prescription_info.setObjectName(u"txt_load_prescription_info")
        self.txt_load_prescription_info.setGeometry(QRect(24, 90, 261, 16))
        self.txt_load_prescription_info.setStyleSheet(u"QLabel\n"
"{\n"
"color: #2E344F;\n"
"font-family: \"Microsoft YaHei\";\n"
"font-size: 16px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.txt_load_prescription_info.setAlignment(Qt.AlignLeading|Qt.AlignLeft|Qt.AlignVCenter)
        self.txt_2 = QLabel(self.rx_page)
        self.txt_2.setObjectName(u"txt_2")
        self.txt_2.setGeometry(QRect(20, 50, 611, 41))
        self.txt_2.setStyleSheet(u"QLabel {\n"
"    color: #737373;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 22px;\n"
"    font-style: normal;\n"
"    font-weight: 400;   /* Normal */\n"
"}\n"
"")
        self.txt_3 = QLabel(self.rx_page)
        self.txt_3.setObjectName(u"txt_3")
        self.txt_3.setGeometry(QRect(20, 20, 181, 31))
        self.txt_3.setStyleSheet(u"QLabel {\n"
"    color: #000;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 24px;\n"
"    font-style: normal;\n"
"    font-weight: 700;   /* Bold */\n"
"}\n"
"")
        self.label_3 = QLabel(self.rx_page)
        self.label_3.setObjectName(u"label_3")
        self.label_3.setGeometry(QRect(190, 160, 400, 240))
        self.label_3.setStyleSheet(u"QLabel\n"
"{\n"
"    min-width: 400px;\n"
"    max-width: 400px;\n"
"    min-height: 240px;\n"
"    max-height: 240px;\n"
"\n"
"}")
        self.label_3.setPixmap(QPixmap(u":/images/img_put_in_tray_guide.png"))
        self.label_3.setScaledContents(True)
        self.stackedWidget.addWidget(self.rx_page)
        self.dispensing_page = QWidget()
        self.dispensing_page.setObjectName(u"dispensing_page")
        self.img_pillcount_frame = QLabel(self.dispensing_page)
        self.img_pillcount_frame.setObjectName(u"img_pillcount_frame")
        self.img_pillcount_frame.setGeometry(QRect(450, 160, 320, 240))
        self.img_pillcount_frame.setStyleSheet(u"QLabel\n"
"{\n"
"    min-width: 320px;\n"
"    max-width: 320px;\n"
"    min-height: 240px;\n"
"    max-height: 240px;\n"
"}")
        self.txt_patient_info = QLabel(self.dispensing_page)
        self.txt_patient_info.setObjectName(u"txt_patient_info")
        self.txt_patient_info.setGeometry(QRect(60, 100, 181, 31))
        self.txt_patient_info.setStyleSheet(u"QLabel {\n"
"    color: #3A3A3A;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 16px;\n"
"    font-style: normal;\n"
"    font-weight: 400;   /* Normal */\n"
"}\n"
"")
        self.txt_current_medicine_percentage = QLabel(self.dispensing_page)
        self.txt_current_medicine_percentage.setObjectName(u"txt_current_medicine_percentage")
        self.txt_current_medicine_percentage.setGeometry(QRect(290, 490, 71, 41))
        self.txt_current_medicine_percentage.setStyleSheet(u"QLabel\n"
"{\n"
"color: #515151;\n"
"font-family: \"Microsoft YaHei\";\n"
"font-size: 24px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.progressBar_current_medicine = QProgressBar(self.dispensing_page)
        self.progressBar_current_medicine.setObjectName(u"progressBar_current_medicine")
        self.progressBar_current_medicine.setGeometry(QRect(120, 499, 161, 26))
        self.progressBar_current_medicine.setStyleSheet(u"QProgressBar {\n"
"    border: 2px solid #ddd;\n"
"    border-radius: 10px;  /* \u7a0d\u5fae\u5927\u4e00\u70b9\u7684\u5706\u89d2 */\n"
"    background-color: hsla(0, 0%, 84%, 1);\n"
"    text-align: center;\n"
"    padding: 1px;  /* \u5185\u8fb9\u8ddd */\n"
"}\n"
"\n"
"QProgressBar::chunk {\n"
"    background-color: hsla(165, 33%, 62%, 1);\n"
"    border-radius: 7px;  /* \u6bd4\u5916\u6846\u5c0f2px */\n"
"    margin: 1px;  /* \u4e0epadding\u914d\u5408\u4f7f\u7528 */\n"
"}")
        self.progressBar_current_medicine.setValue(0)
        self.progressBar_current_medicine.setTextVisible(False)
        self.pills_num_msg_2 = QLabel(self.dispensing_page)
        self.pills_num_msg_2.setObjectName(u"pills_num_msg_2")
        self.pills_num_msg_2.setGeometry(QRect(300, 440, 31, 41))
        self.pills_num_msg_2.setStyleSheet(u"QLabel {\n"
"    color: #3A3A3A;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 24px;\n"
"    font-style: normal;\n"
"    font-weight: 700;   /* Bold */\n"
"}\n"
"")
        self.pills_num_msg_1 = QLabel(self.dispensing_page)
        self.pills_num_msg_1.setObjectName(u"pills_num_msg_1")
        self.pills_num_msg_1.setGeometry(QRect(110, 440, 101, 41))
        font = QFont()
        font.setFamilies([u"Microsoft YaHei"])
        font.setBold(True)
        font.setItalic(False)
        self.pills_num_msg_1.setFont(font)
        self.pills_num_msg_1.setStyleSheet(u"QLabel {\n"
"    color: #3A3A3A;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 24px;\n"
"    font-style: normal;\n"
"    font-weight: 700;   /* Bold */\n"
"}\n"
"")
        self.txt_pills_num_needed = QLabel(self.dispensing_page)
        self.txt_pills_num_needed.setObjectName(u"txt_pills_num_needed")
        self.txt_pills_num_needed.setGeometry(QRect(184, 410, 111, 71))
        self.txt_pills_num_needed.setStyleSheet(u"QLabel\n"
"{\n"
"color: #3A3A3A;\n"
"text-align: center;\n"
"font-family: \"Microsoft YaHei\";\n"
"font-size: 64px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}\n"
"\n"
"")
        self.txt_pills_num_needed.setAlignment(Qt.AlignCenter)
        self.txt_6 = QLabel(self.dispensing_page)
        self.txt_6.setObjectName(u"txt_6")
        self.txt_6.setGeometry(QRect(10, 10, 181, 31))
        self.txt_6.setStyleSheet(u"QLabel {\n"
"    color: #3A3A3A;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 24px;\n"
"    font-style: normal;\n"
"    font-weight: 700;   /* Bold */\n"
"}\n"
"")
        self.txt_7 = QLabel(self.dispensing_page)
        self.txt_7.setObjectName(u"txt_7")
        self.txt_7.setGeometry(QRect(10, 40, 401, 41))
        self.txt_7.setStyleSheet(u"QLabel {\n"
"    color: #737373;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 22px;\n"
"    font-style: normal;\n"
"    font-weight: 400;   /* Normal */\n"
"}\n"
"")
        self.img_current_medicine = QLabel(self.dispensing_page)
        self.img_current_medicine.setObjectName(u"img_current_medicine")
        self.img_current_medicine.setGeometry(QRect(60, 160, 320, 240))
        self.img_current_medicine.setStyleSheet(u"QLabel\n"
"{\n"
"    min-width: 320px;\n"
"    max-width: 320px;\n"
"    min-height: 240px;\n"
"    max-height: 240px;\n"
"}")
        self.img_current_medicine.setPixmap(QPixmap(u":/images/img_medicine_placeholder.png"))
        self.img_current_medicine.setScaledContents(True)
        self.img_current_medicine.setAlignment(Qt.AlignBottom|Qt.AlignHCenter)
        self.txt_current_medicine = QLabel(self.dispensing_page)
        self.txt_current_medicine.setObjectName(u"txt_current_medicine")
        self.txt_current_medicine.setGeometry(QRect(60, 126, 181, 31))
        self.txt_current_medicine.setStyleSheet(u"QLabel {\n"
"    color: #3A3A3A;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 16px;\n"
"    font-style: normal;\n"
"    font-weight: 400;   /* Normal */\n"
"}\n"
"")
        self.txt_pillcount_num = QLabel(self.dispensing_page)
        self.txt_pillcount_num.setObjectName(u"txt_pillcount_num")
        self.txt_pillcount_num.setGeometry(QRect(564, 410, 111, 71))
        self.txt_pillcount_num.setStyleSheet(u"QLabel\n"
"{\n"
"color: #3A3A3A;\n"
"text-align: center;\n"
"font-family: \"Microsoft YaHei\";\n"
"font-size: 64px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}\n"
"\n"
"")
        self.txt_pillcount_num.setAlignment(Qt.AlignCenter)
        self.pills_num_msg_3 = QLabel(self.dispensing_page)
        self.pills_num_msg_3.setObjectName(u"pills_num_msg_3")
        self.pills_num_msg_3.setGeometry(QRect(490, 440, 101, 41))
        self.pills_num_msg_3.setFont(font)
        self.pills_num_msg_3.setStyleSheet(u"QLabel {\n"
"    color: #3A3A3A;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 24px;\n"
"    font-style: normal;\n"
"    font-weight: 700;   /* Bold */\n"
"}\n"
"")
        self.pills_num_msg_4 = QLabel(self.dispensing_page)
        self.pills_num_msg_4.setObjectName(u"pills_num_msg_4")
        self.pills_num_msg_4.setGeometry(QRect(680, 440, 31, 41))
        self.pills_num_msg_4.setStyleSheet(u"QLabel {\n"
"    color: #3A3A3A;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 24px;\n"
"    font-style: normal;\n"
"    font-weight: 700;   /* Bold */\n"
"}\n"
"")
        self.txt_8 = QLabel(self.dispensing_page)
        self.txt_8.setObjectName(u"txt_8")
        self.txt_8.setGeometry(QRect(440, 120, 111, 41))
        self.txt_8.setStyleSheet(u"QLabel {\n"
"    color: #3A3A3A;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 16px;\n"
"    font-style: normal;\n"
"    font-weight: 400;   /* Normal */\n"
"}\n"
"")
        self.stackedWidget.addWidget(self.dispensing_page)
        self.finish_page = QWidget()
        self.finish_page.setObjectName(u"finish_page")
        self.btn_continue_dispensing = QPushButton(self.finish_page)
        self.btn_continue_dispensing.setObjectName(u"btn_continue_dispensing")
        self.btn_continue_dispensing.setGeometry(QRect(270, 300, 260, 60))
        self.btn_continue_dispensing.setStyleSheet(u"QPushButton {\n"
"    min-width: 260px;\n"
"    min-height: 60px;\n"
"    max-width: 260px;\n"
"    max-height: 60px;\n"
"\n"
"    background-color: #8BD2C0;      /* \u80cc\u666f\u8272 */\n"
"    border-radius: 30px;            /* \u5706\u89d2 */\n"
"\n"
"    color: #000;                    /* \u6587\u5b57\u989c\u8272 */\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 20px;\n"
"    font-style: normal;\n"
"    font-weight: 500;\n"
"}\n"
"\n"
"/* \u9f20\u6807\u60ac\u505c */\n"
"QPushButton:hover {\n"
"    background-color: #7CC9B3;  /* \u7a0d\u6df1\u4e00\u70b9\u7684\u7eff\u8272 */\n"
"}\n"
"\n"
"/* \u9f20\u6807\u6309\u4e0b */\n"
"QPushButton:pressed {\n"
"    background-color: #6BBFA5;  /* \u66f4\u6df1\u4e00\u70b9\u7684\u7eff\u8272 */\n"
"}\n"
"")
        self.btn_finish_dispensing = QPushButton(self.finish_page)
        self.btn_finish_dispensing.setObjectName(u"btn_finish_dispensing")
        self.btn_finish_dispensing.setGeometry(QRect(270, 380, 262, 62))
        self.btn_finish_dispensing.setStyleSheet(u"QPushButton {\n"
"    /* Fixed size */\n"
"    min-width: 260px;\n"
"    max-width: 260px;\n"
"    min-height: 60px;\n"
"    max-height: 60px;\n"
"\n"
"    /* Background & border */\n"
"    background-color: #F9F9F9;      /* Idle background */\n"
"    border-radius: 30px;             /* Rounded corners */\n"
"    border: 1px solid #000;          /* Black border */\n"
"\n"
"    /* Text style */\n"
"    color: #000;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 20px;\n"
"    font-style: normal;\n"
"    font-weight: 500;\n"
"}\n"
"\n"
"/* Hover */\n"
"QPushButton:hover {\n"
"    background-color: #E2E2E2;  /* Slightly darker gray */\n"
"}\n"
"\n"
"/* Pressed */\n"
"QPushButton:pressed {\n"
"    background-color: #C8C8C8;  /* Even darker gray */\n"
"}\n"
"\n"
"")
        self.txt_4 = QLabel(self.finish_page)
        self.txt_4.setObjectName(u"txt_4")
        self.txt_4.setGeometry(QRect(20, 40, 611, 41))
        self.txt_4.setStyleSheet(u"QLabel {\n"
"    color: #737373;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 22px;\n"
"    font-style: normal;\n"
"    font-weight: 400;   /* Normal */\n"
"}\n"
"")
        self.txt_5 = QLabel(self.finish_page)
        self.txt_5.setObjectName(u"txt_5")
        self.txt_5.setGeometry(QRect(20, 10, 181, 31))
        self.txt_5.setStyleSheet(u"QLabel {\n"
"    color: #000;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 24px;\n"
"    font-style: normal;\n"
"    font-weight: 700;   /* Bold */\n"
"}\n"
"")
        self.label_4 = QLabel(self.finish_page)
        self.label_4.setObjectName(u"label_4")
        self.label_4.setGeometry(QRect(360, 190, 76, 76))
        self.label_4.setPixmap(QPixmap(u":/icons/capsule.png"))
        self.label_4.setScaledContents(True)
        self.stackedWidget.addWidget(self.finish_page)
        self.setting_page = QWidget()
        self.setting_page.setObjectName(u"setting_page")
        self.stackedWidget.addWidget(self.setting_page)

        self.horizontalLayout.addWidget(self.stackedWidget)

        MainWindow.setCentralWidget(self.centralwidget)

        self.retranslateUi(MainWindow)

        self.btn_dispense_page.setDefault(False)
        self.btn_setting_page.setDefault(False)
        self.btn_refresh_database.setDefault(False)
        self.btn_contact_us_page.setDefault(False)
        self.stackedWidget.setCurrentIndex(4)


        QMetaObject.connectSlotsByName(MainWindow)
    # setupUi

    def retranslateUi(self, MainWindow):
        MainWindow.setWindowTitle(QCoreApplication.translate("MainWindow", u"MainWindow", None))
        self.img_profile_photo.setText("")
        self.btn_dispense_page.setText(QCoreApplication.translate("MainWindow", u"   \u5206\u836f            ", None))
        self.btn_setting_page.setText(QCoreApplication.translate("MainWindow", u"   \u8bbe\u7f6e            ", None))
        self.btn_refresh_database.setText(QCoreApplication.translate("MainWindow", u"   \u5237\u65b0\u6570\u636e\u5e93  ", None))
        self.btn_contact_us_page.setText(QCoreApplication.translate("MainWindow", u"   \u53cd\u9988\u548c\u5efa\u8bae  ", None))
        self.btn_start_find_patient.setText(QCoreApplication.translate("MainWindow", u"\u5f00\u59cb\u5206\u836f", None))
        self.label.setText("")
        self.img_cam_frame.setText("")
        self.txt_.setText(QCoreApplication.translate("MainWindow", u"\u8bf7\u5c06\u836f\u76d8\u4e0a\u7684\u4e8c\u7ef4\u7801\u5bf9\u51c6\u6444\u50cf\u5934", None))
        self.txt_1.setText(QCoreApplication.translate("MainWindow", u"\u51c6\u5907\u9636\u6bb5", None))
        self.label_2.setText("")
        self.btn_start_dispensing.setText(QCoreApplication.translate("MainWindow", u"\u786e\u8ba4\u653e\u5165", None))
        self.txt_load_prescription_info.setText(QCoreApplication.translate("MainWindow", u"\u6210\u529f\u83b7\u53d6\u5230\u60a3\u8005\u674e\u963f\u59e8\u7684\u5904\u65b9\u4fe1\u606f", None))
        self.txt_2.setText(QCoreApplication.translate("MainWindow", u"\u8bf7\u5c06\u7a7a\u836f\u76d8\u653e\u5165\u5206\u836f\u673a", None))
        self.txt_3.setText(QCoreApplication.translate("MainWindow", u"\u51c6\u5907\u9636\u6bb5", None))
        self.label_3.setText("")
        self.img_pillcount_frame.setText("")
        self.txt_patient_info.setText(QCoreApplication.translate("MainWindow", u"\u963f\u83b2     \u5e8a\u53f7\uff1a327", None))
        self.txt_current_medicine_percentage.setText(QCoreApplication.translate("MainWindow", u"0%", None))
        self.pills_num_msg_2.setText(QCoreApplication.translate("MainWindow", u"\u7247", None))
        self.pills_num_msg_1.setText(QCoreApplication.translate("MainWindow", u"\u5171\u9700\u8981", None))
        self.txt_pills_num_needed.setText(QCoreApplication.translate("MainWindow", u"77", None))
        self.txt_6.setText(QCoreApplication.translate("MainWindow", u"\u5206\u836f\u9636\u6bb5", None))
        self.txt_7.setText(QCoreApplication.translate("MainWindow", u"\u5206\u836f\u4e2d...  \u8bf7\u5c06\u5bf9\u5e94\u836f\u54c1\u6295\u5165\u5230\u5206\u836f\u673a\u4e2d", None))
        self.img_current_medicine.setText("")
        self.txt_current_medicine.setText(QCoreApplication.translate("MainWindow", u"\u963f\u83ab\u897f\u6797\u80f6\u56ca", None))
        self.txt_pillcount_num.setText(QCoreApplication.translate("MainWindow", u"0", None))
        self.pills_num_msg_3.setText(QCoreApplication.translate("MainWindow", u"\u68c0\u6d4b\u5230", None))
        self.pills_num_msg_4.setText(QCoreApplication.translate("MainWindow", u"\u7247", None))
        self.txt_8.setText(QCoreApplication.translate("MainWindow", u"\u6570\u836f\u753b\u9762", None))
        self.btn_continue_dispensing.setText(QCoreApplication.translate("MainWindow", u"\u7ee7\u7eed\u5206\u836f", None))
        self.btn_finish_dispensing.setText(QCoreApplication.translate("MainWindow", u"\u5b8c\u6210\u5206\u836f", None))
        self.txt_4.setText(QCoreApplication.translate("MainWindow", u"\u5df2\u5206\u5b8c608\u5e8a\u5f20\u963f\u8054\u7684\u4eca\u5929\u7684\u6240\u6709\u836f\u54c1", None))
        self.txt_5.setText(QCoreApplication.translate("MainWindow", u"\u5206\u836f\u9636\u6bb5", None))
        self.label_4.setText("")
    # retranslateUi

