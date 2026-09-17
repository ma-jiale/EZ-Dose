using System;

namespace EZDose.Hardware
{
    /// <summary>
    /// Represents a discovered Bluetooth device
    /// Used for device discovery and connection management
    /// </summary>
    [Serializable]
    public class BluetoothDeviceInfo
    {
        /// <summary>
        /// Display name of the Bluetooth device
        /// Default: "智慧分药机" (Smart Dispenser)
        /// </summary>
        public string DeviceName;

        /// <summary>
        /// MAC address of the Bluetooth device (e.g., "AA:BB:CC:DD:EE:FF")
        /// </summary>
        public string MacAddress;

        /// <summary>
        /// Whether this device is already paired with the Android device
        /// </summary>
        public bool IsPaired;

        /// <summary>
        /// Signal strength in dBm (negative value, closer to 0 = stronger signal)
        /// -1 means not available or not measured
        /// Typical range: -30 (excellent) to -100 (very weak)
        /// </summary>
        public int SignalStrength;

        /// <summary>
        /// Whether this device is currently connected
        /// </summary>
        public bool IsConnected;

        /// <summary>
        /// Get signal strength as a percentage (0-100)
        /// </summary>
        public int GetSignalStrengthPercent()
        {
            if (SignalStrength == -1 || SignalStrength < -100)
            {
                return 0;
            }

            // Convert dBm to percentage
            // -30 dBm = 100%, -100 dBm = 0%
            int percent = 100 + (SignalStrength + 30) * 100 / 70;
            return UnityEngine.Mathf.Clamp(percent, 0, 100);
        }

        /// <summary>
        /// Get signal quality description
        /// </summary>
        public string GetSignalQuality()
        {
            if (SignalStrength == -1)
            {
                return "Unknown";
            }

            if (SignalStrength >= -50)
            {
                return "Excellent";
            }
            else if (SignalStrength >= -70)
            {
                return "Good";
            }
            else if (SignalStrength >= -85)
            {
                return "Fair";
            }
            else
            {
                return "Weak";
            }
        }

        /// <summary>
        /// Create a copy of this device info
        /// </summary>
        public BluetoothDeviceInfo Clone()
        {
            return new BluetoothDeviceInfo
            {
                DeviceName = this.DeviceName,
                MacAddress = this.MacAddress,
                IsPaired = this.IsPaired,
                SignalStrength = this.SignalStrength,
                IsConnected = this.IsConnected
            };
        }
    }
}
