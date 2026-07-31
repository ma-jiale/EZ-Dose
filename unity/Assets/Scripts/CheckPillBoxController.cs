using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using ZXing;
using ZXing.Common;
using EZDose;

namespace EZDose.CheckPillBox
{
    /// <summary>
    /// barcode/QR scanner to verify the pill box before dispensing.
    /// </summary>
    public class CheckPillBoxController : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private int cameraIndex = -1; // -1 = auto-detect front camera on first use
        [SerializeField] private int requestedWidth = 1280;
        [SerializeField] private int requestedHeight = 720;
        [SerializeField] private int requestedFps = 30;

        [Header("UI")]
        [SerializeField] private RawImage preview;
        [SerializeField] private Text statusText;

        [Header("Scan settings")]
        [SerializeField] private float scanInterval = 0.1f; // seconds between decode attempts

        private WebCamTexture webCamTexture;
        private BarcodeReader barcodeReader;
        private bool isScanning;
        private bool hasSelectedCamera;
        private string expectedPatientId; // We expect box id == patient id
        private float lastValidCameraFrameRealtime = -1f;

        // The scanner object belongs to a scene, but the physical camera does not.
        // Remember the camera that actually decoded the pill-box code so the
        // completion scene monitors the same camera at the end of the track.
        private static string lastBarcodeCameraName;

        /// <summary>
        /// Returns true if the currently active camera is front-facing.
        /// </summary>
        public bool IsFrontFacing
        {
            get
            {
                var devices = WebCamTexture.devices;
                if (devices != null && cameraIndex >= 0 && cameraIndex < devices.Length)
                    return devices[cameraIndex].isFrontFacing;
                return false;
            }
        }

        // Events for integration with the main dispensing flow
        public event Action<string> OnBoxVerified;      // called with decoded text when it matches expectation
        public event Action<string> OnBoxMismatch;      // called when decoded text is valid but not expected
        public event Action<string> OnScanError;        // called on errors (camera or decode)

        private void Awake()
        {
            // Configure barcode reader optimized for Code128 barcodes
            barcodeReader = new BarcodeReader
            {
                AutoRotate = true,
                Options = new DecodingOptions
                {
                    // Try harder for better accuracy (trades speed for reliability)
                    TryHarder = true,
                    
                    // Specify Code128 as primary format for faster, more accurate scanning
                    PossibleFormats = new[]
                    {
                        BarcodeFormat.CODE_128,
                        BarcodeFormat.CODE_39,
                        BarcodeFormat.QR_CODE
                    }
                }
            };
        }

        private void OnDestroy()
        {
            StopScanner();
        }

        private void OnDisable()
        {
            StopScanner();
        }

        private void Update()
        {
            // didUpdateThisFrame is easy to miss from a coroutine that only wakes
            // every 0.2 seconds. Capture it every Unity frame, then let the removal
            // loop consume the latest valid image just like the original scanner.
            if (webCamTexture != null &&
                webCamTexture.isPlaying &&
                webCamTexture.didUpdateThisFrame &&
                webCamTexture.width > 16 &&
                webCamTexture.height > 16)
            {
                lastValidCameraFrameRealtime = Time.realtimeSinceStartup;
            }
        }

        /// <summary>
        /// Start scanning; we expect the box code to equal the patient ID.
        /// </summary>
        public void StartScanner(string expectedPatientId, string expectedPatientName = "")
        {
            this.expectedPatientId = expectedPatientId;

            // Ensure valid values for dynamically added components at runtime
            if (requestedWidth <= 0) requestedWidth = 1280;
            if (requestedHeight <= 0) requestedHeight = 720;
            if (requestedFps <= 0) requestedFps = 30;
            if (!hasSelectedCamera)
            {
                cameraIndex = -1;
            }

            if (isScanning)
                return;

            try
            {
                StartCamera();
                isScanning = true;
                StartCoroutine(ScanLoop());
                if (!string.IsNullOrEmpty(expectedPatientName))
                {
                    SetStatus($"请放入【{expectedPatientName}】的药盘...");
                }
                else
                {
                    SetStatus("Scanning box code...");
                }
            }
            catch (Exception e)
            {
                SetStatus("Camera error.");
                OnScanError?.Invoke(e.Message);
            }
        }

