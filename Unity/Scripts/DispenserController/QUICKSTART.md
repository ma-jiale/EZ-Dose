# 🚀 快速开始指南

## 第一步：准备工作 (5分钟)

### 1. 确认文件已导入
在 Unity Project 面板中，应该看到：
```
Assets/Scripts/new_scripts/
├── SerialProtocol.cs              ✅ 协议定义
├── DispenserController.cs         ✅ 控制器主类
├── DispenserControllerTest.cs     ✅ 完整测试
├── SimpleDispenserExample.cs      ✅ 简单示例
├── README_Dispenser.md            📖 详细文档
├── IMPLEMENTATION_SUMMARY.md      📖 实现总结
└── QUICKSTART.md                  📖 本文件
```

### 2. 配置蓝牙插件
确保您的 Android 蓝牙插件已经配置好：
- 插件类名: `com.unity.bluetooth.BluetoothSerial`
- 已添加到 `Assets/Plugins/Android/` 目录

### 3. 获取设备MAC地址
- 在 Windows 上通过蓝牙设置查看
- 或使用您现有的 `BluetoothTest.cs` 扫描设备
- 格式: `00:23:11:01:48:DF` (大写，冒号分隔)

---

## 第二步：30秒测试 (最简单方式)

### 方法1：使用简单示例

1. **创建测试场景**
   - 打开任意场景
   - 创建空 GameObject (右键 Hierarchy -> Create Empty)
   - 命名为 "DispenserTest"

2. **添加脚本**
   - 选中 GameObject
   - 在 Inspector 中点击 "Add Component"
   - 搜索并添加 `SimpleDispenserExample`

3. **设置MAC地址**
   - 在 Inspector 中找到 "Mac Address" 字段
   - 输入您的设备MAC地址

4. **右键测试**
   - 在 Inspector 中右键点击 `SimpleDispenserExample` 组件
   - 按顺序测试：
     ```
     1. 连接设备       ✅
     2. 打开舱门       ✅
     3. 关闭舱门       ✅
     4. 发送简单药片矩阵 ✅
     5. 执行完整分药流程 ✅
     6. 断开连接       ✅
     ```

5. **查看结果**
   - 打开 Console 窗口 (Window -> General -> Console)
   - 查看日志输出

---

## 第三步：集成到您的项目 (10分钟)

### 在任意脚本中使用

```csharp
using UnityEngine;
using EZDose.Hardware;

public class YourScript : MonoBehaviour
{
    private DispenserController dispenser;
    
    void Start()
    {
        // 1. 创建控制器
        dispenser = gameObject.AddComponent<DispenserController>();
        
        // 2. 连接设备
        bool connected = dispenser.Initialize("00:23:11:01:48:DF");
        
        if (connected)
        {
            Debug.Log("设备已连接");
            
            // 3. 订阅事件
            dispenser.OnDispensingComplete += () => {
                Debug.Log("分药完成！");
            };
            
            // 4. 发送命令
            dispenser.OpenTray((success) => {
                if (success) Debug.Log("舱门已打开");
            });
        }
    }
}
```

---

## 第四步：实现分药流程 (参考示例)

### 完整流程代码

```csharp
void StartDispensing()
{
    StartCoroutine(DispensingProcess());
}

IEnumerator DispensingProcess()
{
    // 1. 复位机器
    bool resetDone = false;
    dispenser.ResetDispenser(success => resetDone = success);
    yield return new WaitUntil(() => resetDone);
    
    // 2. 设置参数
    bool paramDone = false;
    dispenser.SetTurntableSpeed(150f, s1 => {
        dispenser.SetServoAngle(45f, s2 => {
            paramDone = s1 && s2;
        });
    });
    yield return new WaitUntil(() => paramDone);
    
    // 3. 发送药片矩阵
    byte[,] matrix = CreatePillMatrix();
    bool matrixDone = false;
    dispenser.SendPillMatrix(matrix, s => matrixDone = s);
    yield return new WaitUntil(() => matrixDone);
    
    // 4. 开始分药
    bool closeDone = false;
    dispenser.CloseTray(s => closeDone = s);
    yield return new WaitUntil(() => closeDone);
    
    // 5. 等待完成
    yield return new WaitUntil(() => dispenser.MachineState == 3);
    
    Debug.Log("分药完成！");
}

byte[,] CreatePillMatrix()
{
    // 创建4x7矩阵
    return new byte[4, 7]
    {
        { 1, 1, 1, 1, 1, 1, 1 }, // 晚上
        { 2, 2, 2, 2, 2, 2, 2 }, // 中午
        { 1, 1, 1, 1, 1, 1, 1 }, // 早上
        { 0, 0, 0, 0, 0, 0, 0 }  // 预留
    };
}
```

---

## 常见问题速查

### ❌ 连接失败
```
检查清单:
☐ MAC地址是否正确？(大写，冒号分隔)
☐ 设备是否已配对？
☐ 蓝牙是否已启用？
☐ 权限是否已授予？
☐ 插件是否正确配置？
```

### ❌ 发送命令无响应
```
检查清单:
☐ IsConnected == true？
☐ 上一个命令是否已完成？
☐ Console 是否有错误日志？
☐ 设备是否正常工作？
```

### ❌ 编译错误
```
可能原因:
☐ 命名空间冲突？确保导入 EZDose.Hardware
☐ Unity版本过低？需要 2021.3+
☐ .NET 版本？需要 .NET Standard 2.1+
```

---

## 调试技巧

### 1. 启用详细日志
所有操作都会输出到 Console：
```
[DispenserController] 开始初始化，MAC地址: XX:XX:XX:XX:XX:XX
[DispenserController] 已连接到设备: XX:XX:XX:XX:XX:XX
[DispenserController] 收到消息: ACK
[DispenserController] 已出药: 5, 剩余: 23
```

### 2. 检查状态
```csharp
Debug.Log($"连接状态: {dispenser.IsConnected}");
Debug.Log($"舱门状态: {dispenser.IsTrayOpened}");
Debug.Log($"机器状态: {dispenser.MachineState}");
Debug.Log($"剩余药片: {dispenser.PillRemain}");
```

### 3. 订阅错误事件
```csharp
dispenser.OnError += (error) => {
    Debug.LogError($"错误: {error}");
    // 显示给用户或记录日志
};
```

---

## 下一步

### ✅ 基础测试完成后
1. 阅读 `README_Dispenser.md` 了解完整API
2. 参考 `DispenserControllerTest.cs` 学习高级用法
3. 查看 `IMPLEMENTATION_SUMMARY.md` 了解实现细节

### 🚀 准备集成到主程序
1. 将 DispenserController 添加到您的管理器脚本
2. 实现分药UI界面
3. 连接处方管理系统
4. 实现错误恢复机制

---

## 需要帮助？

### 📖 文档位置
- **完整API**: `README_Dispenser.md`
- **实现说明**: `IMPLEMENTATION_SUMMARY.md`
- **测试示例**: `DispenserControllerTest.cs`
- **简单示例**: `SimpleDispenserExample.cs`

### 🔍 日志查看
- Unity Console: Window -> General -> Console
- Android Logcat: Android SDK/platform-tools/adb logcat

### 🐛 调试工具
- Unity Profiler: Window -> Analysis -> Profiler
- Android Debug Bridge (ADB)

---

**预计时间**: 
- 第一次使用: 15分钟
- 熟练使用: 5分钟
- 集成到项目: 1-2小时

**难度**: ⭐⭐☆☆☆ (简单)

祝您使用顺利！🎉
