using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace EZDose.Calibration
{
    /// <summary>
    /// Manages pill size calibration for the dispenser system.
    /// Handles pixel-to-mm² conversion using a reference pill standard.
    /// </summary>
    public class PillCalibrationManager : MonoBehaviour
    {
        // Calibration constants
        private const float DEFAULT_REFERENCE_DIAMETER_MM = 9.0f;
        private const float MIN_VALID_AREA_MM2 = 10f;
        private const float MAX_VALID_AREA_MM2 = 300f;
        
        // Dispenser settings range
        private const float MIN_MOTOR_SPEED = 0.1f;
        private const float MAX_MOTOR_SPEED = 1.4f;
        private const float MIN_SERVO_ANGLE = 0.1f;
        private const float MAX_SERVO_ANGLE = 1.0f;
        
        // Area range for interpolation
        private const float SMALL_PILL_AREA_MM2 = 13f;   // ~5mm diameter
        private const float LARGE_PILL_AREA_MM2 = 200f;  // ~16mm diameter

        [Header("Calibration Settings")]
        [SerializeField] private float referencePillDiameterMm = DEFAULT_REFERENCE_DIAMETER_MM;
        
        // Calibration state (persisted via PlayerPrefs)
        private float pixelToMm2Ratio = 0f;
        
        // Server URL for fetching/saving settings
        private string serverUrl;

        // Events
        public event Action<float> OnSystemCalibrationComplete;  // Returns conversion ratio
        public event Action<string> OnCalibrationError;

        #region Properties
        
        /// <summary>
        /// Whether the system has been calibrated with a reference pill.
        /// </summary>
        public bool IsSystemCalibrated => pixelToMm2Ratio > 0;
        
        /// <summary>
        /// Current reference pill diameter in mm (configurable).
        /// </summary>
        public float ReferencePillDiameterMm
        {
            get => referencePillDiameterMm;
            set => referencePillDiameterMm = Mathf.Max(1f, value);
        }
        
        /// <summary>
        /// Current pixel-to-mm² conversion ratio.
        /// </summary>
        public float PixelToMm2Ratio => pixelToMm2Ratio;
        
        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Load saved calibration from PlayerPrefs
            LoadCalibration();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the calibration manager with server URL.
        /// </summary>
        public void Initialize(string serverUrl)
        {
            this.serverUrl = serverUrl?.TrimEnd('/') ?? string.Empty;
            LoadCalibration();
        }

        /// <summary>
        /// Fetch current calibration settings from server.
        /// </summary>
        public async Task FetchSettingsFromServerAsync()
        {
            if (string.IsNullOrEmpty(serverUrl))
            {
                Debug.LogWarning("[PillCalibrationManager] Server URL not set");
                return;
            }

            try
            {
                using (var request = UnityWebRequest.Get($"{serverUrl}/packer/settings/calibration"))
                {
                    request.timeout = 10;
                    var op = request.SendWebRequest();
                    while (!op.isDone)
                    {
                        await Task.Yield();
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        var response = JsonUtility.FromJson<CalibrationSettingsResponse>(request.downloadHandler.text);
                        if (response != null && response.success)
                        {
                            referencePillDiameterMm = response.data.reference_pill_diameter_mm;
                            Debug.Log($"[PillCalibrationManager] Loaded reference diameter: {referencePillDiameterMm}mm");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PillCalibrationManager] Failed to fetch settings: {e.Message}");
            }
        }

        #endregion

        #region System Calibration

        /// <summary>
        /// Calibrate the system using a detected reference pill pixel area.
        /// This establishes the pixel-to-mm² conversion ratio.
        /// </summary>
        /// <param name="detectedPixelArea">Pixel area detected by pill counter</param>
        /// <returns>True if calibration succeeded</returns>
        public bool CalibrateWithReferencePill(float detectedPixelArea)
        {
            if (detectedPixelArea <= 0)
            {
                OnCalibrationError?.Invoke("检测到的像素面积无效");
                return false;
            }

            // Calculate actual area of reference pill (πr²)
            float radius = referencePillDiameterMm / 2f;
            float actualAreaMm2 = Mathf.PI * radius * radius;
            
            // Calculate conversion ratio
            pixelToMm2Ratio = actualAreaMm2 / detectedPixelArea;
            
            // Save to PlayerPrefs
            SaveCalibration();
            
            Debug.Log($"[PillCalibrationManager] System calibrated: {detectedPixelArea}px → {actualAreaMm2:.2f}mm² (ratio: {pixelToMm2Ratio:.6f})");
            
            OnSystemCalibrationComplete?.Invoke(pixelToMm2Ratio);
            return true;
        }

        /// <summary>
        /// Reset system calibration.
        /// </summary>
        public void ResetCalibration()
        {
            pixelToMm2Ratio = 0f;
            PlayerPrefs.DeleteKey("PillCalibration_Ratio");
            PlayerPrefs.Save();
            Debug.Log("[PillCalibrationManager] Calibration reset");
        }

        #endregion

        #region Medicine Calibration

        /// <summary>
        /// Convert detected pixel area to actual pill area in mm².
        /// Requires system to be calibrated first.
        /// </summary>
        /// <param name="pixelArea">Pixel area from pill counter</param>
        /// <returns>Actual area in mm², or 0 if not calibrated</returns>
        public float ConvertPixelAreaToMm2(float pixelArea)
        {
            if (!IsSystemCalibrated)
            {
                Debug.LogWarning("[PillCalibrationManager] System not calibrated");
                return 0f;
            }

            float actualArea = pixelArea * pixelToMm2Ratio;
            
            // Validate result is within reasonable range
            if (actualArea < MIN_VALID_AREA_MM2 || actualArea > MAX_VALID_AREA_MM2)
            {
                Debug.LogWarning($"[PillCalibrationManager] Calculated area {actualArea:.2f}mm² is outside valid range");
            }
            
            return actualArea;
        }

        #endregion

        #region Dispenser Settings Calculation

        /// <summary>
        /// Calculate dispenser motor speed and servo angle based on pill area.
        /// Uses linear interpolation between small and large pill settings.
        /// </summary>
        /// <param name="pillAreaMm2">Pill area in mm²</param>
        /// <returns>Tuple of (motorSpeed, servoAngle) normalized to 0-1 range</returns>
        public (float motorSpeed, float servoAngle) CalculateDispenserSettings(float pillAreaMm2)
        {
            // Clamp area to valid interpolation range
            float clampedArea = Mathf.Clamp(pillAreaMm2, SMALL_PILL_AREA_MM2, LARGE_PILL_AREA_MM2);
            
            // Calculate interpolation factor (0 = small pill, 1 = large pill)
            float t = Mathf.InverseLerp(SMALL_PILL_AREA_MM2, LARGE_PILL_AREA_MM2, clampedArea);
            
            // Interpolate settings:
            // Small pills: slow speed (0.3), large opening (0.8)
            // Large pills: fast speed (0.8), small opening (0.2)
            float motorSpeed = Mathf.Lerp(MIN_MOTOR_SPEED, MAX_MOTOR_SPEED, t);
            float servoAngle = Mathf.Lerp(MAX_SERVO_ANGLE, MIN_SERVO_ANGLE, t);
            
            Debug.Log($"[PillCalibrationManager] Area {pillAreaMm2:.1f}mm² → motor={motorSpeed:.2f}, servo={servoAngle:.2f}");
            
            return (motorSpeed, servoAngle);
        }

        /// <summary>
        /// Get dispenser settings for a given pill area, or default Medium settings if area is invalid.
        /// </summary>
        public (float motorSpeed, float servoAngle) GetDispenserSettingsOrDefault(float pillAreaMm2)
        {
            if (pillAreaMm2 <= 0)
            {
                // Default to Medium settings
                Debug.LogWarning("[PillCalibrationManager] Using default Medium settings for uncalibrated pill");
                return (0.5f, 0.5f);
            }
            
            return CalculateDispenserSettings(pillAreaMm2);
        }

        #endregion

        #region Server Communication

        /// <summary>
        /// Update pill size area for a prescription on the server.
        /// </summary>
        public async Task<bool> UpdatePillSizeOnServerAsync(int prescriptionId, float pillSizeAreaMm2)
        {
            // Use AppConfig as fallback if serverUrl not initialized
            var url = string.IsNullOrEmpty(serverUrl) ? AppConfig.Instance?.ServerUrl : serverUrl;
            url = url?.TrimEnd('/');
            
            if (string.IsNullOrEmpty(url))
            {
                Debug.LogError("[PillCalibrationManager] Server URL not set");
                return false;
            }

            try
            {
                var payload = new PillSizeUpdatePayload { pill_size_area = pillSizeAreaMm2 };
                var json = JsonUtility.ToJson(payload);
                var body = System.Text.Encoding.UTF8.GetBytes(json);

                using (var request = new UnityWebRequest($"{url}/packer/prescription/{prescriptionId}/pill-size", "POST"))
                {
                    request.uploadHandler = new UploadHandlerRaw(body);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.timeout = 10;

                    var op = request.SendWebRequest();
                    while (!op.isDone)
                    {
                        await Task.Yield();
                    }

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"[PillCalibrationManager] Failed to update pill size: {request.error}");
                        return false;
                    }

                    Debug.Log($"[PillCalibrationManager] Updated prescription {prescriptionId} pill size to {pillSizeAreaMm2:.1f}mm²");
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PillCalibrationManager] Exception updating pill size: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Save reference diameter to server.
        /// </summary>
        public async Task<bool> SaveReferenceDiameterToServerAsync(float diameterMm)
        {
            if (string.IsNullOrEmpty(serverUrl))
            {
                return false;
            }

            try
            {
                var payload = new ReferenceDiameterPayload { reference_pill_diameter_mm = diameterMm };
                var json = JsonUtility.ToJson(payload);
                var body = System.Text.Encoding.UTF8.GetBytes(json);

                using (var request = new UnityWebRequest($"{serverUrl}/packer/settings/calibration", "POST"))
                {
                    request.uploadHandler = new UploadHandlerRaw(body);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.timeout = 10;

                    var op = request.SendWebRequest();
                    while (!op.isDone)
                    {
                        await Task.Yield();
                    }

                    return request.result == UnityWebRequest.Result.Success;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[PillCalibrationManager] Exception saving reference diameter: {e.Message}");
                return false;
            }
        }

        #endregion

        #region Persistence

        private void LoadCalibration()
        {
            pixelToMm2Ratio = PlayerPrefs.GetFloat("PillCalibration_Ratio", 0f);
            referencePillDiameterMm = PlayerPrefs.GetFloat("PillCalibration_RefDiameter", DEFAULT_REFERENCE_DIAMETER_MM);
            
            if (IsSystemCalibrated)
            {
                Debug.Log($"[PillCalibrationManager] Loaded calibration: ratio={pixelToMm2Ratio:.6f}, ref={referencePillDiameterMm}mm");
            }
        }

        private void SaveCalibration()
        {
            PlayerPrefs.SetFloat("PillCalibration_Ratio", pixelToMm2Ratio);
            PlayerPrefs.SetFloat("PillCalibration_RefDiameter", referencePillDiameterMm);
            PlayerPrefs.Save();
        }

        #endregion

        #region JSON Data Classes

        [Serializable]
        private class CalibrationSettingsResponse
        {
            public bool success;
            public CalibrationSettingsData data;
        }

        [Serializable]
        private class CalibrationSettingsData
        {
            public float reference_pill_diameter_mm;
        }

        [Serializable]
        private class PillSizeUpdatePayload
        {
            public float pill_size_area;
        }

        [Serializable]
        private class ReferenceDiameterPayload
        {
            public float reference_pill_diameter_mm;
        }

        #endregion
    }
}