        /// <summary>
        /// Stop scanning and release the camera.
        /// </summary>
        public void StopScanner()
        {
            isScanning = false;

            if (webCamTexture != null)
            {
                if (webCamTexture.isPlaying)
                {
                    webCamTexture.Stop();
                }

                if (preview != null && preview.texture == webCamTexture)
                {
                    preview.texture = null;
                }

                Destroy(webCamTexture);
                webCamTexture = null;
                lastValidCameraFrameRealtime = -1f;
                EZLog.I(EZLog.Module.Scanner, "Camera released.");
            }
        }

        /// <summary>
        /// Switches between available cameras (front/back) without stopping the scan loop.
        /// </summary>
        public void SwitchCamera()
        {
            var devices = WebCamTexture.devices;
            if (devices == null || devices.Length < 2)
            {
                EZLog.W(EZLog.Module.Scanner, "Only one camera available, cannot switch.");
                return;
            }

            // Stop current camera (but do NOT set isScanning = false)
            if (webCamTexture != null)
            {
                if (webCamTexture.isPlaying)
                    webCamTexture.Stop();
                Destroy(webCamTexture);
                webCamTexture = null;
            }

            // Cycle to next camera
            if (!hasSelectedCamera || cameraIndex < 0 || cameraIndex >= devices.Length)
            {
                cameraIndex = GetPreferredCameraIndex(devices);
            }

            cameraIndex = (cameraIndex + 1) % devices.Length;
            hasSelectedCamera = true;
            EZLog.I(EZLog.Module.Scanner, $"Switching to camera [{cameraIndex}]: {devices[cameraIndex].name} (front={devices[cameraIndex].isFrontFacing})");

            // Restart with new camera
            StartCamera();
        }

        private void StartCamera()
        {
            if (requestedWidth <= 0) requestedWidth = 1280;
            if (requestedHeight <= 0) requestedHeight = 720;
            if (requestedFps <= 0) requestedFps = 30;

            var devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
                throw new Exception("No camera found");

            // A new scanner is created after changing scenes. Reuse the camera that
            // decoded this pill box; otherwise auto-detection can select a different
            // USB camera and never observe the code at the end of the track.
            if (!hasSelectedCamera || cameraIndex < 0 || cameraIndex >= devices.Length)
            {
                int rememberedCameraIndex = FindCameraIndex(devices, lastBarcodeCameraName);
                cameraIndex = rememberedCameraIndex >= 0
                    ? rememberedCameraIndex
                    : GetPreferredCameraIndex(devices);
                hasSelectedCamera = true;
                string selectionReason = rememberedCameraIndex >= 0
                    ? "reused barcode camera"
                    : "auto-selected camera";
                EZLog.I(EZLog.Module.Scanner,
                    $"{selectionReason} [{cameraIndex}]: {devices[cameraIndex].name} (front={devices[cameraIndex].isFrontFacing})");
            }

            lastValidCameraFrameRealtime = -1f;
            webCamTexture = new WebCamTexture(devices[cameraIndex].name, requestedWidth, requestedHeight, requestedFps);
            webCamTexture.Play();

            if (preview != null)
            {
                preview.texture = webCamTexture;
            }
        }

