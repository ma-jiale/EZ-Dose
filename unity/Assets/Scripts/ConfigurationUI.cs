using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using EZDose.Calibration;
using EZDose.PillCounter;
using EZDose.Hardware;
using EZDose;

namespace EZDose.UI
{
    /// <summary>
    /// Validates and saves configuration settings from the UI.
    /// Also handles system calibration for pill size measurement.
    /// </summary>
    public class ConfigurationUI : MonoBehaviour
    {
        [Header("Server URL Settings")]
        [Tooltip("Input field for the Server URL.")]
        [SerializeField] private InputField urlInputField;

        [Tooltip("Button to save the configuration.")]
        [SerializeField] private Button saveButton;

        [Header("Dispensing Settings")]
        [Tooltip("Input field for max dispensing days (1-30).")]
        [SerializeField] private InputField maxDispensingDaysInput;
        
        [Tooltip("Input field for expiry days threshold (0-14).")]
        [SerializeField] private InputField expiryDaysThresholdInput;
        
        [Tooltip("Button to save dispensing settings.")]
        [SerializeField] private Button saveDispensingButton;

        [Tooltip("Text to show status messages (optional).")]
        [SerializeField] private Text statusText;

        [Header("Calibration Settings")]
        [Tooltip("Panel containing calibration UI elements")]
        [SerializeField] private GameObject calibrationPanel;
        
        [Tooltip("Input field for reference pill diameter (mm)")]
        [SerializeField] private InputField referenceDiameterInput;
        
        [Tooltip("Button to start calibration process")]
        [SerializeField] private Button startCalibrationButton;
        
        [Tooltip("Button to confirm calibration")]
        [SerializeField] private Button confirmCalibrationButton;
        
        [Tooltip("Button to cancel/close calibration")]
        [SerializeField] private Button cancelCalibrationButton;
        
        [Tooltip("Text showing calibration status")]
        [SerializeField] private Text calibrationStatusText;
        
        [Tooltip("Text showing current calibration state")]
        [SerializeField] private Text calibrationStateText;
        
        [Tooltip("Camera preview for calibration")]
        [SerializeField] private RawImage calibrationCameraPreview;

        [Header("References")]
        [SerializeField] private PillCalibrationManager calibrationManager;
        [SerializeField] private PillCounterController pillCounterController;

        [Header("Hardware Control")]
        [Tooltip("重置分药摆锤零点按钮")]
        [SerializeField] private Button resetPendulumButton;

        [Header("Settings")]
        [SerializeField] private float messageDisplayTime = 3f;

        // Calibration state
        private float pendingPixelArea;
        private bool isCalibrating;
        private Coroutine calibrationCoroutine;
        
        // Preview texture for calibration (center 100x100 crop)
        private Texture2D previewTexture;
        private const int PREVIEW_SIZE = 200;

        private void Start()
        {
            // Load current config into UI
            if (urlInputField != null)
            {
                urlInputField.text = AppConfig.Instance.ServerUrl;
            }

            if (saveButton != null)
            {
                saveButton.onClick.AddListener(OnSaveClicked);
            }
            
            // Load dispensing settings
            LoadDispensingSettings();
            
            if (saveDispensingButton != null)
            {
                saveDispensingButton.onClick.AddListener(OnSaveDispensingSettingsClicked);
            }
            
            if (statusText != null)
            {
                statusText.text = "";
            }

            // Setup calibration buttons
            if (startCalibrationButton != null)
            {
                startCalibrationButton.onClick.AddListener(OnStartCalibrationClicked);
            }
            
            if (confirmCalibrationButton != null)
            {
                confirmCalibrationButton.onClick.AddListener(OnConfirmCalibrationClicked);
                confirmCalibrationButton.interactable = false;
            }
            
            if (cancelCalibrationButton != null)
            {
                cancelCalibrationButton.onClick.AddListener(OnCancelCalibrationClicked);
            }

            // Initialize calibration UI
            InitializeCalibrationUI();

            // Setup reset pendulum button
            if (resetPendulumButton != null)
            {
                resetPendulumButton.onClick.AddListener(OnResetPendulumClicked);
            }
        }

        private void OnEnable()
        {
            // Refresh UI whenever the settings page is opened
            if (urlInputField != null)
            {
                urlInputField.text = AppConfig.Instance.ServerUrl;
            }
            
            // Refresh dispensing settings
            LoadDispensingSettings();
            
            if (statusText != null)
            {
                statusText.text = "";
            }
            
            // Refresh calibration state display
            UpdateCalibrationStateDisplay();
        }

        private void OnDisable()
        {
            // Stop calibration if settings page is closed
            if (isCalibrating)
            {
                StopCalibration();
            }
        }

        #region Server URL Settings

