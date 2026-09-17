using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using EZDose;

namespace EZDose.Hardware
{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    public class WindowsSerialTransport : IDispenserTransport
    {
        private readonly int baudRate;
        private readonly int readTimeoutMs;
        private readonly int writeTimeoutMs;
        private IntPtr serialHandle = IntPtr.Zero;

        public WindowsSerialTransport(int baudRate = 115200, int readTimeoutMs = 50, int writeTimeoutMs = 200)
        {
            this.baudRate = baudRate;
            this.readTimeoutMs = readTimeoutMs;
            this.writeTimeoutMs = writeTimeoutMs;
        }

        public bool IsConnected => serialHandle != IntPtr.Zero && serialHandle.ToInt64() != Win32.INVALID_HANDLE_VALUE;

        public bool Connect(string connectionId)
        {
            if (string.IsNullOrWhiteSpace(connectionId))
            {
                EZLog.E(EZLog.Module.Dispenser, "Serial port name is empty");
                return false;
            }

            Disconnect();

            string portName = NormalizePortName(connectionId);
            string devicePath = @"\\.\" + portName;

            serialHandle = Win32.CreateFile(
                devicePath,
                Win32.GENERIC_READ | Win32.GENERIC_WRITE,
                0,
                IntPtr.Zero,
                Win32.OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (!IsConnected)
            {
                EZLog.E(EZLog.Module.Dispenser, $"Failed to open serial port {portName}, Win32 error: {Marshal.GetLastWin32Error()}");
                serialHandle = IntPtr.Zero;
                return false;
            }

            if (!ConfigureSerialPort(portName))
            {
                Disconnect();
                return false;
            }

            Win32.EscapeCommFunction(serialHandle, Win32.CLRDTR);
            Win32.EscapeCommFunction(serialHandle, Win32.CLRRTS);
            Win32.PurgeComm(serialHandle, Win32.PURGE_RXCLEAR | Win32.PURGE_TXCLEAR);

            EZLog.I(EZLog.Module.Dispenser, $"Opened serial port {portName} at {baudRate} baud");
            return true;
        }

        public void Disconnect()
        {
            if (!IsConnected)
            {
                serialHandle = IntPtr.Zero;
                return;
            }

            try
            {
                Win32.CloseHandle(serialHandle);
            }
            catch (Exception e)
            {
                EZLog.W(EZLog.Module.Dispenser, $"Serial close failed: {e.Message}");
            }
            finally
            {
                serialHandle = IntPtr.Zero;
            }
        }

        public bool Write(byte[] data)
        {
            if (!IsConnected)
            {
                EZLog.E(EZLog.Module.Protocol, "Serial port is not connected");
                return false;
            }

            try
            {
                bool ok = Win32.WriteFile(serialHandle, data, (uint)data.Length, out uint bytesWritten, IntPtr.Zero);
                if (!ok || bytesWritten != data.Length)
                {
                    EZLog.E(EZLog.Module.Protocol, $"Serial write failed, written={bytesWritten}, error={Marshal.GetLastWin32Error()}");
                    return false;
                }

                Win32.FlushFileBuffers(serialHandle);
                EZLog.D(EZLog.Module.Protocol, $"Serial TX HEX: {BitConverter.ToString(data)}");
                return true;
            }
            catch (Exception e)
            {
                EZLog.E(EZLog.Module.Protocol, "Serial write failed", e);
                return false;
            }
        }

        public string Read()
        {
            if (!IsConnected)
            {
                return string.Empty;
            }

            try
            {
                if (!Win32.ClearCommError(serialHandle, out _, out Win32.COMSTAT status) || status.cbInQue == 0)
                {
                    return string.Empty;
                }

                int bytesToRead = (int)Math.Min(status.cbInQue, 4096u);
                byte[] buffer = new byte[bytesToRead];
                bool ok = Win32.ReadFile(serialHandle, buffer, bytesToRead, out uint bytesRead, IntPtr.Zero);

                if (!ok || bytesRead == 0)
                {
                    return string.Empty;
                }

                string data = Encoding.ASCII.GetString(buffer, 0, (int)bytesRead);
                EZLog.D(EZLog.Module.Protocol, $"Serial RX HEX: {BitConverter.ToString(buffer, 0, (int)bytesRead)} TXT: {data}");
                return data;
            }
            catch (Exception e)
            {
                EZLog.W(EZLog.Module.Protocol, $"Serial read failed: {e.Message}");
                return string.Empty;
            }
        }

        public List<BluetoothDeviceInfo> DiscoverDevices()
        {
            var devices = new List<BluetoothDeviceInfo>();

            for (int i = 1; i <= 256; i++)
            {
                string portName = "COM" + i;
                if (!PortExists(portName))
                {
                    continue;
                }

                devices.Add(new BluetoothDeviceInfo
                {
                    DeviceName = $"STM32 Dispenser ({portName})",
                    MacAddress = portName,
                    IsPaired = true,
                    SignalStrength = -1
                });
            }

            return devices;
        }

        private bool ConfigureSerialPort(string portName)
        {
            var dcb = new Win32.DCB();
            dcb.DCBlength = (uint)Marshal.SizeOf(typeof(Win32.DCB));

            dcb.BaudRate = (uint)baudRate;
            dcb.Flags = Win32.BINARY_FLAG |
                        (Win32.DTR_CONTROL_DISABLE << Win32.DTR_CONTROL_SHIFT) |
                        (Win32.RTS_CONTROL_DISABLE << Win32.RTS_CONTROL_SHIFT);
            dcb.ByteSize = 8;
            dcb.Parity = 0;
            dcb.StopBits = 0;

            if (!Win32.SetCommState(serialHandle, ref dcb))
            {
                EZLog.E(EZLog.Module.Dispenser, $"SetCommState failed for {portName}, Win32 error: {Marshal.GetLastWin32Error()}");
                return false;
            }

            var timeouts = new Win32.COMMTIMEOUTS
            {
                ReadIntervalTimeout = 1,
                ReadTotalTimeoutMultiplier = 0,
                ReadTotalTimeoutConstant = (uint)readTimeoutMs,
                WriteTotalTimeoutMultiplier = 0,
                WriteTotalTimeoutConstant = (uint)writeTimeoutMs
            };

            if (!Win32.SetCommTimeouts(serialHandle, ref timeouts))
            {
                EZLog.E(EZLog.Module.Dispenser, $"SetCommTimeouts failed for {portName}, Win32 error: {Marshal.GetLastWin32Error()}");
                return false;
            }

            return true;
        }

        private static bool PortExists(string portName)
        {
            var target = new StringBuilder(256);
            return Win32.QueryDosDevice(portName, target, target.Capacity) != 0;
        }

        private static string NormalizePortName(string connectionId)
        {
            string portName = connectionId.Trim();
            if (portName.StartsWith(@"\\.\", StringComparison.OrdinalIgnoreCase))
            {
                return portName.Substring(4);
            }

            return portName;
        }

        private static class Win32
        {
            public const uint GENERIC_READ = 0x80000000;
            public const uint GENERIC_WRITE = 0x40000000;
            public const uint OPEN_EXISTING = 3;
            public const int INVALID_HANDLE_VALUE = -1;
            public const int CLRDTR = 6;
            public const int CLRRTS = 4;
            public const uint PURGE_RXCLEAR = 0x0008;
            public const uint PURGE_TXCLEAR = 0x0004;
            public const uint BINARY_FLAG = 0x00000001;
            public const uint DTR_CONTROL_DISABLE = 0x00000000;
            public const uint RTS_CONTROL_DISABLE = 0x00000000;
            public const int DTR_CONTROL_SHIFT = 4;
            public const int RTS_CONTROL_SHIFT = 12;

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern IntPtr CreateFile(
                string lpFileName,
                uint dwDesiredAccess,
                uint dwShareMode,
                IntPtr lpSecurityAttributes,
                uint dwCreationDisposition,
                uint dwFlagsAndAttributes,
                IntPtr hTemplateFile);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool CloseHandle(IntPtr hObject);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool FlushFileBuffers(IntPtr hFile);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool ReadFile(
                IntPtr hFile,
                [Out] byte[] lpBuffer,
                int nNumberOfBytesToRead,
                out uint lpNumberOfBytesRead,
                IntPtr lpOverlapped);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool WriteFile(
                IntPtr hFile,
                byte[] lpBuffer,
                uint nNumberOfBytesToWrite,
                out uint lpNumberOfBytesWritten,
                IntPtr lpOverlapped);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool SetCommState(IntPtr hFile, ref DCB lpDCB);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool SetCommTimeouts(IntPtr hFile, ref COMMTIMEOUTS lpCommTimeouts);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool ClearCommError(IntPtr hFile, out uint lpErrors, out COMSTAT lpStat);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool EscapeCommFunction(IntPtr hFile, int dwFunc);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool PurgeComm(IntPtr hFile, uint dwFlags);

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

            [StructLayout(LayoutKind.Sequential)]
            public struct DCB
            {
                public uint DCBlength;
                public uint BaudRate;
                public uint Flags;
                public ushort wReserved;
                public ushort XonLim;
                public ushort XoffLim;
                public byte ByteSize;
                public byte Parity;
                public byte StopBits;
                public sbyte XonChar;
                public sbyte XoffChar;
                public sbyte ErrorChar;
                public sbyte EofChar;
                public sbyte EvtChar;
                public ushort wReserved1;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct COMMTIMEOUTS
            {
                public uint ReadIntervalTimeout;
                public uint ReadTotalTimeoutMultiplier;
                public uint ReadTotalTimeoutConstant;
                public uint WriteTotalTimeoutMultiplier;
                public uint WriteTotalTimeoutConstant;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct COMSTAT
            {
                public uint Flags;
                public uint cbInQue;
                public uint cbOutQue;
            }
        }
    }
#else
    public class WindowsSerialTransport : EditorMockTransport
    {
        public WindowsSerialTransport(int baudRate = 115200, int readTimeoutMs = 50, int writeTimeoutMs = 200)
        {
        }
    }
#endif
}
