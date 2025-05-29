import serial
import time
from typing import Dict, List, Tuple, Optional

class RFIDReader:
    """RFID读卡器类"""
    
    def __init__(self, port='COM9', baudrate=115200, timeout=1):
        self.port = port
        self.baudrate = baudrate
        self.timeout = timeout
        self.ser = None
        self.is_reading = False
        self.response_buffer = bytearray()
        
    def __enter__(self):
        """上下文管理器入口"""
        self.connect()
        return self
        
    def __exit__(self, exc_type, exc_val, exc_tb):
        """上下文管理器退出"""
        self.disconnect()
        
    def connect(self) -> bool:
        """连接RFID读卡器"""
        try:
            self.ser = serial.Serial(self.port, self.baudrate, timeout=self.timeout)
            self.ser.reset_input_buffer()
            self.ser.reset_output_buffer()
            print(f"[连接] RFID读卡器已连接到 {self.port}")
            return True
        except Exception as e:
            print(f"[错误] 连接失败: {e}")
            return False
            
    def disconnect(self):
        """断开连接"""
        if self.ser and self.ser.is_open:
            self.stop_polling()
            self.ser.close()
            print("[断开] RFID读卡器连接已关闭")
    
    @staticmethod
    def calculate_checksum(data: List[int]) -> int:
        """计算校验位"""
        return sum(data) & 0xFF
    
    def build_polling_command(self, polling_count: int = 10000) -> bytearray:
        """构建轮询命令"""
        if not (0 <= polling_count <= 65535):
            raise ValueError("轮询次数必须在0-65535之间")
            
        header = 0xBB
        end = 0x7E
        frame_type = 0x00
        command_code = 0x27
        param_length = [0x00, 0x03]
        reserved = 0x22
        cnt_high = (polling_count >> 8) & 0xFF
        cnt_low = polling_count & 0xFF
        
        data = [frame_type, command_code, *param_length, reserved, cnt_high, cnt_low]
        checksum = self.calculate_checksum(data)
        
        return bytearray([header] + data + [checksum] + [end])
    
    def build_stop_command(self) -> bytearray:
        """构建停止命令"""
        header = 0xBB
        end = 0x7E
        frame_type = 0x00
        command_code = 0x28
        param_length = [0x00, 0x00]
        
        data = [frame_type, command_code, *param_length]
        checksum = self.calculate_checksum(data)
        
        return bytearray([header] + data + [checksum] + [end])
    
    def parse_response(self, response: bytes) -> Tuple[bool, Dict]:
        """解析响应帧"""
        try:
            if len(response) < 8:
                return False, {"error": "响应帧长度不足"}
                
            if response[0] != 0xBB or response[-1] != 0x7E:
                return False, {"error": "帧头或帧尾错误"}
                
            frame_type = response[1]
            command_code = response[2]
            param_length = (response[3] << 8) + response[4]
            
            expected_length = 7 + param_length
            if len(response) != expected_length:
                return False, {"error": f"帧长度错误: 期望{expected_length}, 实际{len(response)}"}
            
            # 错误帧
            if frame_type == 0x01 and command_code == 0xFF:
                error_code = response[5]
                return False, {"error": f"设备错误码: {error_code}"}
            
            # 成功帧 - EPC数据
            if frame_type == 0x02 and command_code == 0x22:
                rssi = response[5]
                pc = response[6:8].hex().upper()
                epc_length = param_length - 5
                epc = response[8:8 + epc_length].hex().upper()
                
                return True, {
                    "type": "epc_data",
                    "rssi": rssi,
                    "pc": pc,
                    "epc": epc
                }
            
            # 停止命令响应
            if frame_type == 0x01 and command_code == 0x28:
                if param_length == 1 and response[5] == 0x00:
                    return True, {"type": "stop_success", "message": "停止命令执行成功"}
                
            return False, {"error": "未知响应格式"}
            
        except Exception as e:
            return False, {"error": f"解析异常: {e}"}
    
    def split_frames(self, data: bytes) -> List[bytes]:
        """分割数据帧"""
        frames = []
        start = 0
        
        while start < len(data):
            header_pos = data.find(0xBB, start)
            if header_pos == -1:
                break
                
            end_pos = data.find(0x7E, header_pos)
            if end_pos == -1:
                break
                
            frames.append(data[header_pos:end_pos + 1])
            start = end_pos + 1
            
        return frames
    
    def start_polling(self, polling_count: int = 10000) -> bool:
        """开始轮询"""
        if not self.ser or not self.ser.is_open:
            print("[错误] 串口未连接")
            return False
            
        try:
            self.is_reading = True
            command = self.build_polling_command(polling_count)
            self.ser.write(command)
            print(f"[发送] 轮询命令: {command.hex().upper()}")
            return True
        except Exception as e:
            print(f"[错误] 发送轮询命令失败: {e}")
            self.is_reading = False
            return False
    
    def stop_polling(self) -> bool:
        """停止轮询"""
        if not self.ser or not self.ser.is_open:
            return True
            
        try:
            self.is_reading = False
            command = self.build_stop_command()
            self.ser.write(command)
            print(f"[发送] 停止命令: {command.hex().upper()}")
            
            # 等待停止确认
            start_time = time.time()
            while time.time() - start_time < 3:
                response = self.ser.read(256)
                if response:
                    frames = self.split_frames(response)
                    for frame in frames:
                        success, result = self.parse_response(frame)
                        if success and result.get("type") == "stop_success":
                            print("[成功] 停止命令确认")
                            return True
                time.sleep(0.1)
                
            print("[警告] 停止命令未收到确认")
            return False
            
        except Exception as e:
            print(f"[错误] 发送停止命令失败: {e}")
            return False
    
    def read_single(self, timeout: float = 5.0) -> Dict:
        """读取单张卡片"""
        if not self.start_polling():
            return {"epc": None, "error_code": 1, "error": "启动轮询失败"}
        
        start_time = time.time()
        self.response_buffer.clear()
        
        try:
            while time.time() - start_time < timeout:
                if not self.is_reading:
                    break
                    
                data = self.ser.read(256)
                if data:
                    self.response_buffer.extend(data)
                    
                    # 检查是否有完整帧
                    frames = self.split_frames(self.response_buffer)
                    for frame in frames:
                        success, result = self.parse_response(frame)
                        if success and result.get("type") == "epc_data":
                            epc = result.get("epc")
                            if epc:
                                print(f"[成功] 读取EPC: {epc}")
                                return {
                                    "epc": epc,
                                    "error_code": 0,
                                    "rssi": result.get("rssi"),
                                    "pc": result.get("pc")
                                }
                        elif not success:
                            print(f"[解析错误] {result.get('error')}")
                    
                    # 清理已处理的帧
                    self.response_buffer.clear()
                
                time.sleep(0.01)
            
            print("[超时] 未读取到RFID卡片")
            return {"epc": None, "error_code": 2, "error": "读取超时"}
            
        except Exception as e:
            print(f"[异常] 读取过程发生错误: {e}")
            return {"epc": None, "error_code": 3, "error": str(e)}
        finally:
            self.stop_polling()

# 兼容原有接口的函数
def get_rfid(port='COM9', baudrate=115200, timeout=5) -> Dict:
    """
    获取RFID EPC号 - 兼容原有接口
    :param port: 串口端口号
    :param baudrate: 波特率
    :param timeout: 超时时间
    :return: {"epc": EPC号或None, "error_code": 错误码, "error": 错误信息}
    """
    try:
        with RFIDReader(port, baudrate) as reader:
            return reader.read_single(timeout)
    except Exception as e:
        print(f"[错误] RFID读取失败: {e}")
        return {"epc": None, "error_code": -1, "error": str(e)}
