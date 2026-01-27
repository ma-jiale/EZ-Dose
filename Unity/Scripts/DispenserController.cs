using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace EZDose.Hardware
{
    /// <summary>
    /// Dispenser controller - Communicates with STM32 via Bluetooth serial port
    /// Controls pill dispensing, receives feedback, manages machine state
    /// </summary>
    public class DispenserController : MonoBehaviour
    {
        [Header("蓝牙配置")]
        [SerializeField] private string deviceMacAddress = "00:00:00:00:00:00";
        [Header("分药机配置")]
        [SerializeField] private int maxRetryCount = 5;
        [SerializeField] private float ackTimeout = 0.2f;
        [SerializeField] private float resetDoneTimeout = 10f;

        // 蓝牙通信对象
        private AndroidJavaObject bluetoothSerial;
        
        // Currently connected device info
        private BluetoothDeviceInfo connectedDevice;
        private List<BluetoothDeviceInfo> discoveredDevices = new List<BluetoothDeviceInfo>();
        
        // 状态变量
        private bool isConnected = false;
        private bool isTrayOpened = false;
        private bool isReceiving = false;
        private bool isSendingPackage = false;
        
        // 机器状态 0:空闲 1:工作中 2:暂停 3:完成
        private int machineState = 0;
        
        // 错误代码 0:正常 1:超时 2:计数错误
        private int errorCode = 0;
        
        // 药片计数
        private int pillRemain = -1;
        private int totalPills = 0;
        
        // 反馈标志
        private bool ackReceived = false;
        private bool doneReceived = false;
        
        // 接收缓冲区
        private StringBuilder receiveBuffer = new StringBuilder();
        
        // events
        public event Action OnMachineInit; // unused
        public event Action OnDispensingComplete;
        public event Action OnCountError;
        public event Action<string> OnBTError; 
        public event Action<string> OnError; 
        public event Action<int> OnPillCountUpdate;

        
        // Bluetooth device discovery events
        public event Action<List<BluetoothDeviceInfo>> OnDevicesFound;
        public event Action OnDiscoveryStarted;
        public event Action OnDiscoveryCompleted;
        public event Action<string> OnConnectionStateChanged; // Connected, Disconnected, Connecting

        #region 初始化和连接

        private void Awake()
        {
            // Keep dispenser alive across scenes (like MainController)
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Initialize dispenser controller and connect to Bluetooth device
        /// </summary>
        public bool Initialize(string macAddress = null)
        {
            if (!string.IsNullOrEmpty(macAddress))
            {
                deviceMacAddress = macAddress;
            }

            Debug.Log($"[DispenserController] Initialization started, MAC address: {deviceMacAddress}");

            try
            {
                if (!ConnectBluetooth())
                {
                    Debug.LogError("[DispenserController] Bluetooth connection failed");
                    return false;
                }

                StartReceiving();
                Debug.Log("[DispenserController] Initialization succeeded");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[DispenserController] Initialization exception: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 连接蓝牙设备
        /// </summary>
        private bool ConnectBluetooth()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (bluetoothSerial == null)
                {
                    AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    bluetoothSerial = new AndroidJavaObject("com.unity.bluetooth.BluetoothSerial", activity);
                }

                bool isAvailable = bluetoothSerial.Call<bool>("isBluetoothAvailable");
                if (!isAvailable)
                {
                    Debug.LogError("[DispenserController] Device does not support Bluetooth");
                    return false;
                }

                bool isEnabled = bluetoothSerial.Call<bool>("isBluetoothEnabled");
                if (!isEnabled)
                {
                    Debug.LogError("[DispenserController] Bluetooth is not enabled");
                    return false;
                }

                bool connected = bluetoothSerial.Call<bool>("connect", deviceMacAddress);
                if (connected)
                {
                    isConnected = true;
                    Debug.Log($"[DispenserController] Connected to device: {deviceMacAddress}");
                    return true;
                }
                else
                {
                    Debug.LogError($"[DispenserController] Connection failed: {deviceMacAddress}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[DispenserController] Bluetooth connection exception: {e.Message}");
                return false;
            }
#else
            // 编辑器模式模拟连接
            Debug.Log("[DispenserController] Editor mode - Simulated connection succeeded");
            isConnected = true;
            return true;
#endif
        }

        /// <summary>
        /// 断开连接
        /// </summary>
        public void Disconnect()
        {
            isReceiving = false;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (bluetoothSerial != null && isConnected)
                {
                    bluetoothSerial.Call("disconnect");
                    Debug.Log("[DispenserController] Bluetooth disconnected");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[DispenserController] Disconnect exception: {e.Message}");
            }
#endif

            isConnected = false;
            ResetState();
        }

        /// <summary>
        /// 重置所有状态
        /// </summary>
        private void ResetState()
        {
            machineState = 0;
            errorCode = 0;
            pillRemain = -1;
            totalPills = 0;
            ackReceived = false;
            doneReceived = false;
            isSendingPackage = false;
            receiveBuffer.Clear();
        }

        #endregion

        #region Device Discovery

        /// <summary>
        /// Start scanning for nearby Bluetooth devices
        /// </summary>
        public void StartDeviceDiscovery()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (bluetoothSerial == null)
                {
                    AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    bluetoothSerial = new AndroidJavaObject("com.unity.bluetooth.BluetoothSerial", activity);
                }

                bool isAvailable = bluetoothSerial.Call<bool>("isBluetoothAvailable");
                if (!isAvailable)
                {
                    Debug.LogError("[DispenserController] Bluetooth not available on this device");
                    OnBTError?.Invoke("蓝牙不可用");
                    return;
                }

                bool isEnabled = bluetoothSerial.Call<bool>("isBluetoothEnabled");
                if (!isEnabled)
                {
                    Debug.LogError("[DispenserController] Bluetooth is not enabled");
                    OnBTError?.Invoke("请先打开蓝牙");
                    return;
                }

                discoveredDevices.Clear();
                OnDiscoveryStarted?.Invoke();
                Debug.Log("[DispenserController] Starting device discovery...");

                StartCoroutine(DiscoveryCoroutine());
            }
            catch (Exception e)
            {
                Debug.LogError($"[DispenserController] Discovery error: {e.Message}");
                OnBTError?.Invoke($"Discovery failed: {e.Message}");
            }
#else
            // Editor mode - simulate discovery
            Debug.Log("[DispenserController] Editor mode - Simulating device discovery");
            discoveredDevices.Clear();
            OnDiscoveryStarted?.Invoke();
            StartCoroutine(SimulateDiscoveryCoroutine());
#endif
        }

        /// <summary>
        /// Discovery coroutine for Android devices
        /// </summary>
        private IEnumerator DiscoveryCoroutine()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Exception discoveryException = null;
            try
            {
                // Use the plugin API - it returns JSON format: [{"name":"DeviceName","address":"XX:XX:XX:XX:XX:XX"},...]
                string pairedDevicesJson = bluetoothSerial.Call<string>("getPairedDevices");
                Debug.Log($"[DispenserController] Paired devices JSON: {pairedDevicesJson}");
                
                if (!string.IsNullOrEmpty(pairedDevicesJson) && pairedDevicesJson != "[]")
                {
                    // Parse JSON array manually (avoiding dependency on external JSON library)
                    // Format: [{"name":"DeviceName","address":"XX:XX:XX:XX:XX:XX"},...]
                    var devices = ParsePairedDevicesJson(pairedDevicesJson);
                    foreach (var device in devices)
                    {
                        discoveredDevices.Add(device);
                        Debug.Log($"[DispenserController] Found paired device: {device.DeviceName} ({device.MacAddress})");
                    }
                }

                // Notify about found devices - moved outside try block
                discoveryException = null;
            }
            catch (Exception e)
            {
                discoveryException = e;
                Debug.LogError($"[DispenserController] Discovery exception: {e.Message}");
                OnBTError?.Invoke($"搜索异常: {e.Message}");
                OnDiscoveryCompleted?.Invoke();
            }

            // yield must be outside try-catch block
            if (discoveryException == null)
            {
                yield return new WaitForSeconds(0.5f);
                OnDevicesFound?.Invoke(new List<BluetoothDeviceInfo>(discoveredDevices));
                OnDiscoveryCompleted?.Invoke();
                Debug.Log($"[DispenserController] Discovery completed. Found {discoveredDevices.Count} devices.");
            }
#else
            yield break;
#endif
        }

        /// <summary>
        /// Simulate device discovery in editor mode
        /// </summary>
        private IEnumerator SimulateDiscoveryCoroutine()
        {
            yield return new WaitForSeconds(1f);

            // Simulate finding 2-3 devices
            discoveredDevices.Add(new BluetoothDeviceInfo
            {
                DeviceName = "智慧分药机",
                MacAddress = "AA:BB:CC:DD:EE:01",
                IsPaired = true,
                SignalStrength = -45
            });

            yield return new WaitForSeconds(0.5f);

            discoveredDevices.Add(new BluetoothDeviceInfo
            {
                DeviceName = "智慧分药机",
                MacAddress = "AA:BB:CC:DD:EE:02",
                IsPaired = false,
                SignalStrength = -67
            });

            yield return new WaitForSeconds(0.5f);

            discoveredDevices.Add(new BluetoothDeviceInfo
            {
                DeviceName = "Test Device",
                MacAddress = "AA:BB:CC:DD:EE:03",
                IsPaired = true,
                SignalStrength = -82
            });

            OnDevicesFound?.Invoke(new List<BluetoothDeviceInfo>(discoveredDevices));
            OnDiscoveryCompleted?.Invoke();
            Debug.Log($"[DispenserController] Editor simulation: Found {discoveredDevices.Count} devices.");
        }

        /// <summary>
        /// Connect to a specific Bluetooth device by MAC address
        /// </summary>
        public void ConnectToDevice(string macAddress, string deviceName = "智慧分药机")
        {
            if (isConnected)
            {
                Debug.LogWarning("[DispenserController] Already connected to a device. Disconnect first.");
                return;
            }

            deviceMacAddress = macAddress;
            OnConnectionStateChanged?.Invoke("Connecting");
            
            if (Initialize(macAddress))
            {
                connectedDevice = new BluetoothDeviceInfo
                {
                    DeviceName = deviceName,
                    MacAddress = macAddress,
                    IsPaired = true,
                    SignalStrength = -1
                };
                OnConnectionStateChanged?.Invoke("Connected");
                Debug.Log($"[DispenserController] Connected to {deviceName} ({macAddress})");
            }
            else
            {
                OnConnectionStateChanged?.Invoke("Disconnected");
                OnBTError?.Invoke("连接失败");
                Debug.LogError($"[DispenserController] Failed to connect to {macAddress}");
            }
        }

        /// <summary>
        /// Disconnect from current device
        /// </summary>
        public void DisconnectCurrentDevice()
        {
            Disconnect();
            connectedDevice = null;
            OnConnectionStateChanged?.Invoke("Disconnected");
            Debug.Log("[DispenserController] Disconnected from device");
        }

        /// <summary>
        /// Get currently connected device info
        /// </summary>
        public BluetoothDeviceInfo GetConnectedDevice()
        {
            return connectedDevice;
        }

        /// <summary>
        /// Get list of discovered devices
        /// </summary>
        public List<BluetoothDeviceInfo> GetDiscoveredDevices()
        {
            return new List<BluetoothDeviceInfo>(discoveredDevices);
        }

        #endregion

        #region 数据接收

        /// <summary>
        /// 启动数据接收协程
        /// </summary>
        private void StartReceiving()
        {
            if (!isReceiving)
            {
                isReceiving = true;
                StartCoroutine(ReceiveDataCoroutine());
                Debug.Log("[DispenserController] 数据接收已启动");
            }
        }

        /// <summary>
        /// 接收数据协程
        /// </summary>
        private IEnumerator ReceiveDataCoroutine()
        {
            while (isReceiving && isConnected)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    if (bluetoothSerial != null)
                    {
                        string data = bluetoothSerial.Call<string>("read");
                        if (!string.IsNullOrEmpty(data))
                        {
                            ProcessReceivedData(data);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[DispenserController] Receive data exception: {e.Message}");
                }
#endif
                yield return new WaitForSeconds(0.05f); // 20Hz 接收频率
            }
        }

        /// <summary>
        /// 处理接收到的数据
        /// </summary>
        private void ProcessReceivedData(string data)
        {
            receiveBuffer.Append(data);
            string bufferContent = receiveBuffer.ToString();

            // 按换行符分割消息
            string[] lines = bufferContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // 处理完整的行
            for (int i = 0; i < lines.Length - 1; i++)
            {
                HandleFeedbackMessage(lines[i].Trim());
            }

            // 保留最后一个不完整的行
            if (bufferContent.EndsWith("\n") || bufferContent.EndsWith("\r"))
            {
                if (lines.Length > 0)
                {
                    HandleFeedbackMessage(lines[lines.Length - 1].Trim());
                }
                receiveBuffer.Clear();
            }
            else
            {
                receiveBuffer.Clear();
                if (lines.Length > 0)
                {
                    receiveBuffer.Append(lines[lines.Length - 1]);
                }
            }
        }

        /// <summary>
        /// 处理反馈消息
        /// </summary>
        private void HandleFeedbackMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            Debug.Log($"[DispenserController] Received message: {message}");

            var feedback = SerialProtocol.FeedbackParser.Parse(message);
            if (feedback == null) return;

            switch (feedback.Type)
            {
                case FeedbackType.ACK:
                    ackReceived = true;
                    Debug.Log("[DispenserController] ACK received");
                    break;

                case FeedbackType.DONE:
                    doneReceived = true;
                    Debug.Log("[DispenserController] DONE signal received");
                    break;

                case FeedbackType.MachineInit:
                    Debug.Log("[DispenserController] Machine initialization detected");
                    OnMachineInit?.Invoke();
                    break;

                case FeedbackType.StateFinish:
                    machineState = 3;
                    Debug.Log("[DispenserController] Dispensing completed");
                    OnDispensingComplete?.Invoke();
                    break;

                case FeedbackType.StateCountError:
                    errorCode = 2;
                    Debug.LogWarning("[DispenserController] Count error");
                    OnCountError?.Invoke();
                    break;

                case FeedbackType.PillsOut:
                    pillRemain = totalPills - feedback.PillCount;
                    Debug.Log($"[DispenserController] Pills dispensed: {feedback.PillCount}, remaining: {pillRemain}");
                    OnPillCountUpdate?.Invoke(feedback.PillCount);
                    break;

                case FeedbackType.Unknown:
                    Debug.LogWarning($"[DispenserController] Unknown message: {feedback.RawMessage}");
                    break;
            }
        }

        #endregion

        #region 数据发送

        /// <summary>
        /// 发送数据包（带重试机制）
        /// </summary>
        private IEnumerator SendPackageCoroutine(byte[] package, int retryCount, Action<bool> callback)
        {
            if (!isConnected)
            {
                Debug.LogError("[DispenserController] Not connected, cannot send data");
                callback?.Invoke(false);
                yield break;
            }

            if (isSendingPackage)
            {
                Debug.LogWarning("[DispenserController] Previous send not finished");
                callback?.Invoke(false);
                yield break;
            }

            isSendingPackage = true;
            ackReceived = false;

            bool success = false;

            for (int attempt = 0; attempt <= retryCount; attempt++)
            {
                if (attempt > 0)
                {
                    Debug.Log($"[DispenserController] Retrying send ({attempt}/{retryCount})");
                }

                // 发送数据
                if (!SendBytes(package))
                {
                    Debug.LogError("[DispenserController] Send failed");
                    continue;
                }

                // 等待ACK
                float waitTime = 0f;
                while (waitTime < ackTimeout && !ackReceived)
                {
                    yield return new WaitForSeconds(0.01f);
                    waitTime += 0.01f;
                }

                if (ackReceived)
                {
                    Debug.Log("[DispenserController] Send succeeded, ACK received");
                    success = true;
                    break;
                }
            }

            if (!success)
            {
                Debug.LogError($"[DispenserController] Send failed, no ACK after {retryCount} retries");
                errorCode = 1;
            }

            isSendingPackage = false;
            callback?.Invoke(success);
        }

        /// <summary>
        /// 发送字节数组
        /// </summary>
        private bool SendBytes(byte[] data)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
    try
    {
        if (bluetoothSerial != null)
        {
            // 使用 writeBytes 方法（根据您的 Java 代码）
            bool success = bluetoothSerial.Call<bool>("writeBytes", data);
            
            if (success)
            {
                Debug.Log($"[DispenserController] Sent {data.Length} bytes successfully: [{string.Join(" ", data.Select(b => $"0x{b:X2}"))}]");
            }
            else
            {
                Debug.LogError("[DispenserController] writeBytes returned false");
            }
            
            return success;
        }
        else
        {
            Debug.LogError("[DispenserController] bluetoothSerial is null");
            return false;
        }
    }
    catch (Exception e)
    {
        Debug.LogError($"[DispenserController] Send data exception: {e.Message}");
        Debug.LogError($"[DispenserController] Stack trace: {e.StackTrace}");
        return false;
    }