        private void OnSaveClicked()
        {
            if (urlInputField == null) return;

            string newUrl = urlInputField.text;
            bool success = AppConfig.Instance.SaveServerUrl(newUrl);

            if (success)
            {
                ShowMessage("设置已保存", Color.green);
            }
            else
            {
                ShowMessage("URL格式错误 (需以 http:// 或 https:// 开头)", Color.red);
            }
        }

        /// <summary>
        /// Load dispensing settings from AppConfig into UI.
        /// </summary>
        private void LoadDispensingSettings()
        {
            if (maxDispensingDaysInput != null)
            {
                maxDispensingDaysInput.text = AppConfig.Instance.MaxDispensingDays.ToString();
            }
            
            if (expiryDaysThresholdInput != null)
            {
                expiryDaysThresholdInput.text = AppConfig.Instance.ExpiryDaysThreshold.ToString();
            }
        }

        /// <summary>
        /// Save dispensing settings button clicked.
        /// </summary>
        private void OnSaveDispensingSettingsClicked()
        {
            int maxDays = AppConfig.Instance.MaxDispensingDays;
            int expiryThreshold = AppConfig.Instance.ExpiryDaysThreshold;
            
            // Parse max dispensing days
            if (maxDispensingDaysInput != null)
            {
                if (!int.TryParse(maxDispensingDaysInput.text, out maxDays))
                {
                    ShowMessage("最大分药天数格式错误", Color.red);
                    return;
                }
            }
            
            // Parse expiry threshold
            if (expiryDaysThresholdInput != null)
            {
                if (!int.TryParse(expiryDaysThresholdInput.text, out expiryThreshold))
                {
                    ShowMessage("到期提醒天数格式错误", Color.red);
                    return;
                }
            }
            
            // Save settings
            bool success = AppConfig.Instance.SaveDispensingSettings(maxDays, expiryThreshold);
            
            if (success)
            {
                ShowMessage("分药设置已保存", Color.green);
            }
            else
            {
                ShowMessage("分药设置保存失败 (天数范围: 1-30, 提前天数需大于0)", Color.red);
            }
        }

        #endregion

        #region Calibration

        private void InitializeCalibrationUI()
        {
            if (referenceDiameterInput != null)
            {
                referenceDiameterInput.text = "9.0";
            }
            
            // Hide calibration panel initially
            if (calibrationPanel != null)
            {
                calibrationPanel.SetActive(false);
            }
            
            // Initialize preview texture for center 100x100 crop
            previewTexture = new Texture2D(PREVIEW_SIZE, PREVIEW_SIZE, TextureFormat.RGBA32, false);
            if (calibrationCameraPreview != null)
            {
                calibrationCameraPreview.texture = previewTexture;
            }
            
            UpdateCalibrationStateDisplay();
        }

        private void UpdateCalibrationStateDisplay()
        {
            if (calibrationStateText == null) return;
            
            calibrationStateText.text = "已启用自动脉冲校准";
            calibrationStateText.color = Color.green;
        }

        /// <summary>
        /// Start calibration button clicked
        /// </summary>
        public void OnStartCalibrationClicked()
        {
            if (calibrationManager == null || pillCounterController == null)
            {
                ShowMessage("校准组件未配置", Color.red);
                return;
            }

            // Reference diameter input handled locally
            if (referenceDiameterInput != null && float.TryParse(referenceDiameterInput.text, out float diameter))
            {
                // Unused
            }

            // Show calibration panel
            if (calibrationPanel != null)
            {
                calibrationPanel.SetActive(true);
            }

            // Reset pill counter background for fresh detection
            pillCounterController.ResetBackground();

            // Start calibration detection loop
            isCalibrating = true;
            pendingPixelArea = 0f;
            
            if (confirmCalibrationButton != null)
            {
                confirmCalibrationButton.interactable = false;
            }
            
            calibrationCoroutine = StartCoroutine(CalibrationDetectionLoop());
        }

        /// <summary>
        /// Confirm calibration button clicked
        /// </summary>
        public void OnConfirmCalibrationClicked()
        {
            if (calibrationManager == null || pendingPixelArea <= 0) return;

            // Visual calibration is disabled (auto pulse width calibration is used)
            float refDiameter = 9.0f;
            float refArea = Mathf.PI * (refDiameter / 2f) * (refDiameter / 2f);
            
            ShowMessage($"系统已采用自动脉冲校准 ({refDiameter}mm 药片参考)", Color.green);
            UpdateCalibrationStateDisplay();

            StopCalibration();
        }

        /// <summary>
        /// Cancel calibration button clicked
        /// </summary>
        public void OnCancelCalibrationClicked()
        {
            StopCalibration();
        }

