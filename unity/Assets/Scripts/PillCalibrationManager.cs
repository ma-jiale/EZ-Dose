using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace EZDose.Calibration
{
    /// <summary>
    /// Manages dispenser settings (motor speed and servo angle) calibration.
    /// Directly calculates motor speed and servo angle from lower optocoupler pulse width.
    /// </summary>
    public class PillCalibrationManager : MonoBehaviour
    {
        // Dispenser settings range
        public const float MIN_MOTOR_SPEED = 0.1f;
        public const float MAX_MOTOR_SPEED = 1.4f;
        public const float MIN_SERVO_ANGLE = 0.1f;
        public const float MAX_SERVO_ANGLE = 1.0f;

        [Header("脉冲宽度 → 分药参数系数")]
        [Tooltip("转盘速度 = Clamp(avgPulseWidth × K_motor, 0.1, 1.4)")]
        [SerializeField] private float kMotorSpeed = 0.035f;

        [Tooltip("舵机角度 = Clamp(1.0 − avgPulseWidth × K_servo, 0.1, 1.0)")]
        [SerializeField] private float kServoAngle = 0.02f;

        // Server URL for fetching/saving settings
        private string serverUrl;

        // Events
        public event Action<string> OnCalibrationError;

        #region Public Properties

        public float KMotorSpeed
        {
            get => kMotorSpeed;
            set => kMotorSpeed = value;
        }

        public float KServoAngle
        {
            get => kServoAngle;
            set => kServoAngle = value;
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the calibration manager with server URL.
        /// </summary>
        public void Initialize(string serverUrl)
        {
            this.serverUrl = serverUrl?.TrimEnd('/') ?? string.Empty;
        }

        #endregion

        #region Dispenser Settings Calculation

        /// <summary>
        /// Calculate dispenser motor speed and servo angle directly from average pulse width.
        /// Uses linear coefficients configurable via Unity Inspector.
        /// </summary>
        /// <param name="avgPulseWidth">Average optocoupler pulse width</param>
        /// <returns>Tuple of (motorSpeed, servoAngle)</returns>
        public (float motorSpeed, float servoAngle) CalculateSettingsFromPulseWidth(float avgPulseWidth)
        {
            float motorSpeed = Mathf.Clamp(avgPulseWidth * kMotorSpeed, MIN_MOTOR_SPEED, MAX_MOTOR_SPEED);
            float servoAngle = Mathf.Clamp(MAX_SERVO_ANGLE - avgPulseWidth * kServoAngle, MIN_SERVO_ANGLE, MAX_SERVO_ANGLE);

            EZLog.D(EZLog.Module.Calibration, $"Pulse width {avgPulseWidth:.1f} -> motor={motorSpeed:.2f}, servo={servoAngle:.2f}");
            return (motorSpeed, servoAngle);
        }

        /// <summary>
        /// Get dispenser settings for a given prescription.
        /// Returns saved settings if valid (>0), otherwise returns default Medium settings (motor=0.3, servo=0.7).
        /// </summary>
        public (float motorSpeed, float servoAngle) GetSettingsOrDefault(float savedMotorSpeed, float savedServoAngle)
        {
            if (savedMotorSpeed > 0 && savedServoAngle > 0)
            {
                return (savedMotorSpeed, savedServoAngle);
            }

            EZLog.D(EZLog.Module.Calibration, "Using default settings for uncalibrated prescription (motor=0.3, servo=0.7)");
            return (0.3f, 0.7f);
        }

        #endregion

        #region Server Communication

        /// <summary>
        /// Update dispenser settings (motor speed and servo angle) for a prescription on the server.
        /// </summary>
        public async Task<bool> UpdateDispenserSettingsOnServerAsync(int prescriptionId, float motorSpeed, float servoAngle)
        {
            var url = string.IsNullOrEmpty(serverUrl) ? AppConfig.Instance?.ServerUrl : serverUrl;
            url = url?.TrimEnd('/');

            if (string.IsNullOrEmpty(url))
            {
                EZLog.E(EZLog.Module.Calibration, "Server URL not set for dispenser settings update");
                return false;
            }

            try
            {
                var payload = new DispenserSettingsUpdatePayload
                {
                    motor_speed = motorSpeed,
                    servo_angle = servoAngle
                };
                var json = JsonUtility.ToJson(payload);
                var body = System.Text.Encoding.UTF8.GetBytes(json);

                using (var request = new UnityWebRequest($"{url}/packer/prescription/{prescriptionId}/dispenser-settings", "POST"))
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
                        EZLog.E(EZLog.Module.Calibration, $"Failed to update dispenser settings: {request.error}");
                        return false;
                    }

                    EZLog.I(EZLog.Module.Calibration, $"Updated prescription {prescriptionId} settings: motor={motorSpeed:.2f}, servo={servoAngle:.2f}");
                    return true;
                }
            }
            catch (Exception e)
            {
                EZLog.E(EZLog.Module.Calibration, "Exception updating dispenser settings", e);
                return false;
            }
        }

        /// <summary>
        /// Update dispenser settings and optionally upload pill image for a prescription on the server.
        /// Uses multipart/form-data to upload motor_speed, servo_angle and image file.
        /// </summary>
        public async Task<(bool success, string imageResourceId)> UpdateSettingsWithImageAsync(
            int prescriptionId,
            float motorSpeed,
            float servoAngle,
            byte[] imageBytes)
        {
            var url = string.IsNullOrEmpty(serverUrl) ? AppConfig.Instance?.ServerUrl : serverUrl;
            url = url?.TrimEnd('/');

            if (string.IsNullOrEmpty(url))
            {
                EZLog.E(EZLog.Module.Calibration, "Server URL not set for calibration update");
                return (false, null);
            }

            try
            {
                var form = new WWWForm();
                form.AddField("motor_speed", motorSpeed.ToString("F2"));
                form.AddField("servo_angle", servoAngle.ToString("F2"));

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

                    var response = JsonUtility.FromJson<CalibrationUpdateResponse>(request.downloadHandler.text);
                    if (response != null && response.success)
                    {
                        EZLog.I(EZLog.Module.Calibration, $"Updated prescription {prescriptionId}: motor={motorSpeed:.2f}, servo={servoAngle:.2f}, image={response.image_resource_id ?? "none"}");
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

        #endregion

        #region JSON Data Classes

        [Serializable]
        private class DispenserSettingsUpdatePayload
        {
            public float motor_speed;
            public float servo_angle;
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
