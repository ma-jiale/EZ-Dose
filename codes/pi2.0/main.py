import sys
from PySide6.QtWidgets import QApplication, QMainWindow
from PySide6.QtCore import QObject
from main_window_ui import Ui_MainWindow
from dispenser import Dispenser

class DispenserController(QObject):
    """分药机控制器，运行在单独的线程中"""

    def __init__(self):
        super().__init__()


class MainWindow(QMainWindow):
    def __init__(self):
        super(MainWindow, self).__init__()
        self.ui = Ui_MainWindow()
        self.ui.setupUi(self)


        self.ui.rignt_stackedWidget.setCurrentIndex(0)  # Set the initial page
        self.ui.RFID_msg.hide()  # Hide the RFID message initially
        self.ui.prescription_msg.hide()  # Hide the prescription message initially



        

if __name__ == "__main__":
    app = QApplication(sys.argv)

    window = MainWindow()
    window.show()

    sys.exit(app.exec())