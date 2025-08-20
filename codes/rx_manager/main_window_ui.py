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
from PySide6.QtGui import (QAction, QBrush, QColor, QConicalGradient,
    QCursor, QFont, QFontDatabase, QGradient,
    QIcon, QImage, QKeySequence, QLinearGradient,
    QPainter, QPalette, QPixmap, QRadialGradient,
    QTransform)
from PySide6.QtWidgets import (QApplication, QGroupBox, QHBoxLayout, QLabel,
    QLineEdit, QMainWindow, QMenu, QMenuBar,
    QPushButton, QScrollArea, QSizePolicy, QStatusBar,
    QVBoxLayout, QWidget)

class Ui_MainWindow(object):
    def setupUi(self, MainWindow):
        if not MainWindow.objectName():
            MainWindow.setObjectName(u"MainWindow")
        MainWindow.resize(728, 700)
        self.refresh_action = QAction(MainWindow)
        self.refresh_action.setObjectName(u"refresh_action")
        self.exit_action = QAction(MainWindow)
        self.exit_action.setObjectName(u"exit_action")
        self.about_action = QAction(MainWindow)
        self.about_action.setObjectName(u"about_action")
        self.centralwidget = QWidget(MainWindow)
        self.centralwidget.setObjectName(u"centralwidget")
        self.main_layout = QVBoxLayout(self.centralwidget)
        self.main_layout.setObjectName(u"main_layout")
        self.search_group = QGroupBox(self.centralwidget)
        self.search_group.setObjectName(u"search_group")
        font = QFont()
        font.setFamilies([u"Microsoft YaHei"])
        font.setPointSize(11)
        font.setBold(True)
        self.search_group.setFont(font)
        self.search_layout = QHBoxLayout(self.search_group)
        self.search_layout.setObjectName(u"search_layout")
        self.search_label = QLabel(self.search_group)
        self.search_label.setObjectName(u"search_label")
        font1 = QFont()
        font1.setFamilies([u"Microsoft YaHei"])
        font1.setPointSize(10)
        font1.setBold(True)
        self.search_label.setFont(font1)

        self.search_layout.addWidget(self.search_label)

        self.search_input = QLineEdit(self.search_group)
        self.search_input.setObjectName(u"search_input")
        self.search_input.setFont(font1)

        self.search_layout.addWidget(self.search_input)

        self.search_button = QPushButton(self.search_group)
        self.search_button.setObjectName(u"search_button")
        self.search_button.setFont(font1)

        self.search_layout.addWidget(self.search_button)

        self.clear_button = QPushButton(self.search_group)
        self.clear_button.setObjectName(u"clear_button")
        self.clear_button.setFont(font1)

        self.search_layout.addWidget(self.clear_button)


        self.main_layout.addWidget(self.search_group)

        self.patient_info_group = QGroupBox(self.centralwidget)
        self.patient_info_group.setObjectName(u"patient_info_group")
        self.patient_info_group.setFont(font)
        self.patient_info_layout = QVBoxLayout(self.patient_info_group)
        self.patient_info_layout.setObjectName(u"patient_info_layout")
        self.patient_info_label = QLabel(self.patient_info_group)
        self.patient_info_label.setObjectName(u"patient_info_label")
        self.patient_info_label.setFont(font1)
        self.patient_info_label.setStyleSheet(u"color: gray; padding: 10px;")

        self.patient_info_layout.addWidget(self.patient_info_label)


        self.main_layout.addWidget(self.patient_info_group)

        self.medicine_group = QGroupBox(self.centralwidget)
        self.medicine_group.setObjectName(u"medicine_group")
        self.medicine_group.setFont(font)
        self.medicine_layout = QVBoxLayout(self.medicine_group)
        self.medicine_layout.setObjectName(u"medicine_layout")
        self.no_medicine_label = QLabel(self.medicine_group)
        self.no_medicine_label.setObjectName(u"no_medicine_label")
        font2 = QFont()
        font2.setFamilies([u"Microsoft YaHei"])
        font2.setPointSize(12)
        font2.setBold(True)
        self.no_medicine_label.setFont(font2)
        self.no_medicine_label.setStyleSheet(u"color: gray; padding: 50px;")
        self.no_medicine_label.setAlignment(Qt.AlignCenter)

        self.medicine_layout.addWidget(self.no_medicine_label)

        self.scroll_area = QScrollArea(self.medicine_group)
        self.scroll_area.setObjectName(u"scroll_area")
        self.scroll_area.setVerticalScrollBarPolicy(Qt.ScrollBarAsNeeded)
        self.scroll_area.setHorizontalScrollBarPolicy(Qt.ScrollBarAlwaysOff)
        self.scroll_area.setWidgetResizable(True)
        self.medicine_container = QWidget()
        self.medicine_container.setObjectName(u"medicine_container")
        self.medicine_container.setGeometry(QRect(0, 0, 688, 314))
        self.scroll_area.setWidget(self.medicine_container)

        self.medicine_layout.addWidget(self.scroll_area)


        self.main_layout.addWidget(self.medicine_group)

        MainWindow.setCentralWidget(self.centralwidget)
        self.menubar = QMenuBar(MainWindow)
        self.menubar.setObjectName(u"menubar")
        self.menubar.setGeometry(QRect(0, 0, 728, 22))
        self.file_menu = QMenu(self.menubar)
        self.file_menu.setObjectName(u"file_menu")
        self.help_menu = QMenu(self.menubar)
        self.help_menu.setObjectName(u"help_menu")
        MainWindow.setMenuBar(self.menubar)
        self.statusbar = QStatusBar(MainWindow)
        self.statusbar.setObjectName(u"statusbar")
        MainWindow.setStatusBar(self.statusbar)

        self.menubar.addAction(self.file_menu.menuAction())
        self.menubar.addAction(self.help_menu.menuAction())
        self.file_menu.addAction(self.refresh_action)
        self.file_menu.addSeparator()
        self.file_menu.addAction(self.exit_action)
        self.help_menu.addAction(self.about_action)

        self.retranslateUi(MainWindow)

        QMetaObject.connectSlotsByName(MainWindow)
    # setupUi

    def retranslateUi(self, MainWindow):
        MainWindow.setWindowTitle(QCoreApplication.translate("MainWindow", u"\u60a3\u8005\u5904\u65b9\u7ba1\u7406\u7cfb\u7edf", None))
        self.refresh_action.setText(QCoreApplication.translate("MainWindow", u"\u5237\u65b0\u6570\u636e", None))
