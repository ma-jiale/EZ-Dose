using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using EZDose.Hardware;

public class SimpleDispenserExample : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private string macAddress = "00:25:06:01:1C:B1";
    
    [Header("UI引用")]
    [SerializeField] private InputField macAddressInput; // MAC地址输入框
    [SerializeField] private Slider servoAngleSlider;
    [SerializeField] private Text servoValueText; // 显示当前值的文本（可选）
    [SerializeField] private Slider motorSpeedSlider; // 电机转盘速度滑动条
    [SerializeField] private Text motorSpeedText; // 显示当前电机速度的文本（可选）

    private DispenserController controller;
    private float pendingServoAngle = -1f;
    private bool isSliderDragging = false;
    private float pendingMotorSpeed = -1f;
    private bool isMotorSliderDragging = false;
    
    void Start()
    {
        // 创建控制器
        controller = gameObject.AddComponent<DispenserController>();
        
        // 设置MAC地址输入框默认值
        if (macAddressInput != null)
        {
            macAddressInput.text = macAddress;
        }
        
        // 订阅事件
        controller.OnDispensingComplete += () => {
            Debug.Log("✓ 分药完成！");
        };
        
        controller.OnPillCountUpdate += (count) => {
            Debug.Log($"已分出 {count} 片药");
        };
        
        controller.OnError += (error) => {
            Debug.LogError($"错误: {error}");
        };

        // 设置滑动条
        if (servoAngleSlider != null)
        {
            servoAngleSlider.minValue = 0.1f;
            servoAngleSlider.maxValue = 1.0f;
            servoAngleSlider.value = 0.5f;
            
            // 监听滑动条值改变（仅用于显示）
            servoAngleSlider.onValueChanged.AddListener(OnServoSliderValueChanged);
            
            // 添加事件触发器以检测拖动结束
            EventTrigger trigger = servoAngleSlider.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = servoAngleSlider.gameObject.AddComponent<EventTrigger>();
            }
            
            // 添加 PointerDown 事件（开始拖动）
            EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
            pointerDownEntry.eventID = EventTriggerType.PointerDown;
            pointerDownEntry.callback.AddListener((data) => { OnSliderPointerDown(); });
            trigger.triggers.Add(pointerDownEntry);
            
            // 添加 PointerUp 事件（停止拖动）
            EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
            pointerUpEntry.eventID = EventTriggerType.PointerUp;
            pointerUpEntry.callback.AddListener((data) => { OnSliderPointerUp(); });
            trigger.triggers.Add(pointerUpEntry);
        }

        // 设置电机转盘速度滑动条
        if (motorSpeedSlider != null)
        {
            motorSpeedSlider.minValue = 0.1f;
            motorSpeedSlider.maxValue = 1.4f;
            motorSpeedSlider.value = 0.5f;
            
            // 监听滑动条值改变（仅用于显示）
            motorSpeedSlider.onValueChanged.AddListener(OnMotorSpeedSliderValueChanged);
            
            // 添加事件触发器以检测拖动结束
            EventTrigger motorTrigger = motorSpeedSlider.gameObject.GetComponent<EventTrigger>();
            if (motorTrigger == null)
            {
                motorTrigger = motorSpeedSlider.gameObject.AddComponent<EventTrigger>();
            }
            
            // 添加 PointerDown 事件（开始拖动）
            EventTrigger.Entry motorPointerDownEntry = new EventTrigger.Entry();
            motorPointerDownEntry.eventID = EventTriggerType.PointerDown;
            motorPointerDownEntry.callback.AddListener((data) => { OnMotorSliderPointerDown(); });
            motorTrigger.triggers.Add(motorPointerDownEntry);
            
            // 添加 PointerUp 事件（停止拖动）
            EventTrigger.Entry motorPointerUpEntry = new EventTrigger.Entry();
            motorPointerUpEntry.eventID = EventTriggerType.PointerUp;
            motorPointerUpEntry.callback.AddListener((data) => { OnMotorSliderPointerUp(); });
            motorTrigger.triggers.Add(motorPointerUpEntry);
        }
    }
    
    /// <summary>
    /// 示例1：连接设备
    /// </summary>
    [ContextMenu("1. 连接设备")]
    public void Example1_Connect()
    {
        // 优先使用InputField中的MAC地址，如果没有则使用默认配置
        string targetMacAddress = (macAddressInput != null && !string.IsNullOrWhiteSpace(macAddressInput.text)) 
            ? macAddressInput.text 
            : macAddress;
            
        Debug.Log($"正在连接到设备: {targetMacAddress}");
        bool success = controller.Initialize(targetMacAddress);
        Debug.Log(success ? "✓ 连接成功" : "✗ 连接失败");
    }
    
    /// <summary>
    /// 示例2：打开/关闭舱门
    /// </summary>
    [ContextMenu("2. 打开舱门")]
    public void Example2_OpenTray()
    {
        controller.OpenTray((success) => {
            Debug.Log(success ? "✓ 舱门已打开" : "✗ 打开失败");
        });
    }
    
    [ContextMenu("3. 关闭舱门")]
    public void Example3_CloseTray()
    {
        controller.CloseTray((success) => {
            Debug.Log(success ? "✓ 舱门已关闭" : "✗ 关闭失败");
        });
    }
    
    /// <summary>
    /// 示例4：发送简单的药片矩阵
    /// </summary>
    [ContextMenu("4. 发送3*3药片矩阵")]
    public void Example4_SendSimpleMatrix()
    {
        // 创建一个简单的矩阵
        byte[,] matrix = new byte[4, 7]
        {
            { 1, 1, 1, 0, 0, 0, 0 }, // 晚上
            { 1, 1, 1, 0, 0, 0, 0 }, // 中午
            { 1, 1, 1, 0, 0, 0, 0 }, // 早上
            { 0, 0, 0, 0, 0, 0, 0 }  // 预留
        };
        
        // 设置参数并发送
        controller.SetTurntableSpeed(0.5f, (s1) => {
            controller.SetServoAngle(0.5f, (s2) => {
                controller.SendPillMatrix(matrix, (s3) => {
                    Debug.Log(s3 ? $"✓ 矩阵已发送 (共{controller.TotalPills}片)" : "✗ 发送失败");
                });
            });
        });
    }
    
    /// <summary>
    /// 示例5：完整分药流程
    /// </summary>
    [ContextMenu("5.  发送2*4药片矩阵")]
    public void Example5_FullProcess()
    {
        // 创建一个简单的矩阵
        byte[,] matrix = new byte[4, 7]
        {
            { 0, 0, 0, 0, 0, 0, 0 }, // 晚上
            { 1, 1, 1, 1, 0, 0, 0 }, // 中午
            { 1, 1, 1, 1, 0, 0, 0 }, // 早上
            { 0, 0, 0, 0, 0, 0, 0 }  // 预留
        };
        
        // 设置参数并发送
        controller.SetTurntableSpeed(0.5f, (s1) => {
            controller.SetServoAngle(0.5f, (s2) => {
                controller.SendPillMatrix(matrix, (s3) => {
                    Debug.Log(s3 ? $"✓ 矩阵已发送 (共{controller.TotalPills}片)" : "✗ 发送失败");
                });
            });
        });
    }
    
    /// <summary>
    /// 示例6：断开连接
    /// </summary>
    [ContextMenu("6. 断开连接")]
    public void Example6_Disconnect()
    {
        controller.Disconnect();
        Debug.Log("已断开连接");
    }

    
    void OnDestroy()
    {
        if (controller != null)
        {
            controller.Disconnect();
        }
    }

    
    /// <summary>
    /// 示例7：复位分药机摆锤
    /// </summary>
    [ContextMenu("7. 复位分药机摆锤")]
    public void Example7_ResetDispenser()
    {
        controller.ResetDispenser();
        Debug.Log("复位分药机摆锤完成");
    }

        /// <summary>
    /// 滑动条开始拖动
    /// </summary>
    private void OnSliderPointerDown()
    {
        isSliderDragging = true;
        Debug.Log("开始拖动滑动条");
    }
    
    /// <summary>
    /// 滑动条停止拖动（发送命令）
    /// </summary>
    private void OnSliderPointerUp()
    {
        isSliderDragging = false;
        Debug.Log("停止拖动滑动条");
        
        if (servoAngleSlider != null && pendingServoAngle >= 0)
        {
            float angle = pendingServoAngle;
            Debug.Log($"发送舵机角度命令: {angle:F2}");
            
            controller.SetServoAngle(angle, (success) => {
                if (success)
                {
                    Debug.Log($"✓ 舵机角度已设置为 {angle:F2}");
                }
                else
                {
                    Debug.LogWarning("✗ 舵机角度设置失败");
                }
            });
        }
    }

    /// <summary>
    /// 滑动条值改变（仅更新显示）
    /// </summary>
    private void OnServoSliderValueChanged(float value)
    {
        pendingServoAngle = value;
        
        // 更新显示文本
        if (servoValueText != null)
        {
            servoValueText.text = $"舵机角度: {value:F2}";
        }
        
        // 如果正在拖动，不发送命令
        if (isSliderDragging)
        {
            Debug.Log($"滑动中: {value:F2}（未发送）");
        }
    }

    /// <summary>
    /// 电机转盘速度滑动条开始拖动
    /// </summary>
    private void OnMotorSliderPointerDown()
    {
        isMotorSliderDragging = true;
        Debug.Log("开始拖动电机转盘速度滑动条");
    }
    
    /// <summary>
    /// 电机转盘速度滑动条停止拖动（发送命令）
    /// </summary>
    private void OnMotorSliderPointerUp()
    {
        isMotorSliderDragging = false;
        Debug.Log("停止拖动电机转盘速度滑动条");
        
        if (motorSpeedSlider != null && pendingMotorSpeed >= 0)
        {
            float speed = pendingMotorSpeed;
            Debug.Log($"发送电机转盘速度命令: {speed:F2}");
            
            controller.SetTurntableSpeed(speed, (success) => {
                if (success)
                {
                    Debug.Log($"✓ 电机转盘速度已设置为 {speed:F2}");
                }
                else
                {
                    Debug.LogWarning("✗ 电机转盘速度设置失败");
                }
            });
        }
    }

    /// <summary>
    /// 电机转盘速度滑动条值改变（仅更新显示）
    /// </summary>
    private void OnMotorSpeedSliderValueChanged(float value)
    {
        pendingMotorSpeed = value;
        
        // 更新显示文本
        if (motorSpeedText != null)
        {
            motorSpeedText.text = $"电机速度: {value:F2}";
        }
        
        // 如果正在拖动，不发送命令
        if (isMotorSliderDragging)
        {
            Debug.Log($"电机速度滑动中: {value:F2}（未发送）");
        }
    }
}
