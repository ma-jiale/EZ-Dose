using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using EZDose.PillCounter;
using EZDose.MainFlow;
using EZDose.Calibration;
using EZDose;

namespace EZDose.UI
{
    /// <summary>
    /// 药片校准对话框 - 新药物分药前的校准流程
    /// 用户必须完成校准才能继续分药
    /// </summary>
    public class PillCalibrationDialog : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject dialogRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text statusText;
        [SerializeField] private RawImage cameraPreview;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button retryButton;

        [Header("Settings")]
        [Tooltip("面积稳定判定时间（秒）")]
        [SerializeField] private float stableTimeRequired = 2f;
        
        [Tooltip("面积变化阈值（像素）")]
        [SerializeField] private float areaChangeThreshold = 50f;

        [Header("Camera Preview")]
        [Tooltip("原始相机画面宽度（像素）")]
        [SerializeField] private float sourceWidth = 1280f;
        
        [Tooltip("原始相机画面高度（像素）")]
        [SerializeField] private float sourceHeight = 720f;
        
        [Tooltip("中心裁剪正方形边长（像素），设为0则使用原始高度")]
        [SerializeField] private float cropSquareSize = 200f;

        [Header("References")]
        [SerializeField] private PillCounterController pillCounterController;
        [SerializeField] private PillCalibrationManager calibrationManager;

        // 检测状态
        private float lastDetectedArea;
        private float stableStartTime;
        private float confirmedPixelArea;
        private Coroutine detectionCoroutine;
        private bool calibrationComplete;
        
        // 图像捕获
        private byte[] capturedImageBytes;

