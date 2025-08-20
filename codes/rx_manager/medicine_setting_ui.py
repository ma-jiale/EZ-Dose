# -*- coding: utf-8 -*-

################################################################################
## Form generated from reading UI file 'medicine_setting.ui'
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
from PySide6.QtWidgets import (QApplication, QComboBox, QDateEdit, QFormLayout,
    QHBoxLayout, QLabel, QLineEdit, QPushButton,
    QSizePolicy, QSpinBox, QVBoxLayout, QWidget)

class Ui_medicine_setting(object):
    def setupUi(self, medicine_setting):
        if not medicine_setting.objectName():
            medicine_setting.setObjectName(u"medicine_setting")
        medicine_setting.resize(596, 380)
        self.horizontalLayout_3 = QHBoxLayout(medicine_setting)
        self.horizontalLayout_3.setObjectName(u"horizontalLayout_3")
        self.verticalLayout_3 = QVBoxLayout()
        self.verticalLayout_3.setObjectName(u"verticalLayout_3")
        self.img_cam_frame = QLabel(medicine_setting)
        self.img_cam_frame.setObjectName(u"img_cam_frame")
        sizePolicy = QSizePolicy(QSizePolicy.Policy.Fixed, QSizePolicy.Policy.Fixed)
        sizePolicy.setHorizontalStretch(0)
        sizePolicy.setVerticalStretch(0)
        sizePolicy.setHeightForWidth(self.img_cam_frame.sizePolicy().hasHeightForWidth())
        self.img_cam_frame.setSizePolicy(sizePolicy)
        self.img_cam_frame.setMinimumSize(QSize(400, 300))
        self.img_cam_frame.setMaximumSize(QSize(16777215, 16777215))

        self.verticalLayout_3.addWidget(self.img_cam_frame)

        self.btn_take_photo = QPushButton(medicine_setting)
        self.btn_take_photo.setObjectName(u"btn_take_photo")

        self.verticalLayout_3.addWidget(self.btn_take_photo)

        self.btb_delete_photo = QPushButton(medicine_setting)
        self.btb_delete_photo.setObjectName(u"btb_delete_photo")

        self.verticalLayout_3.addWidget(self.btb_delete_photo)


        self.horizontalLayout_3.addLayout(self.verticalLayout_3)

        self.verticalLayout_2 = QVBoxLayout()
        self.verticalLayout_2.setObjectName(u"verticalLayout_2")
        self.formLayout_2 = QFormLayout()
        self.formLayout_2.setObjectName(u"formLayout_2")
        self.txt_medicine_name = QLabel(medicine_setting)
        self.txt_medicine_name.setObjectName(u"txt_medicine_name")

        self.formLayout_2.setWidget(0, QFormLayout.ItemRole.LabelRole, self.txt_medicine_name)

        self.lineEdit_medicine_name = QLineEdit(medicine_setting)
        self.lineEdit_medicine_name.setObjectName(u"lineEdit_medicine_name")

        self.formLayout_2.setWidget(0, QFormLayout.ItemRole.FieldRole, self.lineEdit_medicine_name)


        self.verticalLayout_2.addLayout(self.formLayout_2)

        self.verticalLayout = QVBoxLayout()
        self.verticalLayout.setObjectName(u"verticalLayout")
        self.txt_dosage = QLabel(medicine_setting)
        self.txt_dosage.setObjectName(u"txt_dosage")

        self.verticalLayout.addWidget(self.txt_dosage)

        self.formLayout = QFormLayout()
        self.formLayout.setObjectName(u"formLayout")
        self.txt_morning = QLabel(medicine_setting)
        self.txt_morning.setObjectName(u"txt_morning")

        self.formLayout.setWidget(0, QFormLayout.ItemRole.LabelRole, self.txt_morning)

        self.spinBox_morning_dosage = QSpinBox(medicine_setting)
        self.spinBox_morning_dosage.setObjectName(u"spinBox_morning_dosage")

        self.formLayout.setWidget(0, QFormLayout.ItemRole.FieldRole, self.spinBox_morning_dosage)

        self.txt_noon = QLabel(medicine_setting)
        self.txt_noon.setObjectName(u"txt_noon")

        self.formLayout.setWidget(1, QFormLayout.ItemRole.LabelRole, self.txt_noon)

        self.spinBox_noon_dosage = QSpinBox(medicine_setting)
        self.spinBox_noon_dosage.setObjectName(u"spinBox_noon_dosage")

        self.formLayout.setWidget(1, QFormLayout.ItemRole.FieldRole, self.spinBox_noon_dosage)

        self.txt_evening = QLabel(medicine_setting)
        self.txt_evening.setObjectName(u"txt_evening")

        self.formLayout.setWidget(2, QFormLayout.ItemRole.LabelRole, self.txt_evening)

        self.spinBox_evening_dosage = QSpinBox(medicine_setting)
        self.spinBox_evening_dosage.setObjectName(u"spinBox_evening_dosage")

        self.formLayout.setWidget(2, QFormLayout.ItemRole.FieldRole, self.spinBox_evening_dosage)


        self.verticalLayout.addLayout(self.formLayout)


        self.verticalLayout_2.addLayout(self.verticalLayout)

        self.horizontalLayout_2 = QHBoxLayout()
        self.horizontalLayout_2.setObjectName(u"horizontalLayout_2")
        self.txt_dosage_time = QLabel(medicine_setting)
        self.txt_dosage_time.setObjectName(u"txt_dosage_time")

        self.horizontalLayout_2.addWidget(self.txt_dosage_time)

        self.comboBox_dosage_time = QComboBox(medicine_setting)
        self.comboBox_dosage_time.addItem("")
        self.comboBox_dosage_time.addItem("")
        self.comboBox_dosage_time.addItem("")
        self.comboBox_dosage_time.setObjectName(u"comboBox_dosage_time")

        self.horizontalLayout_2.addWidget(self.comboBox_dosage_time)


        self.verticalLayout_2.addLayout(self.horizontalLayout_2)

        self.horizontalLayout_6 = QHBoxLayout()
        self.horizontalLayout_6.setObjectName(u"horizontalLayout_6")
        self.txt_start_time = QLabel(medicine_setting)
        self.txt_start_time.setObjectName(u"txt_start_time")

        self.horizontalLayout_6.addWidget(self.txt_start_time)

        self.dateEdit_start_date = QDateEdit(medicine_setting)
        self.dateEdit_start_date.setObjectName(u"dateEdit_start_date")

        self.horizontalLayout_6.addWidget(self.dateEdit_start_date)


        self.verticalLayout_2.addLayout(self.horizontalLayout_6)

        self.horizontalLayout = QHBoxLayout()
        self.horizontalLayout.setObjectName(u"horizontalLayout")
        self.txt_duration_days = QLabel(medicine_setting)
        self.txt_duration_days.setObjectName(u"txt_duration_days")

        self.horizontalLayout.addWidget(self.txt_duration_days)

        self.spinBox_duration_days = QSpinBox(medicine_setting)
        self.spinBox_duration_days.setObjectName(u"spinBox_duration_days")

        self.horizontalLayout.addWidget(self.spinBox_duration_days)


        self.verticalLayout_2.addLayout(self.horizontalLayout)

        self.horizontalLayout_4 = QHBoxLayout()
        self.horizontalLayout_4.setObjectName(u"horizontalLayout_4")
        self.txt_size = QLabel(medicine_setting)
        self.txt_size.setObjectName(u"txt_size")

        self.horizontalLayout_4.addWidget(self.txt_size)

        self.comboBox_size = QComboBox(medicine_setting)
        self.comboBox_size.addItem("")
        self.comboBox_size.addItem("")
        self.comboBox_size.addItem("")
        self.comboBox_size.setObjectName(u"comboBox_size")

        self.horizontalLayout_4.addWidget(self.comboBox_size)


        self.verticalLayout_2.addLayout(self.horizontalLayout_4)

        self.horizontalLayout_5 = QHBoxLayout()
        self.horizontalLayout_5.setObjectName(u"horizontalLayout_5")
        self.label = QLabel(medicine_setting)
        self.label.setObjectName(u"label")

        self.horizontalLayout_5.addWidget(self.label)

        self.comboBox_isActive = QComboBox(medicine_setting)
        self.comboBox_isActive.addItem("")
        self.comboBox_isActive.addItem("")
        self.comboBox_isActive.setObjectName(u"comboBox_isActive")

        self.horizontalLayout_5.addWidget(self.comboBox_isActive)


        self.verticalLayout_2.addLayout(self.horizontalLayout_5)

        self.btn_save_medicine = QPushButton(medicine_setting)
        self.btn_save_medicine.setObjectName(u"btn_save_medicine")

        self.verticalLayout_2.addWidget(self.btn_save_medicine)


        self.horizontalLayout_3.addLayout(self.verticalLayout_2)

