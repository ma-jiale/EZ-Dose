# EZ Dose 移植项目 - 整体架构

## 📋 项目概述

将 **PyQt Windows 分药系统** 成功移植到 **Unity 移动端**。

- **原系统**: Python + PyQt + OpenCV + 串口通信
- **新系统**: Unity C# + OpenCV for Unity + 蓝牙通信
- **目标平台**: Android / iOS 移动设备

---

## 🗂️ 项目结构

```
Assets/Scripts/
├── PythonRef/                    📖 Python原版参考代码
│   ├── dispenser_controller.py
│   ├── pill_counter.py
│   ├── patient_prescription_manager.py
│   ├── main_controller.py
│   ├── cam_controller.py
│   ├── EZ_dose_gui.py
│   └── README.md
│
├── DispenserController/                  ✅ 分药机控制系统 (已完成)
│   ├── SerialProtocol.cs              (175行) 通信协议
│   ├── DispenserController.cs         (549行) 核心控制器
│   ├── DispenserControllerTest.cs     (342行) 测试脚本
│   ├── SimpleDispenserExample.cs      (162行) 简单示例
│   ├── README_Dispenser.md            📖 完整文档
│   ├── QUICKSTART.md                  📖 快速开始
│   └── IMPLEMENTATION_SUMMARY.md      📖 实现总结
│
├── pill_counter/                 ✅ 药片计数系统 (已完成)
│   ├── PillCounter.cs                 (670行) 计数算法
│   ├── CameraController.cs            (432行) 摄像头控制
│   ├── PillCounterTest.cs             (217行) 测试脚本
│   ├── README_PillCounter.md          📖 完整文档
│   ├── QUICKSTART_PillCounter.md      📖 快速开始
│   └── IMPLEMENTATION_SUMMARY.md      📖 实现总结
│
└── scripts/                      🔄 待实现模块
    ├── PatientPrescriptionManager.cs  (待创建)
    ├── MainController.cs              (待创建)
    └── UIManager.cs                   (待创建)
```

---

## 📊 移植进度总览

### ✅ 已完成模块 (40%)

#### 1. 分药机控制系统 (new_scripts/)
- ✅ **SerialProtocol** - 串口通信协议定义
- ✅ **DispenserController** - 蓝牙通信和命令控制
- ✅ **测试脚本** - 完整的功能测试
- ✅ **文档** - API文档 + 快速开始指南

**状态**: 🟢 生产就绪 (可直接使用)

#### 2. 药片计数系统 (pill_counter/)
- ✅ **PillCounter** - OpenCV图像处理和计数算法
- ✅ **CameraController** - Unity摄像头集成
- ✅ **测试脚本** - UI交互测试
- ✅ **文档** - API文档 + 快速开始指南

**状态**: 🟢 生产就绪 (可直接使用)

### 🔄 进行中模块 (0%)

暂无

### ⏳ 待实现模块 (60%)

#### 3. 处方管理系统
- ⏳ PatientPrescriptionManager.cs
- ⏳ 数据模型 (Patient, Prescription, Medicine)
- ⏳ 网络通信 (API Client)
- ⏳ 本地存储 (CSV/SQLite)

#### 4. 主控制器
- ⏳ MainController.cs
- ⏳ 分药流程协调
- ⏳ 状态机管理

#### 5. UI系统
- ⏳ 7个页面界面
- ⏳ 页面导航管理
- ⏳ 用户交互

#### 6. 二维码扫描
- ⏳ QR/条形码识别
- ⏳ ZXing集成

---

## 🏗️ 系统架构图

```
┌─────────────────────────────────────────────────────────────┐
│                        Unity 应用层                          │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐     │
│  │   UI Manager │  │ Main Controller│ │ Config Manager│     │
│  └──────┬───────┘  └───────┬────────┘ └──────────────┘     │
│         │                   │                                │
│         └───────────────────┼────────────────┐              │
│                             │                 │              │
├─────────────────────────────┼─────────────────┼──────────────┤
│                       业务逻辑层                              │
├─────────────────────────────┼─────────────────┼──────────────┤
│                             │                 │              │
│  ┌──────────────────────────▼──┐  ┌──────────▼─────────┐   │
│  │ PatientPrescriptionManager  │  │  Dispensing Manager │   │
│  │  - 处方管理                   │  │  - 分药流程         │   │
│  │  - 数据同步                   │  │  - 状态监控         │   │
│  │  - 药丸矩阵生成               │  │  - 错误处理         │   │
│  └─────────────────────────────┘  └────────────────────┘   │
│                                                              │
├──────────────────────────────────────────────────────────────┤
│                       硬件控制层                              │
├──────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌──────────────────┐  ┌──────────────────┐  ┌───────────┐ │
│  │ DispenserController│ │ CameraController  │  │QR Scanner │ │
│  │  ✅ 蓝牙通信        │  │  ✅ 摄像头管理     │  │⏳ 扫码    │ │
│  │  ✅ 命令发送        │  │  ✅ 帧处理         │  │           │ │
│  │  ✅ 反馈接收        │  │  ✅ PillCounter   │  │           │ │
│  └──────────┬─────────┘  └─────────┬────────┘  └─────┬─────┘ │
│             │                      │                  │       │
├─────────────┼──────────────────────┼──────────────────┼───────┤
│                       硬件接口层                              │
├─────────────┼──────────────────────┼──────────────────┼───────┤
│             │                      │                  │       │
│  ┌──────────▼─────────┐  ┌────────▼────────┐  ┌─────▼─────┐ │
│  │  Bluetooth Serial  │  │  WebCamTexture  │  │  ZXing    │ │
│  │  (Android Plugin)  │  │  (Unity Built-in)│ │  (Library)│ │
│  └────────────────────┘  └─────────────────┘  └───────────┘ │
│                                                              │
└──────────────────────────────────────────────────────────────┘
             │                      │                  │
             │                      │                  │
             ▼                      ▼                  ▼
    ┌────────────────┐    ┌────────────────┐  ┌──────────────┐
    │  STM32 控制板   │    │   Device Camera │  │  QR/Barcode  │
    │  (分药机硬件)   │    │   (药片拍摄)     │  │  (药盒验证)  │
    └────────────────┘    └────────────────┘  └──────────────┘
```

