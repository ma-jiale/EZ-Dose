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
                HandleError($"初始化失败: {e.Message}");
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
            
            UpdateStatus("正在初始化...");
        }
        
        /// <summary>
        /// 初始化摄像头
        /// </summary>
        private void InitializeCamera()
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            
            if (devices.Length == 0)
            {
                throw new Exception("未找到可用的摄像头设备");
            }
            
            // 选择摄像头
            if (cameraIndex >= devices.Length)
            {
                Debug.LogWarning($"摄像头索引 {cameraIndex} 超出范围，使用默认摄像头");
                cameraIndex = 0;
            }
            
            string deviceName = devices[cameraIndex].name;
            Debug.Log($"使用摄像头: {deviceName}");
            
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
                HandleError("摄像头启动超时");
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
            
            UpdateStatus("摄像头已启动，等待捕捉背景...");
            Debug.Log($"摄像头分辨率: {width}x{height}");
        }
        
        /// <summary>
        /// 初始化药片计数器
        /// </summary>
        private void InitializePillCounter()
        {
            pillCounter = new PillCounter();
            Debug.Log("药片计数器已初始化");
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
                HandleError($"处理帧失败: {e.Message}");
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
                // Detect edges and check if scene is stable enough
                var (edgeCount, edges) = pillCounter.DetectEdges(frameMat);
                edges.Dispose();
                
                if (pillCounter.IsSceneStable(edgeCount))
                {
                    // Auto-capture background when stable
                    pillCounter.CaptureBackground(frameMat);
                    UpdateStatus("背景已自动捕捉，开始计数");
                }
                else
                {
                    // Show waiting state - always update preview
                    frameMat.copyTo(displayMat);
                    UpdateStatus($"等待场景稳定... (边缘数: {edgeCount})");
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
                    UpdateStatus("背景已手动捕捉");
                }
            }
            catch (Exception e)
            {
                HandleError($"捕捉背景失败: {e.Message}");
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
                UpdateStatus("背景已重置，等待捕捉新背景...");
            }
            catch (Exception e)
            {
                HandleError($"重置背景失败: {e.Message}");
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
            UpdateStatus($"错误: {errorMessage}");
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
            
            Debug.Log("药片计数器资源已清理");
        }
        
        /// <summary>
        /// 获取当前药片数量（供外部调用）
        /// </summary>
        public int GetCurrentPillCount()
        {
            return currentPillCount;
        }
        
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
    }
}
