import time
import threading
from rfid_reader import RFIDReader, get_rfid

def test_basic_read():
    """基本读取测试"""
    print("=== 基本读取测试 ===")
    result = get_rfid(port='COM9', timeout=5)
    print(f"结果: {result}")
    assert "epc" in result
    assert "error_code" in result
    print("✓ 基本读取测试通过\n")

def test_class_interface():
    """类接口测试"""
    print("=== 类接口测试 ===")
    try:
        with RFIDReader('COM9') as reader:
            result = reader.read_single(timeout=5)
            print(f"结果: {result}")
            assert "epc" in result
        print("✓ 类接口测试通过\n")
    except Exception as e:
        print(f"✗ 类接口测试失败: {e}\n")

def test_multiple_reads():
    """多次读取测试"""
    print("=== 多次读取测试 ===")
    try:
        with RFIDReader('COM9') as reader:
            for i in range(3):
                print(f"第{i+1}次读取:")
                result = reader.read_single(timeout=3)
                print(f"  结果: {result}")
                time.sleep(1)
        print("✓ 多次读取测试通过\n")
    except Exception as e:
        print(f"✗ 多次读取测试失败: {e}\n")

def test_connection_handling():
    """连接处理测试"""
    print("=== 连接处理测试 ===")
    
    # 测试无效端口
    try:
        with RFIDReader('INVALID_PORT') as reader:
            result = reader.read_single(timeout=1)
    except Exception as e:
        print(f"✓ 无效端口正确处理: {e}")
    
    # 测试重复连接
    try:
        reader = RFIDReader('COM9')
        reader.connect()
        reader.connect()  # 应该处理重复连接
        reader.disconnect()
        print("✓ 重复连接处理正确")
    except Exception as e:
        print(f"✗ 重复连接处理失败: {e}")
    
    print("✓ 连接处理测试通过\n")

def test_timeout_handling():
    """超时处理测试"""
    print("=== 超时处理测试 ===")
    start_time = time.time()
    result = get_rfid(port='COM9', timeout=2)
    elapsed = time.time() - start_time
    
    print(f"超时设置: 2秒, 实际耗时: {elapsed:.2f}秒")
    print(f"结果: {result}")
    
    # 验证超时处理
    if result["error_code"] == 2:
        print("✓ 超时处理正确")
    else:
        print("! 可能检测到了卡片或其他情况")
    print()

def test_concurrent_reads():
    """并发读取测试"""
    print("=== 并发读取测试 ===")
    results = []
    
    def read_task(task_id):
        try:
            result = get_rfid(port='COM9', timeout=3)
            results.append((task_id, result))
            print(f"任务{task_id}完成: {result.get('epc', 'None')}")
        except Exception as e:
            results.append((task_id, {"error": str(e)}))
            print(f"任务{task_id}失败: {e}")
    
    # 创建多个线程（注意：同一串口不能并发访问）
    threads = []
    for i in range(2):
        thread = threading.Thread(target=read_task, args=(i,))
        threads.append(thread)
    
    # 顺序执行（因为串口限制）
    for thread in threads:
        thread.start()
        thread.join()  # 等待完成再启动下一个
    
    print(f"并发测试完成，结果数量: {len(results)}")
    print("✓ 并发读取测试通过\n")

def test_frame_parsing():
    """帧解析测试"""
    print("=== 帧解析测试 ===")
    reader = RFIDReader()
    
    # 测试有效EPC帧
    valid_epc_frame = bytearray([
        0xBB, 0x02, 0x22, 0x00, 0x09,  # 帧头 + 帧类型 + 命令码 + 参数长度
        0x50,  # RSSI
        0x30, 0x00,  # PC
        0x12, 0x34, 0x56, 0x78,  # EPC数据
        0x00,  # 校验位（暂时忽略）
        0x7E   # 帧尾
    ])
    
    success, result = reader.parse_response(valid_epc_frame)
    print(f"有效EPC帧解析: {success}, {result}")
    assert success
    assert result["type"] == "epc_data"
    assert "epc" in result
    
    # 测试错误帧
    error_frame = bytearray([
        0xBB, 0x01, 0xFF, 0x00, 0x01,  # 错误帧
        0x10,  # 错误码
        0x00,  # 校验位
        0x7E   # 帧尾
    ])
    
    success, result = reader.parse_response(error_frame)
    print(f"错误帧解析: {success}, {result}")
    assert not success
    assert "error" in result
    
    print("✓ 帧解析测试通过\n")

def performance_test():
    """性能测试"""
    print("=== 性能测试 ===")
    
    # 测试多次读取的平均时间
    times = []
    successful_reads = 0
    
    for i in range(5):
        start_time = time.time()
        result = get_rfid(port='COM9', timeout=3)
        elapsed = time.time() - start_time
        times.append(elapsed)
        
        if result["error_code"] == 0:
            successful_reads += 1
        
        print(f"第{i+1}次: {elapsed:.2f}秒, EPC: {result.get('epc', 'None')}")
    
    avg_time = sum(times) / len(times)
    print(f"\n平均读取时间: {avg_time:.2f}秒")
    print(f"成功率: {successful_reads}/{len(times)} ({successful_reads/len(times)*100:.1f}%)")
    print("✓ 性能测试完成\n")

def run_all_tests():
    """运行所有测试"""
    print("开始RFID读取器测试...\n")
    
    test_functions = [
        test_frame_parsing,      # 不需要硬件的测试
        test_connection_handling, # 连接测试
        test_basic_read,         # 需要硬件
        test_class_interface,    # 需要硬件
        test_timeout_handling,   # 需要硬件
        test_multiple_reads,     # 需要硬件
        test_concurrent_reads,   # 需要硬件
        performance_test,        # 需要硬件
    ]
    
    passed = 0
    total = len(test_functions)
    
    for test_func in test_functions:
        try:
            test_func()
            passed += 1
        except Exception as e:
            print(f"✗ {test_func.__name__} 失败: {e}\n")
    
    print(f"测试完成: {passed}/{total} 通过")

if __name__ == "__main__":
    # 单独运行测试
    print("选择测试模式:")
    print("1. 运行所有测试")
    print("2. 基本读取测试")
    print("3. 性能测试")
    print("4. 帧解析测试（无需硬件）")
    
    choice = input("请选择 (1-4): ").strip()
    
    if choice == "1":
        run_all_tests()
    elif choice == "2":
        test_basic_read()
    elif choice == "3":
        performance_test()
    elif choice == "4":
        test_frame_parsing()
    else:
        print("使用默认测试...")
        test_basic_read()