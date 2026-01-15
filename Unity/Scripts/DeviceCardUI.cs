using System;
using UnityEngine;
using UnityEngine.UI;
using EZDose.Hardware;

namespace EZDose.UI
{
    /// <summary>
    /// Controls a single device card UI element
    /// Displays device information and handles connect/disconnect actions
    /// </summary>
    public class DeviceCardUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Text displaying device name")]
        [SerializeField] private Text deviceNameText;

        [Tooltip("Text displaying MAC address")]
        [SerializeField] private Text macAddressText;

        [Tooltip("Text displaying signal strength")]
        [SerializeField] private Text signalStrengthText;

        [Tooltip("Image showing signal strength icon")]
        [SerializeField] private Image signalStrengthIcon;

        [Tooltip("Button to connect to this device")]
        [SerializeField] private Button connectButton;

        [Tooltip("Button to disconnect from this device")]
        [SerializeField] private Button disconnectButton;

        [Header("Visual States")]
        [Tooltip("Color for connected state background")]
        [SerializeField] private Color connectedColor = new Color(0.4f, 0.8f, 0.4f, 0.3f);

        [Tooltip("Color for disconnected state background")]
        [SerializeField] private Color disconnectedColor = new Color(1f, 1f, 1f, 0.1f);

        [Tooltip("Background image to colorize based on connection state")]
        [SerializeField] private Image backgroundImage;

        [Header("Paired Device Indicator")]
        [Tooltip("Icon or badge shown for paired devices")]
        [SerializeField] private GameObject pairedBadge;

        // Device data
        private BluetoothDeviceInfo deviceInfo;
        private bool isConnected;

        // Events
        public event Action OnConnectClicked;
        public event Action OnDisconnectClicked;

        #region Initialization

        /// <summary>
        /// Initialize the device card with device information
        /// </summary>
        /// <param name="device">Device information to display</param>
        /// <param name="connected">Whether this device is currently connected</param>
        /// <param name="showSignal">Whether to display signal strength</param>
        public void Initialize(BluetoothDeviceInfo device, bool connected, bool showSignal = true)
        {
            if (device == null)
            {
                Debug.LogError("[DeviceCardUI] Cannot initialize with null device info");
                return;
            }

            deviceInfo = device.Clone();
            isConnected = connected;

            UpdateDeviceInfoDisplay(showSignal);
            UpdateConnectionState(connected);
            SetupButtons();
        }

        private void SetupButtons()
        {
            if (connectButton != null)
            {
                connectButton.onClick.AddListener(() => OnConnectClicked?.Invoke());
            }

            if (disconnectButton != null)
            {
                disconnectButton.onClick.AddListener(() => OnDisconnectClicked?.Invoke());
            }
        }

        #endregion

        #region Display Update

        /// <summary>
        /// Update device information display
        /// </summary>
        private void UpdateDeviceInfoDisplay(bool showSignal)
        {
            // Update device name
            if (deviceNameText != null)
            {
                string displayName = string.IsNullOrEmpty(deviceInfo.DeviceName) 
                    ? "智慧分药机" 
                    : deviceInfo.DeviceName;
                deviceNameText.text = displayName;
            }

            // Update MAC address
            if (macAddressText != null)
            {
                macAddressText.text = deviceInfo.MacAddress;
            }

            // Update signal strength
            if (showSignal && signalStrengthText != null)
            {
                if (deviceInfo.SignalStrength == -1)
                {
                    signalStrengthText.text = "N/A";
                }
                else
                {
                    signalStrengthText.text = $"{deviceInfo.GetSignalStrengthPercent()}%";
                }
            }
            else if (signalStrengthText != null)
            {
                signalStrengthText.gameObject.SetActive(false);
            }

            // Update signal strength icon
            if (showSignal && signalStrengthIcon != null)
            {
                UpdateSignalStrengthIcon();
            }
            else if (signalStrengthIcon != null)
            {
                signalStrengthIcon.gameObject.SetActive(false);
            }

            // Show paired badge if device is paired
            if (pairedBadge != null)
            {
                pairedBadge.SetActive(deviceInfo.IsPaired);
            }
        }

        /// <summary>
        /// Update signal strength icon based on signal quality
        /// </summary>
        private void UpdateSignalStrengthIcon()
        {
            if (signalStrengthIcon == null)
            {
                return;
            }

            int percent = deviceInfo.GetSignalStrengthPercent();
            Color iconColor;

            if (percent >= 70)
            {
                iconColor = Color.green; // Strong signal
            }
            else if (percent >= 40)
            {
                iconColor = Color.yellow; // Medium signal
            }
            else if (percent > 0)
            {
                iconColor = Color.red; // Weak signal
            }
            else
            {
                iconColor = Color.gray; // No signal data
            }

            signalStrengthIcon.color = iconColor;
        }

        /// <summary>
        /// Update connection state (show/hide connect/disconnect buttons)
        /// </summary>
        public void UpdateConnectionState(bool connected)
        {
            isConnected = connected;

            // Toggle button visibility based on connection state
            if (connectButton != null)
            {
                connectButton.gameObject.SetActive(!connected);
            }

            if (disconnectButton != null)
            {
                disconnectButton.gameObject.SetActive(connected);
            }

            // Update background color
            if (backgroundImage != null)
            {
                backgroundImage.color = connected ? connectedColor : disconnectedColor;
            }

            Debug.Log($"[DeviceCardUI] Updated connection state for {deviceInfo.MacAddress}: {(connected ? "Connected" : "Disconnected")}");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get the device information associated with this card
        /// </summary>
        public BluetoothDeviceInfo GetDeviceInfo()
        {
            return deviceInfo?.Clone();
        }

        /// <summary>
        /// Check if this card represents a connected device
        /// </summary>
        public bool IsConnected()
        {
            return isConnected;
        }

        /// <summary>
        /// Update the device name display
        /// </summary>
        public void UpdateDeviceName(string newName)
        {
            if (deviceInfo != null)
            {
                deviceInfo.DeviceName = newName;
                if (deviceNameText != null)
                {
                    deviceNameText.text = newName;
                }
            }
        }

        /// <summary>
        /// Enable or disable interaction with this card
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            if (connectButton != null)
            {
                connectButton.interactable = interactable;
            }

            if (disconnectButton != null)
            {
                disconnectButton.interactable = interactable;
            }
        }

        /// <summary>
        /// Highlight this card (useful for search/filter)
        /// </summary>
        public void SetHighlighted(bool highlighted)
        {
            if (backgroundImage != null)
            {
                Color baseColor = isConnected ? connectedColor : disconnectedColor;
                backgroundImage.color = highlighted 
                    ? new Color(baseColor.r * 1.2f, baseColor.g * 1.2f, baseColor.b * 1.2f, baseColor.a)
                    : baseColor;
            }
        }

        #endregion
    }
}
