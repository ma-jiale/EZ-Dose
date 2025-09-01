# -*- coding: utf-8 -*-

################################################################################
## Form generated from reading UI file 'today_patient.ui'
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
from PySide6.QtWidgets import (QApplication, QDialog, QLabel, QPushButton,
    QSizePolicy, QVBoxLayout, QWidget)

class Ui_today_patient(object):
    def setupUi(self, today_patient):
        if not today_patient.objectName():
            today_patient.setObjectName(u"today_patient")
        today_patient.setEnabled(True)
        today_patient.resize(400, 300)
        today_patient.setContextMenuPolicy(Qt.DefaultContextMenu)
        today_patient.setWindowTitle(u"")
        today_patient.setModal(False)
        self.verticalLayout = QVBoxLayout(today_patient)
        self.verticalLayout.setObjectName(u"verticalLayout")
        self.txt_choose_patient = QLabel(today_patient)
        self.txt_choose_patient.setObjectName(u"txt_choose_patient")

        self.verticalLayout.addWidget(self.txt_choose_patient)

        self.patient_container = QWidget(today_patient)
        self.patient_container.setObjectName(u"patient_container")

        self.verticalLayout.addWidget(self.patient_container)

        self.pushButton = QPushButton(today_patient)
        self.pushButton.setObjectName(u"pushButton")

        self.verticalLayout.addWidget(self.pushButton)


        self.retranslateUi(today_patient)
        self.pushButton.clicked.connect(today_patient.accept)

        QMetaObject.connectSlotsByName(today_patient)
    # setupUi

    def retranslateUi(self, today_patient):
        self.txt_choose_patient.setText(QCoreApplication.translate("today_patient", u"\u9009\u62e9\u5206\u836f\u7684\u60a3\u8005", None))
        self.pushButton.setText(QCoreApplication.translate("today_patient", u"\u53d6\u6d88\u5206\u836f", None))
        pass
    # retranslateUi

