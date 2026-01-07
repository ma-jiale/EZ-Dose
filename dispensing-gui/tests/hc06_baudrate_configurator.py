import serial
import time
import sys

def configure_hc06(port, target_baud=115200):
    """
    配置 HC-06 蓝牙模块波特率
    :param port: HC-06 连接的串口（通过 USB 转 TTL）
    :param target_baud: 目标波特率，默认 115200
    """
    # HC-06 出厂默认波特率通常是 9600
    default_baud = 9600
    
    print("=" * 60)
    print("HC-06 蓝牙模块配置工具")
    print("=" * 60)
    print(f"连接端口: {port}")
    print(f"目标波特率: {target_baud}")
    print("\n重要提示：")
    print("1. HC-06 必须处于未配对状态（LED 快速闪烁）")
    print("2. 确保硬件连接正确（TX-RX 交叉连接）")
    print("3. VCC 连接到 3.3V（部分模块支持 5V）")
    print("=" * 60)
    
    try:
        # 1. 先用默认波特率连接
        print(f"\n[步骤1] 使用默认波特率 {default_baud} 连接...")
        ser = serial.Serial(port, default_baud, timeout=2)
        time.sleep(1)
        
        # 2. 测试连接
        print("[步骤2] 测试 AT 指令...")
        ser.write(b'AT')
        time.sleep(0.5)
        response = ser.read_all()
        
        if response:
            print(f"✓ 收到响应: {response.decode('utf-8', errors='ignore')}")
        else:
            print("✗ 无响应！请检查：")
            print("  - HC-06 是否处于未配对状态")
            print("  - 接线是否正确")
            print("  - 电源是否正常")
            ser.close()
            return False
        
        # 3. 查询当前波特率（部分 HC-06 支持）
        print("\n[步骤3] 查询当前配置...")
        ser.write(b'AT+VERSION')
        time.sleep(0.5)
        response = ser.read_all()
        if response:
            print(f"版本信息: {response.decode('utf-8', errors='ignore')}")
        
        # 4. 设置波特率
        print(f"\n[步骤4] 设置波特率为 {target_baud}...")
        
        # HC-06 的波特率设置指令（两种常见格式）
        baud_commands = {
            9600: 'AT+BAUD4',
            19200: 'AT+BAUD5',
            38400: 'AT+BAUD6',
            57600: 'AT+BAUD7',
            115200: 'AT+BAUD8'
        }
        
        if target_baud in baud_commands:
            cmd = baud_commands[target_baud]
            print(f"发送指令: {cmd}")
            ser.write(cmd.encode())
            time.sleep(0.5)
            response = ser.read_all()
            
            if response:
                print(f"✓ 设备响应: {response.decode('utf-8', errors='ignore')}")
                
                if 'OK' in response.decode('utf-8', errors='ignore'):
                    print(f"\n✓✓✓ 成功！波特率已设置为 {target_baud} ✓✓✓")
                    print("\n后续步骤：")
                    print("1. 断开 USB 转 TTL 模块")
                    print("2. 将 HC-06 连接回 STM32")
                    print("3. 运行 ble.py 测试通信")
                else:
                    print("✗ 设置可能失败，请手动验证")
            else:
                print("✗ 无响应")
        else:
            print(f"✗ 不支持的波特率: {target_baud}")
            print(f"支持的波特率: {list(baud_commands.keys())}")
        
        ser.close()
        
        # 5. 验证新波特率
        print(f"\n[步骤5] 验证新波特率 {target_baud}...")
        time.sleep(1)
        ser = serial.Serial(port, target_baud, timeout=2)
        time.sleep(1)
        ser.write(b'AT')
        time.sleep(0.5)
        response = ser.read_all()
        
        if response:
            print(f"✓ 验证成功！新波特率 {target_baud} 工作正常")
            print(f"响应: {response.decode('utf-8', errors='ignore')}")
        else:
            print("✗ 验证失败，可能需要重新配置")
        
        ser.close()
        return True
        
    except serial.SerialException as e:
        print(f"\n✗ 串口错误: {e}")
        print("请检查：")
        print(f"  - 端口 {port} 是否正确")
        print("  - 端口是否被其他程序占用")
        return False
    except Exception as e:
        print(f"\n✗ 未知错误: {e}")
        import traceback
        traceback.print_exc()
        return False

if __name__ == "__main__":
    # 修改为你的 USB 转 TTL 模块对应的 COM 口
    usb_port = 'COM8'
    
    if len(sys.argv) > 1:
        usb_port = sys.argv[1]
    
    configure_hc06(usb_port, target_baud=115200)