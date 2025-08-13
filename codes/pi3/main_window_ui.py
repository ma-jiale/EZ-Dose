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
    QPushButton, QSizePolicy, QStackedWidget, QVBoxLayout,
    QWidget)

class Ui_MainWindow(object):
    def setupUi(self, MainWindow):
        if not MainWindow.objectName():
            MainWindow.setObjectName(u"MainWindow")
        MainWindow.resize(949, 677)
        self.centralwidget = QWidget(MainWindow)
        self.centralwidget.setObjectName(u"centralwidget")
        self.horizontalLayout = QHBoxLayout(self.centralwidget)
        self.horizontalLayout.setObjectName(u"horizontalLayout")
        self.stackedWidget = QStackedWidget(self.centralwidget)
        self.stackedWidget.setObjectName(u"stackedWidget")
        self.start_page = QWidget()
        self.start_page.setObjectName(u"start_page")
        self.btn_start_find_patient = QPushButton(self.start_page)
        self.btn_start_find_patient.setObjectName(u"btn_start_find_patient")
        self.btn_start_find_patient.setGeometry(QRect(430, 290, 221, 101))
        self.stackedWidget.addWidget(self.start_page)
        self.scan_qrcode_page = QWidget()
        self.scan_qrcode_page.setObjectName(u"scan_qrcode_page")
        self.img_cam_frame = QLabel(self.scan_qrcode_page)
        self.img_cam_frame.setObjectName(u"img_cam_frame")
        self.img_cam_frame.setGeometry(QRect(30, 20, 501, 301))
        self.txt_ = QLabel(self.scan_qrcode_page)
        self.txt_.setObjectName(u"txt_")
        self.txt_.setGeometry(QRect(320, 480, 201, 16))
        self.stackedWidget.addWidget(self.scan_qrcode_page)
        self.scan_rfid_page = QWidget()
        self.scan_rfid_page.setObjectName(u"scan_rfid_page")
        self.stackedWidget.addWidget(self.scan_rfid_page)
        self.rx_page = QWidget()
        self.rx_page.setObjectName(u"rx_page")
        self.btn_start_dispensing = QPushButton(self.rx_page)
        self.btn_start_dispensing.setObjectName(u"btn_start_dispensing")
        self.btn_start_dispensing.setGeometry(QRect(600, 320, 161, 61))
        self.stackedWidget.addWidget(self.rx_page)
        self.dispensing_page = QWidget()
        self.dispensing_page.setObjectName(u"dispensing_page")
        self.drug_card = QWidget(self.dispensing_page)
        self.drug_card.setObjectName(u"drug_card")
        self.drug_card.setGeometry(QRect(70, 310, 334, 280))
        self.drug_card.setStyleSheet(u"QWidget\n"
"{\n"
"border-radius: 15px;\n"
"background: #FFF;\n"
"}")
        self.verticalLayout_2 = QVBoxLayout(self.drug_card)
        self.verticalLayout_2.setSpacing(0)
        self.verticalLayout_2.setObjectName(u"verticalLayout_2")
        self.verticalLayout_2.setContentsMargins(17, 17, 17, 0)
        self.img_current_drug = QLabel(self.drug_card)
        self.img_current_drug.setObjectName(u"img_current_drug")
        self.img_current_drug.setStyleSheet(u"QLabel\n"
"{\n"
"border-radius: 15px;\n"
"}")
        self.img_current_drug.setPixmap(QPixmap(u"../pi2.0/imgs/\u963f\u83ab\u897f\u6797.png"))
        self.img_current_drug.setScaledContents(True)
        self.img_current_drug.setAlignment(Qt.AlignBottom|Qt.AlignHCenter)

        self.verticalLayout_2.addWidget(self.img_current_drug)

        self.txt_current_drug = QLabel(self.drug_card)
        self.txt_current_drug.setObjectName(u"txt_current_drug")
        self.txt_current_drug.setMinimumSize(QSize(0, 63))
        self.txt_current_drug.setStyleSheet(u"QLabel\n"
"{\n"
"color: #363636;\n"
"text-align: center;\n"
"\n"
"/* \u4e8c\u7ea7\u6807\u9898 */\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 16px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.txt_current_drug.setScaledContents(True)
        self.txt_current_drug.setAlignment(Qt.AlignCenter)

        self.verticalLayout_2.addWidget(self.txt_current_drug)

        self.img_pillcount_frame = QLabel(self.dispensing_page)
        self.img_pillcount_frame.setObjectName(u"img_pillcount_frame")
        self.img_pillcount_frame.setGeometry(QRect(530, 70, 320, 240))
        self.txt_pillcount_num = QLabel(self.dispensing_page)
        self.txt_pillcount_num.setObjectName(u"txt_pillcount_num")
        self.txt_pillcount_num.setGeometry(QRect(640, 370, 211, 211))
        self.txt_pillcount_num.setStyleSheet(u"QLabel {\n"
"        color:blue;                 /* \u6587\u5b57\u989c\u8272 */\n"
"        font-size: 64px;              /* \u5b57\u4f53\u5927\u5c0f */\n"
"        font-weight: bold;            /* \u52a0\u7c97 */\n"
"        border-radius: 8px;           /* \u5706\u89d2 */\n"
"        padding: 6px;                 /* \u5185\u8fb9\u8ddd */\n"
"    }\n"
"")
        self.stackedWidget.addWidget(self.dispensing_page)
        self.finish_page = QWidget()
        self.finish_page.setObjectName(u"finish_page")
        self.stackedWidget.addWidget(self.finish_page)

        self.horizontalLayout.addWidget(self.stackedWidget)

        MainWindow.setCentralWidget(self.centralwidget)

        self.retranslateUi(MainWindow)

        self.stackedWidget.setCurrentIndex(4)


        QMetaObject.connectSlotsByName(MainWindow)
    # setupUi

    def retranslateUi(self, MainWindow):
        MainWindow.setWindowTitle(QCoreApplication.translate("MainWindow", u"MainWindow", None))
        self.btn_start_find_patient.setText(QCoreApplication.translate("MainWindow", u"\u5f00\u59cb\u5206\u836f", None))
        self.img_cam_frame.setText("")
        self.txt_.setText(QCoreApplication.translate("MainWindow", u"\u8bf7\u5c06\u836f\u76d2\u7684\u4e8c\u7ef4\u7801\u5bf9\u51c6\u6570\u836f\u673a\u7684\u955c\u5934", None))
        self.btn_start_dispensing.setText(QCoreApplication.translate("MainWindow", u"\u5f00\u59cb\u5206\u836f", None))
        self.img_current_drug.setText("")
        self.txt_current_drug.setText(QCoreApplication.translate("MainWindow", u"\u963f\u83ab\u897f\u6797\u80f6\u56ca", None))
        self.img_pillcount_frame.setText("")
        self.txt_pillcount_num.setText(QCoreApplication.translate("MainWindow", u"NaN", None))
    # retranslateUi

