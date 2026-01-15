using System;
using System.Collections.Generic;

namespace EZDose.Hardware
{
    /// <summary>
    /// 串口通信协议类 - 定义与STM32控制板通信的命令和数据格式
    /// </summary>
    public static class SerialProtocol
    {
        // 命令定义
        public static class Commands
        {
            public const byte RESET_DISPENSER = 0x00;       // 摆锤零位设置
            public const byte PAUSE_DISPENSER = 0x01;       // 暂停分药
            public const byte OPEN_TRAY = 0x03;             // 打开舱门
            public const byte CLOSE_TRAY = 0x04;            // 关闭舱门
            public const byte SEND_PILL_MATRIX = 0x05;      // 发送药片矩阵
            public const byte SET_OPTOCOUPLER_THRESH = 0x06; // 设置光耦阈值
            public const byte SET_OPTOCOUPLER_NORESP = 0x07; // 设置光耦不响应期
            public const byte SET_MOTOR_SPEED = 0x08;        // 设置电机转速/舵机角度
            public const byte SET_MOTOR_DELAY_STOP = 0x09;   // 设置电机刹车延迟
            public const byte ACK = 0x0A;                    // 确认信号
            public const byte SET_CLEAN_SPEED = 0x0B;        // 设置清洁速度
            public const byte SET_CLEAN_DELAY = 0x0C;        // 设置清洁延迟
        }

        // 设备ID定义
        public static class DeviceID
        {
            public const byte TURNTABLE_MOTOR = 0x00;       // 转盘电机
            public const byte SERVO_MOTOR = 0x01;           // 舵机（药物入口控制）
            public const byte UPPER_OPTOCOUPLER = 0x00;     // 上光耦
            public const byte LOWER_OPTOCOUPLER = 0x01;     // 下光耦
        }

        // 包头定义
        private static readonly byte[] PACKAGE_HEADER = new byte[] { 0xAA, 0xBB };

        /// <summary>
        /// 构建完整的数据包：包头 + 命令 + 数据 + CRC校验
        /// </summary>
        public static byte[] BuildPackage(byte command, byte[] data = null)
        {
            List<byte> package = new List<byte>();
            
            // 添加包头
            package.AddRange(PACKAGE_HEADER);
            
            // 添加命令
            package.Add(command);
            
            // 添加数据
            if (data != null && data.Length > 0)
            {
                package.AddRange(data);
            }
            
            // 计算CRC（从命令开始）
            byte[] payload = new byte[1 + (data?.Length ?? 0)];
            payload[0] = command;
            if (data != null && data.Length > 0)
            {
                Array.Copy(data, 0, payload, 1, data.Length);
            }
            
            ushort crc = CalculateCRC(payload);
            
            // 添加CRC（小端序）
            package.Add((byte)(crc & 0xFF));
            package.Add((byte)((crc >> 8) & 0xFF));
            
            return package.ToArray();
        }

        /// <summary>
        /// 计算CRC校验值（简单累加和）
        /// </summary>
        private static ushort CalculateCRC(byte[] data)
        {
            int sum = 0;
            foreach (byte b in data)
            {
                sum += b;
            }
            return (ushort)(sum & 0xFFFF);
        }

        /// <summary>
        /// 将float转换为字节数组（小端序）
        /// </summary>
        public static byte[] FloatToBytes(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        /// <summary>
        /// 将uint32转换为字节数组（小端序）
        /// </summary>
        public static byte[] UInt32ToBytes(uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }
            return bytes;
        }

        /// <summary>
        /// 解析反馈消息
        /// </summary>
        public static class FeedbackParser
        {
            public const string MACHINE_INIT = "machine init";
            public const string MACHINE_STATE_FINISH = "machine_state:FINISH";
            public const string MACHINE_STATE_CNT_ERR = "machine_state:CNT_ERR";
            public const string PILLS_OUT_PREFIX = "pills out:";
            public const string ACK_MSG = "ACK";
            public const string DONE_MSG = "DONE";

            public static FeedbackMessage Parse(string message)
            {
                if (string.IsNullOrEmpty(message))
                    return null;

                message = message.Trim();

                if (message == MACHINE_INIT)
                    return new FeedbackMessage { Type = FeedbackType.MachineInit };

                if (message == MACHINE_STATE_FINISH)
                    return new FeedbackMessage { Type = FeedbackType.StateFinish };

                if (message == MACHINE_STATE_CNT_ERR)
                    return new FeedbackMessage { Type = FeedbackType.StateCountError };

                if (message.StartsWith(PILLS_OUT_PREFIX))
                {
                    string countStr = message.Substring(PILLS_OUT_PREFIX.Length).Trim();
                    if (int.TryParse(countStr, out int count))
                    {
                        return new FeedbackMessage
                        {
                            Type = FeedbackType.PillsOut,
                            PillCount = count
                        };
                    }
                }

                if (message == ACK_MSG)
                    return new FeedbackMessage { Type = FeedbackType.ACK };

                if (message == DONE_MSG)
                    return new FeedbackMessage { Type = FeedbackType.DONE };

                return new FeedbackMessage
                {
                    Type = FeedbackType.Unknown,
                    RawMessage = message
                };
            }
        }
    }

    /// <summary>
    /// 反馈消息类型
    /// </summary>
    public enum FeedbackType
    {
        Unknown,
        MachineInit,
        StateFinish,
        StateCountError,
        PillsOut,
        ACK,
        DONE
    }

    /// <summary>
    /// 反馈消息结构
    /// </summary>
    public class FeedbackMessage
    {
        public FeedbackType Type { get; set; }
        public int PillCount { get; set; }
        public string RawMessage { get; set; }
    }
}
