<p align="center">
  <img src="images/MainPhoto.jpg" alt="EZ-Dose 智能分药系统" width="600"/>
</p>

<h1 align="center">EZ-Dose 智能分药系统</h1>

<p align="center">
  <b>面向康养机构的智能药物管理系统</b><br>
  <sub>处方管理 · 智能分药 · 全程可追溯</sub>
</p>

<p align="center">
  <a href="./README_EN.md">English</a> | 中文
</p>

---

## 项目简介

EZ-Dose 是一个面向康养机构的**智能药物管理系统**，为搭载 STM32 的自动分药机设计并实现了一套完整的"**处方管理—分药控制—操作记录**"多端交互系统。

### 核心价值

> 在康养机构分药场景中，通过软硬件协同的交互系统设计，将原本依赖护理人员记忆、计算和体力操作的分药流程，转化为由**系统主导、护理人员监督**的协作流程，从而**降低认知负担**、**减少体力劳动**，并实现**全过程可追溯**的用药管理。

### 系统组成

| 组件 | 描述 | 技术栈 |
|------|------|--------|
| 🌐 **处方管理网站** | 医护人员管理患者信息、录入处方、查看分药记录 | Flask + SQLite |
| 📱 **分药控制APP** | 连接分药机、扫码识别、控制分药流程 | Unity + Android |
| 🔧 **硬件接口** | 蓝牙模块配置工具 | Python + Serial |

---

## 系统架构

EZ-Dose 系统整体架构图

![系统架构图](images/系统架构图.png)

---

## 功能特性

- ✅ **患者管理** - 管理患者基本信息，支持药盒标签打印
- ✅ **处方管理** - 录入和管理患者处方，支持多时段用药
- ✅ **智能分药** - APP 引导分药流程，自动控制分药机
- ✅ **药片校准** - 首次使用新药品时自动采集尺寸和图像数据
- ✅ **条码识别** - 扫描药盒标签自动匹配患者信息
- ✅ **分药记录** - 完整记录每次分药操作，支持追溯查询
- ✅ **权限管理** - 支持不同角色（医生、护士、管理员）的权限配置

---

## 🚀 快速开始

### 前置要求

- Python 3.7+（用于硬件配置脚本；后端请参见 `EZ_Dose_server` 仓库）
- Unity 6.3（项目当前使用 Unity 6000.3.2f1）
- 运行 HarmonyOS 4（基于 Android）的华为 MatePad
- HC-06 蓝牙串口模块
- 搭载 STM32 的自动分药机

### 1️.克隆仓库

```bash
git clone https://github.com/your-username/EZ-Dose.git
cd EZ-Dose
```

### 2️.配置蓝牙模块

由于自动分药机搭载的 STM32 单片机没有蓝牙功能，需要加装 HC-06 蓝牙串口模块。

HC-06 蓝牙串口模块

<img src="images/image-20260204123339711.png" alt="HC-06蓝牙模块" width="300"/>

#### 修改波特率

STM32 串口通信波特率是 115200，但 HC-06 默认波特率是 9600，需要修改为一致：

```bash
# 安装依赖
pip install pyserial

# 列出可用串口
python hardware/hc06_baudrate_configurator.py --list

# 修改波特率（将 COM6 替换为您的实际端口）
python hardware/hc06_baudrate_configurator.py --port COM6 --current-baud 9600 --target-baud 115200
```

#### 修改蓝牙名称（可选）

```bash
python hardware/hc06_name_configurator.py --port COM6 --name "PillDispenserXX"
```

#### 电压转换电路

由于 HC-06 的高电平是 3.3V，STM32 的高电平是 5V，需要使用 1kΩ 和 2kΩ 电阻制作分压电路：

分压电路原理图

<img src="images/image-20260204123442739.png" alt="电路原理图" width="300"/>

实物接线图

<img src="images/2f036d90c4fdd007d465ec7600c208fd.jpg" alt="实物接线图" width="400"/>

#### 引脚连接

根据下图连接控制板和蓝牙模块的对应引脚：

引脚连接图

<img src="images/image-20260204123818392.png" alt="引脚连接图" width="400"/>

> **✅ 验证**：上电后蓝牙模块指示灯持续闪烁表示连接成功

### 3️.准备后端服务器

```bash
# 后端代码位于独立仓库
git clone https://github.com/ma-jiale/EZ_Dose_server.git
cd EZ_Dose_server
```

