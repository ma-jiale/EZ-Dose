using UnityEngine;
using System;

namespace EZDose
{
    /// <summary>
    /// Singleton class to manage application configuration settings.
    /// Persists settings using PlayerPrefs.
    /// </summary>
    public class AppConfig
    {
        private static AppConfig instance;
        public static AppConfig Instance => instance ?? (instance = new AppConfig());

        // PlayerPrefs keys
        private const string KEY_SERVER_URL = "AppConfig_ServerUrl";
        private const string KEY_MAX_DISPENSING_DAYS = "AppConfig_MaxDispensingDays";
        private const string KEY_EXPIRY_DAYS_THRESHOLD = "AppConfig_ExpiryDaysThreshold";

        // Default values
        private const string DEFAULT_SERVER_URL = "http://127.0.0.1:5000";
        private const int DEFAULT_MAX_DISPENSING_DAYS = 7;
        private const int DEFAULT_EXPIRY_DAYS_THRESHOLD = 2;

        // Properties
        public string ServerUrl { get; private set; }
        
        /// <summary>
        /// Maximum number of days to dispense at once (e.g., 7 days of pills).
        /// </summary>
        public int MaxDispensingDays { get; private set; }
        
        /// <summary>
        /// Days before medicine expiry to trigger dispensing reminder.
        /// Medicines expiring within this many days will be flagged for dispensing.
        /// </summary>
        public int ExpiryDaysThreshold { get; private set; }

        private AppConfig()
        {
            Load();
        }

        public void Load()
        {
            ServerUrl = PlayerPrefs.GetString(KEY_SERVER_URL, DEFAULT_SERVER_URL);
            MaxDispensingDays = PlayerPrefs.GetInt(KEY_MAX_DISPENSING_DAYS, DEFAULT_MAX_DISPENSING_DAYS);
            ExpiryDaysThreshold = PlayerPrefs.GetInt(KEY_EXPIRY_DAYS_THRESHOLD, DEFAULT_EXPIRY_DAYS_THRESHOLD);
            
            // Safety check: if loaded value is empty (shouldn't happen with default, but good to be safe), revert to default
            if (string.IsNullOrWhiteSpace(ServerUrl))
            {
                ServerUrl = DEFAULT_SERVER_URL;
            }
            
            // Ensure values are in valid range
            if (MaxDispensingDays < 1) MaxDispensingDays = DEFAULT_MAX_DISPENSING_DAYS;
            if (ExpiryDaysThreshold < 0) ExpiryDaysThreshold = DEFAULT_EXPIRY_DAYS_THRESHOLD;
        }

        public bool SaveServerUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Debug.LogWarning("[AppConfig] Cannot save empty Server URL.");
                return false;
            }

            url = url.Trim();
            if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
                !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning("[AppConfig] Invalid Server URL format. Must start with http:// or https://");
                return false;
            }

            ServerUrl = url;
            PlayerPrefs.SetString(KEY_SERVER_URL, ServerUrl);
            PlayerPrefs.Save();
            Debug.Log($"[AppConfig] Server URL saved: {ServerUrl}");
            return true;
        }

        /// <summary>
        /// Save dispensing days settings.
        /// </summary>
        /// <param name="maxDays">Maximum days to dispense at once (1-30)</param>
        /// <param name="expiryThreshold">Days before expiry to trigger dispensing (0-14)</param>
        /// <returns>True if saved successfully</returns>
        public bool SaveDispensingSettings(int maxDays, int expiryThreshold)
        {
            // Validate range
            if (maxDays < 1 || maxDays > 30)
            {
                Debug.LogWarning("[AppConfig] MaxDispensingDays must be between 1 and 30.");
                return false;
            }
            
            if (expiryThreshold < 0 || expiryThreshold > 14)
            {
                Debug.LogWarning("[AppConfig] ExpiryDaysThreshold must be between 0 and 14.");
                return false;
            }

            MaxDispensingDays = maxDays;
            ExpiryDaysThreshold = expiryThreshold;
            
            PlayerPrefs.SetInt(KEY_MAX_DISPENSING_DAYS, MaxDispensingDays);
            PlayerPrefs.SetInt(KEY_EXPIRY_DAYS_THRESHOLD, ExpiryDaysThreshold);
            PlayerPrefs.Save();
            
            Debug.Log($"[AppConfig] Dispensing settings saved: maxDays={MaxDispensingDays}, expiryThreshold={ExpiryDaysThreshold}");
            return true;
        }

        public void ResetToDefault()
        {
            ServerUrl = DEFAULT_SERVER_URL;
            MaxDispensingDays = DEFAULT_MAX_DISPENSING_DAYS;
            ExpiryDaysThreshold = DEFAULT_EXPIRY_DAYS_THRESHOLD;
            
            PlayerPrefs.SetString(KEY_SERVER_URL, ServerUrl);
            PlayerPrefs.SetInt(KEY_MAX_DISPENSING_DAYS, MaxDispensingDays);
            PlayerPrefs.SetInt(KEY_EXPIRY_DAYS_THRESHOLD, ExpiryDaysThreshold);
            PlayerPrefs.Save();
        }
    }
}