#else
    Debug.Log($"[DispenserController] Editor mode - Simulated send: {BitConverter.ToString(data)}");
    return true;
#endif
}

        /// <summary>
        /// 等待DONE信号
        /// </summary>
        private IEnumerator WaitForDone(float timeout, Action<bool> callback)
        {
            doneReceived = false;
            float waitTime = 0f;

            while (waitTime < timeout && !doneReceived)
            {
                yield return new WaitForSeconds(0.1f);
                waitTime += 0.1f;
            }

            callback?.Invoke(doneReceived);
        }

        #endregion

        #region 分药机控制命令

        /// <summary>
        /// 发送药片矩阵（4x7矩阵，28字节）
        /// </summary>
        public void SendPillMatrix(byte[,] matrix, Action<bool> callback = null)
        {
            if (matrix.GetLength(0) != 4 || matrix.GetLength(1) != 7)
            {
                Debug.LogError("[DispenserController] Pill matrix must be 4x7");
                callback?.Invoke(false);
                return;
            }

            // 转换为字节数组（行优先）
            byte[] matrixData = new byte[28];
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    matrixData[i * 7 + j] = matrix[i, j];
                }
            }

            // 计算总药片数
            totalPills = 0;
            foreach (byte count in matrixData)
            {
                totalPills += count;
            }
            pillRemain = totalPills;

            Debug.Log($"[DispenserController] Sending pill matrix, total pills: {totalPills}");

            byte[] package = SerialProtocol.BuildPackage(SerialProtocol.Commands.SEND_PILL_MATRIX, matrixData);
            StartCoroutine(SendPackageCoroutine(package, maxRetryCount, callback));
        }

        /// <summary>
        /// 打开舱门
        /// </summary>
        public void OpenTray(Action<bool> callback = null)
        {
            Debug.Log("[DispenserController] Opening tray");
            byte[] package = SerialProtocol.BuildPackage(SerialProtocol.Commands.OPEN_TRAY);
            StartCoroutine(SendPackageCoroutine(package, maxRetryCount, (success) =>
            {
                if (success) isTrayOpened = true;
                callback?.Invoke(success);
            }));
        }

        /// <summary>
        /// 关闭舱门
        /// </summary>
        public void CloseTray(Action<bool> callback = null)
        {
            Debug.Log("[DispenserController] Closing tray");
            byte[] package = SerialProtocol.BuildPackage(SerialProtocol.Commands.CLOSE_TRAY);
            StartCoroutine(SendPackageCoroutine(package, maxRetryCount, (success) =>
            {
                if (success) isTrayOpened = false;
                callback?.Invoke(success);
            }));
        }

        /// <summary>
        /// 暂停分药
        /// </summary>
        public void PauseDispenser(Action<bool> callback = null)
        {
            Debug.Log("[DispenserController] Pausing dispenser");
            byte[] package = SerialProtocol.BuildPackage(SerialProtocol.Commands.PAUSE_DISPENSER);
            StartCoroutine(SendPackageCoroutine(package, maxRetryCount, (success) =>
            {
                if (success) machineState = 2;
                callback?.Invoke(success);
            }));
        }

        /// <summary>
        /// 复位分药机摆锤（阻塞操作，需等待DONE）
        /// </summary>
        public void ResetDispenser(Action<bool> callback = null)
        {
            Debug.Log("[DispenserController] Resetting dispenser");
            byte[] package = SerialProtocol.BuildPackage(SerialProtocol.Commands.RESET_DISPENSER);
            
            StartCoroutine(SendPackageCoroutine(package, maxRetryCount, (ackSuccess) =>
            {
                if (!ackSuccess)
                {
                    callback?.Invoke(false);
                    return;
                }

                // 等待DONE信号
                StartCoroutine(WaitForDone(resetDoneTimeout, (doneSuccess) =>
                {
                    if (doneSuccess)
                    {
                        Debug.Log("[DispenserController] Reset completed");
                    }
                    else
                    {
                        Debug.LogWarning("[DispenserController] Reset timed out");
                    }
                    callback?.Invoke(doneSuccess);
                }));
            }));
        }

        /// <summary>
        /// 设置转盘电机转速
        /// </summary>
        public void SetTurntableSpeed(float speed, Action<bool> callback = null)
        {
            Debug.Log($"[DispenserController] Setting turntable speed: {speed}");
            byte[] data = new byte[5];
            data[0] = SerialProtocol.DeviceID.TURNTABLE_MOTOR;
            byte[] speedBytes = SerialProtocol.FloatToBytes(speed);
            Array.Copy(speedBytes, 0, data, 1, 4);

            byte[] package = SerialProtocol.BuildPackage(SerialProtocol.Commands.SET_MOTOR_SPEED, data);
            StartCoroutine(SendPackageCoroutine(package, maxRetryCount, callback));
        }

        /// <summary>
        /// 设置舵机角度（控制药物入口大小）
        /// </summary>
        public void SetServoAngle(float angle, Action<bool> callback = null)
        {
            Debug.Log($"[DispenserController] Setting servo angle: {angle}");
            byte[] data = new byte[5];
            data[0] = SerialProtocol.DeviceID.SERVO_MOTOR;
            byte[] angleBytes = SerialProtocol.FloatToBytes(angle);
            Array.Copy(angleBytes, 0, data, 1, 4);

            byte[] package = SerialProtocol.BuildPackage(SerialProtocol.Commands.SET_MOTOR_SPEED, data);
            StartCoroutine(SendPackageCoroutine(package, maxRetryCount, callback));
        }

        /// <summary>
        /// 设置清洁速度
        /// </summary>
        public void SetCleanSpeed(float speed, Action<bool> callback = null)
        {
            Debug.Log($"[DispenserController] Setting clean speed: {speed}");
            byte[] data = SerialProtocol.FloatToBytes(speed);
            byte[] package = SerialProtocol.BuildPackage(SerialProtocol.Commands.SET_CLEAN_SPEED, data);
            StartCoroutine(SendPackageCoroutine(package, maxRetryCount, callback));
        }

        /// <summary>
        /// 设置清洁延迟时间（毫秒）
        /// </summary>
        public void SetCleanDelay(uint delayMs, Action<bool> callback = null)
        {
            Debug.Log($"[DispenserController] Setting clean delay: {delayMs}ms");
            byte[] data = SerialProtocol.UInt32ToBytes(delayMs);
            byte[] package = SerialProtocol.BuildPackage(SerialProtocol.Commands.SET_CLEAN_DELAY, data);
            StartCoroutine(SendPackageCoroutine(package, maxRetryCount, callback));
        }

        #endregion

        #region 状态查询

        public bool IsConnected => isConnected;
        public bool IsTrayOpened => isTrayOpened;
        public int MachineState => machineState;
        public int ErrorCode => errorCode;
        public int PillRemain => pillRemain;
        public int TotalPills => totalPills;

        /// <summary>
        /// Attempt to reconnect if not connected (call before operations)
        /// </summary>
        public bool EnsureConnected()
        {
            if (isConnected)
            {
                return true;
            }

            Debug.LogWarning("[DispenserController] Not connected, attempting reconnect...");
            return Initialize();
        }

        #endregion

        #region Unity生命周期

        private void OnDestroy()
        {
            Disconnect();
        }

        private void OnApplicationQuit()
        {
            Disconnect();
        }

        #endregion

        #region JSON Parsing

        /// <summary>
        /// Parse paired devices JSON string from the Bluetooth plugin
        /// Format: [{"name":"DeviceName","address":"XX:XX:XX:XX:XX:XX"},...]
        /// </summary>
        private List<BluetoothDeviceInfo> ParsePairedDevicesJson(string json)
        {
            var devices = new List<BluetoothDeviceInfo>();
            
            if (string.IsNullOrEmpty(json) || json == "[]")
            {
                return devices;
            }

            try
            {
                // Remove the outer brackets
                json = json.Trim();
                if (json.StartsWith("[")) json = json.Substring(1);
                if (json.EndsWith("]")) json = json.Substring(0, json.Length - 1);
                
                // Split by },{ to get individual device objects
                string[] deviceStrings = json.Split(new string[] { "},{" }, StringSplitOptions.RemoveEmptyEntries);
                
                foreach (string deviceStr in deviceStrings)
                {
                    string cleanDeviceStr = deviceStr.Trim();
                    // Clean up the braces
                    if (cleanDeviceStr.StartsWith("{")) cleanDeviceStr = cleanDeviceStr.Substring(1);
                    if (cleanDeviceStr.EndsWith("}")) cleanDeviceStr = cleanDeviceStr.Substring(0, cleanDeviceStr.Length - 1);
                    
                    // Parse name and address
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
                Debug.LogError($"[DispenserController] Failed to parse paired devices JSON: {e.Message}");
            }
            
            return devices;
        }

        /// <summary>
        /// Extract a string value from a simple JSON object
        /// </summary>
        private string ExtractJsonValue(string json, string key)
        {
            string searchKey = "\"" + key + "\":\"";
            int startIndex = json.IndexOf(searchKey);
            if (startIndex == -1) return "";
            
            startIndex += searchKey.Length;
            int endIndex = json.IndexOf("\"", startIndex);
            if (endIndex == -1) return "";
            
            return json.Substring(startIndex, endIndex - startIndex);
        }

        #endregion
    }
}
