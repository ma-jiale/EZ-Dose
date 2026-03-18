using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ZXing;
using ZXing.Common;
using EZDose;

namespace EZDose.CheckPillBox
{
    /// <summary>
    /// Simple barcode/QR scanner to verify the pill box before dispensing.
    /// </summary>
    public class CheckPillBoxController : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private int cameraIndex = 0;
        [SerializeField] private int requestedWidth = 1280;
        [SerializeField] private int requestedHeight = 720;
        [SerializeField] private int requestedFps = 30;

        [Header("UI (optional)")]
        [SerializeField] private RawImage preview;
        [SerializeField] private Text statusText;

        [Header("Scan settings")]
        [SerializeField] private float scanInterval = 0.1f; // seconds between decode attempts

        private WebCamTexture webCamTexture;
        private BarcodeReader barcodeReader;
        private bool isScanning;
        private string expectedPatientId; // We expect box id == patient id

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

        /// <summary>
        /// Start scanning; we expect the box code to equal the patient ID.
        /// </summary>
        public void StartScanner(string expectedPatientId)
        {
            this.expectedPatientId = expectedPatientId;

            if (isScanning)
                return;

            try
            {
                StartCamera();
                isScanning = true;
                StartCoroutine(ScanLoop());
                SetStatus("Scanning box code...");
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
                    webCamTexture.Stop();

                Destroy(webCamTexture);
                webCamTexture = null;
            }
        }

        private void StartCamera()
        {
            var devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0)
                throw new Exception("No camera found");

            if (cameraIndex >= devices.Length)
                cameraIndex = 0;

            webCamTexture = new WebCamTexture(devices[cameraIndex].name, requestedWidth, requestedHeight, requestedFps);
            webCamTexture.Play();

            if (preview != null)
            {
                preview.texture = webCamTexture;
            }
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

        private void HandleDecode(string decoded)
        {
            // We expect the box code to carry the patient id. Accept forms: "PID:<id>", "BOX:<id>", or raw "<id>".
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

            var matches = string.IsNullOrEmpty(expectedPatientId)
                ? false
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

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            EZLog.I(EZLog.Module.Scanner, message);
        }
    }
}
