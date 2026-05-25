using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using EZDose.Hardware;
using EZDose;

namespace EZDose.UI
{
    /// <summary>
    /// Manages the device management dialog UI
    /// Handles device discovery, display, and connection management
    /// </summary>
    public class DeviceManagerUI : MonoBehaviour
    {
        [Header("Dialog References")]
        [Tooltip("Root GameObject of the device manager dialog")]
        [SerializeField] private GameObject dialogRoot;

        [Tooltip("Button to close the device management dialog")]
        [SerializeField] private Button closeButton;

        [Tooltip("Button to start/refresh device discovery")]
        [SerializeField] private Button refreshButton;

        [Header("Device List")]
        [Tooltip("Container where device cards will be spawned")]
        [SerializeField] private Transform deviceListContainer;

        [Tooltip("Prefab for individual device cards")]
        [SerializeField] private GameObject deviceCardPrefab;

        [Header("Status Display")]
        [Tooltip("Text showing current scanning status")]
        [SerializeField] private Text statusText;
        [Tooltip("Fixed color for the status text")]
        [SerializeField] private Color statusTextColor = Color.gray;





        [Header("Settings")]
        [Tooltip("Automatically start discovery when dialog opens")]
        [SerializeField] private bool autoScanOnOpen = true;



        // Reference to dispenser controller
        private DispenserController dispenserController;

        // List of spawned device card instances
        private readonly List<DeviceCardUI> spawnedCards = new List<DeviceCardUI>();

        // Currently connected device MAC address
        private string connectedDeviceMac;

        // Connection state tracking
        private bool isConnecting = false;

        #region Initialization

        private void Start()
        {
            // Find dispenser controller in scene
            dispenserController = FindObjectOfType<DispenserController>();

            if (dispenserController == null)
            {
                EZLog.E(EZLog.Module.UI, "DispenserController not found in scene");
                return;
            }

            // Subscribe to dispenser events
            SubscribeToEvents();

            // Setup button listeners
            SetupButtons();

        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SetupButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(HideDialog);
            }

            if (refreshButton != null)
            {
                refreshButton.onClick.AddListener(StartDeviceScan);
            }
        }

        private void SubscribeToEvents()
        {
            if (dispenserController == null) return;

            dispenserController.OnDiscoveryStarted += OnDiscoveryStarted;
            dispenserController.OnDevicesFound += OnDevicesFound;
            dispenserController.OnDiscoveryCompleted += OnDiscoveryCompleted;
            dispenserController.OnConnectionStateChanged += OnConnectionStateChanged;
            dispenserController.OnBTError += OnDispenserError;
        }

        private void UnsubscribeFromEvents()
        {
            if (dispenserController == null) return;

            dispenserController.OnDiscoveryStarted -= OnDiscoveryStarted;
            dispenserController.OnDevicesFound -= OnDevicesFound;
            dispenserController.OnDiscoveryCompleted -= OnDiscoveryCompleted;
            dispenserController.OnConnectionStateChanged -= OnConnectionStateChanged;
            dispenserController.OnBTError -= OnDispenserError;
        }

        #endregion

        #region Dialog Control

        /// <summary>
        /// Show the device management dialog
        /// </summary>
        public void ShowDialog()
        {
            if (dialogRoot != null)
            {
                dialogRoot.SetActive(true);
                EZLog.D(EZLog.Module.UI, "Device management dialog opened");

                // Auto-start scan if enabled
                if (autoScanOnOpen)
                {
                    StartDeviceScan();
                }
                else
                {
                    // Just refresh the display with current connection state
                    RefreshConnectionState();
                }
            }
        }

        /// <summary>
        /// Hide the device management dialog
        /// </summary>
        public void HideDialog()
        {
            if (dialogRoot != null)
            {
                dialogRoot.SetActive(false);
                EZLog.D(EZLog.Module.UI, "Device management dialog closed");
            }
        }

        /// <summary>
        /// Toggle dialog visibility
        /// </summary>
        public void ToggleDialog()
        {
            if (dialogRoot != null)
            {
                if (dialogRoot.activeSelf)
                {
                    HideDialog();
                }
                else
                {
                    ShowDialog();
                }
            }
        }

