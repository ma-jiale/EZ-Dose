# STM32 现行协议：V1 源码事实

事实来源：`legacy/v1/client-unity/Assets/Scripts/SerialProtocol.cs`、`DispenserController.cs`、`PrescriptionManager.cs`。
Windows COM 为主线，115200 / 8N1；Android HC-06 为兼容传输。

命令帧：AA BB + command + data + checksum_low + checksum_high。
代码虽称 CRC，实际为 command 与 data 字节的 16 位累加和，不含 AA BB；小端序。
float 与 uint32 数据均为小端序。反馈是文本，解析与二进制发送不可混淆。

| 命令 | 值 |
| --- | --- |
| SKIP_TASK | 0x00 |
| RESET_DISPENSER | 0x01 |
| CLEAN_PILLS | 0x02 |
| OPEN_TRAY / CLOSE_TRAY | 0x03 / 0x04 |
| SEND_PILL_MATRIX | 0x05 |
| SET_OPTOCOUPLER_THRESH / NORESP | 0x06 / 0x07 |
| SET_MOTOR_SPEED | 0x08 |
| SET_MOTOR_DELAY_STOP | 0x09 |
| ACK | 0x0A |
| SET_CLEAN_SPEED / DELAY | 0x0B / 0x0C |

例如 OPEN_TRAY 无数据帧：AA BB 03 03 00。
反馈包括 ACK、DONE、machine init、machine_state:FINISH、machine_state:CNT_ERR、
pills out:N、cleaned pills:N、UID:十六进制值、NO CARD，以及 number:N,width:W 脉冲信息。
ACK 不等于动作完成。具体命令 payload、超时/重试与完成条件移植前必须逐一从调用点提取测试向量。

矩阵固定 4×7，单次最多 7 天；row0=晚、row1=午、row2=早、row3=备用。
不得把备用行自动映射成睡前，也不得把 UI 的周一至周日顺序直接当作传输顺序。
处方成功回写日期必须基于本次实际完成天数；跳过与错误不能被当作成功。
当前光耦最小有效脉宽为 3、最大为 200；参数优化使用中位数与默认 7 个样本门槛。

Dart 迁移验收：逐字节比较 V1 编码、串口分包/粘包、重复与未知反馈、超时、断线、RFID/条码来源隔离。
此文档不是完整固件规范，固件版本、物理行为与暂停/恢复语义仍需实机确认。
