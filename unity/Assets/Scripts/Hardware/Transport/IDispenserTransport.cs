using System.Collections.Generic;

namespace EZDose.Hardware
{
    public interface IDispenserTransport
    {
        bool IsConnected { get; }

        bool Connect(string connectionId);
        void Disconnect();
        bool Write(byte[] data);
        string Read();
        List<BluetoothDeviceInfo> DiscoverDevices();
    }
}
