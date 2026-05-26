using System.Collections.Generic;
using EZDose;

namespace EZDose.Hardware
{
    public class EditorMockTransport : IDispenserTransport
    {
        public bool IsConnected { get; private set; }

        public bool Connect(string connectionId)
        {
            IsConnected = true;
            EZLog.I(EZLog.Module.Dispenser, $"Mock transport connected: {connectionId}");
            return true;
        }

        public void Disconnect()
        {
            IsConnected = false;
            EZLog.I(EZLog.Module.Dispenser, "Mock transport disconnected");
        }

        public bool Write(byte[] data)
        {
            EZLog.D(EZLog.Module.Protocol, $"Mock transport send: {System.BitConverter.ToString(data)}");
            return IsConnected;
        }

        public string Read()
        {
            return string.Empty;
        }

        public List<BluetoothDeviceInfo> DiscoverDevices()
        {
            return new List<BluetoothDeviceInfo>
            {
                new BluetoothDeviceInfo
                {
                    DeviceName = "Mock Dispenser",
                    MacAddress = "MOCK-01",
                    IsPaired = true,
                    SignalStrength = -1
                }
            };
        }
    }
}