---

## 🔄 完整工作流程

### 用户操作流程

```
1. 启动应用
   ↓
2. 选择患者 (今日列表)
   ↓
3. 扫描药盒二维码验证
   ↓
4. 放入药盘
   ↓
5. 准备药品计数（与6同时进行）
   ├─ 启动摄像头
   ├─ 捕获背景
   ├─ 用户将指定的药片放到指定区域内
   └─ 实时计数验证
   ↓
6. 开始分药（与5同时进行）
   ├─ 生成4x7药片矩阵
   ├─ 设置舵机参数
   ├─ 发送分药命令
   └─ 实时进度监控
   ↓
7. 完成确认
   ├─ 更新配发记录，保存到服务器
   └─ 显示完成弹窗
```

### 数据流转

```
服务器处方数据
      ↓
PrescriptionManager
      ↓ (加载)
内存处方数据
      ↓ (生成)
4x7药片矩阵 (餐前/餐后)
      ↓
DispenserController
      ↓ (蓝牙)
STM32控制板
      ↓ (机械动作)
药片分发到药盒
      ↓ (光耦计数)
反馈 → DispenserController
      ↓ (更新)
UI进度显示
      ↓ (完成)
更新配发日期 → 服务器
```

---

## 💻 技术栈对比

### Python 原版 → Unity 新版

| 模块 | Python版 | Unity版 | 说明 |
|------|---------|---------|------|
| **GUI** | PySide6 (Qt) | Unity UGUI | ✅ 完全重构 |
| **串口通信** | PySerial | Android Bluetooth | ✅ 蓝牙替代 |
| **图像处理** | OpenCV-Python | OpenCV for Unity | ✅ API兼容 |
| **数据存储** 🔄 待实现 |
| **网络请求** 🔄 待实现 |
| **摄像头** ✅ Unity原生 |
| **扫码** ⏳ 待集成 |


## 🎯 下一步计划

### 阶段3：处方管理系统 (1-2周)

**目标**: 实现处方数据管理和药片矩阵生成


---

### 阶段4：主控制器 (1周)

**目标**: 协调所有模块，实现完整分药流程

**任务**:
1. 创建MainController.cs
   - 集成DispenserController
   - 集成CameraController
   - 集成PrescriptionManager

2. 实现状态机
   - 空闲、准备、数药、分药、完成

3. 错误处理
   - 计数错误恢复
   - 通信错误重连
   - 异常状态处理

**预计代码量**: 500-700行

---

### 阶段5：UI系统 (2-3周)

**目标**: 实现7个页面的用户界面

**任务**:
1. UI管理
   - UIManager.cs (页面切换)
   - 数据绑定
   - 事件处理

2. 美化优化
   - 响应式布局
   - 动画效果
   - 触摸优化

**预计代码量**: 1000-1500行

---

### 阶段6：二维码扫描 (1周)

**目标**: 实现药盒验证功能

---

### 阶段7：集成测试 (1-2周)

**目标**: 完整流程打通和优化

**任务**:
1. 端到端测试
2. 性能优化
3. 错误处理完善
4. 用户体验优化

---

### 阶段8：发布准备 (1周)

**目标**: 移动端打包和发布

**任务**:
1. Android打包配置
2. 权限配置
3. 性能测试
4. 用户文档

---

## 📱 移动端适配要点

### Android配置

```xml
<!-- AndroidManifest.xml -->
<uses-permission android:name="android.permission.CAMERA" />
<uses-permission android:name="android.permission.BLUETOOTH" />
<uses-permission android:name="android.permission.BLUETOOTH_CONNECT" />
<uses-permission android:name="android.permission.BLUETOOTH_SCAN" />
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.INTERNET" />
```

### Unity配置

```
Build Settings:
├─ Platform: Android
├─ Minimum API Level: 24 (Android 7.0)
├─ Target API Level: 33 (Android 13)
├─ Scripting Backend: IL2CPP
└─ Target Architectures: ARM64

Player Settings:
├─ Company Name: [您的公司]
├─ Product Name: EZ Dose
├─ Bundle Identifier: com.yourcompany.ezdose
└─ Version: 1.0.0
```

**创建日期**: 2025-11-29  
**当前版本**: 0.4.0  
**完成度**: 40%  
**预计完成时间**: 8-10周

**下一步**: 开始实现处方管理系统 → 数据模型创建 🚀