        #endregion

        #region Device Discovery

        /// <summary>
        /// Start scanning for nearby Bluetooth devices
        /// </summary>
        public void StartDeviceScan()
        {
            // Try to find DispenserController if not already cached (it may be in DontDestroyOnLoad)
            if (dispenserController == null)
            {
                dispenserController = FindObjectOfType<DispenserController>();
                if (dispenserController != null)
                {
                    SubscribeToEvents();
                    EZLog.D(EZLog.Module.UI, "Found DispenserController (late binding)");
                }
            }

            if (dispenserController == null)
            {
                EZLog.E(EZLog.Module.UI, "DispenserController is null");
                UpdateStatusText("Error: Dispenser controller not available", Color.red);
                return;
            }

            EZLog.D(EZLog.Module.UI, "Starting device scan");
            ClearDeviceList();
            ShowLoadingState(true);
            UpdateStatusText("正在扫描附近的设备...", Color.yellow);

            dispenserController.StartDeviceDiscovery();
        }

        /// <summary>
        /// Called when device discovery starts
        /// </summary>
        private void OnDiscoveryStarted()
        {
            EZLog.D(EZLog.Module.UI, "Discovery started");
            ShowLoadingState(true);
            UpdateStatusText("正在扫描附近的设备...", Color.yellow);
        }

        /// <summary>
        /// Called when devices are found during discovery
        /// </summary>
        private void OnDevicesFound(List<BluetoothDeviceInfo> devices)
        {
            EZLog.D(EZLog.Module.UI, $"Found {devices.Count} devices");
            
            ClearDeviceList();

            if (devices == null || devices.Count == 0)
            {
                return;
            }

            // Mark currently connected device
            var connectedDevice = dispenserController.GetConnectedDevice();
            if (connectedDevice != null)
            {
                connectedDeviceMac = connectedDevice.MacAddress;
            }

            // Create cards for each device
            foreach (var device in devices)
            {
                CreateDeviceCard(device);
            }

            UpdateStatusText($"发现 {devices.Count} 台设备", Color.green);
        }

        /// <summary>
        /// Called when device discovery completes
        /// </summary>
        private void OnDiscoveryCompleted()
        {
            EZLog.D(EZLog.Module.UI, "Discovery completed");
            ShowLoadingState(false);

            if (spawnedCards.Count == 0)
            {
                UpdateStatusText("没有发现设备", Color.gray);
            }
            else
            {
                UpdateStatusText($"发现 {spawnedCards.Count} 台设备", Color.green);
            }
        }

        #endregion

        #region Device Card Management

        /// <summary>
        /// Create a device card UI element for a discovered device
        /// </summary>
        private void CreateDeviceCard(BluetoothDeviceInfo deviceInfo)
        {
            if (deviceCardPrefab == null || deviceListContainer == null)
            {
                EZLog.E(EZLog.Module.UI, "Device card prefab or container not assigned");
                return;
            }

            // Instantiate card
            GameObject cardObj = Instantiate(deviceCardPrefab, deviceListContainer);
            DeviceCardUI card = cardObj.GetComponent<DeviceCardUI>();

            if (card == null)
            {
                EZLog.E(EZLog.Module.UI, "Device card prefab missing DeviceCardUI component");
                Destroy(cardObj);
                return;
            }

            // Check if this is the connected device
            bool isConnected = !string.IsNullOrEmpty(connectedDeviceMac) && 
                               deviceInfo.MacAddress == connectedDeviceMac;

            // Initialize card with device data
            card.Initialize(deviceInfo, isConnected, false);

            // Subscribe to card events
            card.OnConnectClicked += () => HandleConnectDevice(deviceInfo);
            card.OnDisconnectClicked += () => HandleDisconnectDevice(deviceInfo);

            spawnedCards.Add(card);
        }

        /// <summary>
        /// Clear all device cards from the list
        /// </summary>
        private void ClearDeviceList()
        {
            foreach (var card in spawnedCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }
            spawnedCards.Clear();
        }

        #endregion

        #region Connection Management

