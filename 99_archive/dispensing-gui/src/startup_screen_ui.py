# -*- coding: utf-8 -*-

################################################################################
## Form generated from reading UI file 'startup_screen.ui'
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
from PySide6.QtWidgets import (QApplication, QDialog, QLabel, QSizePolicy,
    QWidget)
import icons_rc

class Ui_StartupScreen(object):
    def setupUi(self, StartupScreen):
        if not StartupScreen.objectName():
            StartupScreen.setObjectName(u"StartupScreen")
        StartupScreen.resize(480, 480)
        StartupScreen.setMinimumSize(QSize(480, 480))
        StartupScreen.setMaximumSize(QSize(480, 480))
        self.txt_name = QLabel(StartupScreen)
        self.txt_name.setObjectName(u"txt_name")
        self.txt_name.setGeometry(QRect(140, 72, 151, 61))
        self.txt_name.setStyleSheet(u"QLabel {\n"
"    color: #000;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 32px;\n"
"    font-style: normal;\n"
"    font-weight: 700;   /* Bold */\n"
"}\n"
"")
        self.txt_process_info = QLabel(StartupScreen)
        self.txt_process_info.setObjectName(u"txt_process_info")
        self.txt_process_info.setGeometry(QRect(131, 385, 221, 16))
        self.txt_process_info.setStyleSheet(u"QLabel {\n"
"    color: #909090;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 16px;\n"
"    font-style: normal;\n"
"    font-weight: 400;   /* Normal */\n"
"}\n"
"")
        self.txt_process_info.setAlignment(Qt.AlignCenter)
        self.label = QLabel(StartupScreen)
        self.label.setObjectName(u"label")
        self.label.setGeometry(QRect(120, 140, 161, 121))
        self.txt_version = QLabel(StartupScreen)
        self.txt_version.setObjectName(u"txt_version")
        self.txt_version.setGeometry(QRect(290, 90, 61, 31))
        self.txt_version.setStyleSheet(u"QLabel {\n"
"    color: #868686;\n"
"    font-family: \"Microsoft YaHei\";\n"
"    font-size: 22px;\n"
"    font-style: normal;\n"
"    font-weight: 400;   /* Normal */\n"
"}\n"
"")
        self.label_2 = QLabel(StartupScreen)
        self.label_2.setObjectName(u"label_2")
        self.label_2.setGeometry(QRect(176, 170, 136, 136))
        self.label_2.setMinimumSize(QSize(136, 136))
        self.label_2.setMaximumSize(QSize(136, 136))
        self.label_2.setPixmap(QPixmap(u":/icons/logo_in_startPage.png"))
        self.label_2.setScaledContents(True)

        self.retranslateUi(StartupScreen)

        QMetaObject.connectSlotsByName(StartupScreen)
    # setupUi

    def retranslateUi(self, StartupScreen):
        StartupScreen.setWindowTitle(QCoreApplication.translate("StartupScreen", u"Dialog", None))
        self.txt_name.setText(QCoreApplication.translate("StartupScreen", u" EZ Dose", None))
        self.txt_process_info.setText(QCoreApplication.translate("StartupScreen", u"\u521d\u59cb\u5316\u5206\u836f\u673a\u4e2d...", None))
        self.label.setText("")
        self.txt_version.setText(QCoreApplication.translate("StartupScreen", u"V 3.0", None))
        self.label_2.setText("")
    # retranslateUi