#if QT_CONFIG(shortcut)
        self.txt_medicine_name.setBuddy(self.lineEdit_medicine_name)
        self.txt_morning.setBuddy(self.spinBox_morning_dosage)
        self.txt_noon.setBuddy(self.spinBox_noon_dosage)
        self.txt_evening.setBuddy(self.spinBox_evening_dosage)
        self.txt_dosage_time.setBuddy(self.comboBox_dosage_time)
        self.txt_duration_days.setBuddy(self.spinBox_duration_days)
        self.txt_size.setBuddy(self.comboBox_size)
        self.label.setBuddy(self.comboBox_isActive)
#endif // QT_CONFIG(shortcut)

        self.retranslateUi(medicine_setting)

        QMetaObject.connectSlotsByName(medicine_setting)
    # setupUi

    def retranslateUi(self, medicine_setting):
        medicine_setting.setWindowTitle(QCoreApplication.translate("medicine_setting", u"Form", None))
        self.img_cam_frame.setText("")
        self.btn_take_photo.setText(QCoreApplication.translate("medicine_setting", u"\u62cd\u7167", None))
        self.btb_delete_photo.setText(QCoreApplication.translate("medicine_setting", u"\u6e05\u9664", None))
        self.txt_medicine_name.setText(QCoreApplication.translate("medicine_setting", u"\u836f\u54c1\u540d\u79f0", None))
        self.txt_dosage.setText(QCoreApplication.translate("medicine_setting", u"\u670d\u836f\u5242\u91cf\uff1a", None))
        self.txt_morning.setText(QCoreApplication.translate("medicine_setting", u"\u65e9", None))
        self.txt_noon.setText(QCoreApplication.translate("medicine_setting", u"\u4e2d", None))
        self.txt_evening.setText(QCoreApplication.translate("medicine_setting", u"\u665a", None))
        self.txt_dosage_time.setText(QCoreApplication.translate("medicine_setting", u"\u670d\u836f\u65f6\u95f4\uff1a", None))
        self.comboBox_dosage_time.setItemText(0, QCoreApplication.translate("medicine_setting", u"\u996d\u524d\u670d\u7528", None))
        self.comboBox_dosage_time.setItemText(1, QCoreApplication.translate("medicine_setting", u"\u996d\u540e\u670d\u7528", None))
        self.comboBox_dosage_time.setItemText(2, QCoreApplication.translate("medicine_setting", u"\u4efb\u610f\u65f6\u6bb5", None))

        self.txt_start_time.setText(QCoreApplication.translate("medicine_setting", u"\u5904\u65b9\u8d77\u59cb\u65f6\u95f4", None))
        self.txt_duration_days.setText(QCoreApplication.translate("medicine_setting", u"\u670d\u836f\u6301\u7eed\u65f6\u95f4/\u5929", None))
        self.txt_size.setText(QCoreApplication.translate("medicine_setting", u"\u836f\u7247\u5c3a\u5bf8", None))
        self.comboBox_size.setItemText(0, QCoreApplication.translate("medicine_setting", u"S", None))
        self.comboBox_size.setItemText(1, QCoreApplication.translate("medicine_setting", u"M", None))
        self.comboBox_size.setItemText(2, QCoreApplication.translate("medicine_setting", u"L", None))

        self.label.setText(QCoreApplication.translate("medicine_setting", u"\u662f\u5426\u542f\u4f7f\u7528", None))
        self.comboBox_isActive.setItemText(0, QCoreApplication.translate("medicine_setting", u"\u662f", None))
        self.comboBox_isActive.setItemText(1, QCoreApplication.translate("medicine_setting", u"\u5426", None))

        self.btn_save_medicine.setText(QCoreApplication.translate("medicine_setting", u"\u4fdd\u5b58", None))
    # retranslateUi