请按 `EZ_Dose_server` 仓库 README 启动 Flask 后端，并在 APP 设置页填写对应服务器地址。
Unity 客户端默认服务器地址保留为 `http://127.0.0.1:5000`，实际部署到 Android 平板时通常需要改为实际服务器 IP。

#### 公网访问（可选）

如需从外网访问，可以使用 Nginx 反向代理或内网穿透工具（如 ngrok、frp）。后端的 URL 前缀、端口和部署方式以 `EZ_Dose_server` 仓库配置为准：

```python
# 远程部署时取消下面注释
URL_PREFIX = '/flask'
```

### 4️.编译分药控制 APP

1. 使用 Unity Hub 打开 `unity` 目录
2. 确认 `Assets/OpenCVForUnity`、`Assets/Plugins/Zxing` 和 `Assets/Plugins/Android/bluetooth-serial.aar` 已存在
3. 选择 **File → Build Settings**，切换平台到 **Android**
4. 点击 **Build And Run** 编译并安装到 MatePad

> **⚠️ 注意**：安装后需要在系统设置中手动开启 APP 的**附近设备访问权限**。

---

## 📖 使用说明

### 使用流程概览

EZ-Dose 系统使用流程图

![使用流程图](images/使用流程图.png)

---

### 处方管理网站

#### 登录系统

运行后端服务器后访问对应登录地址，例如 `http://服务器地址/login`；端口和 URL 前缀以 `EZ_Dose_server` 的部署配置为准。

登录界面

![登录界面](images/web_login_page.png)

> **默认管理员账户**  
> 用户名：`admin`  
> 密码：`admin123`  
> 首次登录后请及时修改密码

#### 主页功能

主页包含四大功能板块：**用户管理**、**患者管理**、**处方管理**和**系统记录查询**

主页界面

![主页](images/image-20260204134632640.png)

#### 用户管理

新增用户和修改已有用户信息，可为不同角色（医生、护士、院长等）设置不同权限

用户管理界面

![用户管理](images/image-20260204134852692.png)

用户信息表单

![用户表单](images/image-20260204134937663.png)

#### 患者管理

新增患者和修改患者信息，支持打印患者药盒标签

患者管理界面

![患者管理](images/image-20260204135040150.png)

> **📌 标签打印说明**  
> - 仅支持 Windows 系统
> - 需安装 `print_service` 目录下的精臣打印服务 SDK
> - 使用 USB 连接精臣标签打印机
> - 使用 50×20mm 白色标签纸

#### 处方管理

新增和修改患者处方信息

处方管理界面

![处方管理](images/image-20260204135714765.png)

处方表单

![处方表单](images/image-20260204135844987.png)

#### 系统记录

查询分药记录和系统操作记录（新增、修改、删除患者/用户/处方等）

分药记录查询

![分药记录](images/image-20260204140012335.png)

操作记录查询

![操作记录](images/image-20260204140045611.png)

![操作详情](images/image-20260204140048375.png)

---

### 📱 分药控制 APP

#### 分药前准备

1. 准备贴好患者标签的药盒
2. 插上自动分药机电源并开机
3. 将 MatePad 放在分药机数药支架上
4. 在系统蓝牙设置中配对名为 `PillDispenserXX` 的设备
5. 打开分药控制 APP

分药机设备

<img src="images/7e6906dbcc70b91b3ee85b1d391a2694.jpg" alt="分药机设备" width="500"/>

APP 图标

![APP图标](images/Screenshot_20260203_215126_com.huawei.android.launcher.jpg)

#### 主页预览

医生录入处方后，可在首页看到对应的患者处方卡片

APP 首页

![首页](images/Screenshot_20260203_215159_com.HyggeLab.EasyDosePRO.jpg)

> **💡 提示**：如果看不到处方卡片，请在设置中检查服务器 URL 等参数是否正确

#### 连接分药机

点击右上角按钮连接分药机

连接分药机

![连接分药机](images/Screenshot_20260203_215237_com.HyggeLab.EasyDosePRO.jpg)

连接成功界面

![连接成功](images/Screenshot_20260203_215315_com.HyggeLab.EasyDosePRO-1770185443794.jpg)

> **⚠️ 连接问题排查**  
> - 检查 APP 的"附近设备访问"权限
> - 确认蓝牙已配对 `PillDispenserXX` 设备

