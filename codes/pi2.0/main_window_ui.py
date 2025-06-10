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
from PySide6.QtWidgets import (QApplication, QComboBox, QFrame, QGridLayout,
    QLabel, QMainWindow, QProgressBar, QPushButton,
    QSizePolicy, QStackedWidget, QVBoxLayout, QWidget)

class Ui_MainWindow(object):
    def setupUi(self, MainWindow):
        if not MainWindow.objectName():
            MainWindow.setObjectName(u"MainWindow")
        MainWindow.resize(960, 539)
        MainWindow.setStyleSheet(u"")
        self.centralwidget = QWidget(MainWindow)
        self.centralwidget.setObjectName(u"centralwidget")
        self.centralwidget.setStyleSheet(u"#centralwidget\n"
"{\n"
"background:rgb(240,240,240)\n"
"}")
        self.gridLayout_3 = QGridLayout(self.centralwidget)
        self.gridLayout_3.setObjectName(u"gridLayout_3")
        self.gridLayout_3.setContentsMargins(0, 0, 0, 0)
        self.left_widget = QWidget(self.centralwidget)
        self.left_widget.setObjectName(u"left_widget")
        self.left_widget.setMinimumSize(QSize(208, 0))
        self.left_widget.setMaximumSize(QSize(16777215, 16777215))
        self.left_widget.setStyleSheet(u"#left_widget {\n"
"\n"
"}")
        self.verticalLayout = QVBoxLayout(self.left_widget)
        self.verticalLayout.setSpacing(12)
        self.verticalLayout.setObjectName(u"verticalLayout")
        self.verticalLayout.setContentsMargins(10, 6, -1, 28)
        self.init_widget = QWidget(self.left_widget)
        self.init_widget.setObjectName(u"init_widget")
        self.init_widget.setMinimumSize(QSize(0, 75))
        self.init_widget.setMaximumSize(QSize(16777215, 75))
        self.init_widget.setStyleSheet(u"#init_widget\n"
"{\n"
"border-radius: 7.5px;\n"
"background: #FFF;\n"
"}")
        self.gray_line = QFrame(self.init_widget)
        self.gray_line.setObjectName(u"gray_line")
        self.gray_line.setGeometry(QRect(9, 38, 170, 2))
        self.gray_line.setStyleSheet(u"QFrame\n"
"{\n"
"background: #D9D9D9;\n"
"}")
        self.gray_line.setFrameShape(QFrame.StyledPanel)
        self.gray_line.setFrameShadow(QFrame.Raised)
        self.choose_dispenser = QLabel(self.init_widget)
        self.choose_dispenser.setObjectName(u"choose_dispenser")
        self.choose_dispenser.setGeometry(QRect(13, 10, 71, 21))
        self.choose_dispenser.setStyleSheet(u"QLabel\n"
"{\n"
"color: #2E344F;\n"
"\n"
"/* \u4e09\u7ea7\u6807\u9898 */\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 12px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.choose_dispenser.setAlignment(Qt.AlignLeading|Qt.AlignLeft|Qt.AlignVCenter)
        self.database = QLabel(self.init_widget)
        self.database.setObjectName(u"database")
        self.database.setGeometry(QRect(13, 45, 61, 21))
        self.database.setStyleSheet(u"QLabel\n"
"{\n"
"color: #2E344F;\n"
"\n"
"/* \u4e09\u7ea7\u6807\u9898 */\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 12px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.database.setAlignment(Qt.AlignLeading|Qt.AlignLeft|Qt.AlignVCenter)
        self.refresh_database_button = QPushButton(self.init_widget)
        self.refresh_database_button.setObjectName(u"refresh_database_button")
        self.refresh_database_button.setGeometry(QRect(52, 43, 24, 26))
        self.refresh_database_button.setStyleSheet(u"QPushButton {\n"
"    border: none;\n"
"    background-color: transparent;\n"
"    outline: none;\n"
"}\n"
"\n"
"QPushButton:hover {\n"
"    background-color: rgba(200, 200, 200, 50);\n"
"    border-radius: 5px;\n"
"}\n"
"\n"
"QPushButton:pressed {\n"
"    background-color: rgba(150, 150, 150, 100);\n"
"}")
        icon = QIcon()
        icon.addFile(u"imgs/refresh.png", QSize(), QIcon.Mode.Normal, QIcon.State.Off)
        self.refresh_database_button.setIcon(icon)
        self.refresh_database_button.setIconSize(QSize(20, 20))
        self.dispenser_comboBox = QComboBox(self.init_widget)
        self.dispenser_comboBox.addItem("")
        self.dispenser_comboBox.addItem("")
        self.dispenser_comboBox.setObjectName(u"dispenser_comboBox")
        self.dispenser_comboBox.setGeometry(QRect(80, 45, 102, 22))
        font = QFont()
        font.setPointSize(8)
        self.dispenser_comboBox.setFont(font)
        self.dispenser_comboBox.setStyleSheet(u"QComboBox {\n"
"    border-radius: 4px;\n"
"    background: #E9E9E9;\n"
"    border: none;\n"
"    padding: 5px 10px;\n"
"	color: rgb(136, 136, 136);\n"
"}\n"
"\n"
"QComboBox:hover {\n"
"    background: #DDDDDD;\n"
"}\n"
"\n"
"QComboBox::drop-down {\n"
"    border: none;\n"
"}")
        self.dispenser_comboBox.setEditable(False)
        self.database_comboBox = QComboBox(self.init_widget)
        self.database_comboBox.setObjectName(u"database_comboBox")
        self.database_comboBox.setGeometry(QRect(80, 10, 102, 22))
        self.database_comboBox.setFont(font)
        self.database_comboBox.setStyleSheet(u"QComboBox {\n"
"    border-radius: 4px;\n"
"    background: #E9E9E9;\n"
"    border: none;\n"
"    padding: 5px 10px;\n"
"	color: rgb(136, 136, 136);\n"
"}\n"
"\n"
"QComboBox:hover {\n"
"    background: #DDDDDD;\n"
"}\n"
"\n"
"QComboBox::drop-down {\n"
"    border: none;\n"
"}")
        self.database_comboBox.setEditable(False)

        self.verticalLayout.addWidget(self.init_widget)

        self.process_widget = QWidget(self.left_widget)
        self.process_widget.setObjectName(u"process_widget")
        self.process_widget.setMaximumSize(QSize(16777215, 500))
        self.process_widget.setStyleSheet(u"#process_widget\n"
"{\n"
"border-radius: 5px;\n"
"background: #FFF;\n"
"}")
        self.gridLayout = QGridLayout(self.process_widget)
        self.gridLayout.setSpacing(0)
        self.gridLayout.setObjectName(u"gridLayout")
        self.gridLayout.setContentsMargins(0, 0, 0, 0)
        self.taskwidget = QWidget(self.process_widget)
        self.taskwidget.setObjectName(u"taskwidget")
        self.hardware_status_label = QLabel(self.taskwidget)
        self.hardware_status_label.setObjectName(u"hardware_status_label")
        self.hardware_status_label.setGeometry(QRect(0, 10, 158, 24))
        self.hardware_status_label.setMinimumSize(QSize(0, 20))
        self.hardware_status_label.setMaximumSize(QSize(16777215, 16777215))
        self.hardware_status_label.setStyleSheet(u"QLabel\n"
"{\n"
"border-radius: 4.5px;\n"
"\n"
"	color: rgb(136, 136, 136);\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 12px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.database_status_label = QLabel(self.taskwidget)
        self.database_status_label.setObjectName(u"database_status_label")
        self.database_status_label.setGeometry(QRect(0, 42, 158, 24))
        self.database_status_label.setMinimumSize(QSize(0, 24))
        self.database_status_label.setMaximumSize(QSize(16777215, 24))
        self.database_status_label.setStyleSheet(u"QLabel\n"
"{\n"
"border-radius: 4.5px;\n"
"\n"
"	color: rgb(136, 136, 136);\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 12px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.rfid_status_label = QLabel(self.taskwidget)
        self.rfid_status_label.setObjectName(u"rfid_status_label")
        self.rfid_status_label.setGeometry(QRect(0, 74, 158, 24))
        self.rfid_status_label.setMinimumSize(QSize(0, 24))
        self.rfid_status_label.setMaximumSize(QSize(16777215, 24))
        self.rfid_status_label.setStyleSheet(u"QLabel\n"
"{\n"
"border-radius: 4.5px;\n"
"\n"
"	color: rgb(136, 136, 136);\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 12px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.pills_dispensing_list_label = QLabel(self.taskwidget)
        self.pills_dispensing_list_label.setObjectName(u"pills_dispensing_list_label")
        self.pills_dispensing_list_label.setGeometry(QRect(0, 106, 158, 24))
        self.pills_dispensing_list_label.setMinimumSize(QSize(0, 24))
        self.pills_dispensing_list_label.setMaximumSize(QSize(16777215, 24))
        self.pills_dispensing_list_label.setStyleSheet(u"QLabel\n"
"{\n"
"border-radius: 4.5px;\n"
"\n"
"	color: rgb(136, 136, 136);\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 12px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.plate_closed_label = QLabel(self.taskwidget)
        self.plate_closed_label.setObjectName(u"plate_closed_label")
        self.plate_closed_label.setGeometry(QRect(0, 138, 158, 24))
        self.plate_closed_label.setMinimumSize(QSize(0, 24))
        self.plate_closed_label.setMaximumSize(QSize(16777215, 24))
        self.plate_closed_label.setStyleSheet(u"QLabel\n"
"{\n"
"border-radius: 4.5px;\n"
"\n"
"	color: rgb(136, 136, 136);\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 12px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.dispensing_finished_label = QLabel(self.taskwidget)
        self.dispensing_finished_label.setObjectName(u"dispensing_finished_label")
        self.dispensing_finished_label.setGeometry(QRect(0, 170, 158, 24))
        self.dispensing_finished_label.setMinimumSize(QSize(0, 24))
        self.dispensing_finished_label.setMaximumSize(QSize(16777215, 24))
        self.dispensing_finished_label.setStyleSheet(u"QLabel\n"
"{\n"
"border-radius: 4.5px;\n"
"\n"
"	color: rgb(136, 136, 136);\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 12px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.plate_opened_label = QLabel(self.taskwidget)
        self.plate_opened_label.setObjectName(u"plate_opened_label")
        self.plate_opened_label.setGeometry(QRect(0, 202, 158, 24))
        self.plate_opened_label.setMinimumSize(QSize(0, 24))
        self.plate_opened_label.setMaximumSize(QSize(16777215, 24))
        self.plate_opened_label.setStyleSheet(u"QLabel\n"
"{\n"
"border-radius: 4.5px;\n"
"\n"
"	color: rgb(136, 136, 136);\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 12px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.green_check_mark_1 = QLabel(self.taskwidget)
        self.green_check_mark_1.setObjectName(u"green_check_mark_1")
        self.green_check_mark_1.setGeometry(QRect(140, 14, 12, 12))
        self.green_check_mark_1.setPixmap(QPixmap(u"imgs/green_cheak_mark.png"))
        self.green_check_mark_1.setScaledContents(True)
        self.green_check_mark_2 = QLabel(self.taskwidget)
        self.green_check_mark_2.setObjectName(u"green_check_mark_2")
        self.green_check_mark_2.setGeometry(QRect(140, 46, 12, 12))
        self.green_check_mark_2.setPixmap(QPixmap(u"imgs/green_cheak_mark.png"))
        self.green_check_mark_2.setScaledContents(True)
        self.green_check_mark_3 = QLabel(self.taskwidget)
        self.green_check_mark_3.setObjectName(u"green_check_mark_3")
        self.green_check_mark_3.setGeometry(QRect(140, 78, 12, 12))
        self.green_check_mark_3.setPixmap(QPixmap(u"imgs/green_cheak_mark.png"))
        self.green_check_mark_3.setScaledContents(True)
        self.green_check_mark_4 = QLabel(self.taskwidget)
        self.green_check_mark_4.setObjectName(u"green_check_mark_4")
        self.green_check_mark_4.setGeometry(QRect(140, 110, 12, 12))
        self.green_check_mark_4.setPixmap(QPixmap(u"imgs/green_cheak_mark.png"))
        self.green_check_mark_4.setScaledContents(True)
        self.green_check_mark_5 = QLabel(self.taskwidget)
        self.green_check_mark_5.setObjectName(u"green_check_mark_5")
        self.green_check_mark_5.setGeometry(QRect(140, 142, 12, 12))
        self.green_check_mark_5.setPixmap(QPixmap(u"imgs/green_cheak_mark.png"))
        self.green_check_mark_5.setScaledContents(True)
        self.green_check_mark_6 = QLabel(self.taskwidget)
        self.green_check_mark_6.setObjectName(u"green_check_mark_6")
        self.green_check_mark_6.setGeometry(QRect(140, 174, 12, 12))
        self.green_check_mark_6.setPixmap(QPixmap(u"imgs/green_cheak_mark.png"))
        self.green_check_mark_6.setScaledContents(True)
        self.green_check_mark_7 = QLabel(self.taskwidget)
        self.green_check_mark_7.setObjectName(u"green_check_mark_7")
        self.green_check_mark_7.setGeometry(QRect(140, 206, 12, 12))
        self.green_check_mark_7.setPixmap(QPixmap(u"imgs/green_cheak_mark.png"))
        self.green_check_mark_7.setScaledContents(True)

        self.gridLayout.addWidget(self.taskwidget, 1, 1, 1, 1)

        self.process_title_widget = QWidget(self.process_widget)
        self.process_title_widget.setObjectName(u"process_title_widget")
        self.process_title_widget.setMinimumSize(QSize(0, 34))
        self.process_title_widget.setMaximumSize(QSize(16777215, 30))
        self.process_title_widget.setStyleSheet(u"QWidget\n"
"{\n"
" Border: None\n"
"}")
        self.process_title = QLabel(self.process_title_widget)
        self.process_title.setObjectName(u"process_title")
        self.process_title.setGeometry(QRect(10, 9, 101, 21))
        self.process_title.setStyleSheet(u"QLabel\n"
"{\n"
"color: #2E344F;\n"
"\n"
"/* \u4e09\u7ea7\u6807\u9898 */\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 12px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.process_title.setAlignment(Qt.AlignLeading|Qt.AlignLeft|Qt.AlignVCenter)

        self.gridLayout.addWidget(self.process_title_widget, 0, 0, 1, 2)

        self.progressBar_widget = QWidget(self.process_widget)
        self.progressBar_widget.setObjectName(u"progressBar_widget")
        self.progressBar_widget.setMaximumSize(QSize(20, 16777215))
        self.task_progressBar = QProgressBar(self.progressBar_widget)
        self.task_progressBar.setObjectName(u"task_progressBar")
        self.task_progressBar.setGeometry(QRect(6, 6, 8, 217))
        self.task_progressBar.setStyleSheet(u"QProgressBar {\n"
"    border: none;\n"
"    border-radius: 4px;\n"
"        background-color: hsla(0, 0%, 84%, 1);\n"
"    text-align: center;\n"
"}\n"
"\n"
"QProgressBar::chunk {\n"
"    background-color: hsla(165, 33%, 62%, 1);\n"
"    border-radius: 3px;\n"
"}")
        self.task_progressBar.setMaximum(7)
        self.task_progressBar.setValue(0)
        self.task_progressBar.setTextVisible(False)
        self.task_progressBar.setOrientation(Qt.Vertical)
        self.task_progressBar.setInvertedAppearance(True)
        self.task_progressBar.setTextDirection(QProgressBar.TopToBottom)

        self.gridLayout.addWidget(self.progressBar_widget, 1, 0, 1, 1)


        self.verticalLayout.addWidget(self.process_widget)


        self.gridLayout_3.addWidget(self.left_widget, 1, 0, 1, 1)

        self.menu_bar = QWidget(self.centralwidget)
        self.menu_bar.setObjectName(u"menu_bar")
        self.menu_bar.setMinimumSize(QSize(0, 32))
        self.menu_bar.setStyleSheet(u"QWidget {\n"
"    background: rgb(242, 243, 245);\n"
"    border-bottom: 0.5px solid #ccc;\n"
"}")
        self.setting_page_button = QPushButton(self.menu_bar)
        self.setting_page_button.setObjectName(u"setting_page_button")
        self.setting_page_button.setGeometry(QRect(66, 6, 54, 26))
        self.setting_page_button.setStyleSheet(u"QPushButton {\n"
"    border-top-left-radius: 10px;      /* \u5de6\u4e0a\u89d2\u5706\u89d2 */\n"
"    border-top-right-radius: 10px;     /* \u53f3\u4e0a\u89d2\u5706\u89d2 */\n"
"    border-bottom-left-radius: 0px;    /* \u5de6\u4e0b\u89d2\u65e0\u5706\u89d2 */\n"
"    border-bottom-right-radius: 0px;   /* \u53f3\u4e0b\u89d2\u65e0\u5706\u89d2 */\n"
"    color: #000;\n"
"    \n"
"    /* \u4e09\u7ea7\u6807\u9898 */\n"
"    font-family: \"Source Han Sans SC\";\n"
"    font-size: 12px;\n"
"    font-style: normal;\n"
"    font-weight: 400;\n"
"    line-height: normal;\n"
"}")
        self.main_page_button = QPushButton(self.menu_bar)
        self.main_page_button.setObjectName(u"main_page_button")
        self.main_page_button.setGeometry(QRect(10, 6, 54, 26))
        self.main_page_button.setStyleSheet(u"QPushButton {\n"
"    border-top-left-radius: 10px;      /* \u5de6\u4e0a\u89d2\u5706\u89d2 */\n"
"    border-top-right-radius: 10px;     /* \u53f3\u4e0a\u89d2\u5706\u89d2 */\n"
"    border-bottom-left-radius: 0px;    /* \u5de6\u4e0b\u89d2\u65e0\u5706\u89d2 */\n"
"    border-bottom-right-radius: 0px;   /* \u53f3\u4e0b\u89d2\u65e0\u5706\u89d2 */\n"
"    background: #7EBEAE;\n"
"    color: #FFF;\n"
"    \n"
"    /* \u4e09\u7ea7\u6807\u9898 */\n"
"    font-family: \"Source Han Sans SC\";\n"
"    font-size: 12px;\n"
"    font-style: normal;\n"
"    font-weight: 400;\n"
"    line-height: normal;\n"
"}")

        self.gridLayout_3.addWidget(self.menu_bar, 0, 0, 1, 2)

        self.rignt_stackedWidget = QStackedWidget(self.centralwidget)
        self.rignt_stackedWidget.setObjectName(u"rignt_stackedWidget")
        self.rignt_stackedWidget.setMaximumSize(QSize(16677215, 16777215))
        self.rignt_stackedWidget.setStyleSheet(u"")
        self.start_page = QWidget()
        self.start_page.setObjectName(u"start_page")
        self.guide_msg_1 = QLabel(self.start_page)
        self.guide_msg_1.setObjectName(u"guide_msg_1")
        self.guide_msg_1.setEnabled(True)
        self.guide_msg_1.setGeometry(QRect(48, 70, 671, 131))
        self.guide_msg_1.setLayoutDirection(Qt.LeftToRight)
        self.guide_msg_1.setAutoFillBackground(False)
        self.guide_msg_1.setStyleSheet(u"QLabel\n"
"{\n"
"color: #2E344F;\n"
"text-align: center;\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 72px;\n"
"font-style: normal;\n"
"font-weight: 700;\n"
"line-height: normal;\n"
"}")
        self.guide_msg_1.setFrameShape(QFrame.NoFrame)
        self.guide_msg_1.setLineWidth(1)
        self.guide_msg_1.setTextFormat(Qt.AutoText)
        self.guide_msg_1.setAlignment(Qt.AlignCenter)
        self.start_dispense_button = QPushButton(self.start_page)
        self.start_dispense_button.setObjectName(u"start_dispense_button")
        self.start_dispense_button.setGeometry(QRect(260, 260, 240, 50))
        self.start_dispense_button.setCursor(QCursor(Qt.CursorShape.PointingHandCursor))
        self.start_dispense_button.setLayoutDirection(Qt.LeftToRight)
        self.start_dispense_button.setAutoFillBackground(False)
        self.start_dispense_button.setStyleSheet(u"QPushButton {\n"
"	border-radius: 25px;\n"
"	background: #7EBEAE;\n"
"    border: none;\n"
"    color: #FFF;\n"
"    font-size: 24px;\n"
"    font-weight: 400;\n"
"    padding: 10px;\n"
"}\n"
"\n"
"QPushButton:hover {\n"
"    background-color: #66A696;\n"
"}\n"
"\n"
"QPushButton:pressed {\n"
"    background-color: #7EBEAE;\n"
"}")
        self.rignt_stackedWidget.addWidget(self.start_page)
        self.put_pan_in_page = QWidget()
        self.put_pan_in_page.setObjectName(u"put_pan_in_page")
        self.guide_msg_2 = QLabel(self.put_pan_in_page)
        self.guide_msg_2.setObjectName(u"guide_msg_2")
        self.guide_msg_2.setEnabled(True)
        self.guide_msg_2.setGeometry(QRect(-3, 60, 721, 131))
        self.guide_msg_2.setStyleSheet(u"QLabel\n"
"{\n"
"color: #2E344F;\n"
"text-align: center;\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 64px;\n"
"font-style: normal;\n"
"font-weight: 700;\n"
"line-height: normal;\n"
"}")
        self.guide_msg_2.setFrameShape(QFrame.NoFrame)
        self.guide_msg_2.setLineWidth(1)
        self.guide_msg_2.setTextFormat(Qt.AutoText)
        self.guide_msg_2.setAlignment(Qt.AlignCenter)
        self.get_prescription_msg = QLabel(self.put_pan_in_page)
        self.get_prescription_msg.setObjectName(u"get_prescription_msg")
        self.get_prescription_msg.setGeometry(QRect(243, 209, 371, 16))
        self.get_prescription_msg.setStyleSheet(u"QLabel\n"
"{\n"
"color: #2E344F;\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 16px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.get_prescription_msg.setAlignment(Qt.AlignLeading|Qt.AlignLeft|Qt.AlignVCenter)
        self.send_plate_in_button = QPushButton(self.put_pan_in_page)
        self.send_plate_in_button.setObjectName(u"send_plate_in_button")
        self.send_plate_in_button.setGeometry(QRect(230, 270, 240, 50))
        self.send_plate_in_button.setCursor(QCursor(Qt.CursorShape.PointingHandCursor))
        self.send_plate_in_button.setLayoutDirection(Qt.LeftToRight)
        self.send_plate_in_button.setStyleSheet(u"QPushButton {\n"
"	border-radius: 25px;\n"
"	background: #7EBEAE;\n"
"    border: none;\n"
"    color: #FFF;\n"
"    font-size: 24px;\n"
"    font-weight: 400;\n"
"    padding: 10px;\n"
"}\n"
"\n"
"QPushButton:hover {\n"
"    background-color: #66A696;\n"
"}\n"
"\n"
"QPushButton:pressed {\n"
"    background-color: #7EBEAE;\n"
"}")
        self.check_mark_2 = QLabel(self.put_pan_in_page)
        self.check_mark_2.setObjectName(u"check_mark_2")
        self.check_mark_2.setGeometry(QRect(220, 210, 16, 16))
        self.check_mark_2.setPixmap(QPixmap(u"imgs/green_check_mark.png"))
        self.check_mark_2.setScaledContents(True)
        self.check_mark_2.setAlignment(Qt.AlignCenter)
        self.refresh_rfid_button = QPushButton(self.put_pan_in_page)
        self.refresh_rfid_button.setObjectName(u"refresh_rfid_button")
        self.refresh_rfid_button.setGeometry(QRect(716, 0, 24, 24))
        self.refresh_rfid_button.setStyleSheet(u"QPushButton {\n"
"    border: none;\n"
"    background-color: transparent;\n"
"    outline: none;\n"
"}\n"
"\n"
"QPushButton:hover {\n"
"    background-color: rgba(200, 200, 200, 50);\n"
"    border-radius: 5px;\n"
"}\n"
"\n"
"QPushButton:pressed {\n"
"    background-color: rgba(150, 150, 150, 100);\n"
"}")
        self.refresh_rfid_button.setIcon(icon)
        self.refresh_rfid_button.setIconSize(QSize(20, 20))
        self.refresh_rfid_msg = QLabel(self.put_pan_in_page)
        self.refresh_rfid_msg.setObjectName(u"refresh_rfid_msg")
        self.refresh_rfid_msg.setGeometry(QRect(640, 0, 81, 21))
        self.refresh_rfid_msg.setStyleSheet(u"QLabel\n"
"{\n"
"color: #000;\n"
"\n"
"/* \u4e09\u7ea7\u6807\u9898 */\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 12px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.pan_img = QLabel(self.put_pan_in_page)
        self.pan_img.setObjectName(u"pan_img")
        self.pan_img.setGeometry(QRect(170, 220, 321, 141))
        self.pan_img.setPixmap(QPixmap(u"imgs/green_pan.png"))
        self.pan_img.setAlignment(Qt.AlignCenter)
        self.green_arrow = QLabel(self.put_pan_in_page)
        self.green_arrow.setObjectName(u"green_arrow")
        self.green_arrow.setGeometry(QRect(460, 210, 121, 141))
        self.green_arrow.setPixmap(QPixmap(u"imgs/green_arrow.png"))
        self.green_arrow.setAlignment(Qt.AlignCenter)
        self.error_mark_2 = QLabel(self.put_pan_in_page)
        self.error_mark_2.setObjectName(u"error_mark_2")
        self.error_mark_2.setGeometry(QRect(220, 210, 16, 16))
        self.error_mark_2.setPixmap(QPixmap(u"imgs/red_error_mark.png"))
        self.error_mark_2.setScaledContents(True)
        self.error_mark_2.setAlignment(Qt.AlignCenter)
        self.rignt_stackedWidget.addWidget(self.put_pan_in_page)
        self.dispense_page = QWidget()
        self.dispense_page.setObjectName(u"dispense_page")
        self.gridLayout_2 = QGridLayout(self.dispense_page)
        self.gridLayout_2.setSpacing(0)
        self.gridLayout_2.setObjectName(u"gridLayout_2")
        self.gridLayout_2.setContentsMargins(0, 0, 0, 0)
        self.dispense_info_widget = QWidget(self.dispense_page)
        self.dispense_info_widget.setObjectName(u"dispense_info_widget")
        self.dispense_info_widget.setMinimumSize(QSize(320, 0))
        self.progressBar_percentage = QLabel(self.dispense_info_widget)
        self.progressBar_percentage.setObjectName(u"progressBar_percentage")
        self.progressBar_percentage.setGeometry(QRect(200, 201, 71, 41))
        self.progressBar_percentage.setStyleSheet(u"QLabel\n"
"{\n"
"color: #515151;\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 24px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.dispense_progressBar = QProgressBar(self.dispense_info_widget)
        self.dispense_progressBar.setObjectName(u"dispense_progressBar")
        self.dispense_progressBar.setGeometry(QRect(10, 210, 181, 26))
        self.dispense_progressBar.setStyleSheet(u"QProgressBar {\n"
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
        self.dispense_progressBar.setValue(0)
        self.dispense_progressBar.setTextVisible(False)
        self.pills_num_msg_1 = QLabel(self.dispense_info_widget)
        self.pills_num_msg_1.setObjectName(u"pills_num_msg_1")
        self.pills_num_msg_1.setGeometry(QRect(10, 150, 101, 41))
        font1 = QFont()
        font1.setFamilies([u"Source Han Sans SC"])
        font1.setBold(True)
        font1.setItalic(False)
        self.pills_num_msg_1.setFont(font1)
        self.pills_num_msg_1.setStyleSheet(u"QLabel\n"
"{\n"
"color: #2E344F;\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 24px;\n"
"font-style: normal;\n"
"font-weight: 700;\n"
"line-height: normal;\n"
"}")
        self.pills_num_msg_3 = QLabel(self.dispense_info_widget)
        self.pills_num_msg_3.setObjectName(u"pills_num_msg_3")
        self.pills_num_msg_3.setGeometry(QRect(180, 150, 31, 41))
        self.pills_num_msg_3.setStyleSheet(u"QLabel\n"
"{\n"
"color: #2E344F;\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 24px;\n"
"font-style: normal;\n"
"font-weight: 700;\n"
"line-height: normal;\n"
"}")
        self.pills_num_msg_2 = QLabel(self.dispense_info_widget)
        self.pills_num_msg_2.setObjectName(u"pills_num_msg_2")
        self.pills_num_msg_2.setGeometry(QRect(70, 120, 111, 71))
        self.pills_num_msg_2.setStyleSheet(u"QLabel\n"
"{\n"
"color: #2E344F;\n"
"text-align: center;\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 64px;\n"
"font-style: normal;\n"
"font-weight: 700;\n"
"line-height: normal;\n"
"}")
        self.pills_num_msg_2.setAlignment(Qt.AlignCenter)
        self.is_dispensing_msg = QLabel(self.dispense_info_widget)
        self.is_dispensing_msg.setObjectName(u"is_dispensing_msg")
        self.is_dispensing_msg.setGeometry(QRect(15, 258, 271, 21))
        self.is_dispensing_msg.setStyleSheet(u"QLabel{\n"
"color: #2E344F;\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 16px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.put_pills_in_msg = QLabel(self.dispense_info_widget)
        self.put_pills_in_msg.setObjectName(u"put_pills_in_msg")
        self.put_pills_in_msg.setGeometry(QRect(15, 281, 271, 21))
        font2 = QFont()
        font2.setFamilies([u"Source Han Sans SC"])
        font2.setBold(False)
        font2.setItalic(False)
        self.put_pills_in_msg.setFont(font2)
        self.put_pills_in_msg.setStyleSheet(u"QLabel{\n"
"color: #2E344F;\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 16px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")

        self.gridLayout_2.addWidget(self.dispense_info_widget, 0, 3, 2, 1)

        self.patient_info_widget = QWidget(self.dispense_page)
        self.patient_info_widget.setObjectName(u"patient_info_widget")
        self.patient_info_widget.setMinimumSize(QSize(0, 100))
        self.patient_info_widget.setMaximumSize(QSize(16777215, 16777215))
        self.patient_info_widget.setStyleSheet(u"QWidget\n"
"{\n"
"	border: none;\n"
"	background-color: rgba(255, 255, 255, 0);\n"
"}")
        self.prescription_data = QLabel(self.patient_info_widget)
        self.prescription_data.setObjectName(u"prescription_data")
        self.prescription_data.setGeometry(QRect(140, 82, 241, 16))
        self.prescription_data.setStyleSheet(u"QLabel\n"
"{\n"
"color: #363636;\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 16px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.patient_name = QLabel(self.patient_info_widget)
        self.patient_name.setObjectName(u"patient_name")
        self.patient_name.setGeometry(QRect(140, 50, 81, 31))
        self.patient_name.setStyleSheet(u"QLabel\n"
"{\n"
"color: #363636;\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 16px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.room_msg = QLabel(self.patient_info_widget)
        self.room_msg.setObjectName(u"room_msg")
        self.room_msg.setGeometry(QRect(260, 50, 131, 31))
        self.room_msg.setStyleSheet(u"QLabel\n"
"{\n"
"color: #363636;\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 16px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.patient_img = QLabel(self.patient_info_widget)
        self.patient_img.setObjectName(u"patient_img")
        self.patient_img.setGeometry(QRect(60, 44, 67, 67))
        self.patient_img.setStyleSheet(u"QLabel\n"
"{\n"
"}")
        self.patient_img.setPixmap(QPixmap(u"imgs/patient.png"))
        self.patient_img.setScaledContents(True)

        self.gridLayout_2.addWidget(self.patient_info_widget, 0, 0, 1, 3)

        self.drug_info_widget = QWidget(self.dispense_page)
        self.drug_info_widget.setObjectName(u"drug_info_widget")
        self.drug_info_widget.setMinimumSize(QSize(400, 390))
        self.drug_info_widget.setMaximumSize(QSize(1677215, 16777215))
        self.drug_info_widget.setStyleSheet(u"QWidget\n"
"{\n"
"border: none;\n"
"	background-color: rgba(255, 255, 255, 0);\n"
"}")
        self.drug_card = QWidget(self.drug_info_widget)
        self.drug_card.setObjectName(u"drug_card")
        self.drug_card.setGeometry(QRect(60, 10, 334, 280))
        self.drug_card.setStyleSheet(u"QWidget\n"
"{\n"
"border-radius: 15px;\n"
"background: #FFF;\n"
"}")
        self.verticalLayout_2 = QVBoxLayout(self.drug_card)
        self.verticalLayout_2.setSpacing(0)
        self.verticalLayout_2.setObjectName(u"verticalLayout_2")
        self.verticalLayout_2.setContentsMargins(17, 17, 17, 0)
        self.current_drug_img = QLabel(self.drug_card)
        self.current_drug_img.setObjectName(u"current_drug_img")
        self.current_drug_img.setStyleSheet(u"QLabel\n"
"{\n"
"border-radius: 15px;\n"
"}")
        self.current_drug_img.setPixmap(QPixmap(u"imgs/\u963f\u83ab\u897f\u6797.png"))
        self.current_drug_img.setScaledContents(True)
        self.current_drug_img.setAlignment(Qt.AlignBottom|Qt.AlignHCenter)

        self.verticalLayout_2.addWidget(self.current_drug_img)

        self.current_drug = QLabel(self.drug_card)
        self.current_drug.setObjectName(u"current_drug")
        self.current_drug.setMinimumSize(QSize(0, 63))
        self.current_drug.setStyleSheet(u"QLabel\n"
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
        self.current_drug.setScaledContents(True)
        self.current_drug.setAlignment(Qt.AlignCenter)

        self.verticalLayout_2.addWidget(self.current_drug)


        self.gridLayout_2.addWidget(self.drug_info_widget, 1, 0, 1, 3)

        self.rignt_stackedWidget.addWidget(self.dispense_page)
        self.finish_dispense_page = QWidget()
        self.finish_dispense_page.setObjectName(u"finish_dispense_page")
        self.guide_msg_4 = QLabel(self.finish_dispense_page)
        self.guide_msg_4.setObjectName(u"guide_msg_4")
        self.guide_msg_4.setEnabled(True)
        self.guide_msg_4.setGeometry(QRect(40, 0, 671, 131))
        self.guide_msg_4.setStyleSheet(u"QLabel\n"
"{\n"
"color: #2E344F;\n"
"text-align: center;\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 64px;\n"
"font-style: normal;\n"
"font-weight: 700;\n"
"line-height: normal;\n"
"}")
        self.guide_msg_4.setFrameShape(QFrame.NoFrame)
        self.guide_msg_4.setLineWidth(1)
        self.guide_msg_4.setTextFormat(Qt.AutoText)
        self.guide_msg_4.setAlignment(Qt.AlignCenter)
        self.dispense_error_msg = QLabel(self.finish_dispense_page)
        self.dispense_error_msg.setObjectName(u"dispense_error_msg")
        self.dispense_error_msg.setGeometry(QRect(303, 219, 231, 16))
        self.dispense_error_msg.setStyleSheet(u"QLabel\n"
"{\n"
"color: #2E344F;\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 16px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.dispense_error_msg.setAlignment(Qt.AlignLeading|Qt.AlignLeft|Qt.AlignVCenter)
        self.check_mark_4 = QLabel(self.finish_dispense_page)
        self.check_mark_4.setObjectName(u"check_mark_4")
        self.check_mark_4.setGeometry(QRect(280, 220, 16, 16))
        self.check_mark_4.setPixmap(QPixmap(u"imgs/green_check_mark.png"))
        self.check_mark_4.setScaledContents(True)
        self.check_mark_4.setAlignment(Qt.AlignCenter)
        self.next_patient_button = QPushButton(self.finish_dispense_page)
        self.next_patient_button.setObjectName(u"next_patient_button")
        self.next_patient_button.setGeometry(QRect(260, 310, 240, 50))
        self.next_patient_button.setCursor(QCursor(Qt.CursorShape.PointingHandCursor))
        self.next_patient_button.setLayoutDirection(Qt.LeftToRight)
        self.next_patient_button.setStyleSheet(u"QPushButton {\n"
"	border-radius: 25px;\n"
"	background: #7EBEAE;\n"
"    border: none;\n"
"    color: #FFF;\n"
"    font-size: 24px;\n"
"    font-weight: 400;\n"
"    padding: 10px;\n"
"}\n"
"\n"
"QPushButton:hover {\n"
"    background-color: #66A696;\n"
"}\n"
"\n"
"QPushButton:pressed {\n"
"    background-color: #7EBEAE;\n"
"}")
        self.guide_msg_5 = QLabel(self.finish_dispense_page)
        self.guide_msg_5.setObjectName(u"guide_msg_5")
        self.guide_msg_5.setEnabled(True)
        self.guide_msg_5.setGeometry(QRect(50, 80, 671, 131))
        self.guide_msg_5.setStyleSheet(u"QLabel\n"
"{\n"
"color: #2E344F;\n"
"text-align: center;\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 64px;\n"
"font-style: normal;\n"
"font-weight: 700;\n"
"line-height: normal;\n"
"}")
        self.guide_msg_5.setFrameShape(QFrame.NoFrame)
        self.guide_msg_5.setLineWidth(1)
        self.guide_msg_5.setTextFormat(Qt.AutoText)
        self.guide_msg_5.setAlignment(Qt.AlignCenter)
        self.finish_dispensing_button = QPushButton(self.finish_dispense_page)
        self.finish_dispensing_button.setObjectName(u"finish_dispensing_button")
        self.finish_dispensing_button.setGeometry(QRect(260, 370, 240, 50))
        self.finish_dispensing_button.setCursor(QCursor(Qt.CursorShape.PointingHandCursor))
        self.finish_dispensing_button.setLayoutDirection(Qt.LeftToRight)
        self.finish_dispensing_button.setStyleSheet(u"QPushButton {\n"
"	border-radius: 25px;\n"
"	background: #2E344F;\n"
"    border: none;\n"
"    color: #FFF;\n"
"    font-size: 24px;\n"
"    font-weight: 400;\n"
"    padding: 10px;\n"
"}\n"
"\n"
"QPushButton:hover {\n"
"    background-color: #212538;\n"
"}\n"
"\n"
"QPushButton:pressed {\n"
"    background-color: #2E344F;\n"
"}")
        self.error_mark_4 = QLabel(self.finish_dispense_page)
        self.error_mark_4.setObjectName(u"error_mark_4")
        self.error_mark_4.setGeometry(QRect(280, 220, 16, 16))
        self.error_mark_4.setPixmap(QPixmap(u"imgs/red_error_mark.png"))
        self.error_mark_4.setScaledContents(True)
        self.error_mark_4.setAlignment(Qt.AlignCenter)
        self.fail_dispense_medicines_msg = QLabel(self.finish_dispense_page)
        self.fail_dispense_medicines_msg.setObjectName(u"fail_dispense_medicines_msg")
        self.fail_dispense_medicines_msg.setGeometry(QRect(40, 250, 661, 20))
        self.fail_dispense_medicines_msg.setStyleSheet(u"QLabel\n"
"{\n"
"color: rgb(255, 0, 0);\n"
"font-family: \"Source Han Sans SC\";\n"
"font-size: 16px;\n"
"font-style: normal;\n"
"font-weight: 400;\n"
"line-height: normal;\n"
"}")
        self.fail_dispense_medicines_msg.setAlignment(Qt.AlignCenter)
        self.rignt_stackedWidget.addWidget(self.finish_dispense_page)

        self.gridLayout_3.addWidget(self.rignt_stackedWidget, 1, 1, 1, 1)

        MainWindow.setCentralWidget(self.centralwidget)

        self.retranslateUi(MainWindow)

        self.rignt_stackedWidget.setCurrentIndex(2)


        QMetaObject.connectSlotsByName(MainWindow)
    # setupUi

    def retranslateUi(self, MainWindow):
        MainWindow.setWindowTitle(QCoreApplication.translate("MainWindow", u"MainWindow", None))
        self.choose_dispenser.setText(QCoreApplication.translate("MainWindow", u"\u9009\u62e9\u5206\u836f\u673a", None))
        self.database.setText(QCoreApplication.translate("MainWindow", u"\u5904\u65b9\u5e93", None))
        self.refresh_database_button.setText("")
        self.dispenser_comboBox.setItemText(0, QCoreApplication.translate("MainWindow", u"\u672c\u5730\u6570\u636e\u5e93", None))
        self.dispenser_comboBox.setItemText(1, QCoreApplication.translate("MainWindow", u"\u4e91\u7aef\u6570\u636e\u5e93", None))

        self.database_comboBox.setPlaceholderText(QCoreApplication.translate("MainWindow", u"\u5df2\u8fde\u63a5\u5206\u836f\u673a", None))
        self.hardware_status_label.setText(QCoreApplication.translate("MainWindow", u"  \u8fde\u63a5\u5206\u836f\u673a", None))
        self.database_status_label.setText(QCoreApplication.translate("MainWindow", u"  \u8fde\u63a5\u5904\u65b9\u5e93", None))
        self.rfid_status_label.setText(QCoreApplication.translate("MainWindow", u"  \u8bfb\u53d6RFID", None))
        self.pills_dispensing_list_label.setText(QCoreApplication.translate("MainWindow", u"  \u83b7\u53d6\u5206\u836f\u6e05\u5355", None))
        self.plate_closed_label.setText(QCoreApplication.translate("MainWindow", u"  \u9001\u5165\u836f\u76d8", None))
        self.dispensing_finished_label.setText(QCoreApplication.translate("MainWindow", u"  \u5206\u836f\u54c1", None))
        self.plate_opened_label.setText(QCoreApplication.translate("MainWindow", u"  \u9000\u51fa\u836f\u76d8", None))
        self.green_check_mark_1.setText("")
        self.green_check_mark_2.setText("")
        self.green_check_mark_3.setText("")
        self.green_check_mark_4.setText("")
        self.green_check_mark_5.setText("")
        self.green_check_mark_6.setText("")
        self.green_check_mark_7.setText("")
        self.process_title.setText(QCoreApplication.translate("MainWindow", u"\u5206\u836f\u4efb\u52a1\u6d41\u7a0b", None))
        self.setting_page_button.setText(QCoreApplication.translate("MainWindow", u"\u8bbe\u7f6e", None))
        self.main_page_button.setText(QCoreApplication.translate("MainWindow", u"\u4e3b\u9875", None))
        self.guide_msg_1.setText(QCoreApplication.translate("MainWindow", u"EZ Dose", None))
        self.start_dispense_button.setText(QCoreApplication.translate("MainWindow", u"\u5f00\u59cb\u5206\u836f", None))
        self.guide_msg_2.setText(QCoreApplication.translate("MainWindow", u"\u5c06\u836f\u76d8\u653e\u5165\u673a\u5668\u6258\u76d8\u4e2d", None))
        self.get_prescription_msg.setText(QCoreApplication.translate("MainWindow", u"\u6210\u529f\u83b7\u53d6\u5230\u5904\u65b9,\u5f53\u524d\u60a3\u8005\uff1a\u674e\u5c0f\u4e8c", None))
        self.send_plate_in_button.setText(QCoreApplication.translate("MainWindow", u"\u9001\u5165\u836f\u76d8", None))
        self.check_mark_2.setText("")
        self.refresh_rfid_button.setText("")
        self.refresh_rfid_msg.setText(QCoreApplication.translate("MainWindow", u"\u91cd\u65b0\u83b7\u53d6\u5904\u65b9", None))
        self.pan_img.setText("")
        self.green_arrow.setText("")
        self.error_mark_2.setText("")
        self.progressBar_percentage.setText(QCoreApplication.translate("MainWindow", u"0%", None))
        self.pills_num_msg_1.setText(QCoreApplication.translate("MainWindow", u"\u5171\u9700\u8981", None))
        self.pills_num_msg_3.setText(QCoreApplication.translate("MainWindow", u"\u7247", None))
        self.pills_num_msg_2.setText(QCoreApplication.translate("MainWindow", u"7", None))
        self.is_dispensing_msg.setText(QCoreApplication.translate("MainWindow", u"\u5206\u836f\u4e2d\u00b7\u00b7\u00b7", None))
        self.put_pills_in_msg.setText(QCoreApplication.translate("MainWindow", u"\u8bf7\u6295\u5165\u836f\u54c1\u5230\u5206\u836f\u673a\u4e2d", None))
        self.prescription_data.setText(QCoreApplication.translate("MainWindow", u"\u5904\u65b9\u8d77\u59cb\u65e5\u671f\uff1a2025\u5e743\u670824\u65e5", None))
        self.patient_name.setText(QCoreApplication.translate("MainWindow", u"\u5f20\u963f\u83b2", None))
        self.room_msg.setText(QCoreApplication.translate("MainWindow", u"6\u697c608\u53f7\u623f\u95f4", None))
        self.patient_img.setText("")
        self.current_drug_img.setText("")
        self.current_drug.setText(QCoreApplication.translate("MainWindow", u"\u963f\u83ab\u897f\u6797\u80f6\u56ca", None))
        self.guide_msg_4.setText(QCoreApplication.translate("MainWindow", u"\u5df2\u5206\u5b8c\u6240\u6709\u836f\u54c1", None))
        self.dispense_error_msg.setText(QCoreApplication.translate("MainWindow", u"\u672a\u53d1\u73b0\u5206\u836f\u9519\u8bef", None))
        self.check_mark_4.setText("")
        self.next_patient_button.setText(QCoreApplication.translate("MainWindow", u"\u7ee7\u7eed\u5206\u836f", None))
        self.guide_msg_5.setText(QCoreApplication.translate("MainWindow", u"\u8bf7\u62ff\u51fa\u836f\u76d8", None))
        self.finish_dispensing_button.setText(QCoreApplication.translate("MainWindow", u"\u7ed3\u675f\u5206\u836f", None))
        self.error_mark_4.setText("")
        self.fail_dispense_medicines_msg.setText(QCoreApplication.translate("MainWindow", u"\u963f\u83ab\u897f\u6797\uff0c\u590d\u65b9\u7518\u8349\u7247", None))
    # retranslateUi

