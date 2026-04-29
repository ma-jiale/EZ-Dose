# EZ-Dose 项目接手要点

## 项目定位
EZ-Dose 是智能分药系统。本仓库当前主线是 Unity Android 平板端，负责扫码、生成分药矩阵、蓝牙控制 STM32 分药机、药片计数/校准、回写分药结果。

## 当前事实
- Unity 版本：Unity 6.3，项目实际为 `6000.3.2f1`。
- 后端不在本仓库，位于独立仓库 `EZ_Dose_server`。
- 本仓库里的 `99_archive/server` 只是历史参考，不是当前后端。
- 默认服务端地址 `http://127.0.0.1:5000` 不要擅自修改；Android 实机使用时由设置页配置局域网后端地址。
- 当前物理药盘顺序：从上到下(对应分药机是从内到外)是 `午 / 晚 / 早`的顺序。
- 当前矩阵映射：`row0=早`, `row1=晚`, `row2=午`, `row3=备用`。

## 主要目录
- `unity/`：当前 Unity Android 客户端主工程。
- `hardware/`：HC-06 蓝牙模块配置脚本。
- `images/`, `docs/`：文档图片和说明资料。
- `99_archive/`：历史 Python GUI、旧 server、旧 Android app，除非明确要求，不应作为主线修改。

## Unity 关键代码
- `unity/Assets/Scripts/MainController.cs`
  - 主流程协调器：患者刷新、分药计划、分药循环、换盘、跳过、错误恢复、校准触发。
- `unity/Assets/Scripts/PrescriptionManager.cs`
  - 拉取 `/packer/prescriptions`，计算是否需要分药，生成 4x7 分药矩阵，回写处方状态。
- `unity/Assets/Scripts/DispenserController.cs`
  - 蓝牙连接、ACK 重试、发送 STM32 命令、解析硬件反馈。
- `unity/Assets/Scripts/SerialProtocol.cs`
  - STM32 协议封包：`0xAA 0xBB + command + data + checksum`。
- `unity/Assets/Scripts/CheckPillBoxController.cs`
  - ZXing 扫码，校验 Patient ID。
- `unity/Assets/Scripts/PillCounter.cs`
  - OpenCV 药片计数算法，目前处于弃用状态
- `unity/Assets/Scripts/PillCalibrationManager.cs`
  - 药片面积校准，以及根据药片大小计算电机/舵机参数。
- `unity/Assets/Scripts/UIManager.cs`
  - Home / Scan / Dispense 场景 UI 绑定与流程跳转。

## 依赖
- OpenCVForUnity 已在 `unity/Assets/OpenCVForUnity`。
- ZXing DLL 在 `unity/Assets/Plugins/Zxing`。
- Android 蓝牙串口插件在 `unity/Assets/Plugins/Android/bluetooth-serial.aar`。

## 注意事项
- 不要假设根目录有当前 server。
- 不要把 archive 里的旧 API 当成当前后端真相；以后端仓库为准。
- 涉及分药矩阵时必须小心行顺序，业务时间和物理位置不是直觉顺序。
- 修改代码前先检查 git 状态，不要覆盖用户已有改动。
- 每次修改后向用户解释修改的内容和逻辑
- 每次修改后如果有需要后续Agent接手注意的地方请写入此文档