        /// <summary>
        /// Handle connect button click on a device card
        /// </summary>
        private void HandleConnectDevice(BluetoothDeviceInfo deviceInfo)
        {
            if (dispenserController == null || deviceInfo == null)
            {
                return;
            }

            if (isConnecting)
            {
                EZLog.W(EZLog.Module.UI, "Already connecting to a device");
                return;
            }

            EZLog.I(EZLog.Module.UI, $"Connecting to {deviceInfo.DeviceName} ({deviceInfo.MacAddress})");
            isConnecting = true;
            UpdateStatusText($"正在连接 {deviceInfo.DeviceName}...", Color.yellow);

            dispenserController.ConnectToDevice(deviceInfo.MacAddress, deviceInfo.DeviceName);
        }

        /// <summary>
        /// Handle disconnect button click on a device card
        /// </summary>
        private void HandleDisconnectDevice(BluetoothDeviceInfo deviceInfo)
        {
            if (dispenserController == null || deviceInfo == null)
            {
                return;
            }

            EZLog.I(EZLog.Module.UI, $"Disconnecting from {deviceInfo.DeviceName} ({deviceInfo.MacAddress})");
            UpdateStatusText($"正在从{deviceInfo.DeviceName}断开连接...", Color.yellow);

            dispenserController.DisconnectCurrentDevice();
        }

        /// <summary>
        /// Called when connection state changes
        /// </summary>
        private void OnConnectionStateChanged(string state)
        {
            EZLog.D(EZLog.Module.UI, $"Connection state changed: {state}");
            isConnecting = false;

            switch (state)
            {
                case "Connected":
                    var connectedDevice = dispenserController.GetConnectedDevice();
                    if (connectedDevice != null)
                    {
                        connectedDeviceMac = connectedDevice.MacAddress;
                        UpdateStatusText($"连接到 {connectedDevice.DeviceName}", Color.green);
                        RefreshConnectionState();
                    }
                    break;

                case "Disconnected":
                    connectedDeviceMac = null;
                    UpdateStatusText("已经断开连接", Color.gray);
                    RefreshConnectionState();
                    break;

                case "Connecting":
                    isConnecting = true;
                    UpdateStatusText("连接中...", Color.yellow);
                    break;

                default:
                    UpdateStatusText(state, Color.white);
                    break;
            }
        }

        /// <summary>
        /// Refresh connection state for all device cards
        /// </summary>
        private void RefreshConnectionState()
        {
            var connectedDevice = dispenserController?.GetConnectedDevice();
            string currentMac = connectedDevice?.MacAddress;

            foreach (var card in spawnedCards)
            {
                if (card != null)
                {
                    bool isConnected = !string.IsNullOrEmpty(currentMac) && 
                                     card.GetDeviceInfo().MacAddress == currentMac;
                    card.UpdateConnectionState(isConnected);
                }
            }
        }

        #endregion

        #region UI State Management

        /// <summary>
        /// Update UI state during scanning
        /// </summary>
        private void ShowLoadingState(bool show)
        {
            if (refreshButton != null)
            {
                refreshButton.interactable = !show;
            }
        }



        /// <summary>
        /// Update status text. The color is kept fixed for visual consistency.
        /// </summary>
        private void UpdateStatusText(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = statusTextColor;
            }
        }

        /// <summary>
        /// Called when dispenser reports an error
        /// </summary>
        private void OnDispenserError(string errorMessage)
        {
            EZLog.E(EZLog.Module.UI, $"Dispenser error: {errorMessage}");
            UpdateStatusText($"错误: {errorMessage}", Color.red);
            ShowLoadingState(false);
            isConnecting = false;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Check if dialog is currently visible
        /// </summary>
        public bool IsDialogVisible()
        {
            return dialogRoot != null && dialogRoot.activeSelf;
        }

        /// <summary>
        /// Get number of discovered devices
        /// </summary>
        public int GetDeviceCount()
        {
            return spawnedCards.Count;
        }

        /// <summary>
        /// Enable or disable auto-scan on dialog open
        /// </summary>
        public void SetAutoScanEnabled(bool enabled)
        {
            autoScanOnOpen = enabled;
        }



        #endregion
    }
}