        private void StopCalibration()
        {
            isCalibrating = false;
            
            if (calibrationCoroutine != null)
            {
                StopCoroutine(calibrationCoroutine);
                calibrationCoroutine = null;
            }
            
            if (calibrationPanel != null)
            {
                calibrationPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Calibration detection loop coroutine
        /// </summary>
        private IEnumerator CalibrationDetectionLoop()
        {
            if (calibrationStatusText != null)
            {
                calibrationStatusText.text = "等待背景捕捉...";
            }

            // Wait for background capture
            while (isCalibrating && !pillCounterController.IsBackgroundCaptured())
            {
                UpdateCalibrationPreview();
                yield return new WaitForSeconds(0.2f);
            }

            if (!isCalibrating) yield break;

            if (calibrationStatusText != null)
            {
                calibrationStatusText.text = "请放置标准红色药片";
            }

            // Detection loop
            while (isCalibrating)
            {
                // Update camera preview
                UpdateCalibrationPreview();
                
                var (success, pixelArea, message) = pillCounterController.TryCalibrateSinglePill();

                if (calibrationStatusText != null)
                {
                    calibrationStatusText.text = message;
                }

                if (success && pixelArea > 0)
                {
                    pendingPixelArea = pixelArea;
                    
                    // Calculate expected area for display
                    float refDiameter = 9.0f;
                    float expectedArea = Mathf.PI * (refDiameter / 2f) * (refDiameter / 2f);
                    
                    if (calibrationStatusText != null)
                    {
                        calibrationStatusText.text = $"检测到: {pixelArea:F0} 像素\n预期面积: {expectedArea:F1} mm²\n点击确认完成校准";
                    }
                    
                    if (confirmCalibrationButton != null)
                    {
                        confirmCalibrationButton.interactable = true;
                    }
                }
                else
                {
                    pendingPixelArea = 0f;
                    
                    if (confirmCalibrationButton != null)
                    {
                        confirmCalibrationButton.interactable = false;
                    }
                }

                yield return new WaitForSeconds(0.5f);
            }
        }
        
        /// <summary>
        /// Update calibration preview with center 100x100 crop from camera
        /// </summary>
        private void UpdateCalibrationPreview()
        {
            if (pillCounterController == null || calibrationCameraPreview == null || previewTexture == null)
                return;
            
            Texture sourceTexture = pillCounterController.DisplayTexture;
            if (sourceTexture == null)
                return;
            
            // Get source texture as Texture2D
            Texture2D sourceTex2D = sourceTexture as Texture2D;
            if (sourceTex2D == null)
                return;
            
            int sourceWidth = sourceTex2D.width;
            int sourceHeight = sourceTex2D.height;
            
            // Calculate center crop region
            int startX = (sourceWidth - PREVIEW_SIZE) / 2;
            int startY = (sourceHeight - PREVIEW_SIZE) / 2;
            
            // Ensure we don't go out of bounds
            if (startX < 0) startX = 0;
            if (startY < 0) startY = 0;
            if (startX + PREVIEW_SIZE > sourceWidth) startX = sourceWidth - PREVIEW_SIZE;
            if (startY + PREVIEW_SIZE > sourceHeight) startY = sourceHeight - PREVIEW_SIZE;
            
            // Copy center pixels to preview texture
            Color[] centerPixels = sourceTex2D.GetPixels(startX, startY, PREVIEW_SIZE, PREVIEW_SIZE);
            previewTexture.SetPixels(centerPixels);
            previewTexture.Apply();
        }

        /// <summary>
        /// Reset calibration (for testing purposes)
        /// </summary>
        public void OnResetCalibrationClicked()
        {
            UpdateCalibrationStateDisplay();
            ShowMessage("重置完成（现已使用脉冲宽度直算）", Color.yellow);
        }

        #endregion

        #region Hardware Control

        /// <summary>
        /// 重置分药摆锤零点按钮点击事件
        /// </summary>
        private void OnResetPendulumClicked()
        {
            var dispenser = FindObjectOfType<DispenserController>();
            if (dispenser == null || !dispenser.IsConnected)
            {
                ShowMessage("分药机未连接，无法重置", Color.red);
                return;
            }

            EZLog.D(EZLog.Module.UI, "Reset pendulum button clicked");
            if (resetPendulumButton != null) resetPendulumButton.interactable = false;
            ShowMessage("正在重置摆锤零点...", Color.yellow);

            dispenser.ResetDispenser((success) =>
            {
                if (resetPendulumButton != null) resetPendulumButton.interactable = true;
                if (success)
                {
                    ShowMessage("摆锤零点重置成功", Color.green);
                }
                else
                {
                    ShowMessage("摆锤零点重置失败，请重试", Color.red);
                }
            });
        }

        #endregion

        #region Utility

        private void ShowMessage(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
                StopAllCoroutines();
                StartCoroutine(ClearMessageAfterDelay());
            }
            else
            {
                EZLog.D(EZLog.Module.UI, $"ConfigurationUI: {message}");
            }
        }

        private IEnumerator ClearMessageAfterDelay()
        {
            yield return new WaitForSeconds(messageDisplayTime);
            if (statusText != null)
            {
                statusText.text = "";
            }
        }

        #endregion
    }
}
