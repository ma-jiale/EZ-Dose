using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace EZDose.Hardware
{
    /// <summary>
    /// Represents a single pulse record sample collected during medicine dispensing.
    /// </summary>
    public class PulseRecordSample
    {
        public int SequenceNumber;
        public int PulseWidth;
        public long TimeOffsetMs;
        public long PulseIntervalMs;
        public float UsedMotorSpeed;
        public float UsedServoAngle;
        public bool IsValid;
    }

    /// <summary>
    /// Thread-safe CSV recorder for logging optocoupler pulse data during dispensing.
    /// Used for dataset collection and parameter calibration (e.g. K_motor, K_servo).
    /// </summary>
    public static class PulseDataRecorder
    {
        private static readonly object fileLock = new object();
        private static string cachedCsvPath = null;

        private const string CSV_HEADER = "timestamp,patient_id,patient_name,prescription_id,medicine_name,plate_number,target_pills,pill_seq,pulse_width,time_offset_ms,pulse_interval_ms,used_motor_speed,used_servo_angle,is_valid,dispense_result";

        /// <summary>
        /// Gets the absolute path of the pulse records CSV file.
        /// </summary>
        public static string GetCsvPath()
        {
            if (!string.IsNullOrEmpty(cachedCsvPath))
            {
                return cachedCsvPath;
            }

            string dir;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            try
            {
                dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            catch
            {
                dir = Path.Combine(Application.persistentDataPath, "Logs");
            }
#elif UNITY_EDITOR
            dir = Path.Combine(Application.dataPath, "..", "Logs");
#else
            dir = Path.Combine(Application.persistentDataPath, "Logs");
#endif

            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            catch (Exception ex)
            {
                EZLog.E(EZLog.Module.Dispenser, "Failed to create pulse logs directory", ex);
            }

            cachedCsvPath = Path.Combine(dir, "pulse_records.csv");
            return cachedCsvPath;
        }

        /// <summary>
        /// Record all pulse samples for a single medicine dispensing session into the CSV file.
        /// </summary>
        public static void RecordSession(
            string patientId,
            string patientName,
            int prescriptionId,
            string medicineName,
            int plateNumber,
            int targetPills,
            float usedMotorSpeed,
            float usedServoAngle,
            string dispenseResult,
            IReadOnlyList<PulseRecordSample> samples)
        {
            string csvPath = GetCsvPath();
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            var sb = new StringBuilder();

            if (samples != null && samples.Count > 0)
            {
                foreach (var sample in samples)
                {
                    float speed = sample.UsedMotorSpeed > 0f ? sample.UsedMotorSpeed : usedMotorSpeed;
                    float angle = sample.UsedServoAngle > 0f ? sample.UsedServoAngle : usedServoAngle;

                    sb.Append(timestamp).Append(',');
                    sb.Append(EscapeCsv(patientId)).Append(',');
                    sb.Append(EscapeCsv(patientName)).Append(',');
                    sb.Append(prescriptionId).Append(',');
                    sb.Append(EscapeCsv(medicineName)).Append(',');
                    sb.Append(plateNumber).Append(',');
                    sb.Append(targetPills).Append(',');
                    sb.Append(sample.SequenceNumber).Append(',');
                    sb.Append(sample.PulseWidth).Append(',');
                    sb.Append(sample.TimeOffsetMs).Append(',');
                    sb.Append(sample.PulseIntervalMs).Append(',');
                    sb.Append(speed.ToString("F2")).Append(',');
                    sb.Append(angle.ToString("F2")).Append(',');
                    sb.Append(sample.IsValid ? "true" : "false").Append(',');
                    sb.AppendLine(EscapeCsv(dispenseResult));
                }
            }
            else
            {
                // If skipped or finished with 0 samples, write a single summary line
                sb.Append(timestamp).Append(',');
                sb.Append(EscapeCsv(patientId)).Append(',');
                sb.Append(EscapeCsv(patientName)).Append(',');
                sb.Append(prescriptionId).Append(',');
                sb.Append(EscapeCsv(medicineName)).Append(',');
                sb.Append(plateNumber).Append(',');
                sb.Append(targetPills).Append(',');
                sb.Append(0).Append(',');
                sb.Append(0).Append(',');
                sb.Append(0).Append(',');
                sb.Append(0).Append(',');
                sb.Append(usedMotorSpeed.ToString("F2")).Append(',');
                sb.Append(usedServoAngle.ToString("F2")).Append(',');
                sb.Append("false").Append(',');
                sb.AppendLine(EscapeCsv(dispenseResult));
            }

            lock (fileLock)
            {
                try
                {
                    bool fileExists = File.Exists(csvPath);
                    using (var writer = new StreamWriter(csvPath, append: true, encoding: Encoding.UTF8))
                    {
                        if (!fileExists || new FileInfo(csvPath).Length == 0)
                        {
                            writer.WriteLine(CSV_HEADER);
                        }
                        writer.Write(sb.ToString());
                        writer.Flush();
                    }
                    EZLog.I(EZLog.Module.Dispenser, $"Saved {samples?.Count ?? 0} pulse samples to CSV: {csvPath}");
                }
                catch (Exception ex)
                {
                    EZLog.E(EZLog.Module.Dispenser, $"Failed to write pulse data to CSV ({csvPath})", ex);
                }
            }
        }

        private static string EscapeCsv(string field)
        {
            if (string.IsNullOrEmpty(field))
            {
                return string.Empty;
            }

            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                return $"\"{field.Replace("\"", "\"\"")}\"";
            }

            return field;
        }
    }
}
