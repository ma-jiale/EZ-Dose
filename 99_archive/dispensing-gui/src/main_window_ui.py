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
from PySide6.QtWidgets import (QApplication, QFrame, QGridLayout, QHBoxLayout,
    QLabel, QLineEdit, QMainWindow, QProgressBar,
    QPushButton, QSizePolicy, QSpacerItem, QSpinBox,
    QStackedWidget, QVBoxLayout, QWidget)
import icons_rc
import images_rc

class Ui_MainWindow(object):
    def setupUi(self, MainWindow):
        if not MainWindow.objectName():
            MainWindow.setObjectName(u"MainWindow")
        MainWindow.resize(978, 656)
        icon = QIcon()
        icon.addFile(u":/icons/icon.ico", QSize(), QIcon.Mode.Normal, QIcon.State.Off)
        MainWindow.setWindowIcon(icon)
        MainWindow.setStyleSheet(u"QPushButton {\n"
"    min-width: 213px;\n"
"    min-height: 61px;\n"
"    max-width: 213px;\n"
"    max-height: 61px;\n"
"}\n"
"")
        MainWindow.setLocale(QLocale(QLocale.Chinese, QLocale.China))
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
        icon1 = QIcon()
        icon1.addFile(u":/icons/icon_machine.png", QSize(), QIcon.Mode.Normal, QIcon.State.Off)
        self.btn_dispense_page.setIcon(icon1)
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
        icon2 = QIcon()
        icon2.addFile(u":/icons/icon_setting.png", QSize(), QIcon.Mode.Normal, QIcon.State.Off)
        self.btn_setting_page.setIcon(icon2)
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
        icon3 = QIcon()
        icon3.addFile(u":/icons/icon_refresh.png", QSize(), QIcon.Mode.Normal, QIcon.State.Off)
        self.btn_refresh_database.setIcon(icon3)
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
        icon4 = QIcon()
        icon4.addFile(u":/icons/icon_help.png", QSize(), QIcon.Mode.Normal, QIcon.State.Off)
        self.btn_contact_us_page.setIcon(icon4)
        self.btn_contact_us_page.setIconSize(QSize(28, 28))

        self.verticalLayout.addWidget(self.btn_contact_us_page)


        self.horizontalLayout.addWidget(self.sideBar)

        self.stackedWidget = QStackedWidget(self.centralwidget)
        self.stackedWidget.setObjectName(u"stackedWidget")
        self.stackedWidget.setStyleSheet(u"")
        self.start_page = QWidget()
        self.start_page.setObjectName(u"start_page")
        self.gridLayout_2 = QGridLayout(self.start_page)
        self.gridLayout_2.setObjectName(u"gridLayout_2")
        self.verticalSpacer_3 = QSpacerItem(20, 182, QSizePolicy.Policy.Minimum, QSizePolicy.Policy.Expanding)

        self.gridLayout_2.addItem(self.verticalSpacer_3, 0, 1, 1, 1)

        self.horizontalSpacer_3 = QSpacerItem(247, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.gridLayout_2.addItem(self.horizontalSpacer_3, 1, 0, 1, 1)

        self.horizontalSpacer_4 = QSpacerItem(246, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.gridLayout_2.addItem(self.horizontalSpacer_4, 1, 2, 1, 1)

        self.verticalSpacer_2 = QSpacerItem(20, 182, QSizePolicy.Policy.Minimum, QSizePolicy.Policy.Expanding)

        self.gridLayout_2.addItem(self.verticalSpacer_2, 2, 1, 1, 1)

        self.frame = QFrame(self.start_page)
        self.frame.setObjectName(u"frame")
        self.frame.setMinimumSize(QSize(0, 200))
        self.frame.setMaximumSize(QSize(16777215, 200))
        self.frame.setStyleSheet(u"QFrame {\n"
"    border: none;\n"
"}")
        self.frame.setFrameShape(QFrame.StyledPanel)
        self.frame.setFrameShadow(QFrame.Raised)
        self.gridLayout = QGridLayout(self.frame)
        self.gridLayout.setSpacing(0)
        self.gridLayout.setObjectName(u"gridLayout")
        self.gridLayout.setContentsMargins(20, 0, 20, 0)
        self.btn_start_find_patient = QPushButton(self.frame)
        self.btn_start_find_patient.setObjectName(u"btn_start_find_patient")
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

        self.gridLayout.addWidget(self.btn_start_find_patient, 2, 1, 1, 3)

        self.horizontalSpacer = QSpacerItem(89, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.gridLayout.addItem(self.horizontalSpacer, 0, 1, 1, 1)

        self.horizontalSpacer_5 = QSpacerItem(40, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.gridLayout.addItem(self.horizontalSpacer_5, 2, 0, 1, 1)

        self.label = QLabel(self.frame)
        self.label.setObjectName(u"label")
        self.label.setMinimumSize(QSize(76, 76))
        self.label.setMaximumSize(QSize(76, 76))
        self.label.setPixmap(QPixmap(u":/icons/capsule.png"))
        self.label.setScaledContents(True)

        self.gridLayout.addWidget(self.label, 0, 2, 1, 1)

        self.horizontalSpacer_2 = QSpacerItem(89, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.gridLayout.addItem(self.horizontalSpacer_2, 0, 3, 1, 1)

        self.horizontalSpacer_6 = QSpacerItem(40, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.gridLayout.addItem(self.horizontalSpacer_6, 2, 4, 1, 1)


        self.gridLayout_2.addWidget(self.frame, 1, 1, 1, 1)

        self.stackedWidget.addWidget(self.start_page)
        self.scan_qrcode_page = QWidget()
        self.scan_qrcode_page.setObjectName(u"scan_qrcode_page")
        self.gridLayout_4 = QGridLayout(self.scan_qrcode_page)
        self.gridLayout_4.setObjectName(u"gridLayout_4")
        self.horizontalLayout_4 = QHBoxLayout()
        self.horizontalLayout_4.setObjectName(u"horizontalLayout_4")
        self.horizontalSpacer_11 = QSpacerItem(40, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.horizontalLayout_4.addItem(self.horizontalSpacer_11)

        self.img_cam_frame = QLabel(self.scan_qrcode_page)
        self.img_cam_frame.setObjectName(u"img_cam_frame")
        self.img_cam_frame.setStyleSheet(u"QLabel\n"
"{\n"
"    min-width: 400px;\n"
"    max-width: 400px;\n"
"    min-height: 300px;\n"
"    max-height: 300px;\n"
"}")
        self.img_cam_frame.setScaledContents(True)

        self.horizontalLayout_4.addWidget(self.img_cam_frame)

        self.horizontalSpacer_10 = QSpacerItem(40, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.horizontalLayout_4.addItem(self.horizontalSpacer_10)


        self.gridLayout_4.addLayout(self.horizontalLayout_4, 2, 1, 1, 1)

        self.horizontalLayout_3 = QHBoxLayout()
        self.horizontalLayout_3.setObjectName(u"horizontalLayout_3")
        self.verticalLayout_2 = QVBoxLayout()
        self.verticalLayout_2.setObjectName(u"verticalLayout_2")
        self.txt_period_in_scan_qrcode = QLabel(self.scan_qrcode_page)
        self.txt_period_in_scan_qrcode.setObjectName(u"txt_period_in_scan_qrcode")
        self.txt_period_in_scan_qrcode.setStyleSheet(u"QLabel {\n"
"    color: #000;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 24px;\n"
"    font-style: normal;\n"
"    font-weight: 700;   /* Bold */\n"
"}\n"
"")

        self.verticalLayout_2.addWidget(self.txt_period_in_scan_qrcode)

        self.txt_guide_in_scan_qrcode = QLabel(self.scan_qrcode_page)
        self.txt_guide_in_scan_qrcode.setObjectName(u"txt_guide_in_scan_qrcode")
        self.txt_guide_in_scan_qrcode.setStyleSheet(u"QLabel {\n"
"    color: #737373;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 22px;\n"
"    font-style: normal;\n"
"    font-weight: 400;   /* Normal */\n"
"}\n"
"")

        self.verticalLayout_2.addWidget(self.txt_guide_in_scan_qrcode)


        self.horizontalLayout_3.addLayout(self.verticalLayout_2)

        self.horizontalSpacer_8 = QSpacerItem(40, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.horizontalLayout_3.addItem(self.horizontalSpacer_8)


        self.gridLayout_4.addLayout(self.horizontalLayout_3, 0, 0, 1, 1)

        self.horizontalLayout_2 = QHBoxLayout()
        self.horizontalLayout_2.setObjectName(u"horizontalLayout_2")
        self.horizontalSpacer_9 = QSpacerItem(40, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.horizontalLayout_2.addItem(self.horizontalSpacer_9)

        self.label_2 = QLabel(self.scan_qrcode_page)
        self.label_2.setObjectName(u"label_2")
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

        self.horizontalLayout_2.addWidget(self.label_2)

        self.horizontalSpacer_7 = QSpacerItem(40, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.horizontalLayout_2.addItem(self.horizontalSpacer_7)


        self.gridLayout_4.addLayout(self.horizontalLayout_2, 2, 0, 1, 1)

        self.verticalSpacer_4 = QSpacerItem(20, 91, QSizePolicy.Policy.Minimum, QSizePolicy.Policy.Expanding)

        self.gridLayout_4.addItem(self.verticalSpacer_4, 1, 0, 1, 1)

        self.verticalSpacer_6 = QSpacerItem(20, 40, QSizePolicy.Policy.Minimum, QSizePolicy.Policy.Expanding)

        self.gridLayout_4.addItem(self.verticalSpacer_6, 3, 0, 1, 2)

        self.verticalSpacer_5 = QSpacerItem(20, 92, QSizePolicy.Policy.Minimum, QSizePolicy.Policy.Expanding)

        self.gridLayout_4.addItem(self.verticalSpacer_5, 4, 0, 1, 2)

        self.stackedWidget.addWidget(self.scan_qrcode_page)
        self.scan_rfid_page = QWidget()
        self.scan_rfid_page.setObjectName(u"scan_rfid_page")
        self.stackedWidget.addWidget(self.scan_rfid_page)
        self.rx_page = QWidget()
        self.rx_page.setObjectName(u"rx_page")
        self.gridLayout_7 = QGridLayout(self.rx_page)
        self.gridLayout_7.setObjectName(u"gridLayout_7")
        self.horizontalLayout_8 = QHBoxLayout()
        self.horizontalLayout_8.setObjectName(u"horizontalLayout_8")
        self.verticalLayout_5 = QVBoxLayout()
        self.verticalLayout_5.setObjectName(u"verticalLayout_5")
        self.txt_period_in_rx = QLabel(self.rx_page)
        self.txt_period_in_rx.setObjectName(u"txt_period_in_rx")
        self.txt_period_in_rx.setStyleSheet(u"QLabel {\n"
"    color: #000;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 24px;\n"
"    font-style: normal;\n"
"    font-weight: 700;   /* Bold */\n"
"}\n"
"")

        self.verticalLayout_5.addWidget(self.txt_period_in_rx)

        self.txt_guide_in_rx = QLabel(self.rx_page)
        self.txt_guide_in_rx.setObjectName(u"txt_guide_in_rx")
        self.txt_guide_in_rx.setStyleSheet(u"QLabel {\n"
"    color: #737373;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 22px;\n"
"    font-style: normal;\n"
"    font-weight: 400;   /* Normal */\n"
"}\n"
"")

        self.verticalLayout_5.addWidget(self.txt_guide_in_rx)

        self.txt_load_prescription_info = QLabel(self.rx_page)
        self.txt_load_prescription_info.setObjectName(u"txt_load_prescription_info")
        self.txt_load_prescription_info.setStyleSheet(u"QLabel {\n"
"    color: #3A3A3A;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 16px;\n"
"    font-style: normal;\n"
"    font-weight: 400;   /* Normal */\n"
"}")
        self.txt_load_prescription_info.setAlignment(Qt.AlignLeading|Qt.AlignLeft|Qt.AlignVCenter)

        self.verticalLayout_5.addWidget(self.txt_load_prescription_info)


        self.horizontalLayout_8.addLayout(self.verticalLayout_5)

        self.horizontalSpacer_20 = QSpacerItem(40, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.horizontalLayout_8.addItem(self.horizontalSpacer_20)


        self.gridLayout_7.addLayout(self.horizontalLayout_8, 0, 0, 1, 2)

        self.verticalSpacer_10 = QSpacerItem(20, 65, QSizePolicy.Policy.Minimum, QSizePolicy.Policy.Expanding)

        self.gridLayout_7.addItem(self.verticalSpacer_10, 1, 1, 1, 1)

        self.horizontalLayout_7 = QHBoxLayout()
        self.horizontalLayout_7.setObjectName(u"horizontalLayout_7")
        self.txt_rx_detail = QLabel(self.rx_page)
        self.txt_rx_detail.setObjectName(u"txt_rx_detail")
        self.txt_rx_detail.setMinimumSize(QSize(360, 360))
        self.txt_rx_detail.setMaximumSize(QSize(360, 360))
        self.txt_rx_detail.setTextFormat(Qt.MarkdownText)

        self.horizontalLayout_7.addWidget(self.txt_rx_detail)

        self.verticalLayout_3 = QVBoxLayout()
        self.verticalLayout_3.setObjectName(u"verticalLayout_3")
        self.label_3 = QLabel(self.rx_page)
        self.label_3.setObjectName(u"label_3")
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

        self.verticalLayout_3.addWidget(self.label_3)

        self.horizontalLayout_6 = QHBoxLayout()
        self.horizontalLayout_6.setObjectName(u"horizontalLayout_6")
        self.horizontalSpacer_18 = QSpacerItem(40, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.horizontalLayout_6.addItem(self.horizontalSpacer_18)

        self.btn_start_dispensing = QPushButton(self.rx_page)
        self.btn_start_dispensing.setObjectName(u"btn_start_dispensing")
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

        self.horizontalLayout_6.addWidget(self.btn_start_dispensing)

        self.horizontalSpacer_19 = QSpacerItem(40, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.horizontalLayout_6.addItem(self.horizontalSpacer_19)


        self.verticalLayout_3.addLayout(self.horizontalLayout_6)


        self.horizontalLayout_7.addLayout(self.verticalLayout_3)


        self.gridLayout_7.addLayout(self.horizontalLayout_7, 2, 1, 2, 1)

        self.verticalSpacer_9 = QSpacerItem(20, 65, QSizePolicy.Policy.Minimum, QSizePolicy.Policy.Expanding)

        self.gridLayout_7.addItem(self.verticalSpacer_9, 5, 1, 1, 1)

        self.horizontalSpacer_21 = QSpacerItem(61, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.gridLayout_7.addItem(self.horizontalSpacer_21, 3, 2, 1, 1)

        self.horizontalSpacer_22 = QSpacerItem(4, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.gridLayout_7.addItem(self.horizontalSpacer_22, 2, 0, 1, 1)

        self.verticalSpacer_13 = QSpacerItem(20, 40, QSizePolicy.Policy.Minimum, QSizePolicy.Policy.Expanding)

        self.gridLayout_7.addItem(self.verticalSpacer_13, 4, 1, 1, 1)

        self.stackedWidget.addWidget(self.rx_page)
        self.dispensing_page = QWidget()
        self.dispensing_page.setObjectName(u"dispensing_page")
        self.gridLayout_10 = QGridLayout(self.dispensing_page)
        self.gridLayout_10.setObjectName(u"gridLayout_10")
        self.verticalSpacer_11 = QSpacerItem(20, 40, QSizePolicy.Policy.Minimum, QSizePolicy.Policy.Expanding)

        self.gridLayout_10.addItem(self.verticalSpacer_11, 5, 2, 1, 1)

        self.horizontalSpacer_28 = QSpacerItem(37, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.gridLayout_10.addItem(self.horizontalSpacer_28, 3, 4, 1, 1)

        self.gridLayout_9 = QGridLayout()
        self.gridLayout_9.setObjectName(u"gridLayout_9")
        self.txt_8 = QLabel(self.dispensing_page)
        self.txt_8.setObjectName(u"txt_8")
        self.txt_8.setStyleSheet(u"QLabel {\n"
"    color: #3A3A3A;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 16px;\n"
"    font-style: normal;\n"
"    font-weight: 400;   /* Normal */\n"
"}\n"
"")

        self.gridLayout_9.addWidget(self.txt_8, 0, 0, 1, 1)

        self.horizontalSpacer_24 = QSpacerItem(40, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.gridLayout_9.addItem(self.horizontalSpacer_24, 0, 1, 1, 1)

        self.img_pillcount_frame = QLabel(self.dispensing_page)
        self.img_pillcount_frame.setObjectName(u"img_pillcount_frame")
        self.img_pillcount_frame.setStyleSheet(u"QLabel\n"
"{\n"
"    min-width: 320px;\n"
"    max-width: 320px;\n"
"    min-height: 240px;\n"
"    max-height: 240px;\n"
"}")

        self.gridLayout_9.addWidget(self.img_pillcount_frame, 1, 0, 1, 2)

        self.frame_7 = QFrame(self.dispensing_page)
        self.frame_7.setObjectName(u"frame_7")
        self.frame_7.setMinimumSize(QSize(0, 120))
        self.frame_7.setStyleSheet(u"QFrame {\n"
"    background: transparent;\n"
"}")
        self.frame_7.setFrameShape(QFrame.StyledPanel)
        self.frame_7.setFrameShadow(QFrame.Raised)
        self.pills_num_msg_4 = QLabel(self.frame_7)
        self.pills_num_msg_4.setObjectName(u"pills_num_msg_4")
        self.pills_num_msg_4.setGeometry(QRect(210, 60, 41, 41))
        self.pills_num_msg_4.setStyleSheet(u"QLabel {\n"
"    color: #3A3A3A;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 24px;\n"
"    font-style: normal;\n"
"    font-weight: 700;   /* Bold */\n"
"}\n"
"")
        self.pills_num_msg_3 = QLabel(self.frame_7)
        self.pills_num_msg_3.setObjectName(u"pills_num_msg_3")
        self.pills_num_msg_3.setGeometry(QRect(20, 60, 81, 41))
        font = QFont()
        font.setFamilies([u"Microsoft YaHei"])
        font.setBold(True)
        font.setItalic(False)
        self.pills_num_msg_3.setFont(font)
        self.pills_num_msg_3.setStyleSheet(u"QLabel {\n"
"    color: #3A3A3A;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 24px;\n"
"    font-style: normal;\n"
"    font-weight: 700;   /* Bold */\n"
"}\n"
"")
        self.txt_pillcount_num = QLabel(self.frame_7)
        self.txt_pillcount_num.setObjectName(u"txt_pillcount_num")
        self.txt_pillcount_num.setGeometry(QRect(60, 30, 162, 71))
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

        self.gridLayout_9.addWidget(self.frame_7, 2, 0, 1, 2)


        self.gridLayout_10.addLayout(self.gridLayout_9, 2, 3, 2, 1)

        self.gridLayout_8 = QGridLayout()
        self.gridLayout_8.setObjectName(u"gridLayout_8")
        self.verticalLayout_6 = QVBoxLayout()
        self.verticalLayout_6.setObjectName(u"verticalLayout_6")
        self.txt_patient_info = QLabel(self.dispensing_page)
        self.txt_patient_info.setObjectName(u"txt_patient_info")
        self.txt_patient_info.setStyleSheet(u"QLabel {\n"
"    color: #3A3A3A;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 16px;\n"
"    font-style: normal;\n"
"    font-weight: 400;   /* Normal */\n"
"}\n"
"")

        self.verticalLayout_6.addWidget(self.txt_patient_info)

        self.txt_current_medicine = QLabel(self.dispensing_page)
        self.txt_current_medicine.setObjectName(u"txt_current_medicine")
        self.txt_current_medicine.setStyleSheet(u"QLabel {\n"
"    color: #3A3A3A;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 16px;\n"
"    font-style: normal;\n"
"    font-weight: 400;   /* Normal */\n"
"}\n"
"")

        self.verticalLayout_6.addWidget(self.txt_current_medicine)


        self.gridLayout_8.addLayout(self.verticalLayout_6, 0, 0, 1, 1)

        self.horizontalSpacer_23 = QSpacerItem(40, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.gridLayout_8.addItem(self.horizontalSpacer_23, 0, 1, 1, 1)

        self.img_current_medicine = QLabel(self.dispensing_page)
        self.img_current_medicine.setObjectName(u"img_current_medicine")
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

        self.gridLayout_8.addWidget(self.img_current_medicine, 1, 0, 1, 2)

        self.frame_6 = QFrame(self.dispensing_page)
        self.frame_6.setObjectName(u"frame_6")
        self.frame_6.setMinimumSize(QSize(40, 120))
        self.frame_6.setStyleSheet(u"QFrame {\n"
"    background: transparent;\n"
"}\n"
"")
        self.frame_6.setFrameShape(QFrame.StyledPanel)
        self.frame_6.setFrameShadow(QFrame.Raised)
        self.pills_num_msg_1 = QLabel(self.frame_6)
        self.pills_num_msg_1.setObjectName(u"pills_num_msg_1")
        self.pills_num_msg_1.setGeometry(QRect(50, 30, 81, 41))
        self.pills_num_msg_1.setFont(font)
        self.pills_num_msg_1.setStyleSheet(u"QLabel {\n"
"    color: #3A3A3A;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 24px;\n"
"    font-style: normal;\n"
"    font-weight: 700;   /* Bold */\n"
"}\n"
"")
        self.progressBar_current_medicine = QProgressBar(self.frame_6)
        self.progressBar_current_medicine.setObjectName(u"progressBar_current_medicine")
        self.progressBar_current_medicine.setGeometry(QRect(60, 89, 166, 26))
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
        self.txt_pills_num_needed = QLabel(self.frame_6)
        self.txt_pills_num_needed.setObjectName(u"txt_pills_num_needed")
        self.txt_pills_num_needed.setGeometry(QRect(100, 0, 162, 71))
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
        self.txt_current_medicine_percentage = QLabel(self.frame_6)
        self.txt_current_medicine_percentage.setObjectName(u"txt_current_medicine_percentage")
        self.txt_current_medicine_percentage.setGeometry(QRect(230, 80, 61, 41))
        self.txt_current_medicine_percentage.setStyleSheet(u"QLabel\n"
"{\n"
"color: #515151;\n"
"font-family: \"Microsoft YaHei\";\n"
"font-size: 24px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.pills_num_msg_2 = QLabel(self.frame_6)
        self.pills_num_msg_2.setObjectName(u"pills_num_msg_2")
        self.pills_num_msg_2.setGeometry(QRect(230, 30, 31, 41))
        self.pills_num_msg_2.setStyleSheet(u"QLabel {\n"
"    color: #3A3A3A;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 24px;\n"
"    font-style: normal;\n"
"    font-weight: 700;   /* Bold */\n"
"}\n"
"")

        self.gridLayout_8.addWidget(self.frame_6, 2, 0, 1, 2)


        self.gridLayout_10.addLayout(self.gridLayout_8, 2, 1, 2, 1)

        self.horizontalLayout_9 = QHBoxLayout()
        self.horizontalLayout_9.setObjectName(u"horizontalLayout_9")
        self.verticalLayout_7 = QVBoxLayout()
        self.verticalLayout_7.setObjectName(u"verticalLayout_7")
        self.txt_6 = QLabel(self.dispensing_page)
        self.txt_6.setObjectName(u"txt_6")
        self.txt_6.setStyleSheet(u"QLabel {\n"
"    color: #3A3A3A;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 24px;\n"
"    font-style: normal;\n"
"    font-weight: 700;   /* Bold */\n"
"}\n"
"")

        self.verticalLayout_7.addWidget(self.txt_6)

        self.txt_7 = QLabel(self.dispensing_page)
        self.txt_7.setObjectName(u"txt_7")
        self.txt_7.setStyleSheet(u"QLabel {\n"
"    color: #737373;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 22px;\n"
"    font-style: normal;\n"
"    font-weight: 400;   /* Normal */\n"
"}\n"
"")

        self.verticalLayout_7.addWidget(self.txt_7)


        self.horizontalLayout_9.addLayout(self.verticalLayout_7)

        self.horizontalSpacer_25 = QSpacerItem(40, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.horizontalLayout_9.addItem(self.horizontalSpacer_25)


        self.gridLayout_10.addLayout(self.horizontalLayout_9, 0, 0, 1, 4)

        self.horizontalSpacer_27 = QSpacerItem(37, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.gridLayout_10.addItem(self.horizontalSpacer_27, 2, 0, 1, 1)

        self.verticalSpacer_14 = QSpacerItem(20, 40, QSizePolicy.Policy.Minimum, QSizePolicy.Policy.Expanding)

        self.gridLayout_10.addItem(self.verticalSpacer_14, 4, 2, 1, 1)

        self.verticalSpacer_12 = QSpacerItem(20, 30, QSizePolicy.Policy.Minimum, QSizePolicy.Policy.Expanding)

        self.gridLayout_10.addItem(self.verticalSpacer_12, 1, 2, 1, 1)

        self.horizontalSpacer_26 = QSpacerItem(38, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.gridLayout_10.addItem(self.horizontalSpacer_26, 2, 2, 1, 1)

        self.stackedWidget.addWidget(self.dispensing_page)
        self.finish_page = QWidget()
        self.finish_page.setObjectName(u"finish_page")
        self.gridLayout_5 = QGridLayout(self.finish_page)
        self.gridLayout_5.setObjectName(u"gridLayout_5")
        self.gridLayout_5.setContentsMargins(10, 10, -1, -1)
        self.verticalLayout_4 = QVBoxLayout()
        self.verticalLayout_4.setObjectName(u"verticalLayout_4")
        self.txt_5 = QLabel(self.finish_page)
        self.txt_5.setObjectName(u"txt_5")
        self.txt_5.setStyleSheet(u"QLabel {\n"
"    color: #000;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 24px;\n"
"    font-style: normal;\n"
"    font-weight: 700;   /* Bold */\n"
"}\n"
"")

        self.verticalLayout_4.addWidget(self.txt_5)

        self.txt_4 = QLabel(self.finish_page)
        self.txt_4.setObjectName(u"txt_4")
        self.txt_4.setStyleSheet(u"QLabel {\n"
"    color: #737373;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 22px;\n"
"    font-style: normal;\n"
"    font-weight: 400;   /* Normal */\n"
"}\n"
"")

        self.verticalLayout_4.addWidget(self.txt_4)


        self.gridLayout_5.addLayout(self.verticalLayout_4, 0, 0, 1, 2)

        self.verticalSpacer_8 = QSpacerItem(20, 124, QSizePolicy.Policy.Minimum, QSizePolicy.Policy.Expanding)

        self.gridLayout_5.addItem(self.verticalSpacer_8, 1, 1, 1, 1)

        self.horizontalSpacer_14 = QSpacerItem(207, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.gridLayout_5.addItem(self.horizontalSpacer_14, 2, 0, 1, 1)

        self.gridLayout_6 = QGridLayout()
        self.gridLayout_6.setObjectName(u"gridLayout_6")
        self.gridLayout_6.setVerticalSpacing(11)
        self.horizontalSpacer_16 = QSpacerItem(40, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.gridLayout_6.addItem(self.horizontalSpacer_16, 2, 0, 1, 1)

        self.horizontalSpacer_17 = QSpacerItem(40, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.gridLayout_6.addItem(self.horizontalSpacer_17, 2, 2, 1, 1)

        self.btn_finish_dispensing = QPushButton(self.finish_page)
        self.btn_finish_dispensing.setObjectName(u"btn_finish_dispensing")
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

        self.gridLayout_6.addWidget(self.btn_finish_dispensing, 3, 1, 1, 1)

        self.horizontalLayout_5 = QHBoxLayout()
        self.horizontalLayout_5.setObjectName(u"horizontalLayout_5")
        self.horizontalSpacer_12 = QSpacerItem(40, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.horizontalLayout_5.addItem(self.horizontalSpacer_12)

        self.label_4 = QLabel(self.finish_page)
        self.label_4.setObjectName(u"label_4")
        self.label_4.setMinimumSize(QSize(76, 76))
        self.label_4.setMaximumSize(QSize(76, 76))
        self.label_4.setPixmap(QPixmap(u":/icons/capsule.png"))
        self.label_4.setScaledContents(True)

        self.horizontalLayout_5.addWidget(self.label_4)

        self.horizontalSpacer_13 = QSpacerItem(40, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.horizontalLayout_5.addItem(self.horizontalSpacer_13)


        self.gridLayout_6.addLayout(self.horizontalLayout_5, 0, 1, 1, 1)

        self.btn_continue_dispensing = QPushButton(self.finish_page)
        self.btn_continue_dispensing.setObjectName(u"btn_continue_dispensing")
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

        self.gridLayout_6.addWidget(self.btn_continue_dispensing, 2, 1, 1, 1)

        self.verticalSpacer_15 = QSpacerItem(20, 40, QSizePolicy.Policy.Minimum, QSizePolicy.Policy.Expanding)

        self.gridLayout_6.addItem(self.verticalSpacer_15, 4, 1, 1, 1)

        self.frame_4 = QFrame(self.finish_page)
        self.frame_4.setObjectName(u"frame_4")
        self.frame_4.setFrameShape(QFrame.StyledPanel)
        self.frame_4.setFrameShadow(QFrame.Raised)

        self.gridLayout_6.addWidget(self.frame_4, 1, 1, 1, 1)


        self.gridLayout_5.addLayout(self.gridLayout_6, 2, 1, 1, 1)

        self.horizontalSpacer_15 = QSpacerItem(207, 20, QSizePolicy.Policy.Expanding, QSizePolicy.Policy.Minimum)

        self.gridLayout_5.addItem(self.horizontalSpacer_15, 2, 2, 1, 1)

        self.verticalSpacer_7 = QSpacerItem(20, 123, QSizePolicy.Policy.Minimum, QSizePolicy.Policy.Expanding)

        self.gridLayout_5.addItem(self.verticalSpacer_7, 3, 1, 1, 1)

        self.stackedWidget.addWidget(self.finish_page)
        self.setting_page = QWidget()
        self.setting_page.setObjectName(u"setting_page")
        self.btn_save_settings = QPushButton(self.setting_page)
        self.btn_save_settings.setObjectName(u"btn_save_settings")
        self.btn_save_settings.setGeometry(QRect(540, 490, 215, 63))
        self.frame_2 = QFrame(self.setting_page)
        self.frame_2.setObjectName(u"frame_2")
        self.frame_2.setGeometry(QRect(50, 70, 631, 101))
        self.frame_2.setFrameShape(QFrame.StyledPanel)
        self.frame_2.setFrameShadow(QFrame.Raised)
        self.gridLayout_3 = QGridLayout(self.frame_2)
        self.gridLayout_3.setObjectName(u"gridLayout_3")
        self.label_7 = QLabel(self.frame_2)
        self.label_7.setObjectName(u"label_7")

        self.gridLayout_3.addWidget(self.label_7, 1, 5, 1, 1)

        self.label_8 = QLabel(self.frame_2)
        self.label_8.setObjectName(u"label_8")

        self.gridLayout_3.addWidget(self.label_8, 0, 4, 1, 1)

        self.label_5 = QLabel(self.frame_2)
        self.label_5.setObjectName(u"label_5")

        self.gridLayout_3.addWidget(self.label_5, 0, 0, 1, 1)

        self.spinBox_expiry_threshold = QSpinBox(self.frame_2)
        self.spinBox_expiry_threshold.setObjectName(u"spinBox_expiry_threshold")

        self.gridLayout_3.addWidget(self.spinBox_expiry_threshold, 1, 2, 1, 3)

        self.label_6 = QLabel(self.frame_2)
        self.label_6.setObjectName(u"label_6")

        self.gridLayout_3.addWidget(self.label_6, 1, 0, 1, 2)

        self.spinBox_max_days = QSpinBox(self.frame_2)
        self.spinBox_max_days.setObjectName(u"spinBox_max_days")
        self.spinBox_max_days.setMinimum(1)

        self.gridLayout_3.addWidget(self.spinBox_max_days, 0, 1, 1, 3)

        self.label_9 = QLabel(self.frame_2)
        self.label_9.setObjectName(u"label_9")

        self.gridLayout_3.addWidget(self.label_9, 2, 0, 1, 1)

        self.server_url_lineEdit = QLineEdit(self.frame_2)
        self.server_url_lineEdit.setObjectName(u"server_url_lineEdit")

        self.gridLayout_3.addWidget(self.server_url_lineEdit, 2, 1, 1, 4)

        self.btn_zero_motor_position = QPushButton(self.setting_page)
        self.btn_zero_motor_position.setObjectName(u"btn_zero_motor_position")
        self.btn_zero_motor_position.setGeometry(QRect(540, 400, 215, 63))
        self.stackedWidget.addWidget(self.setting_page)

        self.horizontalLayout.addWidget(self.stackedWidget)

        MainWindow.setCentralWidget(self.centralwidget)

        self.retranslateUi(MainWindow)

        self.btn_dispense_page.setDefault(False)
        self.btn_setting_page.setDefault(False)
        self.btn_refresh_database.setDefault(False)
        self.btn_contact_us_page.setDefault(False)
        self.stackedWidget.setCurrentIndex(0)


        QMetaObject.connectSlotsByName(MainWindow)
    # setupUi

    def retranslateUi(self, MainWindow):
        MainWindow.setWindowTitle(QCoreApplication.translate("MainWindow", u"EZ Dose", None))
        self.img_profile_photo.setText("")
        self.btn_dispense_page.setText(QCoreApplication.translate("MainWindow", u"   \u5206\u836f            ", None))
        self.btn_setting_page.setText(QCoreApplication.translate("MainWindow", u"   \u8bbe\u7f6e            ", None))
        self.btn_refresh_database.setText(QCoreApplication.translate("MainWindow", u"   \u5237\u65b0\u6570\u636e\u5e93  ", None))
        self.btn_contact_us_page.setText(QCoreApplication.translate("MainWindow", u"   \u53cd\u9988\u548c\u5efa\u8bae  ", None))
        self.btn_start_find_patient.setText(QCoreApplication.translate("MainWindow", u"\u5f00\u59cb\u5206\u836f", None))
        self.label.setText("")
        self.img_cam_frame.setText("")
        self.txt_period_in_scan_qrcode.setText(QCoreApplication.translate("MainWindow", u"\u51c6\u5907\u9636\u6bb5     ", None))
        self.txt_guide_in_scan_qrcode.setText(QCoreApplication.translate("MainWindow", u"\u8bf7\u626b\u63cf\u836f\u76d2\u4e8c\u7ef4\u7801", None))
        self.label_2.setText("")
        self.txt_period_in_rx.setText(QCoreApplication.translate("MainWindow", u"\u51c6\u5907\u9636\u6bb5", None))
        self.txt_guide_in_rx.setText(QCoreApplication.translate("MainWindow", u"\u8bf7\u5c06\u7a7a\u836f\u76d8\u653e\u5165\u5206\u836f\u673a                                                                                     ", None))
        self.txt_load_prescription_info.setText(QCoreApplication.translate("MainWindow", u"\u6210\u529f\u83b7\u53d6\u5230\u60a3\u8005\u674e\u963f\u59e8\u7684\u5904\u65b9\u4fe1\u606f", None))
        self.txt_rx_detail.setText(QCoreApplication.translate("MainWindow", u"\u963f\u83ab\u897f\u6797\u80f6\u56ca  1\u7247 1\u7247 1\u7247   \u996d\u540e\u670d\u7528", None))
        self.label_3.setText("")
        self.btn_start_dispensing.setText(QCoreApplication.translate("MainWindow", u"\u786e\u8ba4\u5df2\u653e\u5165\u836f\u76d8", None))
        self.txt_8.setText(QCoreApplication.translate("MainWindow", u"\u6570\u836f\u753b\u9762", None))
        self.img_pillcount_frame.setText("")
        self.pills_num_msg_4.setText(QCoreApplication.translate("MainWindow", u"\u7247", None))
        self.pills_num_msg_3.setText(QCoreApplication.translate("MainWindow", u"\u68c0\u6d4b\u5230", None))
        self.txt_pillcount_num.setText(QCoreApplication.translate("MainWindow", u"0", None))
        self.txt_patient_info.setText(QCoreApplication.translate("MainWindow", u"\u963f\u83b2     \u5e8a\u53f7\uff1a327", None))
        self.txt_current_medicine.setText(QCoreApplication.translate("MainWindow", u"\u963f\u83ab\u897f\u6797\u80f6\u56ca", None))
        self.img_current_medicine.setText("")
        self.pills_num_msg_1.setText(QCoreApplication.translate("MainWindow", u"\u5171\u9700\u8981", None))
        self.txt_pills_num_needed.setText(QCoreApplication.translate("MainWindow", u"77", None))
        self.txt_current_medicine_percentage.setText(QCoreApplication.translate("MainWindow", u"0%", None))
        self.pills_num_msg_2.setText(QCoreApplication.translate("MainWindow", u"\u7247", None))
        self.txt_6.setText(QCoreApplication.translate("MainWindow", u"\u5206\u836f\u9636\u6bb5", None))
        self.txt_7.setText(QCoreApplication.translate("MainWindow", u"\u5206\u836f\u4e2d...  \u8bf7\u5c06\u5bf9\u5e94\u836f\u54c1\u6295\u5165\u5230\u5206\u836f\u673a\u4e2d", None))
        self.txt_5.setText(QCoreApplication.translate("MainWindow", u"\u5206\u836f\u9636\u6bb5", None))
        self.txt_4.setText(QCoreApplication.translate("MainWindow", u"\u5df2\u5206\u5b8c\u8be5\u75c5\u4eba\u7684\u4eca\u5929\u7684\u6240\u6709\u836f\u54c1", None))
        self.btn_finish_dispensing.setText(QCoreApplication.translate("MainWindow", u"\u5b8c\u6210\u5206\u836f", None))
        self.label_4.setText("")
        self.btn_continue_dispensing.setText(QCoreApplication.translate("MainWindow", u"\u7ee7\u7eed\u5206\u836f", None))
        self.btn_save_settings.setText(QCoreApplication.translate("MainWindow", u"\u4fdd\u5b58\u8bbe\u7f6e", None))
        self.label_7.setText(QCoreApplication.translate("MainWindow", u"\u5929\uff0c\u7ee7\u7eed\u5206\u836f", None))
        self.label_8.setText(QCoreApplication.translate("MainWindow", u"\u5929", None))
        self.label_5.setText(QCoreApplication.translate("MainWindow", u"\u6700\u5927\u5206\u836f\u5929\u6570", None))
        self.label_6.setText(QCoreApplication.translate("MainWindow", u"\u5f53\u5df2\u5206\u836f\u7269\u4f59\u91cf\u5c11\u7b49\u4e8e", None))
        self.spinBox_max_days.setSuffix("")
        self.spinBox_max_days.setPrefix("")
        self.label_9.setText(QCoreApplication.translate("MainWindow", u"\u670d\u52a1\u5668URL:", None))
        self.btn_zero_motor_position.setText(QCoreApplication.translate("MainWindow", u"\u5206\u914d\u7535\u673a\u4f4d\u7f6e\u5f52\u96f6", None))
    # retranslateUi

