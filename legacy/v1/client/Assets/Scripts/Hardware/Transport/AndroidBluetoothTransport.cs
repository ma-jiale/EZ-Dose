using System;
using System.Collections.Generic;
using UnityEngine;
using EZDose;

namespace EZDose.Hardware
{
#if UNITY_ANDROID && !UNITY_EDITOR
    public class AndroidBluetoothTransport : IDispenserTransport
    {
        private AndroidJavaObject bluetoothSerial;

        public bool IsConnected
        {
            get
            {
                try
                {
                    return bluetoothSerial != null && bluetoothSerial.Call<bool>("isConnected");
                }
                catch (Exception e)
                {
                    EZLog.W(EZLog.Module.Dispenser, $"Bluetooth isConnected failed: {e.Message}");
                    return false;
                }
            }
        }

        public bool Connect(string connectionId)
        {
            EnsureBluetoothSerial();

            if (!bluetoothSerial.Call<bool>("isBluetoothAvailable"))
            {
                EZLog.E(EZLog.Module.Dispenser, "Device does not support Bluetooth");
                return false;
            }

            if (!bluetoothSerial.Call<bool>("isBluetoothEnabled"))
            {
                EZLog.E(EZLog.Module.Dispenser, "Bluetooth is not enabled");
                return false;
            }

            return bluetoothSerial.Call<bool>("connect", connectionId);
        }

        public void Disconnect()
        {
            if (bluetoothSerial == null)
            {
                return;
            }

            bluetoothSerial.Call("disconnect");
        }

        public bool Write(byte[] data)
        {
            if (bluetoothSerial == null)
            {
                EZLog.E(EZLog.Module.Protocol, "bluetoothSerial is null");
                return false;
            }

            sbyte[] sdata = new sbyte[data.Length];
            Buffer.BlockCopy(data, 0, sdata, 0, data.Length);
            return bluetoothSerial.Call<bool>("writeBytes", sdata);
        }

        public string Read()
        {
            if (bluetoothSerial == null)
            {
                return string.Empty;
            }

            return bluetoothSerial.Call<string>("read");
        }

        public List<BluetoothDeviceInfo> DiscoverDevices()
        {
            EnsureBluetoothSerial();

            if (!bluetoothSerial.Call<bool>("isBluetoothAvailable"))
            {
                throw new InvalidOperationException("蓝牙不可用");
            }

            if (!bluetoothSerial.Call<bool>("isBluetoothEnabled"))
            {
                throw new InvalidOperationException("请先打开蓝牙");
            }

            string pairedDevicesJson = bluetoothSerial.Call<string>("getPairedDevices");
            EZLog.V(EZLog.Module.Protocol, $"Paired devices JSON: {pairedDevicesJson}");
            return ParsePairedDevicesJson(pairedDevicesJson);
        }

        private void EnsureBluetoothSerial()
        {
            if (bluetoothSerial != null)
            {
                return;
            }

            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            bluetoothSerial = new AndroidJavaObject("com.unity.bluetooth.BluetoothSerial", activity);
        }

        private List<BluetoothDeviceInfo> ParsePairedDevicesJson(string json)
        {
            var devices = new List<BluetoothDeviceInfo>();

            if (string.IsNullOrEmpty(json) || json == "[]")
            {
                return devices;
            }

            try
            {
                json = json.Trim();
                if (json.StartsWith("[")) json = json.Substring(1);
                if (json.EndsWith("]")) json = json.Substring(0, json.Length - 1);

                string[] deviceStrings = json.Split(new string[] { "},{" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string deviceStr in deviceStrings)
                {
                    string cleanDeviceStr = deviceStr.Trim();
                    if (cleanDeviceStr.StartsWith("{")) cleanDeviceStr = cleanDeviceStr.Substring(1);
                    if (cleanDeviceStr.EndsWith("}")) cleanDeviceStr = cleanDeviceStr.Substring(0, cleanDeviceStr.Length - 1);

                    string name = ExtractJsonValue(cleanDeviceStr, "name");
                    string address = ExtractJsonValue(cleanDeviceStr, "address");

                    if (!string.IsNullOrEmpty(address))
                    {
                        devices.Add(new BluetoothDeviceInfo
                        {
                            DeviceName = string.IsNullOrEmpty(name) ? "智慧分药机" : name,
                            MacAddress = address,
                            IsPaired = true,
                            SignalStrength = -1
                        });
                    }
                }
            }
            catch (Exception e)
            {
                EZLog.E(EZLog.Module.Dispenser, "Failed to parse paired devices JSON", e);
            }

            return devices;
        }

        private string ExtractJsonValue(string json, string key)
        {
            string searchKey = "\"" + key + "\":\"";
            int startIndex = json.IndexOf(searchKey);
            if (startIndex == -1) return string.Empty;

            startIndex += searchKey.Length;
            int endIndex = json.IndexOf("\"", startIndex);
            if (endIndex == -1) return string.Empty;

            return json.Substring(startIndex, endIndex - startIndex);
        }
    }
#endif
}
