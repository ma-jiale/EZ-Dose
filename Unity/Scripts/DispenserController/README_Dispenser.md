# 分药机控制器 Unity 实现

## 📁 文件说明

### 核心文件
- **SerialProtocol.cs** - 串口通信协议定义
- **DispenserController.cs** - 分药机控制器主类
- **DispenserControllerTest.cs** - 测试脚本（演示用法）

## 🚀 快速开始

### 1. 前置准备

确保您已经：
- ✅ 集成了蓝牙串口通信插件（Android）
- ✅ 配置了必要的 Android 权限（见下方）
- ✅ 知道分药机的蓝牙 MAC 地址

### 2. 基本使用

```csharp
using EZDose.Hardware;

// 1. 添加控制器组件
DispenserController controller = gameObject.AddComponent<DispenserController>();

// 2. 初始化并连接
bool success = controller.Initialize("00:23:11:01:48:DF");

// 3. 发送命令
controller.OpenTray((success) => {
    if (success) {
        Debug.Log("舱门已打开");
    }
});
```

### 3. 完整分药流程

```csharp
// 创建药片矩阵（4行x7列 = 4个时段x7天）
byte[,] pillMatrix = new byte[4, 7]
{
    { 1, 1, 1, 1, 1, 1, 1 }, // 晚上
    { 2, 2, 2, 2, 2, 2, 2 }, // 中午
    { 1, 1, 1, 1, 1, 1, 1 }, // 早上
    { 0, 0, 0, 0, 0, 0, 0 }  // 预留
};

// 1. 复位机器
controller.ResetDispenser((success) => {
    if (!success) return;
    
    // 2. 设置参数
    controller.SetTurntableSpeed(150f, (s1) => {
        controller.SetServoAngle(45f, (s2) => {
            
            // 3. 发送药片矩阵
            controller.SendPillMatrix(pillMatrix, (s3) => {
                
                // 4. 关闭舱门开始分药
                controller.CloseTray();
            });
        });
    });
});
```

## 📋 API 参考

### 初始化和连接

```csharp
// 初始化控制器
bool Initialize(string macAddress = null)

// 断开连接
void Disconnect()
```

### 基本控制命令

```csharp
// 打开舱门
void OpenTray(Action<bool> callback = null)

// 关闭舱门
void CloseTray(Action<bool> callback = null)

// 暂停分药
void PauseDispenser(Action<bool> callback = null)

// 复位机器（阻塞操作，需等待DONE信号）
void ResetDispenser(Action<bool> callback = null)
```

### 分药控制

```csharp
// 发送药片矩阵（4x7矩阵）
void SendPillMatrix(byte[,] matrix, Action<bool> callback = null)

// 设置转盘电机转速
void SetTurntableSpeed(float speed, Action<bool> callback = null)

// 设置舵机角度（控制药物入口大小）
void SetServoAngle(float angle, Action<bool> callback = null)
```

### 清洁参数

```csharp
// 设置清洁速度
void SetCleanSpeed(float speed, Action<bool> callback = null)

// 设置清洁延迟（毫秒）
void SetCleanDelay(uint delayMs, Action<bool> callback = null)
```

### 状态查询

```csharp
bool IsConnected     // 是否已连接
bool IsTrayOpened    // 舱门是否打开
int MachineState     // 机器状态（0:空闲 1:工作 2:暂停 3:完成）
int ErrorCode        // 错误代码（0:正常 1:超时 2:计数错误）
int PillRemain       // 剩余药片数
int TotalPills       // 总药片数
```

### 事件回调

```csharp
// 订阅事件
controller.OnMachineInit += () => {
    Debug.Log("机器初始化");
};

controller.OnDispensingComplete += () => {
    Debug.Log("分药完成");
};

controller.OnCountError += () => {
    Debug.LogWarning("计数错误");
};

controller.OnPillCountUpdate += (count) => {
    Debug.Log($"已分药: {count}");
};

controller.OnError += (message) => {
    Debug.LogError($"错误: {message}");
};
```

## 🔧 配置参数

在 Inspector 面板中可调整的参数：

| 参数 | 默认值 | 说明 |
|------|--------|------|
| Device Mac Address | 00:00:00:00:00:00 | 蓝牙设备MAC地址 |
| Max Retry Count | 5 | 命令发送失败最大重试次数 |
| Ack Timeout | 0.2s | 等待ACK确认超时时间 |
| Done Timeout | 10s | 等待DONE信号超时时间 |

## 📱 Android 配置

### 1. 添加权限（AndroidManifest.xml）

