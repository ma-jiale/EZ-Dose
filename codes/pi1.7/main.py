import sys
import ui
import data
from control_test import *
from PyQt5 import QtCore, QtWidgets


@QtCore.pyqtSlot()
def to_list(patient_info, pill_list, pill_matrix):
    '''显示药品列表'''
    global form1, form2, ui2, ui1
    ui2.__init__()
    ui2.cap_off.connect(lambda: QtCore.QMetaObject.invokeMethod(worker_data, 'cap_off', QtCore.Qt.QueuedConnection))
    ui2.start_distribute.connect(lambda single_pill_matrix: QtCore.QMetaObject.invokeMethod(worker_ctrl, 'distribute', QtCore.Qt.QueuedConnection, QtCore.Q_ARG(object, single_pill_matrix)))
    ui2.open_dialog.connect(lambda kind, error_text: open_dialog(kind, error_text))
    ui2.return_to_main.connect(to_main)
    ui2.get_rfid.connect(worker_ctrl.get_rfid)

    ui2.update_parameters(patient_info["perception_number"], patient_info["patient_name"], patient_info["patient_portrait"], pill_list, pill_matrix)
    ui2.init_status()

    form2.close()
    form2.deleteLater()
    form2 = QtWidgets.QWidget()
    ui2.setupUi(form2)
    ui2.update_card()
    worker_data.pill_count_detected.emit(20)
    form2.show()
    form1.hide()
    ui1.init_btn()
    

@QtCore.pyqtSlot()
def to_main():
    '''返回主界面'''
    global form1, form2, ui2
    form1.show()
    form2.close()
    form2.deleteLater()
    form2 = QtWidgets.QWidget()
    
    ui2.__init__()
    # 重新连接信号槽
    ui2.cap_off.connect(lambda: QtCore.QMetaObject.invokeMethod(worker_data, 'cap_off', QtCore.Qt.QueuedConnection))
    ui2.start_distribute.connect(lambda single_pill_matrix: QtCore.QMetaObject.invokeMethod(worker_ctrl, 'distribute', QtCore.Qt.QueuedConnection, QtCore.Q_ARG(object, single_pill_matrix)))
    ui2.open_dialog.connect(lambda kind, error_text: open_dialog(kind, error_text))
    ui2.return_to_main.connect(to_main)
    ui2.get_rfid.connect(worker_ctrl.get_rfid)
    # QtCore.QMetaObject.invokeMethod(worker_ctrl, 'check_timer', QtCore.Qt.QueuedConnection)

def open_dialog(kind, error_text = ""):
    '''打开提醒界面'''
    print("打开提醒界面中")
    dialog = QtWidgets.QDialog()
    ui_temp.update_parameters(kind, error_text)
    ui_temp.setupUi(dialog)
    dialog.exec_()
    form1.show()

if __name__ == "__main__":
    app = QtWidgets.QApplication(sys.argv)
    app.aboutToQuit.connect(sys.exit)

    # 样式初始化
    ui.init_font()  # 初始化字体
    ui.load_stylesheet(app)  # 加载样式表 

    # 主线程并初始化worker
    form1 = QtWidgets.QWidget()
    ui1 = ui.Ui_Form1() 
    ui1.setupUi(form1)
    form1.show()


    form2 = QtWidgets.QWidget()
    ui2 = ui.Ui_Form2()
    form2.hide()

    dialog = QtWidgets.QDialog()
    ui_temp = ui.Ui_Dialog()

    # 创建数据处理线程并初始化worker
    thread_data = QtCore.QThread()
    worker_data = data.DataFetcher()
    worker_data.moveToThread(thread_data)

    # 创建机器控制线程并初始化worker
    thread_ctrl = QtCore.QThread()
    worker_ctrl = Control_thread("COM6")
    worker_ctrl.moveToThread(thread_ctrl)


    print(f'Main thread created: {id(app.thread())}')
    print(f'Data thread created: {id(thread_data)}, Data worker thread: {id(worker_data.thread())}')
    print(f'Control thread created: {id(thread_ctrl)}, Control worker thread: {id(worker_ctrl.thread())}')

    thread_data.started.connect(worker_data.on_thread)

    thread_ctrl.started.connect(lambda: setattr(worker_ctrl, 'repeat', 0))
    thread_ctrl.started.connect(worker_ctrl.open_plate)
    thread_ctrl.started.connect(worker_ctrl.on_thread)
    thread_ctrl.finished.connect(worker_ctrl.close_plate)

    # 连接信号槽
    ui1.ctrl_ready.connect(worker_ctrl.close_plate)
    ui1.ctrl_ready.connect(worker_ctrl.get_rfid)

    ui2.cap_off.connect(lambda: QtCore.QMetaObject.invokeMethod(worker_data, 'cap_off', QtCore.Qt.QueuedConnection))
    ui2.start_distribute.connect(lambda single_pill_matrix: QtCore.QMetaObject.invokeMethod(worker_ctrl, 'distribute', QtCore.Qt.QueuedConnection, QtCore.Q_ARG(object, single_pill_matrix)))
    ui2.open_dialog.connect(lambda kind, error_text: open_dialog(kind, error_text))
    ui2.return_to_main.connect(to_main)
    ui2.get_rfid.connect(worker_ctrl.get_rfid)

    ui_temp.return_to_main.connect(worker_ctrl.open_plate)
    ui_temp.return_to_main.connect(to_main)
    ui_temp.open_plate.connect(worker_ctrl.open_plate)
    ui_temp.close_plate.connect(worker_ctrl.close_plate)

    worker_data.data_ready.connect(lambda patient_info, pill_list, pill_matrix: to_list(patient_info, pill_list, pill_matrix))
    worker_data.pill_count_detected.connect(ui2.update_status)

    worker_ctrl.rfid_ready.connect(lambda rfid: QtCore.QMetaObject.invokeMethod(worker_data, 'fetch_info', QtCore.Qt.QueuedConnection, QtCore.Q_ARG(str, rfid)))
    worker_ctrl.distributed.connect(ui2.distributed)
    worker_ctrl.progress_distributed.connect(lambda pill_distributed : ui2.progress_update(2, pill_distributed))

    # 启动子线程
    thread_data.start()
    thread_ctrl.start()

    # 启动事件循环
    sys.exit(app.exec_())
