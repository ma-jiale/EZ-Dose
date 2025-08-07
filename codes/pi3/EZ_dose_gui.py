import sys
from PySide6.QtWidgets import QApplication
from PySide6.QtCore import Signal, Slot, QTimer, QObject
from dispenser_controller import DispenserController
from patient_prescription_manager import PatientPrescriptionManager
from cam_controller import CamController
from ui_manager import UIManager
class MainController(QObject):

    def __init__(self):
        super().__init__()
        self.ui_manager = UIManager()
        self.dispenser_controller = DispenserController()
        self.rx_manager = PatientPrescriptionManager()
        self.cam_controller = CamController()

        self.hardware_initialized = False
        self.database_connected = False

    def connect_signals(self):
        # Connect signals of UI with slots of maincontroller
        self.ui_manager.main_window.ui.start_dispense_button.clicked.connect(self.prepare_for_dispensing)

    @Slot()
    def initialize_hardware(self):
        self.dispenser_controller.start_dispenser_feedback_handler()
        self.dispenser_controller.reset_dispenser()
        self.cam_controller.reset_cam()
        self.hardware_initialized = True
        
    @Slot()
    def connect_database(self):
        self.rx_manager.connect_database()

    @Slot()
    def prepare_for_dispensing(self):
        print("测试")
        if not self.hardware_initialized:
            from PySide6.QtCore import QMetaObject, Qt
            QMetaObject.invokeMethod(self, "initialize_hardware", Qt.QueuedConnection)
        # if not self.controller.prescription_database_initialized:
        #     self.ui.guide_msg_2.setText("初始化处方数据库中...")
        #     from PySide6.QtCore import QMetaObject, Qt
        #     QMetaObject.invokeMethod(self.controller, "initialize_prescription_database", Qt.QueuedConnection)
        # from PySide6.QtCore import QMetaObject, Qt
        # QMetaObject.invokeMethod(self.controller, "prepare_for_rfid_detection", Qt.QueuedConnection)
        # # 延迟1秒后执行start_rfid_detection
        # QTimer.singleShot(1000, lambda: QMetaObject.invokeMethod(self.controller, "start_rfid_detection", Qt.QueuedConnection))

if __name__ == "__main__":
    app = QApplication(sys.argv)
    
    controller = MainController()
    controller.connect_signals()
    controller.ui_manager.main_window.show()
    sys.exit(app.exec())
    