```xml
<uses-permission android:name="android.permission.BLUETOOTH" />
<uses-permission android:name="android.permission.BLUETOOTH_ADMIN" />
<uses-permission android:name="android.permission.BLUETOOTH_CONNECT" />
<uses-permission android:name="android.permission.BLUETOOTH_SCAN" />
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
```

### 2. 蓝牙插件要求

确保您的蓝牙插件（`com.unity.bluetooth.BluetoothSerial`）支持以下方法：

```java
boolean isBluetoothAvailable()
boolean isBluetoothEnabled()
boolean connect(String address)
void disconnect()
boolean write(String data)
String read()
```

## 🧪 测试步骤

### 使用测试脚本

1. 在场景中创建空 GameObject
2. 添加 `DispenserControllerTest` 组件
3. 在 Inspector 中设置 MAC 地址
4. 连接 UI 组件（可选）
5. 运行场景，点击"连接"按钮

### Inspector 右键菜单测试

右键点击 `DispenserControllerTest` 组件：
- **测试完整分药流程** - 自动执行完整流程
- **测试设置所有参数** - 测试所有参数设置

## 🔍 调试技巧

### 1. 启用详细日志

所有关键操作都会输出日志：
```
[DispenserController] 开始初始化，MAC地址: XX:XX:XX:XX:XX:XX
[DispenserController] 收到消息: ACK
[DispenserController] 已出药: 5, 剩余: 23
```

### 2. 检查连接状态

```csharp
if (!controller.IsConnected)
{
    Debug.LogError("未连接设备");
    return;
}
```

### 3. 处理错误

```csharp
controller.OnError += (message) => {
    // 显示错误提示给用户
    ShowErrorDialog(message);
};
```

### 4. 编辑器模式

代码支持编辑器模式模拟：
- 连接、发送命令会模拟成功
- 不会实际发送蓝牙数据
- 日志显示"编辑器模式"标记

## ⚠️ 注意事项

### 1. 线程安全

所有回调都在主线程执行，可以安全操作 Unity UI 和 GameObject。

### 2. 命令队列

当前实现不支持并发发送命令，需等待上一个命令完成（收到ACK）后再发送下一个。

### 3. 异常处理

所有公共方法都包含 try-catch，不会抛出未处理异常。

### 4. 内存管理

组件销毁时会自动断开连接并清理资源。

## 🐛 常见问题

### Q: 无法连接设备
**A:** 检查：
1. MAC地址是否正确（大写，用冒号分隔）
2. 设备是否已配对
3. 蓝牙是否已启用
4. 权限是否已授予

### Q: 发送命令无响应
**A:** 检查：
1. 是否已成功连接（`IsConnected == true`）
2. 是否等待上一个命令完成
3. 查看日志是否有错误信息
4. 尝试增加重试次数

### Q: 药片计数不准确
**A:** 
1. 检查硬件光耦是否正常
2. 调整光耦阈值参数
3. 检查药片是否卡住

## 📊 协议说明

### 数据包格式

```
[包头2字节] [命令1字节] [数据N字节] [CRC2字节]
0xAA 0xBB   0xXX         ...         CRC_L CRC_H
```

### 命令列表

| 命令码 | 功能 | 数据 |
|--------|------|------|
| 0x00 | 复位 | 无 |
| 0x01 | 暂停 | 无 |
| 0x03 | 开门 | 无 |
| 0x04 | 关门 | 无 |
| 0x05 | 药片矩阵 | 28字节（4x7） |
| 0x08 | 设置电机 | ID(1字节) + 值(4字节float) |
| 0x0A | ACK | 无 |
| 0x0B | 清洁速度 | 4字节float |
| 0x0C | 清洁延迟 | 4字节uint32 |

### 反馈消息格式

文本格式，换行符结束：
```
machine init              // 机器初始化
machine_state:FINISH      // 分药完成
machine_state:CNT_ERR     // 计数错误
pills out:5               // 已出药数量
ACK                       // 命令确认
DONE                      // 操作完成
```

## 🎯 下一步

1. **集成到主程序**：将 DispenserController 集成到您的分药系统
2. **UI 开发**：创建用户界面显示分药进度
3. **错误处理**：完善错误提示和恢复机制
4. **参数优化**：根据实际硬件调整电机速度和角度
5. **数据持久化**：保存分药记录到本地或服务器

## 📞 技术支持

如有问题，请检查：
1. Unity Console 日志
2. Android Logcat 输出
3. 硬件串口调试工具

---

**版本**: 1.0  
**更新日期**: 2025-11-29  
**兼容性**: Unity 2021.3+ / Android 7.0+
