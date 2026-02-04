using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using EZDose.Hardware;

/// <summary>
/// Debug controller for pill dispenser hardware testing.
/// Provides UI controls for connecting, dispensing, and adjusting dispenser settings.
/// </summary>
public class DispenserDebugger : MonoBehaviour
{
    #region Configuration
    [Header("Device Settings")]
    [SerializeField] private string macAddress = "00:25:06:01:1C:B1";
    
    [Header("UI References - Connection")]
    [SerializeField] private InputField macAddressInput;
    
    [Header("UI References - Pattern Control")]
    [SerializeField] private InputField rowsInput;           // Pattern rows: 1-4
    [SerializeField] private InputField columnsInput;        // Pattern columns: 1-7
    [SerializeField] private InputField pillsPerCellInput;   // Pills per cell: >= 1
    
    [Header("UI References - Hardware Control")]
    [SerializeField] private Slider servoAngleSlider;
    [SerializeField] private Text servoValueText;
    [SerializeField] private Slider motorSpeedSlider;
    [SerializeField] private Text motorSpeedText;
    
    [Header("UI References - Status Display")]
    [SerializeField] private Text statusText;   // Status messages display
    #endregion

    #region Private Fields
    private DispenserController controller;
    
    // Servo slider state
    private float pendingServoAngle = -1f;
    private bool isServoSliderDragging = false;
    
