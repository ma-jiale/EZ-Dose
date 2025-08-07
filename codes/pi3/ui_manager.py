from PySide6.QtWidgets import QMainWindow
from main_window_ui import Ui_MainWindow
class MainWindow(QMainWindow):
    """根据MainController更新UI界面， 并传递用户输入给MainController"""
    def __init__(self):
        super(MainWindow, self).__init__()
        self.ui = Ui_MainWindow()
        self.ui.setupUi(self)

class UIManager():
    def __init__(self):
        self.main_window = MainWindow()