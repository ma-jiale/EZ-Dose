using System;
using UnityEngine;
using UnityEngine.UI;
using OpenCVForUnity.CoreModule;
using OpenCVForUnity.UnityUtils;

namespace EZDose.PillCounter
{
    /// <summary>
    /// 药片计数控制器 - Unity摄像头集成和UI管理
    /// </summary>
    public class PillCounterController : MonoBehaviour
    {
        [Header("摄像头设置")]
        [SerializeField] private int cameraIndex = 0;
        [SerializeField] private int requestedWidth = 1280;
        [SerializeField] private int requestedHeight = 720;
        [SerializeField] private int requestedFPS = 30;
        
        [Header("UI组件")]
        [SerializeField] private RawImage displayImage;
        [SerializeField] private Text statusText;
        [SerializeField] private Text pillCountText;
        [SerializeField] private Button captureBackgroundButton;
        [SerializeField] private Button resetBackgroundButton;
        
        // 摄像头相关
        private WebCamTexture webCamTexture;
        private Texture2D displayTexture;
        private Mat frameMat;
        private Mat displayMat;
        
        // 计数器
        private PillCounter pillCounter;
        
        // 状态
        private bool isProcessing = false;
        private int currentPillCount = 0;
        
        // 计数控制
        private float lastCountTime = 0f;
        private const float countInterval = 1.0f; // Count pills every 1 second
        
        void Start()
        {
            try
            {
                InitializeUI();
                InitializeCamera();
                InitializePillCounter();
            }
            catch (Exception e)
            {
                HandleError($"Initialization failed: {e.Message}");
            }
        }
        
        void OnDestroy()
        {
            Cleanup();
        }
        
        /// <summary>
        /// 初始化UI
        /// </summary>
        private void InitializeUI()
        {
            if (captureBackgroundButton != null)
            {
                captureBackgroundButton.onClick.AddListener(OnCaptureBackgroundClicked);
            }
            
            if (resetBackgroundButton != null)
            {
                resetBackgroundButton.onClick.AddListener(OnResetBackgroundClicked);
            }
            
            UpdateStatus("Initializing...");
        }
        
        /// <summary>
        /// 初始化摄像头
        /// </summary>
        private void InitializeCamera()
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            
            if (devices.Length == 0)
            {
                throw new Exception("No available camera device found");
            }
            
            // 选择摄像头
            if (cameraIndex >= devices.Length)
            {
                Debug.LogWarning($"Camera index {cameraIndex} out of range, using default camera");
                cameraIndex = 0;
            }
            
            string deviceName = devices[cameraIndex].name;
            Debug.Log($"Using camera: {deviceName}");
            
            // 创建WebCamTexture
            webCamTexture = new WebCamTexture(deviceName, requestedWidth, requestedHeight, requestedFPS);
            webCamTexture.Play();
            
            // 等待摄像头启动
            StartCoroutine(WaitForCameraStart());
        }
        
        /// <summary>
        /// 等待摄像头启动
        /// </summary>
        private System.Collections.IEnumerator WaitForCameraStart()
        {
            int timeout = 100; // 10秒超时
            int elapsed = 0;
            
            while (!webCamTexture.didUpdateThisFrame && elapsed < timeout)
            {
                elapsed++;
                yield return new WaitForSeconds(0.1f);
            }
            
            if (!webCamTexture.didUpdateThisFrame)
            {
                HandleError("Camera startup timeout");
                yield break;
            }
            
            // 初始化纹理和Mat
            int width = webCamTexture.width;
            int height = webCamTexture.height;
            
            displayTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            frameMat = new Mat(height, width, CvType.CV_8UC4);
            displayMat = new Mat(height, width, CvType.CV_8UC4);
            
            if (displayImage != null)
            {
                displayImage.texture = displayTexture;
            }
            
            UpdateStatus("Camera started, waiting to capture background...");
            Debug.Log($"Camera resolution: {width}x{height}");
        }
        
        /// <summary>
        /// 初始化药片计数器
        /// </summary>
        private void InitializePillCounter()
        {
            pillCounter = new PillCounter();
            Debug.Log("Pill counter initialized");
        }
        
        void Update()
        {
            if (webCamTexture == null || !webCamTexture.didUpdateThisFrame || isProcessing)
                return;
            
            try
            {
                isProcessing = true;
                ProcessFrame();
            }
            catch (Exception e)
            {
                HandleError($"Frame processing failed: {e.Message}");
            }
            finally
            {
                isProcessing = false;
            }
        }
        
        /// <summary>
        /// Process each frame from camera for pill detection
        /// </summary>
        private void ProcessFrame()
        {
            // Convert WebCamTexture to OpenCV Mat
            Utils.webCamTextureToMat(webCamTexture, frameMat);
            
            // Safety check: ensure displayMat is initialized
            if (displayMat == null || displayMat.IsDisposed)
            {
                displayMat = new Mat(frameMat.rows(), frameMat.cols(), frameMat.type());
            }
            
            if (!pillCounter.IsBackgroundCaptured)
            {
                // Detect edges and check focus quality
                var (edgeCount, edges) = pillCounter.DetectEdges(frameMat);
                edges.Dispose();
                
                double focusScore = pillCounter.CheckFocusQuality(frameMat);
                
                if (pillCounter.IsSceneStable(edgeCount, focusScore))
                {
                    // Auto-capture background when both stable and focused
                    pillCounter.CaptureBackground(frameMat);
                    UpdateStatus("Background auto-captured, counting started");
                }
                else
                {
                    // Show waiting state with focus stability info
                    frameMat.copyTo(displayMat);
                    UpdateStatus($"Waiting... Edge:{edgeCount} Focus:{focusScore:F1} Stabilizing");
                }
                
                // Always display preview at 30Hz
                DisplayFrame(displayMat);
            }
            else
            {
                // Check if it's time to count pills (every 1 second)
                float currentTime = Time.time;
                bool shouldCount = (currentTime - lastCountTime) >= countInterval;
                
                if (shouldCount)
                {
                    // Perform pill counting
                    var (pillCount, resultFrame, debugInfo) = pillCounter.CountPills(frameMat);
                    
                    currentPillCount = pillCount;
                    
                    // Dispose old displayMat and use new result
                    if (displayMat != null && !displayMat.IsDisposed && displayMat != resultFrame)
                    {
                        displayMat.Dispose();
                    }
                    displayMat = resultFrame;
                    
                    UpdatePillCount(pillCount);
                    UpdateStatus($"Detecting - Single:{debugInfo.SinglePillCount} Multi:{debugInfo.MultiplePillCount}");
                    
                    lastCountTime = currentTime;
                }
                else
                {
                    // Just copy frame for preview without counting
                    frameMat.copyTo(displayMat);
                }
                
                // Always display preview at 30Hz
                DisplayFrame(displayMat);
            }
        }
        