#if QT_CONFIG(shortcut)
        self.refresh_action.setShortcut(QCoreApplication.translate("MainWindow", u"F5", None))
#endif // QT_CONFIG(shortcut)
        self.exit_action.setText(QCoreApplication.translate("MainWindow", u"\u9000\u51fa", None))
#if QT_CONFIG(shortcut)
        self.exit_action.setShortcut(QCoreApplication.translate("MainWindow", u"Ctrl+Q", None))
#endif // QT_CONFIG(shortcut)
        self.about_action.setText(QCoreApplication.translate("MainWindow", u"\u5173\u4e8e", None))
        self.search_group.setTitle(QCoreApplication.translate("MainWindow", u"\u60a3\u8005\u641c\u7d22", None))
        self.search_label.setText(QCoreApplication.translate("MainWindow", u"\u60a3\u8005\u59d3\u540d:", None))
        self.search_input.setPlaceholderText(QCoreApplication.translate("MainWindow", u"\u8bf7\u8f93\u5165\u60a3\u8005\u59d3\u540d\u8fdb\u884c\u641c\u7d22...", None))
        self.search_button.setText(QCoreApplication.translate("MainWindow", u"\u641c\u7d22", None))
        self.clear_button.setText(QCoreApplication.translate("MainWindow", u"\u6e05\u7a7a", None))
        self.patient_info_group.setTitle(QCoreApplication.translate("MainWindow", u"\u60a3\u8005\u4fe1\u606f", None))
        self.patient_info_label.setText(QCoreApplication.translate("MainWindow", u"\u8bf7\u641c\u7d22\u60a3\u8005\u4ee5\u663e\u793a\u4fe1\u606f", None))
        self.medicine_group.setTitle(QCoreApplication.translate("MainWindow", u"\u60a3\u8005\u836f\u54c1", None))
        self.no_medicine_label.setText(QCoreApplication.translate("MainWindow", u"\u8bf7\u5148\u641c\u7d22\u60a3\u8005", None))
        self.file_menu.setTitle(QCoreApplication.translate("MainWindow", u"\u6587\u4ef6", None))
        self.help_menu.setTitle(QCoreApplication.translate("MainWindow", u"\u5e2e\u52a9", None))
    # retranslateUi

