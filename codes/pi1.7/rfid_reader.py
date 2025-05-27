import serial
import time

def calculate_checksum(data):
    """
    根据协议计算校验位 Checksum。
    :param data: 从帧类型到最后一个指令参数的字节数据
    :return: 校验位（1字节，十六进制）
    """
    checksum = sum(data) & 0xFF  # 累加和的最低一个字节
    return checksum

def build_polling_command(polling_count):
    """
    构建多次轮询指令的命令帧（包含帧头和帧尾）。
    :param polling_count: 轮询次数（0-65535）
    :return: 完整命令帧字节数组
    """
    header = 0xBB  # 帧头
    end = 0x7E     # 帧尾

    frame_type = 0x00
    command_code = 0x27
    param_length = [0x00, 0x03]  # 参数长度为3字节
    reserved = 0x22  # 保留位
    cnt_high = (polling_count >> 8) & 0xFF  # 高位字节
    cnt_low = polling_count & 0xFF         # 低位字节

    # 拼接数据帧
    data = [frame_type, command_code, *param_length, reserved, cnt_high, cnt_low]
    checksum = calculate_checksum(data)
    
    # 返回完整的命令帧，包含帧头、数据、校验位和帧尾
    return bytearray([header] + data + [checksum] + [end])

def build_stop_command():
    """
    构建停止多次轮询指令的命令帧。
    :return: 完整命令帧字节数组
    """
    header = 0xBB  # 帧头
    end = 0x7E     # 帧尾

    frame_type = 0x00
    command_code = 0x28
    param_length = [0x00, 0x00]  # 参数长度为0字节

    # 拼接数据帧
    data = [frame_type, command_code, *param_length]
    checksum = calculate_checksum(data)

    # 返回完整的停止命令帧
    return bytearray([header] + data + [checksum] + [end])

def parse_response(response):
    """
    解析响应帧。
    :param response: 接收到的字节数据
    :return: (解析结果: 成功/失败, 数据或错误码)
    """

    try:
        # 最小帧长度检查（错误帧为 8 字节）
        if len(response) < 8:
            return False, {"error": "Invalid response length"}

        # 检查帧头和帧尾
        if response[0] != 0xBB or response[-1] != 0x7E:
            return False, {"error": "Frame header or footer error"}

        # 解析固定字段
        frame_type = response[1]
        command_code = response[2]
        param_length = (response[3] << 8) + response[4]  # 参数长度 PL

        # 检查实际帧长度是否与 PL 一致
        expected_length = 7 + param_length
        if len(response) != expected_length:
            return False, {"error": f"Invalid frame length: expected {expected_length}, got {len(response)}"}

        # 解析错误帧
        if frame_type == 0x01 and command_code == 0xFF:
            error_code = response[5]
            return False, {"error": f"Error Code: {error_code}"}

        # 解析成功帧
        if frame_type == 0x02 and command_code == 0x22:
            rssi = response[5]
            pc = response[6:8].hex().upper()  # PC 字段
            epc_length = param_length - 5  # 参数长度减去固定字段长度
            epc = response[8:8 + epc_length].hex().upper()  # EPC 数据
            return True, {
                "RSSI": rssi,
                "PC": pc,
                "EPC": epc
            }

        # 停止指令响应帧
        if frame_type == 0x01 and command_code == 0x28 and param_length == 1:
            if response[5] == 0x00:  # 指令参数 0x00 表示成功
                return True, {"message": "Stop command executed successfully"}
        
        return False, {"error": "Unknown response format"}
    except Exception as e:
        # 捕获异常，返回错误信息
        return False, {"error": f"Exception during parsing: {e}"}

def split_frames(data):
    """
    从缓冲数据中提取独立帧。
    :param data: 字节数据
    :return: 分割后的帧列表
    """
    frames = []
    start = 0
    while start < len(data):
        if data[start] == 0xBB:  # 帧头
            end = data.find(0x7E, start)  # 找帧尾
            if end != -1:  # 找到帧尾
                frames.append(data[start:end + 1])
                start = end + 1
            else:
                break  # 如果未找到帧尾，退出循环
        else:
            start += 1  # 跳过非帧头字节
    return frames


def get_rfid(port='/dev/serial0', baudrate=115200, timeout=1):
    """
    获取RFID EPC号。
    :param port: 串口端口号
    :param baudrate: 波特率
    :param timeout: 串口超时时间
    :return: 包含 EPC 和错误码的字典 { "epc": EPC号, "error_code": 错误码 }
    """
    ser = serial.Serial(port, baudrate, timeout=timeout)
    polling_count = 10000  # 轮询次数

    try:
        ser.reset_input_buffer()  # 清空输入缓冲区
        ser.reset_output_buffer()  # 清空输出缓冲区

        # 构建轮询命令帧
        polling_command = build_polling_command(polling_count)
        ser.write(polling_command)
        print(f"发送轮询指令: {polling_command.hex().upper()}")

        # 记录起始时间
        start_time = time.time()

        # 缓存数据
        response_buffer = bytearray()
        stop_program = False  # 标记程序是否需要停止
        is_rfid_stopped = False  # 标记RFID是否停止

        while not stop_program:
            # 实时读取字节数据
            byte = ser.read(1)
            if byte:
                response_buffer.extend(byte)

                # 如果检测到帧尾，尝试解析
                if byte == b'\x7E':
                    frames = split_frames(response_buffer)  # 分割多帧
                    for frame in frames:
                        success, result = parse_response(frame)
                        if success:
                            # 获取 EPC 数据
                            epc = result.get('EPC', None)
                            if epc:
                                print(f"读取成功: EPC={epc}")
                                stop_program = True # 读取成功，标记程序需要停止
                                result_epc = {"epc": epc, "error_code": 0}  # 保存结果
                            else:
                                print("解析成功，但未找到 EPC 数据")
                        else:
                            print(f"读取失败: {result.get('error', '未知错误')}")
                    #清空缓存，继续接收下一帧    
                    response_buffer.clear()

            # 超时处理
            if time.time() - start_time > 5:
                print("超时：未接收到完整响应帧")
                stop_program = True  # 超时，标记程序需要停止

            time.sleep(0.01)  # 控制读取频率

        # 程序停止前，发送停止多次轮询指令
        stop_command = build_stop_command()
        ser.write(stop_command)
        print(f"发送停止指令: {stop_command.hex().upper()}")

        # 等待RFID停止响应（增加超时限制）
        wait_start = time.time()
        while not is_rfid_stopped:
            stop_response = ser.read(256)
            if stop_response:
                frames = split_frames(stop_response)  # 分割多帧
                for frame in frames:
                    success, result = parse_response(frame)
                    if success and isinstance(result, dict) and result.get("Command") == "Stop":
                        print(f"RFID 停止成功: {result}")
                        is_rfid_stopped = True  # 标记RFID已停止
                        break
                    elif not success:
                        print(f"停止指令失败: {result}")
            # 超时退出等待
            if time.time() - wait_start > 3:  # 等待最多3秒
                print("停止指令未收到预期响应，程序强制退出")
                break

        # 超时或失败和成功的返回
        return result_epc if 'result_epc' in locals() else {"epc": -1, "error_code": -1}

    finally:
        ser.close()
        print("程序已停止")