        private void Awake()
        {
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinueClicked);
                continueButton.gameObject.SetActive(false);
            }
            
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(OnRetryClicked);
                // retryButton.gameObject.SetActive(false);
            }
            
            if (dialogRoot != null)
            {
                dialogRoot.SetActive(false);
            }
        }

        /// <summary>
        /// 重试按钮点击 - 重新开始检测
        /// </summary>
        private void OnRetryClicked()
        {
            calibrationComplete = false;
            confirmedPixelArea = 0f;
            
            // 隐藏按钮
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
            }
            // if (retryButton != null)
            // {
            //     retryButton.gameObject.SetActive(false);
            // }
            
            // 重置背景并重新开始检测
            if (pillCounterController != null)
            {
                pillCounterController.ResetBackground();
            }
            
            // 停止旧的协程，开始新的
            if (detectionCoroutine != null)
            {
                StopCoroutine(detectionCoroutine);
            }
            detectionCoroutine = StartCoroutine(DetectionLoop());
            
            EZLog.I(EZLog.Module.Calibration, "Retry calibration requested");
        }

        // 不再使用事件订阅 - UIManager 会直接调用 Show() 方法

        /// <summary>
        /// 显示校准对话框
        /// </summary>
        public void Show(string medicineName, string patientName, string bedNumber)
        {
            calibrationComplete = false;
            confirmedPixelArea = 0f;
            
            // 显示对话框
            if (dialogRoot != null)
            {
                dialogRoot.SetActive(true);
            }
            
            // 设置标题: "阿莫西林是303床李爷爷的新药物"
            if (titleText != null)
            {
                string bedInfo = string.IsNullOrEmpty(bedNumber) ? "" : $"{bedNumber}床";
                titleText.text = $"{medicineName}是{bedInfo}{patientName}的新药物";
            }
            
            // 设置初始状态
            if (statusText != null)
            {
                statusText.text = "请放置一片药片到药盘中央以记录药物信息";
            }
            
            // 隐藏继续按钮
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
            }
            
            // 重置数药器背景
            if (pillCounterController != null)
            {
                pillCounterController.ResetBackground();
            }
            
            // 设置预览UV为中央正方形裁剪
            SetupCameraPreviewCrop();
            
            // 开始检测
            detectionCoroutine = StartCoroutine(DetectionLoop());
        }

        /// <summary>
        /// 检测循环 - 等待单颗药片并检测稳定性
        /// </summary>
        private IEnumerator DetectionLoop()
        {
            // 等待背景捕捉
            if (statusText != null)
            {
                statusText.text = "正在初始化相机...";
            }
            
            while (pillCounterController != null && !pillCounterController.IsBackgroundCaptured())
            {
                // 同步纹理到预览
                UpdateCameraPreview();
                yield return new WaitForSeconds(0.2f);
            }
            
            if (statusText != null)
            {
                statusText.text = "请放置一片药片到药盘中央以记录药物信息";
            }
            
            lastDetectedArea = 0f;
            stableStartTime = 0f;
            
            // 持续检测
            while (!calibrationComplete)
            {
                if (pillCounterController == null)
                {
                    yield return new WaitForSeconds(0.5f);
                    continue;
                }
                
                var (success, pixelArea, message) = pillCounterController.TryCalibrateSinglePill();
                
                if (success && pixelArea > 0)
                {
                    // 检查面积是否稳定
                    float areaDiff = Mathf.Abs(pixelArea - lastDetectedArea);
                    
                    if (areaDiff < areaChangeThreshold && lastDetectedArea > 0)
                    {
                        // 面积稳定中
                        if (stableStartTime == 0f)
                        {
                            stableStartTime = Time.time;
                        }
                        
                        float stableTime = Time.time - stableStartTime;
                        float remaining = stableTimeRequired - stableTime;
                        
                        if (statusText != null)
                        {
                            if (remaining > 0)
                            {
                                statusText.text = $"检测到药片，请保持静止... {remaining:F1}秒";
                            }
                        }
                        
                        // 稳定时间达到要求
                        if (stableTime >= stableTimeRequired)
                        {
                            OnCalibrationSuccess(pixelArea);
                            yield break;
                        }
                    }
                    else
                    {
                        // 面积变化，重新计时
                        stableStartTime = 0f;
                        lastDetectedArea = pixelArea;
                        
                        if (statusText != null)
                        {
                            statusText.text = "检测到药片，请保持静止...";
                        }
                    }
                }
                else
                {
                    // 未检测到单颗药片
                    stableStartTime = 0f;
                    lastDetectedArea = 0f;
                    
                    if (statusText != null)
                    {
                        statusText.text = message;
                    }
                }
                
                // 更新预览
                UpdateCameraPreview();
                yield return new WaitForSeconds(0.3f);
            }
        }

        /// <summary>
        /// 更新相机预览（同步纹理）
        /// </summary>
        private void UpdateCameraPreview()
        {
            if (cameraPreview == null || pillCounterController == null)
            {
                return;
            }
            
            // 使用 PillCounterController 的 DisplayTexture 属性
            var texture = pillCounterController.DisplayTexture;
            if (texture != null)
            {
                cameraPreview.texture = texture;
            }
        }

        /// <summary>
        /// 设置相机预览的中心正方形裁剪
        /// </summary>
        private void SetupCameraPreviewCrop()
        {
            if (cameraPreview == null)
            {
                return;
            }

            // 确定裁剪尺寸（如果 cropSquareSize <= 0，则使用原始高度作为正方形边长）
            float squareSize = cropSquareSize > 0 ? cropSquareSize : sourceHeight;
            
            // 确保裁剪尺寸不超过原始尺寸
            squareSize = Mathf.Min(squareSize, Mathf.Min(sourceWidth, sourceHeight));
            
            // 计算 UV 坐标比例
            float cropWidthRatio = squareSize / sourceWidth;   // 宽度占原始画面的比例
            float cropHeightRatio = squareSize / sourceHeight; // 高度占原始画面的比例
            
            // 计算中心偏移
            float offsetX = (1f - cropWidthRatio) / 2f;
            float offsetY = (1f - cropHeightRatio) / 2f;
            
            // UV Rect: x=左偏移, y=下偏移, width=宽度比例, height=高度比例
            cameraPreview.uvRect = new UnityEngine.Rect(offsetX, offsetY, cropWidthRatio, cropHeightRatio);
            
            EZLog.D(EZLog.Module.Calibration, $"Camera crop: {squareSize}x{squareSize} from {sourceWidth}x{sourceHeight}, UV offset=({offsetX:F3}, {offsetY:F3})");
        }

        /// <summary>
        /// 校准成功
        /// </summary>
        private void OnCalibrationSuccess(float pixelArea)
        {
            calibrationComplete = true;
            confirmedPixelArea = pixelArea;
            
            // 捕获当前画面的中央正方形区域作为药片图像
            capturedImageBytes = CaptureSquareImage();
            
            if (statusText != null)
            {
                statusText.text = "保存药物信息完成，请继续分药";
            }
            
            // 显示继续按钮和重试按钮
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
            }
            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(true);
            }
            
            EZLog.I(EZLog.Module.Calibration, $"Calibration complete: {pixelArea:F0} pixels, image captured: {capturedImageBytes?.Length ?? 0} bytes");
        }

        /// <summary>
        /// 捕获中央正方形区域的图像 (200x200 JPG)
        /// </summary>
        /// <returns>JPG 编码的图像字节数组</returns>
        private byte[] CaptureSquareImage()
        {
            if (pillCounterController == null)
            {
                EZLog.W(EZLog.Module.Calibration, "PillCounterController not available for image capture");
                return null;
            }
            
            var sourceTexture = pillCounterController.DisplayTexture;
            if (sourceTexture == null)
            {
                EZLog.W(EZLog.Module.Calibration, "No display texture available for image capture");
                return null;
            }
            
            try
            {
                // 计算裁剪区域 - 从中心取 cropSquareSize x cropSquareSize 区域
                int srcWidth = sourceTexture.width;
                int srcHeight = sourceTexture.height;
                float squareSize = cropSquareSize > 0 ? cropSquareSize : Mathf.Min(srcWidth, srcHeight);
                squareSize = Mathf.Min(squareSize, Mathf.Min(srcWidth, srcHeight));
                
                int cropSize = Mathf.RoundToInt(squareSize);
                int startX = (srcWidth - cropSize) / 2;
                int startY = (srcHeight - cropSize) / 2;
                
                // 创建临时 RenderTexture 用于读取像素
                RenderTexture tempRT = RenderTexture.GetTemporary(srcWidth, srcHeight);
                Graphics.Blit(sourceTexture, tempRT);
                
                RenderTexture prevRT = RenderTexture.active;
                RenderTexture.active = tempRT;
                
                // 创建裁剪后的纹理
                Texture2D croppedTexture = new Texture2D(cropSize, cropSize, TextureFormat.RGB24, false);
                croppedTexture.ReadPixels(new UnityEngine.Rect(startX, startY, cropSize, cropSize), 0, 0);
                croppedTexture.Apply();
                
                RenderTexture.active = prevRT;
                RenderTexture.ReleaseTemporary(tempRT);
                
                // 缩放到 200x200
                const int targetSize = 200;
                Texture2D resizedTexture = ResizeTexture(croppedTexture, targetSize, targetSize);
                UnityEngine.Object.Destroy(croppedTexture);
                
                // 编码为 JPG
                byte[] jpgBytes = resizedTexture.EncodeToJPG(85);
                UnityEngine.Object.Destroy(resizedTexture);
                
                EZLog.D(EZLog.Module.Calibration, $"Captured {targetSize}x{targetSize} JPG image: {jpgBytes.Length} bytes");
                return jpgBytes;
            }
            catch (System.Exception e)
            {
                EZLog.E(EZLog.Module.Calibration, "Failed to capture pill image", e);
                return null;
            }
        }
        
        /// <summary>
        /// 缩放纹理到指定尺寸
        /// </summary>
        private Texture2D ResizeTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            RenderTexture rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
            Graphics.Blit(source, rt);
            
            RenderTexture prevRT = RenderTexture.active;
            RenderTexture.active = rt;
            
            Texture2D result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);
            result.ReadPixels(new UnityEngine.Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply();
            
            RenderTexture.active = prevRT;
            RenderTexture.ReleaseTemporary(rt);
            
            return result;
        }

        /// <summary>
        /// 继续按钮点击 - 开始分药
        /// </summary>
        private void OnContinueClicked()
        {
            // 隐藏对话框
            if (dialogRoot != null)
            {
                dialogRoot.SetActive(false);
            }
            
            // 停止检测协程
            if (detectionCoroutine != null)
            {
                StopCoroutine(detectionCoroutine);
                detectionCoroutine = null;
            }
            
            // 视觉校准已从主流程剔除，此处保留交互日志
            EZLog.I(EZLog.Module.Calibration, $"Visual calibration confirmed with pixel area {confirmedPixelArea:F0}");
        }
    }
}