#### 扫码识别

点击患者卡片进入扫码界面，将药盒标签放置在分药机平台上进行扫描

扫码界面

![扫码界面](images/Screenshot_20260203_215331_com.HyggeLab.EasyDosePRO-1770185598555.jpg)

药盒扫描示意

![扫描药盒](images/be9308cbb0003604f9ff1e2693a01111.jpg)

#### 放置药盒

识别成功后分药机轨道会弹出，提醒放入药盒

放置药盒提示

![放置药盒](images/Screenshot_20260203_215421_com.HyggeLab.EasyDosePRO.jpg)

> **⚠️ 重要**：一定要确认药盒放置到位后再按下确认按钮！

药盒正确放置示意

<img src="images/50093bd8e75b28cdd509f50c75b8cd63.jpg" alt="药盒放置" width="400"/>

#### 药片校准（首次）

如果是新药物，APP 会要求放置一粒药片进行尺寸和图像数据记录

校准界面

![校准要求](images/Screenshot_20260203_215738_com.HyggeLab.EasyDosePRO.jpg)

![校准完成](images/Screenshot_20260203_215746_com.HyggeLab.EasyDosePRO.jpg)

#### 分药流程

根据界面信息提醒，向分药机漏斗处放入对应药品相应数量的药片

分药界面

![分药界面](images/Screenshot_20260203_215840_com.HyggeLab.EasyDosePRO.jpg)

放入药物

<img src="images/6f9e23188ba9da904b9bf3d2f3b216cb.jpg" alt="放入药物" width="400"/>

> **💡 多余药片处理**：如果放置数量超过实际需要，剩余药片会自动流入分药机回收口

药片回收口

<img src="images/358995d5aca0cd1cb43a9f66091b648f.jpg" alt="回收口" width="400"/>

#### 错误处理

如果分药机出现摆药错误，会弹出错误提示弹窗，轨道自动退出，需要手动纠正后按确认键继续

错误提示

![错误提示](images/Screenshot_20260203_215934_com.HyggeLab.EasyDosePRO.jpg)

#### 完成分药

所有药物分发完成后，界面弹出提示，轨道退出，取走药盒后按确认键收回轨道

完成提示

![完成分药](images/Screenshot_20260203_220115_com.HyggeLab.EasyDosePRO.jpg)

取走药盒

<img src="images/52864ccd15def351c3610b6b5a1fd415.jpg" alt="取药盒" width="400"/>

分药流程完成

![流程完成](images/Screenshot_20260203_220148_com.HyggeLab.EasyDosePRO.jpg)

---

## 项目结构

```
EZ-Dose/
├── 📂 99_archive/          # 历史版本与旧实验实现（旧 server / GUI / Android app）
├── 📂 unity/               # Unity 分药控制 APP
│   ├── Assets/             # Unity 资源文件
│   ├── Packages/           # 依赖包
│   └── ProjectSettings/    # 项目设置
├── 📂 hardware/            # 硬件配置工具
│   ├── hc06_baudrate_configurator.py   # 波特率配置
│   └── hc06_name_configurator.py       # 蓝牙名称配置
├── 📂 images/              # 文档图片资源
└── 📂 docs/                # 项目文档
```

> 后端服务器不在本仓库维护，当前请使用独立仓库 `EZ_Dose_server`。本仓库中的旧后端实现仅保留在 `99_archive/server` 作为历史参考。

---

## 故障排除

| 问题 | 解决方案 |
|------|----------|
| 蓝牙连接失败 | 检查 HC-06 波特率是否为 115200，确认蓝牙已配对 |
| APP 无法发现设备 | 开启"附近设备访问"权限，确保蓝牙处于可发现状态 |
| 服务器启动失败 | 请在 `EZ_Dose_server` 仓库中检查端口、依赖和启动日志 |
| 数据库锁定 | 关闭其他访问数据库的进程 |
| 标签打印失败 | 确认打印机已通过 USB 连接，SDK 已正确安装 |
| 处方卡片不显示 | 检查 APP 设置中的服务器 URL 是否正确 |

---

## 许可证

本项目采用 [MIT License](./LICENSE) 开源许可证。

---

## 联系方式

如有问题或建议，欢迎提交 [Issue](https://github.com/ma-jiale/EZ-Dose/issues)！

---

<p align="center">
  <sub>Made by Jiale Ma</sub>
</p>