        /// <summary>
        /// Display frame to UI RawImage component
        /// </summary>
        private void DisplayFrame(Mat mat)
        {
            // Check all components are ready and Mat is valid
            if (mat == null || mat.IsDisposed)
            {
                return;
            }
            
            if (displayTexture != null && displayImage != null)
            {
                Utils.matToTexture2D(mat, displayTexture);
            }
        }
        
        /// <summary>
        /// 捕捉背景按钮点击
        /// </summary>
        private void OnCaptureBackgroundClicked()
        {
            try
            {
                if (frameMat != null)
                {
                    pillCounter.CaptureBackground(frameMat);
                    UpdateStatus("Background manually captured");
                }
            }
            catch (Exception e)
            {
                HandleError($"Capture background failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// 重置背景按钮点击
        /// </summary>
        private void OnResetBackgroundClicked()
        {
            try
            {
                pillCounter.ResetBackground();
                currentPillCount = 0;
                lastCountTime = 0f;
                UpdatePillCount(0);
                UpdateStatus("Background reset, waiting to capture new background...");
            }
            catch (Exception e)
            {
                HandleError($"Reset background failed: {e.Message}");
            }
        }
        
        /// <summary>
        /// Update status text display (English: Updates the status message shown to user)
        /// </summary>
        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"[PillCounter] {message}");
        }
        
        /// <summary>
        /// Update pill count display (English: Shows the current number of pills detected)
        /// </summary>
        private void UpdatePillCount(int count)
        {
            if (pillCountText != null)
            {
                pillCountText.text = $"{count}";
            }
        }
        
        /// <summary>
        /// 错误处理
        /// </summary>
        private void HandleError(string errorMessage)
        {
            Debug.LogError($"[PillCounter Error] {errorMessage}");
            UpdateStatus($"Error: {errorMessage}");
        }
        
        /// <summary>
        /// 清理资源
        /// </summary>
        private void Cleanup()
        {
            if (webCamTexture != null)
            {
                webCamTexture.Stop();
                Destroy(webCamTexture);
                webCamTexture = null;
            }
            
            if (displayTexture != null)
            {
                Destroy(displayTexture);
                displayTexture = null;
            }
            
            if (frameMat != null)
            {
                frameMat.Dispose();
                frameMat = null;
            }
            
            if (displayMat != null)
            {
                displayMat.Dispose();
                displayMat = null;
            }
            
            if (pillCounter != null)
            {
                pillCounter.Dispose();
                pillCounter = null;
            }
            
            Debug.Log("Pill counter resources cleaned up");
        }
        
        /// <summary>
        /// 获取当前药片数量（供外部调用）
        /// </summary>
        public int GetCurrentPillCount()
        {
            return currentPillCount;
        }
        
        /// <summary>
        /// 获取显示纹理（供校准对话框等外部组件使用）
        /// </summary>
        public Texture DisplayTexture => displayTexture;
        
        /// <summary>
        /// 检查背景是否已捕捉（供外部调用）
        /// </summary>
        public bool IsBackgroundCaptured()
        {
            return pillCounter != null && pillCounter.IsBackgroundCaptured;
        }
        
        /// <summary>
        /// 手动触发背景捕捉（供外部调用）
        /// </summary>
        public void CaptureBackground()
        {
            OnCaptureBackgroundClicked();
        }
        
        /// <summary>
        /// 手动触发背景重置（供外部调用）
        /// </summary>
        public void ResetBackground()
        {
            OnResetBackgroundClicked();
        }
        
        /// <summary>
        /// Try to calibrate using a single pill on the counting tray.
        /// Returns the detected pixel area if exactly one pill is found.
        /// Used for pill size calibration before dispensing uncalibrated medicines.
        /// </summary>
        /// <returns>
        /// Tuple of (success, pixelArea, message):
        /// - success: true if exactly one pill was detected
        /// - pixelArea: detected area in pixels (use PillCalibrationManager to convert to mm²)
        /// - message: status message for UI display
        /// </returns>
        public (bool success, float pixelArea, string message) TryCalibrateSinglePill()
        {
            if (pillCounter == null)
            {
                return (false, 0f, "计数器未初始化");
            }
            
            if (frameMat == null || frameMat.IsDisposed)
            {
                return (false, 0f, "摄像头未就绪");
            }
            
            // Use current frame for calibration
            return pillCounter.TryCalibrateSinglePill(frameMat);
        }
        
        /// <summary>
        /// Get the current camera frame's Mat for external processing.
        /// Returns null if camera is not ready.
        /// </summary>
        public Mat GetCurrentFrameMat()
        {
            return (frameMat != null && !frameMat.IsDisposed) ? frameMat : null;
        }
    }
}
