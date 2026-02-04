using UnityEngine;
using UnityEngine.UI;
using System;

public class BluetoothTest : MonoBehaviour
{
    [Header("UI组件")]
    public Button btnConnect;
    public Button btnSendOne;
    public Text txtStatus;
    public InputField inputDeviceAddress; // 输入设备MAC地址

    private AndroidJavaObject bluetoothSerial;
    private bool isConnected = false;

    void Start()
    {
        // 设置按钮事件
        if (btnConnect) btnConnect.onClick.AddListener(OnConnect);
        if (btnSendOne) btnSendOne.onClick.AddListener(OnSendOne);
        
        // 初始状态
        UpdateUI();
        ShowStatus("请输入蓝牙MAC地址并连接");
        
        // 请求权限
        RequestPermissions();
    }

    void RequestPermissions()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.FineLocation))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.FineLocation);
            }
            
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_CONNECT"))
            {
                UnityEngine.Android.Permission.RequestUserPermission("android.permission.BLUETOOTH_CONNECT");
            }
            
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_SCAN"))
            {
                UnityEngine.Android.Permission.RequestUserPermission("android.permission.BLUETOOTH_SCAN");
            }
            
            ShowStatus("正在请求权限...");
        }
        catch (Exception e)
        {
            Debug.LogError("请求权限失败: " + e.Message);
        }
#endif
    }

    public void OnConnect()
    {
        if (isConnected)
        {
            // 断开连接
            Disconnect();
            return;
        }

        string address = inputDeviceAddress.text.Trim().ToUpper(); // 转大写
        
        // 验证MAC地址格式
        if (string.IsNullOrEmpty(address))
        {
            ShowStatus("❌ 请输入设备地址");
            return;
        }
        
        if (!IsValidMacAddress(address))
        {
            ShowStatus("❌ MAC地址格式错误\n应为: 00:11:22:33:44:55");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            ShowStatus("正在初始化蓝牙...");
            Debug.Log("开始连接: " + address);
            
            // 初始化蓝牙
            if (bluetoothSerial == null)
            {
                Debug.Log("初始化 BluetoothSerial...");
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                
                bluetoothSerial = new AndroidJavaObject("com.unity.bluetooth.BluetoothSerial", activity);
                Debug.Log("BluetoothSerial 初始化成功");
            }

            // 检查蓝牙是否可用
            bool isAvailable = bluetoothSerial.Call<bool>("isBluetoothAvailable");
            Debug.Log("蓝牙是否可用: " + isAvailable);
            
            if (!isAvailable)
            {
                ShowStatus("❌ 设备不支持蓝牙");
                return;
            }

            // 检查蓝牙是否启用
            bool isEnabled = bluetoothSerial.Call<bool>("isBluetoothEnabled");
            Debug.Log("蓝牙是否启用: " + isEnabled);
            
            if (!isEnabled)
            {
                ShowStatus("❌ 请先启用蓝牙");
                return;
            }

            ShowStatus("正在连接 " + address + "...");
            Debug.Log("调用 connect 方法...");
            
            // 连接设备
            bool success = bluetoothSerial.Call<bool>("connect", address);
            
            Debug.Log("连接结果: " + success);
            
            if (success)
            {
                isConnected = true;
                ShowStatus("✓ 已连接到: " + address);
                Debug.Log("连接成功!");
            }
            else
            {
                ShowStatus("❌ 连接失败\n请检查:\n1.设备是否已配对\n2.设备是否在范围内\n3.MAC地址是否正确");
                Debug.LogError("连接失败");
            }
        }
        catch (AndroidJavaException aje)
        {
            string errorMsg = "Android异常: " + aje.Message;
            ShowStatus("❌ " + errorMsg);
            Debug.LogError(errorMsg);
            Debug.LogError("StackTrace: " + aje.StackTrace);
        }
        catch (Exception e)
        {
            string errorMsg = "错误: " + e.Message;
            ShowStatus("❌ " + errorMsg);
            Debug.LogError(errorMsg);
            Debug.LogError("StackTrace: " + e.StackTrace);
        }
#else
        // 编辑器模式模拟
        isConnected = true;
        ShowStatus("✓ 模拟连接成功（编辑器模式）");
#endif
        
        UpdateUI();
    }

    // 验证MAC地址格式
    bool IsValidMacAddress(string address)
    {
        if (address.Length != 17) return false;
        
        string[] parts = address.Split(':');
        if (parts.Length != 6) return false;
        
        foreach (string part in parts)
        {
            if (part.Length != 2) return false;
            foreach (char c in part)
            {
                if (!Uri.IsHexDigit(c)) return false;
            }
        }
        
        return true;
    }

    public void OnSendOne()
    {
        if (!isConnected)
        {
            ShowStatus("❌ 请先连接设备！");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            Debug.Log("发送数据: 1");
            
            // 发送字符"1"点亮LED
            bool success = bluetoothSerial.Call<bool>("write", "1");
            
            Debug.Log("发送结果: " + success);
            
            if (success)
            {
                ShowStatus("✓ 已发送: 1 (点亮LED)");
            }
            else
            {
                ShowStatus("❌ 发送失败");
            }
        }
        catch (Exception e)
        {
            ShowStatus("❌ 发送错误: " + e.Message);
            Debug.LogError("发送错误: " + e.Message);
        }
#else
        ShowStatus("✓ 模拟发送: 1 (编辑器模式)");
#endif
    }

    void Disconnect()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (bluetoothSerial != null)
            {
                bluetoothSerial.Call("disconnect");
                Debug.Log("已断开连接");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("断开连接错误: " + e.Message);
        }
#endif
        
        isConnected = false;
        ShowStatus("已断开连接");
        UpdateUI();
    }

    void UpdateUI()
    {
        // 更新按钮文本
        if (btnConnect)
        {
            Text btnText = btnConnect.GetComponentInChildren<Text>();
            if (btnText)
            {
                btnText.text = isConnected ? "断开连接" : "连接设备";
            }
        }
        
        // 发送按钮只在连接后可用
        if (btnSendOne)
        {
            btnSendOne.interactable = isConnected;
        }
    }

    void ShowStatus(string message)
    {
        if (txtStatus)
        {
            txtStatus.text = message;
        }
        Debug.Log("状态: " + message);
    }

    void OnDestroy()
    {
        if (isConnected)
        {
            Disconnect();
        }
    }
}