    // Motor slider state
    private float pendingMotorSpeed = -1f;
    private bool isMotorSliderDragging = false;
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        LockScreenOrientation();
        InitializeController();
        InitializeUI();
        SetupSliders();
    }

    private void OnDestroy()
    {
        controller?.Disconnect();
    }
    #endregion

    #region Initialization
    /// <summary>
    /// Lock screen to landscape orientation for tablet use.
    /// </summary>
    private void LockScreenOrientation()
    {
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
    }

    /// <summary>
    /// Create and configure the dispenser controller.
    /// </summary>
    private void InitializeController()
    {
        controller = gameObject.AddComponent<DispenserController>();
        
        // Subscribe to dispenser events
        controller.OnDispensingComplete += OnDispensingComplete;
        controller.OnCountError += OnCountError;  // Handle counting errors
        controller.OnPillCountUpdate += (count) => ShowStatus($"已分出 {count} 片药");
        controller.OnError += (error) => ShowStatus($"错误: {error}", true);
    }

    /// <summary>
    /// Handle counting errors - stop the motor to prevent continuous buzzing.
    /// </summary>
    private void OnCountError()
    {
        Debug.LogWarning("Count error detected!");
        ShowStatus("⚠ 计数错误! 正在停止电机...", true);
        
        // Stop the motor to prevent buzzing sound
        controller.PauseDispenser((success) =>
        {
            ShowStatus(success 
                ? "⚠ 计数错误，电机已停止，请手动检查" 
                : "✗ 停止电机失败", true);
        });
    }

    /// <summary>
    /// Initialize UI input fields with default values.
    /// </summary>
    private void InitializeUI()
    {
        if (macAddressInput != null)
            macAddressInput.text = macAddress;
        
        // Set default pattern values
        if (rowsInput != null) rowsInput.text = "3";
        if (columnsInput != null) columnsInput.text = "3";
        if (pillsPerCellInput != null) pillsPerCellInput.text = "1";
    }

    /// <summary>
    /// Setup sliders with value ranges and event handlers.
    /// </summary>
    private void SetupSliders()
    {
        SetupSlider(servoAngleSlider, 0.1f, 1.0f, 0.5f, OnServoSliderChanged, OnServoSliderDragStart, OnServoSliderDragEnd);
        SetupSlider(motorSpeedSlider, 0.1f, 1.4f, 0.5f, OnMotorSliderChanged, OnMotorSliderDragStart, OnMotorSliderDragEnd);
    }

    /// <summary>
    /// Generic slider setup with drag event handlers.
    /// </summary>
    private void SetupSlider(Slider slider, float min, float max, float defaultValue,
        UnityEngine.Events.UnityAction<float> onValueChanged,
        UnityEngine.Events.UnityAction onPointerDown,
        UnityEngine.Events.UnityAction onPointerUp)
    {
        if (slider == null) return;

        slider.minValue = min;
        slider.maxValue = max;
        slider.value = defaultValue;
        slider.onValueChanged.AddListener(onValueChanged);

        // Add drag event triggers
        var trigger = slider.gameObject.GetComponent<EventTrigger>() 
            ?? slider.gameObject.AddComponent<EventTrigger>();

        var downEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        downEntry.callback.AddListener(_ => onPointerDown());
        trigger.triggers.Add(downEntry);

        var upEntry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
        upEntry.callback.AddListener(_ => onPointerUp());
        trigger.triggers.Add(upEntry);
    }
    #endregion

    #region Dispenser Events
    /// <summary>
    /// Handle dispensing completion - return to idle state.
    /// </summary>
    private void OnDispensingComplete()
    {
        Debug.Log("✓ Dispensing complete!");
        ShowStatus("✓ 分药完成！正在回到空闲状态...");
        
        // Return dispenser to idle state (reference: MainController.PauseAsync)
        controller.PauseDispenser((success) =>
        {
            ShowStatus(success ? "✓ 已回到空闲状态" : "✗ 回到空闲状态失败", !success);
        });
    }
    #endregion

    #region Public Control Methods
    /// <summary>
    /// Connect to the dispenser device via Bluetooth.
    /// </summary>
    [ContextMenu("1. Connect Device")]
    public void ConnectDevice()
    {
        string targetMac = !string.IsNullOrWhiteSpace(macAddressInput?.text) 
            ? macAddressInput.text 
            : macAddress;
            
        ShowStatus($"正在连接设备: {targetMac}...");
        bool success = controller.Initialize(targetMac);
        ShowStatus(success ? "✓ 连接成功" : "✗ 连接失败", !success);
    }
    
    /// <summary>
    /// Open the pill tray.
    /// </summary>
    [ContextMenu("2. Open Tray")]
    public void OpenTray()
    {
        ShowStatus("正在打开舱门...");
        controller.OpenTray((success) => 
            ShowStatus(success ? "✓ 舱门已打开" : "✗ 打开舱门失败", !success));
    }
    
    /// <summary>
    /// Close the pill tray.
    /// </summary>
    [ContextMenu("3. Close Tray")]
    public void CloseTray()
    {
        ShowStatus("正在关闭舱门...");
        controller.CloseTray((success) => 
            ShowStatus(success ? "✓ 舱门已关闭" : "✗ 关闭舱门失败", !success));
    }
    
    /// <summary>
    /// Send custom pill matrix based on UI input fields.
    /// Pattern dimensions: rows (1-4), columns (1-7), pills per cell.
    /// </summary>
    [ContextMenu("4. Send Custom Matrix")]
    public void SendCustomMatrix()
    {
        // Parse and validate input
        int rows = ParseInputField(rowsInput, 3, 1, 4);
        int cols = ParseInputField(columnsInput, 3, 1, 7);
        int pillsPerCell = ParseInputField(pillsPerCellInput, 1, 1, 255);
        
        // Show validation warnings if values were clamped
        if (!ValidateAndWarnInputs(rows, cols, pillsPerCell)) return;
        
        byte[,] matrix = BuildPillMatrix(rows, cols, (byte)pillsPerCell);
        int totalPills = rows * cols * pillsPerCell;
        
        // Apply current slider settings before sending matrix
        ApplyCurrentSettings(() =>
        {
            ShowStatus($"正在发送矩阵: {rows}行×{cols}列, 每格{pillsPerCell}片, 共{totalPills}片...");
            controller.SendPillMatrix(matrix, (success) =>
            {
                ShowStatus(success 
                    ? $"✓ 发送成功! 共{controller.TotalPills}片药" 
                    : "✗ 发送失败", !success);
            });
        });
    }
    
    /// <summary>
    /// Apply current servo angle and motor speed settings to hardware.
    /// </summary>
    private void ApplyCurrentSettings(System.Action onComplete)
    {
        float servoAngle = servoAngleSlider != null ? servoAngleSlider.value : 0.5f;
        float motorSpeed = motorSpeedSlider != null ? motorSpeedSlider.value : 0.5f;
        
        ShowStatus($"应用设置: 舵机={servoAngle:F2}, 电机={motorSpeed:F2}...");
        
        // Apply servo angle first, then motor speed, then call completion
        controller.SetServoAngle(servoAngle, (servoSuccess) =>
        {
            if (!servoSuccess)
            {
                ShowStatus("⚠ 设置舵机角度失败，继续发送矩阵", true);
            }
            
            controller.SetTurntableSpeed(motorSpeed, (motorSuccess) =>
            {
                if (!motorSuccess)
                {
                    ShowStatus("⚠ 设置电机速度失败，继续发送矩阵", true);
                }
                
                onComplete?.Invoke();
            });
        });
    }
    
    /// <summary>
    /// Disconnect from the dispenser device.
    /// </summary>
    [ContextMenu("5. Disconnect")]
    public void DisconnectDevice()
    {
        controller.Disconnect();
        ShowStatus("已断开连接");
    }
    
    /// <summary>
    /// Reset the dispenser pendulum.
    /// </summary>
    [ContextMenu("6. Reset Pendulum")]
    public void ResetPendulum()
    {
        ShowStatus("正在复位摆锤...");
        controller.ResetDispenser();
        ShowStatus("✓ 摆锤复位完成");
    }
    #endregion

    #region Helper Methods
    /// <summary>
    /// Build a pill matrix with specified dimensions and pills per cell.
    /// </summary>
    private byte[,] BuildPillMatrix(int rows, int cols, byte pillsPerCell)
    {
        byte[,] matrix = new byte[4, 7];
        rows = Mathf.Clamp(rows, 1, 4);
        cols = Mathf.Clamp(cols, 1, 7);
        
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                matrix[r, c] = pillsPerCell;
            }
        }
        
        return matrix;
    }

    /// <summary>
    /// Parse InputField value with clamping and default fallback.
    /// </summary>
    private int ParseInputField(InputField field, int defaultValue, int min, int max)
    {
        if (field == null || !int.TryParse(field.text, out int value))
            return defaultValue;
        return Mathf.Clamp(value, min, max);
    }
    
    /// <summary>
    /// Validate input values and show warnings if out of range.
    /// </summary>
    private bool ValidateAndWarnInputs(int rows, int cols, int pillsPerCell)
    {
        // Check original input values before clamping
        if (int.TryParse(rowsInput?.text, out int inputRows) && (inputRows < 1 || inputRows > 4))
        {
            ShowStatus($"⚠ 行数超出范围(1-4)，已自动调整为{rows}", true);
        }
        if (int.TryParse(columnsInput?.text, out int inputCols) && (inputCols < 1 || inputCols > 7))
        {
            ShowStatus($"⚠ 列数超出范围(1-7)，已自动调整为{cols}", true);
        }
        if (int.TryParse(pillsPerCellInput?.text, out int inputPills) && inputPills < 1)
        {
            ShowStatus($"⚠ 每格药片数必须≥1，已自动调整为{pillsPerCell}", true);
        }
        return true;
    }
    
    /// <summary>
    /// Display status message on UI and log to console.
    /// </summary>
    private void ShowStatus(string message, bool isError = false)
    {
        if (isError)
            Debug.LogWarning(message);
        else
            Debug.Log(message);
            
        if (statusText != null)
            statusText.text = message;
    }
    #endregion

    #region Servo Slider Handlers
    private void OnServoSliderDragStart() => isServoSliderDragging = true;
    
    private void OnServoSliderDragEnd()
    {
        isServoSliderDragging = false;
        
        if (pendingServoAngle >= 0)
        {
            float angle = pendingServoAngle;
            Debug.Log($"Setting servo angle: {angle:F2}");
            controller.SetServoAngle(angle, (success) =>
                Debug.Log(success ? $"✓ Servo angle set to {angle:F2}" : "✗ Failed to set servo angle"));
        }
    }
    
    private void OnServoSliderChanged(float value)
    {
        pendingServoAngle = value;
        if (servoValueText != null)
            servoValueText.text = $"Servo: {value:F2}";
    }
    #endregion

    #region Motor Slider Handlers
    private void OnMotorSliderDragStart() => isMotorSliderDragging = true;
    
    private void OnMotorSliderDragEnd()
    {
        isMotorSliderDragging = false;
        
        if (pendingMotorSpeed >= 0)
        {
            float speed = pendingMotorSpeed;
            Debug.Log($"Setting motor speed: {speed:F2}");
            controller.SetTurntableSpeed(speed, (success) =>
                Debug.Log(success ? $"✓ Motor speed set to {speed:F2}" : "✗ Failed to set motor speed"));
        }
    }
    
    private void OnMotorSliderChanged(float value)
    {
        pendingMotorSpeed = value;
        if (motorSpeedText != null)
            motorSpeedText.text = $"Motor: {value:F2}";
    }
    #endregion
}
