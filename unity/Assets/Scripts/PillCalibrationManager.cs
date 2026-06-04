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
        private const float MIN_MOTOR_SPEED = 0f;
        private const float MAX_MOTOR_SPEED = 1.0f;
        private const float MIN_SERVO_ANGLE = 0.1f;
        private const float MAX_SERVO_ANGLE = 1.0f;
        
        // Area range for interpolation
        private const float SMALL_PILL_AREA_MM2 = 13f;   // ~5mm diameter
        private const float LARGE_PILL_AREA_MM2 = 156f;  // ~14mm diameter

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
                EZLog.W(EZLog.Module.Calibration, "Server URL not set");
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
                            EZLog.I(EZLog.Module.Calibration, $"Loaded reference diameter: {referencePillDiameterMm}mm");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                EZLog.E(EZLog.Module.Calibration, "Failed to fetch calibration settings", e);
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
                OnCalibrationError?.Invoke("Invalid detected pixel area");
                return false;
            }

            // Calculate actual area of reference pill (πr²)
            float radius = referencePillDiameterMm / 2f;
            float actualAreaMm2 = Mathf.PI * radius * radius;
            
            // Calculate conversion ratio
            pixelToMm2Ratio = actualAreaMm2 / detectedPixelArea;
            
            // Save to PlayerPrefs
            SaveCalibration();
            
            EZLog.I(EZLog.Module.Calibration, $"System calibrated: {detectedPixelArea}px -> {actualAreaMm2:.2f}mm2 (ratio: {pixelToMm2Ratio:.6f})");
            
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
            EZLog.I(EZLog.Module.Calibration, "Calibration reset");
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
                EZLog.W(EZLog.Module.Calibration, "System not calibrated, cannot convert pixel area");
                return 0f;
            }

            float actualArea = pixelArea * pixelToMm2Ratio;
            
            // Validate result is within reasonable range
            if (actualArea < MIN_VALID_AREA_MM2 || actualArea > MAX_VALID_AREA_MM2)
            {
                EZLog.W(EZLog.Module.Calibration, $"Calculated area {actualArea:.2f}mm2 is outside valid range");
            }
            
            return actualArea;
        }

        /// <summary>
        /// Convert lower optocoupler pulse width to estimated pill area
        /// based on experimental linear interpolation data.
        /// </summary>
        public float CalculateAreaFromPulseWidth(int pulseWidth)
        {
            float length = 10f; // Default

            if (pulseWidth <= 9.5f)
            {
                // Extrapolate below 9.5
                length = Mathf.Lerp(0f, 6.4f, pulseWidth / 9.5f); 
            }
            else if (pulseWidth <= 13.0f)
            {
                // Interpolate between Small Pill (9.5) and White Pill (13.0)
                float t = (pulseWidth - 9.5f) / (13.0f - 9.5f);
                length = Mathf.Lerp(6.4f, 10.0f, t);
            }
            else if (pulseWidth <= 23.0f)
            {
                // Interpolate between White Pill (13.0) and Amoxicillin (23.0)
                float t = (pulseWidth - 13.0f) / (23.0f - 13.0f);
                length = Mathf.Lerp(10.0f, 18.5f, t);
            }
            else
            {
                // Extrapolate above 23.0 based on 13->23 slope (0.85 mm per pulse unit)
                length = 18.5f + (pulseWidth - 23.0f) * 0.85f;
            }

            // Estimate area assuming circular profile
            float radius = length / 2f;
            return Mathf.PI * radius * radius;
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

            
            EZLog.D(EZLog.Module.Calibration, $"Area {pillAreaMm2:.1f}mm2 -> motor={motorSpeed:.2f}, servo={servoAngle:.2f}");
            
            return (motorSpeed, servoAngle);
        }

        /// <summary>
        /// Get dispenser settings for a given pill area, or default Medium settings if area is invalid.
        /// </summary>
        public (float motorSpeed, float servoAngle) GetDispenserSettingsOrDefault(float pillAreaMm2)
        {
            if (pillAreaMm2 <= 0)
            {
                // Default settings
                EZLog.W(EZLog.Module.Calibration, "Using default settings for uncalibrated pill (motor=0.3, servo=0.7)");
                return (0.3f, 0.7f);
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
                EZLog.E(EZLog.Module.Calibration, "Server URL not set for pill size update");
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
                        EZLog.E(EZLog.Module.Calibration, $"Failed to update pill size: {request.error}");
                        return false;
                    }

                    EZLog.I(EZLog.Module.Calibration, $"Updated prescription {prescriptionId} pill size to {pillSizeAreaMm2:.1f}mm2");
                    return true;
                }
            }
            catch (Exception e)
            {
                EZLog.E(EZLog.Module.Calibration, "Exception updating pill size", e);
                return false;
            }
        }

        /// <summary>
        /// Update pill size area and optionally upload pill image for a prescription on the server.
        /// Uses multipart/form-data to upload both pill_size_area and image file.
        /// </summary>
        /// <param name="prescriptionId">Prescription ID to update</param>
        /// <param name="pillSizeAreaMm2">Calibrated pill area in mm²</param>
        /// <param name="imageBytes">Optional JPG image bytes to upload</param>
        /// <returns>Tuple of (success, imageResourceId)</returns>
        public async Task<(bool success, string imageResourceId)> UpdatePillSizeWithImageAsync(
            int prescriptionId, 
            float pillSizeAreaMm2, 
            byte[] imageBytes)
        {
            // Use AppConfig as fallback if serverUrl not initialized
            var url = string.IsNullOrEmpty(serverUrl) ? AppConfig.Instance?.ServerUrl : serverUrl;
            url = url?.TrimEnd('/');
            
            if (string.IsNullOrEmpty(url))
            {
                EZLog.E(EZLog.Module.Calibration, "Server URL not set for calibration update");
                return (false, null);
            }

            try
            {
                // Create multipart form data
                var form = new WWWForm();
                form.AddField("pill_size_area", pillSizeAreaMm2.ToString("F2"));
                
                // Add image if provided
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    form.AddBinaryData("pill_image", imageBytes, "pill_image.jpg", "image/jpeg");
                    EZLog.D(EZLog.Module.Calibration, $"Uploading pill image: {imageBytes.Length} bytes");
                }

                using (var request = UnityWebRequest.Post($"{url}/packer/prescription/{prescriptionId}/calibration", form))
                {
                    request.timeout = 30;

                    var op = request.SendWebRequest();
                    while (!op.isDone)
                    {
                        await Task.Yield();
                    }

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        EZLog.E(EZLog.Module.Calibration, $"Failed to update calibration: {request.error}");
                        return (false, null);
                    }

                    // Parse response to get image_resource_id
                    var response = JsonUtility.FromJson<CalibrationUpdateResponse>(request.downloadHandler.text);
                    if (response != null && response.success)
                    {
                        EZLog.I(EZLog.Module.Calibration, $"Updated prescription {prescriptionId}: area={pillSizeAreaMm2:.1f}mm2, image={response.image_resource_id ?? "none"}");
                        return (true, response.image_resource_id);
                    }

                    EZLog.W(EZLog.Module.Calibration, $"Server returned error: {request.downloadHandler.text}");
                    return (false, null);
                }
            }
            catch (Exception e)
            {
                EZLog.E(EZLog.Module.Calibration, "Exception updating calibration", e);
                return (false, null);
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
                EZLog.E(EZLog.Module.Calibration, "Exception saving reference diameter", e);
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
                EZLog.D(EZLog.Module.Calibration, $"Loaded calibration: ratio={pixelToMm2Ratio:.6f}, ref={referencePillDiameterMm}mm");
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

        [Serializable]
        private class CalibrationUpdateResponse
        {
            public bool success;
            public string message;
            public string image_resource_id;
        }

        #endregion
    }
}
