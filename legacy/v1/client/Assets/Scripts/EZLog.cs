using System;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace EZDose
{
    /// <summary>
    /// Centralized logging utility for the EZ-Dose application.
    /// Provides structured log output with levels, module tags, and timestamps.
    /// 
    /// Usage:
    ///   EZLog.I(EZLog.Module.Main, "Dispensing started for patient 000042");
    ///   EZLog.D(EZLog.Module.Dispenser, $"Sending {bytes.Length} bytes");
    ///   EZLog.E(EZLog.Module.Network, "Fetch failed", exception);
    /// 
    /// Output format:
    ///   [14:32:05.123] [INFO ] [Main      ] Dispensing started for patient 000042
    /// </summary>
    public static class EZLog
    {
        // =============================================
        // Enums
        // =============================================

        public enum Level
        {
            Verbose = 0,
            Debug = 1,
            Info = 2,
            Warning = 3,
            Error = 4
        }

        public enum Module
        {
            Main,         // MainController - core dispensing flow
            Dispenser,    // DispenserController - Bluetooth / hardware
            Protocol,     // SerialProtocol - packet parsing
            Prescription, // PrescriptionManager - data & plans
            PillCount,    // PillCounter / PillCounterController
            Calibration,  // PillCalibrationManager / PillCalibrationDialog
            Scanner,      // CheckPillBoxController - barcode
            UI,           // UIManager / ConfigurationUI / DeviceManagerUI etc.
            Config,       // AppConfig
            Network,      // PillImageLoader / HTTP requests
        }

        // =============================================
        // Configuration
        // =============================================

        /// <summary>
        /// Global minimum log level. Messages below this level are discarded.
        /// Default: Verbose in Editor/Development builds, Info in Release.
        /// </summary>
        public static Level MinLevel { get; set; }

        // Per-module enable/disable flags (all enabled by default)
        private static readonly Dictionary<Module, bool> moduleEnabled = new Dictionary<Module, bool>();

        // Padded label strings for aligned output
        private static readonly string[] levelLabels = { "VERB", "DEBG", "INFO", "WARN", "EROR" };
        private static readonly Dictionary<Module, string> moduleLabels = new Dictionary<Module, string>();

        // =============================================
        // Static Constructor
        // =============================================

        static EZLog()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            MinLevel = Level.Verbose;
#else
            MinLevel = Level.Info;
#endif
            // Pre-compute padded module labels for aligned output
            int maxLen = 0;
            foreach (Module m in Enum.GetValues(typeof(Module)))
            {
                if (m.ToString().Length > maxLen)
                    maxLen = m.ToString().Length;
            }
            foreach (Module m in Enum.GetValues(typeof(Module)))
            {
                moduleLabels[m] = m.ToString().PadRight(maxLen);
                moduleEnabled[m] = true;
            }
        }

        // =============================================
        // Module Control
        // =============================================

        /// <summary>
        /// Enable or disable logging for a specific module at runtime.
        /// </summary>
        public static void SetModuleEnabled(Module module, bool enabled)
        {
            moduleEnabled[module] = enabled;
        }

        /// <summary>
        /// Check if a specific module is currently enabled.
        /// </summary>
        public static bool IsModuleEnabled(Module module)
        {
            return moduleEnabled.TryGetValue(module, out var enabled) ? enabled : true;
        }

        // =============================================
        // Core Log Methods
        // =============================================

        /// <summary>
        /// Verbose: High-frequency debug data (polling loops, per-frame).
        /// Stripped from Release builds via [Conditional] attribute.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void V(Module module, string message)
        {
            Log(Level.Verbose, module, message);
        }

        /// <summary>
        /// Debug: Detailed diagnostic data (matrix contents, byte dumps).
        /// Stripped from Release builds via [Conditional] attribute.
        /// </summary>
        [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
        public static void D(Module module, string message)
        {
            Log(Level.Debug, module, message);
        }

        /// <summary>
        /// Info: Notable state changes (connected, dispensing started, calibration done).
        /// Always included in builds.
        /// </summary>
        public static void I(Module module, string message)
        {
            Log(Level.Info, module, message);
        }

        /// <summary>
        /// Warning: Recoverable issues (timeout retry, missing config, fallback used).
        /// Always included in builds.
        /// </summary>
        public static void W(Module module, string message)
        {
            Log(Level.Warning, module, message);
        }

        /// <summary>
        /// Error: Failures requiring attention (connection lost, API error).
        /// Always included in builds.
        /// </summary>
        public static void E(Module module, string message)
        {
            Log(Level.Error, module, message);
        }

        /// <summary>
        /// Error with exception: Logs the message and exception details.
        /// </summary>
        public static void E(Module module, string message, Exception exception)
        {
            Log(Level.Error, module, $"{message}: {exception.Message}\n{exception.StackTrace}");
        }

        // =============================================
        // Internal
        // =============================================

        private static void Log(Level level, Module module, string message)
        {
            // Level filter
            if (level < MinLevel) return;

            // Module filter
            if (moduleEnabled.TryGetValue(module, out var enabled) && !enabled) return;

            // Format: [HH:mm:ss.fff] [LEVEL] [Module    ] message
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var levelLabel = levelLabels[(int)level];
            var moduleLabel = moduleLabels.TryGetValue(module, out var label) ? label : module.ToString();
            var formatted = $"[{timestamp}] [{levelLabel}] [{moduleLabel}] {message}";

            // Route to the appropriate Unity log method
            switch (level)
            {
                case Level.Verbose:
                case Level.Debug:
                case Level.Info:
                    Debug.Log(formatted);
                    break;
                case Level.Warning:
                    Debug.LogWarning(formatted);
                    break;
                case Level.Error:
                    Debug.LogError(formatted);
                    break;
            }
        }
    }
}