        private static int FindCameraIndex(WebCamDevice[] devices, string cameraName)
        {
            if (devices == null || string.IsNullOrWhiteSpace(cameraName))
            {
                return -1;
            }

            for (int i = 0; i < devices.Length; i++)
            {
                if (string.Equals(devices[i].name, cameraName, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        private void RememberCurrentBarcodeCamera()
        {
            var devices = WebCamTexture.devices;
            if (devices == null || cameraIndex < 0 || cameraIndex >= devices.Length)
            {
                return;
            }

            string cameraName = devices[cameraIndex].name;
            if (string.Equals(lastBarcodeCameraName, cameraName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            lastBarcodeCameraName = cameraName;
            EZLog.I(EZLog.Module.Scanner,
                $"Remembered barcode camera [{cameraIndex}]: {lastBarcodeCameraName}");
        }

        private static int GetPreferredCameraIndex(WebCamDevice[] devices)
        {
            // 1. 优先寻找名称中包含 "usb" 的外接摄像头
            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i].name.IndexOf("usb", StringComparison.OrdinalIgnoreCase) >= 0)
                    return i;
            }

            // 2. 其次寻找前置摄像头
            for (int i = 0; i < devices.Length; i++)
            {
                if (devices[i].isFrontFacing)
                    return i;
            }

            return 0;
        }

        private IEnumerator ScanLoop()
        {
            var wait = new WaitForSeconds(scanInterval);

            while (isScanning)
            {
                if (webCamTexture == null || !webCamTexture.isPlaying)
                {
                    SetStatus("Camera stopped.");
                    yield return wait;
                    continue;
                }

                // Wait until camera has updated this frame
                if (!webCamTexture.didUpdateThisFrame)
                {
                    yield return null;
                    continue;
                }

                try
                {
                    // Get actual dimensions from camera (may differ from requested)
                    int width = webCamTexture.width;
                    int height = webCamTexture.height;
                    
                    // Allocate color array with correct size
                    Color32[] colors = webCamTexture.GetPixels32();
                    
                    // Decode with actual camera dimensions
                    var result = barcodeReader.Decode(colors, width, height);

                    if (result != null)
                    {
                        RememberCurrentBarcodeCamera();
                        HandleDecode(result.Text);
                        // Stop after a decision; main flow can restart if needed.
                        yield break;
                    }
                }
                catch (Exception e)
                {
                    SetStatus("Decode error.");
                    OnScanError?.Invoke(e.Message);
                }

                yield return wait;
            }
        }

        public static string ParsePatientIdFromBarcode(string decoded)
        {
            if (string.IsNullOrEmpty(decoded)) return "";
            var parsedId = decoded;

            if (decoded.Contains(":"))
            {
                var parts = decoded.Split('|');
                foreach (var part in parts)
                {
                    if (part.StartsWith("PID:", StringComparison.OrdinalIgnoreCase))
                        parsedId = part.Substring(4).Trim();
                    else if (part.StartsWith("BOX:", StringComparison.OrdinalIgnoreCase))
                        parsedId = part.Substring(4).Trim();
                }
            }
            return parsedId;
        }

        private void HandleDecode(string decoded)
        {
            // We expect the box code to carry the patient id. Accept forms: "PID:<id>", "BOX:<id>", or raw "<id>".
            var parsedId = ParsePatientIdFromBarcode(decoded);

            var matches = string.IsNullOrEmpty(expectedPatientId)
                ? true
                : string.Equals(parsedId, expectedPatientId, StringComparison.OrdinalIgnoreCase);

            if (matches)
            {
                SetStatus("Box verified. You can continue.");
                OnBoxVerified?.Invoke(decoded);
            }
            else
            {
                SetStatus("Box does not match patient.");
                OnBoxMismatch?.Invoke(decoded);
            }

            StopScanner();
        }

        public void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            EZLog.I(EZLog.Module.Scanner, message);
        }

        private bool isWaitingForNoBarcode = false;
        private float noBarcodeWaitTimer = 0f;
        private float targetNoBarcodeDuration = 1f;
        private Action<bool> onNoBarcodeCompleted;

        public void StartWaitingForNoBarcode(float duration, Action<bool> onCompleted)
        {
            if (isWaitingForNoBarcode)
            {
                EZLog.W(EZLog.Module.Scanner, "Barcode removal monitoring is already running");
                onCompleted?.Invoke(false);
                return;
            }

            targetNoBarcodeDuration = duration;
            onNoBarcodeCompleted = onCompleted;
            isWaitingForNoBarcode = true;
            StartCoroutine(WaitForNoBarcodeCoroutine());
        }

        public Task<bool> WaitForNoBarcodeAsync(float durationSeconds)
        {
            var tcs = new TaskCompletionSource<bool>();
            StartWaitingForNoBarcode(durationSeconds, success => {
                tcs.TrySetResult(success);
            });
            return tcs.Task;
        }

        private IEnumerator WaitForNoBarcodeCoroutine()
        {
            if (webCamTexture == null || !webCamTexture.isPlaying)
            {
                try
                {
                    StartCamera();
                }
                catch (Exception e)
                {
                    EZLog.E(EZLog.Module.Scanner, $"Failed to start camera: {e.Message}");
                    isWaitingForNoBarcode = false;
                    var cameraStartFailedCallback = onNoBarcodeCompleted;
                    onNoBarcodeCompleted = null;
                    cameraStartFailedCallback?.Invoke(false);
                    yield break;
                }
            }

            isScanning = true;
            noBarcodeWaitTimer = 0f;
            float cameraFailureTimer = 0f;
            var wait = new WaitForSecondsRealtime(0.2f);
            EZLog.I(EZLog.Module.Scanner,
                $"Started barcode removal monitoring; stable absence required for {targetNoBarcodeDuration:F1}s");

            while (isScanning && isWaitingForNoBarcode)
            {
                bool hasFreshCameraFrame = lastValidCameraFrameRealtime >= 0f &&
                    Time.realtimeSinceStartup - lastValidCameraFrameRealtime <= 2f;
                if (webCamTexture == null || !webCamTexture.isPlaying ||
                    webCamTexture.width <= 16 || webCamTexture.height <= 16 ||
                    !hasFreshCameraFrame)
                {
                    cameraFailureTimer += 0.2f;
                    if (cameraFailureTimer >= 10f)
                    {
                        SetStatus("摄像头画面不可用，无法确认药盒是否取出");
                        break;
                    }
                    yield return wait;
                    continue;
                }

                bool barcodeDetected = false;
                bool decodeSucceeded = true;
                try
                {
                    int width = webCamTexture.width;
                    int height = webCamTexture.height;
                    Color32[] colors = webCamTexture.GetPixels32();
                    var result = barcodeReader.Decode(colors, width, height);
                    if (result != null)
                    {
                        barcodeDetected = true;
                        RememberCurrentBarcodeCamera();
                    }
                }
                catch (Exception e)
                {
                    decodeSucceeded = false;
                    EZLog.D(EZLog.Module.Scanner, $"Error during no-barcode decode check: {e.Message}");
                }

                if (!decodeSucceeded)
                {
                    cameraFailureTimer += 0.2f;
                    if (cameraFailureTimer >= 10f)
                    {
                        SetStatus("二维码识别持续失败，无法确认药盒是否取出");
                        break;
                    }
                    yield return wait;
                    continue;
                }

                cameraFailureTimer = 0f;

                if (barcodeDetected)
                {
                    noBarcodeWaitTimer = 0f;
                    SetStatus("检测到药盘仍在仓内，请取出药盘...");
                }
                else
                {
                    noBarcodeWaitTimer += 0.2f;
                    SetStatus($"请取出药盘... ({Mathf.Max(0f, targetNoBarcodeDuration - noBarcodeWaitTimer):F1}秒后关仓)");
                }

                if (noBarcodeWaitTimer >= targetNoBarcodeDuration)
                {
                    break;
                }

                yield return wait;
            }

            isWaitingForNoBarcode = false;
            bool barcodeRemoved = noBarcodeWaitTimer >= targetNoBarcodeDuration;
            EZLog.I(EZLog.Module.Scanner,
                barcodeRemoved
                    ? "Barcode remained absent; pill box removal confirmed"
                    : "Barcode removal monitoring stopped without confirming removal");
            var completed = onNoBarcodeCompleted;
            onNoBarcodeCompleted = null;
            completed?.Invoke(barcodeRemoved);
        }
    }
}
