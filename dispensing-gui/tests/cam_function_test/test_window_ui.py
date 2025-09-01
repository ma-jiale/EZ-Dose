# -*- coding: utf-8 -*-

################################################################################
## Form generated from reading UI file 'test_window.ui'
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
from PySide6.QtWidgets import (QApplication, QGridLayout, QLabel, QLineEdit,
    QPushButton, QSizePolicy, QVBoxLayout, QWidget)

class Ui_Form(object):
    def setupUi(self, Form):
        if not Form.objectName():
            Form.setObjectName(u"Form")
        Form.resize(582, 359)
        self.verticalLayout = QVBoxLayout(Form)
        self.verticalLayout.setObjectName(u"verticalLayout")
        self.cam_frame = QLabel(Form)
        self.cam_frame.setObjectName(u"cam_frame")
        self.cam_frame.setStyleSheet(u"")

        self.verticalLayout.addWidget(self.cam_frame)

        self.gridLayout = QGridLayout()
        self.gridLayout.setObjectName(u"gridLayout")
        self.start_camera = QPushButton(Form)
        self.start_camera.setObjectName(u"start_camera")

        self.gridLayout.addWidget(self.start_camera, 0, 0, 1, 1)

        self.save_medicine_img = QPushButton(Form)
        self.save_medicine_img.setObjectName(u"save_medicine_img")

        self.gridLayout.addWidget(self.save_medicine_img, 2, 3, 1, 1)

        self.pills_number = QLabel(Form)
        self.pills_number.setObjectName(u"pills_number")

        self.gridLayout.addWidget(self.pills_number, 2, 2, 1, 1)

        self.qrdata = QLabel(Form)
        self.qrdata.setObjectName(u"qrdata")

        self.gridLayout.addWidget(self.qrdata, 0, 2, 1, 1)

        self.stop_camera = QPushButton(Form)
        self.stop_camera.setObjectName(u"stop_camera")

        self.gridLayout.addWidget(self.stop_camera, 2, 0, 1, 1)

        self.count_pill_mode = QPushButton(Form)
        self.count_pill_mode.setObjectName(u"count_pill_mode")

        self.gridLayout.addWidget(self.count_pill_mode, 2, 1, 1, 1)

        self.scan_qrcode_mode = QPushButton(Form)
        self.scan_qrcode_mode.setObjectName(u"scan_qrcode_mode")

        self.gridLayout.addWidget(self.scan_qrcode_mode, 0, 1, 1, 1)

        self.edit_medicine_name = QLineEdit(Form)
        self.edit_medicine_name.setObjectName(u"edit_medicine_name")

        self.gridLayout.addWidget(self.edit_medicine_name, 0, 3, 1, 1)

        self.pause_camera = QPushButton(Form)
        self.pause_camera.setObjectName(u"pause_camera")

        self.gridLayout.addWidget(self.pause_camera, 1, 0, 1, 1)


        self.verticalLayout.addLayout(self.gridLayout)


        self.retranslateUi(Form)
        self.edit_medicine_name.returnPressed.connect(self.save_medicine_img.animateClick)

        QMetaObject.connectSlotsByName(Form)
    # setupUi

    def retranslateUi(self, Form):
        Form.setWindowTitle(QCoreApplication.translate("Form", u"Form", None))
        self.cam_frame.setText("")
        self.start_camera.setText(QCoreApplication.translate("Form", u"start_camera", None))
        self.save_medicine_img.setText(QCoreApplication.translate("Form", u"save_medicine_img", None))
        self.pills_number.setText(QCoreApplication.translate("Form", u"NaN", None))
        self.qrdata.setText(QCoreApplication.translate("Form", u"NaN", None))
        self.stop_camera.setText(QCoreApplication.translate("Form", u"stop_camera", None))
        self.count_pill_mode.setText(QCoreApplication.translate("Form", u"count_pill_mode", None))
        self.scan_qrcode_mode.setText(QCoreApplication.translate("Form", u"scan_qrcode_mode", None))
        self.pause_camera.setText(QCoreApplication.translate("Form", u"pause_camera", None))
    # retranslateUi

