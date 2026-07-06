using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using EZDose.MainFlow;
using EZDose.CheckPillBox;
using EZDose.PillCounter;
using EZDose.Hardware;
using EZDose.Calibration;
using UnityEngine.EventSystems;
using EZDose;

namespace EZDose.UI
{
    /// <summary>
    /// Defines the available sub-pages on the right panel of the Home screen.
    /// Add new page types here when extending the UI.
    /// </summary>
    public enum HomeSubPage
    {
        PatientCard = 0,  // Default page showing patient card information
        CountPills = 1,   // Page for counting pills / dispensing overview
        Setting = 2       // Settings page for app configuration
    }

    /// <summary>
    /// Handles UI binding, scene transitions, and simple user prompts.
    /// Also manages Home page sub-page switching (stacked pages on the right panel).
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Scene Names")]
        [SerializeField] private string homeSceneName = "Home";
        [SerializeField] private string scanSceneName = "Scan";
        [SerializeField] private string dispenseSceneName = "Dispense";

        [Header("Home UI")]
        [SerializeField] private Transform patientListRoot;
        [SerializeField] private GameObject patientButtonPrefab;
        [SerializeField] private Button refreshButton;
        [SerializeField] private Text homeHintText;

        [Header("Device Management")]
        [Tooltip("Button to open device management dialog")]
        [SerializeField] private Button manageDevicesButton;
        [Tooltip("Button to clean pills from the turntable")]
        [SerializeField] private Button cleanTurntableButton;

        [Header("Device Button Visuals")]
        [SerializeField] private Image connectedIcon;
        [SerializeField] private Image disconnectedIcon;
        [SerializeField] private Text manageDevicesButtonText;
        
        [Tooltip("Reference to DeviceManagerUI component")]
        [SerializeField] private DeviceManagerUI deviceManagerUI;

        [Header("Dispenser Connection Check")]
        [Tooltip("Dialog shown when clicking patient card and dispenser is not connected")]
        [SerializeField] private GameObject connectDispenserDialog;
        [Tooltip("Confirm button in the connection required dialog")]
        [SerializeField] private Button connectDialogConfirmButton;

        [Header("Device Lost Dialog")]
        [Tooltip("Dialog shown when the dispenser becomes unreachable during scan or dispensing")]
        [SerializeField] private GameObject deviceLostDialog;
        [Tooltip("Confirm button in the device lost dialog")]
        [SerializeField] private Button deviceLostConfirmButton;
        [Tooltip("Optional message text in the device lost dialog")]
        [SerializeField] private Text deviceLostMessageText;

        [Header("Home Sub-Pages (Right Panel)")]
        [Tooltip("Reference to the PatientCard sub-page container. This page shows patient information.")]
        [SerializeField] private GameObject patientCardPage;

        [Tooltip("Reference to the CountPills sub-page container. This page handles pill counting display.")]
        [SerializeField] private GameObject countPillsPage;

        [Tooltip("Reference to the Setting sub-page container. This page provides app settings.")]
        [SerializeField] private GameObject settingPage;

        [Header("Home Sub-Page Navigation Buttons")]
        [Tooltip("Button that switches to the PatientCard page when clicked.")]
        [SerializeField] private Button patientCardButton;

        [Tooltip("Button that switches to the CountPills page when clicked.")]
        [SerializeField] private Button countPillsButton;

        [Tooltip("Button that switches to the Setting page when clicked.")]
        [SerializeField] private Button settingButton;

        [Header("Home Sub-Page Configuration")]
        [Tooltip("The sub-page to display when the Home scene first loads.")]
        [SerializeField] private HomeSubPage defaultSubPage = HomeSubPage.PatientCard;

        [Tooltip("Color applied to the active navigation button to indicate the current page.")]
        [SerializeField] private Color activeButtonColor = new Color(0.2f, 0.6f, 1f, 1f);

        [Tooltip("Color applied to inactive navigation buttons.")]
        [SerializeField] private Color inactiveButtonColor = Color.white;

        [Tooltip("Enable/disable visual highlighting on active navigation button.")]
        [SerializeField] private bool useButtonHighlight = true;

        [Header("Scan UI")]
        [SerializeField] private Button backToHomeButton;
        [SerializeField] private RectTransform lightBar;
        [SerializeField] private float lightBarSpeed = 200f;
        [SerializeField] private CheckPillBoxController scanner;
        [SerializeField] private GameObject correctBoxDialog;
        [SerializeField] private Button correctDialogConfirmButton;
        [Tooltip("扫码成功提示文本组件，留空则自动查找")]
        [SerializeField] private Text correctBoxDialogText;
        [SerializeField] private GameObject mismatchDialog;
        [SerializeField] private Button mismatchHomeButton;
        [SerializeField] private Button mismatchRetryButton;

        [Tooltip("Button to switch camera")]
        [SerializeField] private Button switchCameraButton;
        [Tooltip("Text for camera switch button")]
        [SerializeField] private Text switchCameraButtonText;

        [Header("Dispense UI")]
        [SerializeField] private Text totalPillsText;
        [SerializeField] private Text medicineNameText;
        [SerializeField] private Text patientNameText;
        [SerializeField] private Text progressPercentText;
        [Tooltip("展示当前分发药品剂量规格的文本框")]
        [SerializeField] private Text dosageSpecText;
        [Tooltip("Progress fill image, should use Image Type Filled")]
        [SerializeField] private Image progressFillImage;
        [SerializeField] private RawImage pillPreview;
        [SerializeField] private Button captureBackgroundButton;
        [SerializeField] private GameObject plateSwitchDialog;
        [SerializeField] private Button plateSwitchConfirmButton;
        [Tooltip("换盘提示文本组件，留空则自动查找")]
        [SerializeField] private Text plateSwitchDialogText;
        [SerializeField] private GameObject completeDialog;
        [SerializeField] private Button completeDialogConfirmButton;
        [SerializeField] private PillCounterController pillCounterController;
        
        [Tooltip("Pill calibration dialog")]
        [SerializeField] private PillCalibrationDialog pillCalibrationDialog;
        
        [Header("Manual Servo Tuning")]
        [Tooltip("Slider for directly tuning the servo angle")]
        [SerializeField] private Slider servoAngleTuningSlider;
        [Tooltip("Text showing the current servo angle setting")]
        [SerializeField] private Text servoAngleTuningText;
        [Tooltip("Keyboard step for servo angle tuning with Left/Right arrows.")]
        [SerializeField] private float servoKeyboardStep = 0.02f;
        [Tooltip("Repeat interval while holding Left/Right arrows.")]
        [SerializeField] private float servoKeyboardRepeatInterval = 0.08f;
        
        [Header("Dispense UI (Drug & Controls)")]
        [Tooltip("Image for the current drug")]
        [SerializeField] private Image drugImage;
        
        [Tooltip("Button to skip the current medicine")]
        [SerializeField] private Button skipMedicineButton;
        
        [Tooltip("Pause/resume dispensing button")]
        [SerializeField] private Button pauseResumeButton;
        
        [Tooltip("Pause/resume button text")]
        [SerializeField] private Text pauseResumeButtonText;
        
        [Header("Skip Confirm Dialog")]
        [Tooltip("Skip confirmation dialog")]
        [SerializeField] private GameObject skipConfirmDialog;
        
        [Tooltip("Skip confirmation button")]
        [SerializeField] private Button skipConfirmButton;
        [Tooltip("Button to clean pills from the turntable while the skip confirm dialog is open")]
        [SerializeField] private Button skipCleanTurntableButton;
        
        [Tooltip("Toggle to mark the skipped medicine as dispensed")]
        [SerializeField] private Toggle skipMarkDispensedToggle;

        [Tooltip("Text showing the skipped medicine name")]
        [SerializeField] private Text skipMedicineNameText;
        
        [Header("Next Medicine Preview")]
        [Tooltip("Text showing next medicine information")]
        [SerializeField] private Text nextMedicineText;
        private readonly List<GameObject> spawnedPatientButtons = new List<GameObject>();
        private Coroutine lightBarRoutine;

        // Progress bar smooth animation
        private float targetProgressValue;
        private Coroutine progressAnimRoutine;

        // Currently active sub-page in Home scene
        private HomeSubPage currentSubPage;

        // Dictionary mapping sub-page enum to page GameObject for easy access
        private Dictionary<HomeSubPage, GameObject> subPageMap;

        // Dictionary mapping sub-page enum to its corresponding button
        private Dictionary<HomeSubPage, Button> subPageButtonMap;
        private bool isServoKeyboardTuning;
        private bool servoKeyboardValueChanged;
        private float nextServoKeyboardStepTime;
        private bool deviceLostDialogVisible;

        /// <summary>
        /// Event fired when the active Home sub-page changes.
        /// Subscribers receive the newly activated page type.
        /// </summary>
        public event Action<HomeSubPage> OnSubPageChanged;

        /// <summary>
        /// Gets the currently displayed Home sub-page.
        /// </summary>
        public HomeSubPage CurrentSubPage => currentSubPage;

        private void Start()
        {
            SetupDeviceLostDialog();
            var main = MainController.Instance;
            if (main != null)
            {
                main.DeviceLost += OnDeviceLost;
            }

            var scene = SceneManager.GetActiveScene().name;
            if (scene == homeSceneName)
            {
                InitHome();
            }
            else if (scene == scanSceneName)
            {
                InitScan();
            }
            else if (scene == dispenseSceneName)
            {
                InitDispense();
            }
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (ShortcutInput.IsTextInputFocused())
            {
                return;
            }

            var scene = SceneManager.GetActiveScene().name;
            if (HandleGlobalDialogShortcuts())
            {
                return;
            }

            if (scene == homeSceneName)
            {
                HandleHomeShortcuts();
            }
            else if (scene == scanSceneName)
            {
                HandleScanShortcuts();
            }
            else if (scene == dispenseSceneName)
            {
                HandleDispenseShortcuts();
            }
#endif
        }

        #region Home

        private void InitHome()
        {
            var main = MainController.Instance;
            if (main != null)
            {
                main.PatientsUpdated += RenderPatientButtons;
                RenderPatientButtons(main.GetPatients());
            }

            if (refreshButton != null)
            {
                refreshButton.onClick.AddListener(() => FireAndForget(RefreshPatientsAsync()));
            }

            // Setup device management button
            if (manageDevicesButton != null)
            {
                manageDevicesButton.onClick.AddListener(OpenDeviceManagementDialog);
            }

            if (cleanTurntableButton != null)
            {
                cleanTurntableButton.onClick.AddListener(() => FireAndForget(CleanTurntableAsync(cleanTurntableButton)));
            }

            // Setup connection required dialog confirm button
            if (connectDialogConfirmButton != null)
            {
                connectDialogConfirmButton.onClick.AddListener(() =>
                {
                    if (connectDispenserDialog != null) connectDispenserDialog.SetActive(false);
                    OpenDeviceManagementDialog();
                });
            }

            // Initialize device manager UI if present
            if (deviceManagerUI == null)
            {
                deviceManagerUI = FindObjectOfType<DeviceManagerUI>();
            }

            // [NEW] Subscribe to connection events
            var dispenser = FindObjectOfType<DispenserController>();
            if (dispenser != null)
            {
                dispenser.OnConnectionStateChanged += OnDispenserConnectionChanged;
                UpdateDeviceButtonState(dispenser.IsConnected);
            }

            // Initialize sub-page switching system
            InitializeSubPages();
        }

        #region Home Sub-Page Management

        /// <summary>
        /// Initializes the sub-page system: creates mappings and sets up button listeners.
        /// Called during Home scene initialization.
        /// </summary>
        private void InitializeSubPages()
        {
            // Create page mapping: enum -> GameObject
            subPageMap = new Dictionary<HomeSubPage, GameObject>
            {
                { HomeSubPage.PatientCard, patientCardPage },
                { HomeSubPage.CountPills, countPillsPage },
                { HomeSubPage.Setting, settingPage }
            };

            // Create button mapping: enum -> Button
            subPageButtonMap = new Dictionary<HomeSubPage, Button>
            {
                { HomeSubPage.PatientCard, patientCardButton },
                { HomeSubPage.CountPills, countPillsButton },
                { HomeSubPage.Setting, settingButton }
            };

            // Setup button click listeners
            SetupSubPageButtonListeners();

            // Show the default page on startup
            ShowSubPage(defaultSubPage);
        }

        /// <summary>
        /// Attaches click listeners to all sub-page navigation buttons.
        /// Each button is configured to switch to its corresponding page when clicked.
        /// </summary>
        private void SetupSubPageButtonListeners()
        {
            // Assign click handler for PatientCard button
            if (patientCardButton != null)
            {
                patientCardButton.onClick.AddListener(() => ShowSubPage(HomeSubPage.PatientCard));
            }

            // Assign click handler for CountPills button
            if (countPillsButton != null)
            {
                countPillsButton.onClick.AddListener(() => ShowSubPage(HomeSubPage.CountPills));
            }

            // Assign click handler for Setting button
            if (settingButton != null)
            {
                settingButton.onClick.AddListener(() => ShowSubPage(HomeSubPage.Setting));
            }
        }

        /// <summary>
        /// Switches to the specified sub-page, hiding all other pages.
        /// This is the main method for sub-page navigation within the Home scene.
        /// </summary>
        /// <param name="targetPage">The sub-page to display.</param>
        public void ShowSubPage(HomeSubPage targetPage)
        {
            // Skip if already on the target page and it's visible
            if (currentSubPage == targetPage && IsSubPageActive(targetPage))
            {
                return;
            }

            // Hide all sub-pages first
            HideAllSubPages();

            // Show the target sub-page
            if (subPageMap != null && subPageMap.TryGetValue(targetPage, out var pageObject) && pageObject != null)
            {
                pageObject.SetActive(true);
                currentSubPage = targetPage;

                // Update button visual states to reflect active page
                UpdateSubPageButtonStates(targetPage);

                // Notify subscribers about the page change
                OnSubPageChanged?.Invoke(targetPage);

                EZLog.D(EZLog.Module.UI, $"Switched to sub-page: {targetPage}");
            }
            else
            {
                EZLog.W(EZLog.Module.UI, $"Sub-page not found or not assigned: {targetPage}");
            }
        }

        /// <summary>
        /// Hides all sub-pages by setting their GameObjects inactive.
        /// Called before showing a new page to ensure only one page is visible at a time.
        /// </summary>
        private void HideAllSubPages()
        {
            if (subPageMap == null)
            {
                return;
            }

            foreach (var kvp in subPageMap)
            {
                if (kvp.Value != null)
                {
                    kvp.Value.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Checks if a specific sub-page is currently active (visible).
        /// </summary>
        /// <param name="page">The sub-page to check.</param>
        /// <returns>True if the sub-page is active, false otherwise.</returns>
        private bool IsSubPageActive(HomeSubPage page)
        {
            if (subPageMap != null && subPageMap.TryGetValue(page, out var pageObject) && pageObject != null)
            {
                return pageObject.activeSelf;
            }
            return false;
        }

        /// <summary>
        /// Updates the visual appearance of navigation buttons to indicate the active sub-page.
        /// The active button gets a highlighted color while others return to default.
        /// </summary>
        /// <param name="activePage">The currently active sub-page.</param>
        private void UpdateSubPageButtonStates(HomeSubPage activePage)
        {
            if (!useButtonHighlight || subPageButtonMap == null)
            {
                return;
            }

            foreach (var kvp in subPageButtonMap)
            {
                var button = kvp.Value;
                if (button == null)
                {
                    continue;
                }

                // Get the button's Image component for color modification
                var buttonImage = button.GetComponent<Image>();
                if (buttonImage == null)
                {
                    continue;
                }

                // Apply active or inactive color based on whether this button's page is active
                bool isActive = kvp.Key == activePage;
                buttonImage.color = isActive ? activeButtonColor : inactiveButtonColor;
            }
        }

        /// <summary>
        /// Programmatically switches to the PatientCard sub-page.
        /// Convenience method for external scripts.
        /// </summary>
        public void GoToPatientCardPage()
        {
            ShowSubPage(HomeSubPage.PatientCard);
        }

        /// <summary>
        /// Programmatically switches to the CountPills sub-page.
        /// Convenience method for external scripts.
        /// </summary>
        public void GoToCountPillsPage()
        {
            ShowSubPage(HomeSubPage.CountPills);
        }

        /// <summary>
        /// Programmatically switches to the Setting sub-page.
        /// Convenience method for external scripts.
        /// </summary>
        public void GoToSettingPage()
        {
            ShowSubPage(HomeSubPage.Setting);
        }

        /// <summary>
        /// Navigates to the next sub-page in the sequence (wraps around).
        /// Useful for implementing swipe gestures or keyboard navigation.
        /// </summary>
        public void GoToNextSubPage()
        {
            int totalPages = Enum.GetValues(typeof(HomeSubPage)).Length;
            int nextIndex = ((int)currentSubPage + 1) % totalPages;
            ShowSubPage((HomeSubPage)nextIndex);
        }

        /// <summary>
        /// Navigates to the previous sub-page in the sequence (wraps around).
        /// Useful for implementing swipe gestures or keyboard navigation.
        /// </summary>
        public void GoToPreviousSubPage()
        {
            int totalPages = Enum.GetValues(typeof(HomeSubPage)).Length;
            int prevIndex = ((int)currentSubPage - 1 + totalPages) % totalPages;
            ShowSubPage((HomeSubPage)prevIndex);
        }

        /// <summary>
        /// Resets to the default sub-page configured in the Inspector.
        /// </summary>
        public void ResetToDefaultSubPage()
        {
            ShowSubPage(defaultSubPage);
        }

        /// <summary>
        /// Opens the device management dialog for Bluetooth connection management
        /// </summary>
        public void OpenDeviceManagementDialog()
        {
            if (deviceManagerUI != null)
            {
                deviceManagerUI.ShowDialog();
                EZLog.D(EZLog.Module.UI, "Device management dialog opened");
            }
            else
            {
                EZLog.W(EZLog.Module.UI, "DeviceManagerUI not assigned or found");
            }
        }

        /// <summary>
        /// Closes the device management dialog
        /// </summary>
        public void CloseDeviceManagementDialog()
        {
            if (deviceManagerUI != null)
            {
                deviceManagerUI.HideDialog();
                EZLog.D(EZLog.Module.UI, "Device management dialog closed");
            }
        }

        /// <summary>
        /// Gets the GameObject reference for a specific sub-page.
        /// Useful for external scripts that need to access page contents.
        /// </summary>
        /// <param name="page">The sub-page type to retrieve.</param>
        /// <returns>The GameObject for the specified sub-page, or null if not found.</returns>
        public GameObject GetSubPageObject(HomeSubPage page)
        {
            if (subPageMap != null && subPageMap.TryGetValue(page, out var pageObject))
            {
                return pageObject;
            }
            return null;
        }

        private void OnDispenserConnectionChanged(string state)
        {
            // state is "Connected", "Disconnected", "Connecting"
            bool isConnected = (state == "Connected");
            UpdateDeviceButtonState(isConnected);
        }

        private void UpdateDeviceButtonState(bool isConnected)
        {
            if (manageDevicesButtonText != null)
            {
                manageDevicesButtonText.text = isConnected ? "分药机已连接" : "分药机未连接";
            }

            if (connectedIcon != null)
            {
                connectedIcon.gameObject.SetActive(isConnected);
            }

            if (disconnectedIcon != null)
            {
                disconnectedIcon.gameObject.SetActive(!isConnected);
            }
        }

        #endregion

        private async Task RefreshPatientsAsync()
        {
            var main = MainController.Instance;
            if (main == null)
            {
                return;
            }

            await main.RefreshPatientsAsync();
        }

        private async Task CleanTurntableAsync(Button sourceButton)
        {
            var main = MainController.Instance;
            if (main == null)
            {
                return;
            }

            var dispenser = FindObjectOfType<DispenserController>();
            if (dispenser == null || !dispenser.IsConnected)
            {
                EZLog.W(EZLog.Module.UI, "Dispenser not connected, showing prompt");
                if (connectDispenserDialog != null)
                {
                    connectDispenserDialog.SetActive(true);
                }
                return;
            }

            if (sourceButton != null)
            {
                sourceButton.interactable = false;
            }

            try
            {
                bool success = await main.CleanTurntableAsync();
                EZLog.I(EZLog.Module.UI, success ? "Turntable cleaned successfully" : "Turntable cleaning failed");
            }
            finally
            {
                if (sourceButton != null)
                {
                    sourceButton.interactable = true;
                }
            }
        }

        private void RenderPatientButtons(List<PatientStatus> patients)
        {
            ClearPatientButtons();

            if (patientListRoot == null || patientButtonPrefab == null || patients == null)
            {
                return;
            }

            foreach (var patient in patients)
            {
                var go = Instantiate(patientButtonPrefab, patientListRoot);
                spawnedPatientButtons.Add(go);

                var button = go.GetComponent<Button>();
                var texts = go.GetComponentsInChildren<Text>();
                
                // Find and set patient name text (usually the first or larger text)
                // Find and set medicine count text (the one showing "X种药品需要分配")
                foreach (var textComponent in texts)
                {
                    if (textComponent.name.Contains("Name") || textComponent == texts[0])
                    {
                        string displayName = patient.PatientName;
                        if (!string.IsNullOrEmpty(patient.BedNumber))
                        {
                            displayName += $" {patient.BedNumber}床";
                        }
                        textComponent.text = displayName;
                    }
                    else if (textComponent.name.Contains("Medicine") || textComponent.name.Contains("Count"))
                    {
                        textComponent.text = $"{patient.MedicineCount}种药品需要分配";
                    }
                }

                if (button != null)
                {
                    // All patients in the list need dispensing, so all buttons are enabled
                    button.interactable = true;
                    var id = patient.PatientId;
                    button.onClick.AddListener(() => OnPatientClicked(id));
                }
            }

            UpdateHomeHint(patients);
        }

        private void ClearPatientButtons()
        {
            foreach (var item in spawnedPatientButtons)
            {
                if (item != null)
                {
                    Destroy(item);
                }
            }
            spawnedPatientButtons.Clear();
        }

        private void UpdateHomeHint(List<PatientStatus> patients)
        {
            if (homeHintText == null || patients == null)
            {
                return;
            }

            var remaining = patients.Count;
            homeHintText.text = remaining > 0
                ? "请点击患者卡片扫描药盒条形码开始分药"
                : "辛苦了，所有患者的药品都已经分完了";
        }

        private void OnPatientClicked(string patientId)
        {
            var main = MainController.Instance;
            if (main == null)
            {
                return;
            }

            // Check if dispenser is connected before proceeding
            var dispenser = FindObjectOfType<DispenserController>();
            if (dispenser == null || !dispenser.IsConnected)
            {
                EZLog.W(EZLog.Module.UI, "Dispenser not connected, showing prompt");
                if (connectDispenserDialog != null)
                {
                    connectDispenserDialog.SetActive(true);
                }
                return;
            }

            if (!main.TrySelectPatient(patientId, out _))
            {
                EZLog.W(EZLog.Module.UI, "Failed to select patient");
                return;
            }

            SceneManager.LoadScene(scanSceneName);
        }

        #endregion

        #region Scan

        private void InitScan()
        {
            var main = MainController.Instance;
            var patient = main?.GetCurrentPatient();

            if (backToHomeButton != null)
            {
                backToHomeButton.onClick.AddListener(() => FireAndForget(ReturnHomeFromScanAsync()));
            }

            if (scanner != null && patient != null)
            {
                scanner.OnBoxVerified += OnBoxVerified;
                scanner.OnBoxMismatch += OnBoxMismatch;
                scanner.OnScanError += OnScanError;
                scanner.StartScanner(patient.PatientId);
            }

            // Setup camera switch button
            if (switchCameraButton != null)
            {
                switchCameraButton.onClick.AddListener(OnSwitchCameraClicked);
                UpdateSwitchCameraButtonText();
            }

            if (lightBar != null)
            {
                lightBarRoutine = StartCoroutine(AnimateLightBar());
            }
        }

        /// <summary>
        /// 点击切换摄像头按钮时调用，切换前置/后置摄像头
        /// </summary>
        private void OnSwitchCameraClicked()
        {
            if (scanner == null) return;

            scanner.SwitchCamera();
            UpdateSwitchCameraButtonText();
            EZLog.D(EZLog.Module.UI, $"Camera switched, isFrontFacing={scanner.IsFrontFacing}");
        }

        /// <summary>
        /// 更新切换摄像头按钮的显示文字
        /// </summary>
        private void UpdateSwitchCameraButtonText()
        {
            if (switchCameraButtonText == null || scanner == null) return;
            switchCameraButtonText.text = scanner.IsFrontFacing ? "切换后置" : "切换前置";
        }

        private IEnumerator AnimateLightBar()
        {
            // Simple loop that moves the bar from top to bottom.
            var rect = lightBar;
            var canvasHeight = rect.parent is RectTransform parent ? parent.rect.height : 400f;
            var halfHeight = rect.rect.height * 0.5f;
            var top = canvasHeight * 0.5f - halfHeight;
            var bottom = -top;

            while (true)
            {
                var pos = rect.anchoredPosition;
                pos.y -= lightBarSpeed * Time.deltaTime;
                if (pos.y < bottom)
                {
                    pos.y = top;
                }
                rect.anchoredPosition = pos;
                yield return null;
            }
        }

        private void OnBoxVerified(string code)
        {
            FireAndForget(HandleBoxVerifiedAsync());
        }

        private async Task HandleBoxVerifiedAsync()
        {
            if (scanner != null)
            {
                scanner.StopScanner();
            }

            var main = MainController.Instance;
            string plateName = "药盒";
            string patientName = "";

            if (main != null)
            {
                var patient = main.GetCurrentPatient();
                if (patient != null)
                {
                    patientName = patient.PatientName;
                }

                // 预先准备计划，以便确认第一盘要分什么药
                await main.PreparePlanAsync();
                var plan = main.CurrentPlan;
                if (plan != null)
                {
                    if (plan.MedicinesPlate1.Count > 0)
                    {
                        // 检查 Plate 1 中的药品是否全部为饭后或随餐药
                        bool allPlate1AfterMeal = plan.MedicinesPlate1.All(m => IsAfterMealTiming(m.MealTiming));
                        plateName = allPlate1AfterMeal ? "饭后/随餐药盒" : "饭前药盒";
                    }
                    else if (plan.MedicinesPlate2.Count > 0)
                    {
                        plateName = "饭后/随餐药盒";
                    }
                }

                var opened = await main.OpenTrayAsync();
                if (!opened)
                {
                    return;
                }
            }

            var targetText = correctBoxDialogText ?? FindDialogMessageText(correctBoxDialog, "Message");
            if (targetText != null)
            {
                targetText.text = $"请放入【{patientName}】的【{plateName}】。";
            }

            if (correctBoxDialog != null)
            {
                correctBoxDialog.SetActive(true);
            }

            if (correctDialogConfirmButton != null)
            {
                correctDialogConfirmButton.onClick.RemoveAllListeners();
                correctDialogConfirmButton.onClick.AddListener(() => FireAndForget(ProceedToDispenseAsync()));
            }
        }

        private void OnBoxMismatch(string code)
        {
            if (scanner != null)
            {
                scanner.StopScanner();
            }

            ShowMismatchDialog();
        }

        private void OnScanError(string error)
        {
            EZLog.W(EZLog.Module.UI, $"Scan error: {error}");
            ShowMismatchDialog();
        }

        private void ShowMismatchDialog()
        {
            if (mismatchDialog != null)
            {
                mismatchDialog.SetActive(true);
            }

            if (mismatchHomeButton != null)
            {
                mismatchHomeButton.onClick.RemoveAllListeners();
                mismatchHomeButton.onClick.AddListener(() => FireAndForget(ReturnHomeFromScanAsync()));
            }

            if (mismatchRetryButton != null)
            {
                mismatchRetryButton.onClick.RemoveAllListeners();
                mismatchRetryButton.onClick.AddListener(() =>
                {
                    if (mismatchDialog != null)
                    {
                        mismatchDialog.SetActive(false);
                    }

                    var main = MainController.Instance;
                    var patient = main?.GetCurrentPatient();
                    if (scanner != null && patient != null)
                    {
                        scanner.StartScanner(patient.PatientId);
                    }
                });
            }
        }

        private async Task ProceedToDispenseAsync()
        {
            if (correctBoxDialog != null)
            {
                correctBoxDialog.SetActive(false);
            }

            // main.PreparePlanAsync() is already called in HandleBoxVerifiedAsync

            await LoadSceneAsyncSafe(dispenseSceneName);
        }

        private async Task ReturnHomeFromScanAsync()
        {
            if (scanner != null)
            {
                scanner.StopScanner();
            }

            await Task.Delay(200);
            await LoadSceneAsyncSafe(homeSceneName);
        }

        #endregion

        #region Dispense

        private void InitDispense()
        {
            var main = MainController.Instance;
            if (main != null)
            {
                main.DispensingProgressChanged += UpdateDispenseUI;
                main.DispensingError += ShowDispenseError;
                main.DispensingCompleted += OnDispenseCompleted;
                main.PlateSwitchRequired += OnPlateSwitchRequired;
                main.PillCalibrationRequired += OnPillCalibrationRequired;
                main.MedicineSkipped += OnMedicineSkipped;
            }

            if (captureBackgroundButton != null && pillCounterController != null)
            {
                captureBackgroundButton.onClick.AddListener(() => pillCounterController.CaptureBackground());
            }
            
            // Setup skip button to skip current medicine when clicked
            if (skipMedicineButton != null)
            {
                skipMedicineButton.onClick.AddListener(OnSkipMedicineClicked);
            }

            // Setup pause/resume button
            if (pauseResumeButton != null)
            {
                pauseResumeButton.onClick.AddListener(OnPauseResumeClicked);
            }

            // Subscribe to pause state changes from dispenser
            var dispenser = FindObjectOfType<DispenserController>();
            if (dispenser != null)
            {
                dispenser.OnPauseStateChanged += UpdatePauseButtonUI;
                // Initialize button text to current state
                UpdatePauseButtonUI(dispenser.IsPaused);
            }

            if (main != null)
            {
                main.SkipConfirmRequired += OnSkipConfirmRequired;
                FireAndForget(main.StartDispensingAsync());
            }

            // Setup skip confirm dialog button
            if (skipConfirmButton != null)
            {
                skipConfirmButton.onClick.AddListener(OnSkipConfirmClicked);
            }

            if (skipCleanTurntableButton != null)
            {
                skipCleanTurntableButton.onClick.AddListener(() => FireAndForget(CleanTurntableAsync(skipCleanTurntableButton)));
            }

            // Hide skip dialog initially
            if (skipConfirmDialog != null)
            {
                skipConfirmDialog.SetActive(false);
            }

            // Setup manual servo angle tuning slider
            if (servoAngleTuningSlider != null)
            {
                servoAngleTuningSlider.minValue = 0.1f;
                servoAngleTuningSlider.maxValue = 1.2f;
                servoAngleTuningSlider.value = 0.7f;
                servoAngleTuningSlider.onValueChanged.AddListener(OnServoAngleTuningChanged);
                OnServoAngleTuningChanged(servoAngleTuningSlider.value);
                var sliderNavigation = servoAngleTuningSlider.navigation;
                sliderNavigation.mode = Navigation.Mode.None;
                servoAngleTuningSlider.navigation = sliderNavigation;

                var trigger = servoAngleTuningSlider.gameObject.GetComponent<EventTrigger>() ?? 
                              servoAngleTuningSlider.gameObject.AddComponent<EventTrigger>();
                
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
                entry.callback.AddListener((data) => { OnServoAngleTuningReleased(); });
                trigger.triggers.Add(entry);
            }
        }
        
        /// <summary>
        /// Called when the servo angle slider is dragged (updates UI text only)
        /// </summary>
        private void OnServoAngleTuningChanged(float servoAngle)
        {
            if (servoAngleTuningText != null)
            {
                servoAngleTuningText.text = $"舵机：{servoAngle:F2}";
            }
        }

        /// <summary>
        /// Called when the user releases the slider pointer, sending command to STM32.
        /// </summary>
        private void OnServoAngleTuningReleased()
        {
            if (servoAngleTuningSlider == null) return;

            float servoAngle = servoAngleTuningSlider.value;
            FireAndForget(ApplyServoAngleTuningAsync(servoAngle));
        }

        private async Task ApplyServoAngleTuningAsync(float servoAngle)
        {
            var dispenser = FindObjectOfType<DispenserController>();
            
            if (dispenser != null && dispenser.IsConnected)
            {
                EZLog.I(EZLog.Module.UI, $"Manual servo tuning released: servo={servoAngle:F2}");

                var servoTcs = new TaskCompletionSource<bool>();
                dispenser.SetServoAngle(servoAngle, success => { servoTcs.TrySetResult(success); });
                await servoTcs.Task;
            }
        }

        /// <summary>
        /// Called when skip button is clicked - skips current medicine
        /// </summary>
        private void OnSkipMedicineClicked()
        {
            var main = MainController.Instance;
            if (main != null)
            {
                EZLog.D(EZLog.Module.UI, "Skip button clicked");
                main.SkipCurrentMedicine();
            }
        }
        
        /// <summary>
        /// Called when a medicine is skipped - shows feedback to user
        /// </summary>
        private void OnMedicineSkipped(string medicineName)
        {
            EZLog.D(EZLog.Module.UI, $"Medicine skipped: {medicineName}");
        }

        /// <summary>
        /// Called when skip command sent — show confirmation dialog
        /// </summary>
        private void OnSkipConfirmRequired(string medicineName)
        {
            EZLog.D(EZLog.Module.UI, $"Showing skip confirm dialog for: {medicineName}");
            
            if (skipMedicineNameText != null)
            {
                skipMedicineNameText.text = $"已跳过药物：{medicineName}";
            }
            
            // Reset toggle to unchecked (default: don't mark as dispensed)
            if (skipMarkDispensedToggle != null)
            {
                skipMarkDispensedToggle.isOn = false;
            }
            
            if (skipConfirmDialog != null)
            {
                skipConfirmDialog.SetActive(true);
            }
        }

        /// <summary>
        /// Called when user clicks confirm in skip dialog
        /// </summary>
        private void OnSkipConfirmClicked()
        {
            bool markAsDispensed = skipMarkDispensedToggle != null && skipMarkDispensedToggle.isOn;
            
            EZLog.D(EZLog.Module.UI, $"Skip confirmed, markAsDispensed={markAsDispensed}");
            
            if (skipConfirmDialog != null)
            {
                skipConfirmDialog.SetActive(false);
            }
            
            var main = MainController.Instance;
            main?.ConfirmSkipReady(markAsDispensed);
        }

        /// <summary>
        /// Called when pause/resume button is clicked - toggles dispensing pause state
        /// </summary>
        private void OnPauseResumeClicked()
        {
            var dispenser = FindObjectOfType<DispenserController>();
            if (dispenser == null) return;

            if (dispenser.IsPaused)
            {
                EZLog.D(EZLog.Module.UI, "Resume button clicked");
                dispenser.ResumeDispensing();
            }
            else
            {
                EZLog.D(EZLog.Module.UI, "Pause button clicked");
                dispenser.PauseDispenser();
            }
        }

        /// <summary>
        /// Updates pause button UI based on current pause state
        /// </summary>
        private void UpdatePauseButtonUI(bool isPaused)
        {
            if (pauseResumeButtonText != null)
            {
                pauseResumeButtonText.text = isPaused ? "继续" : "暂停";
            }
        }

        /// <summary>
        /// MainController 触发校准事件时，直接调用对话框
        /// </summary>
        private void OnPillCalibrationRequired(EZDose.Prescriptions.DispensingMedicine medicine)
        {
            EZLog.I(EZLog.Module.UI, $"Calibration required for: {medicine.MedicineName}");
            
            if (pillCalibrationDialog != null)
            {
                pillCalibrationDialog.Show(medicine.MedicineName, medicine.PatientName, medicine.BedNumber);
            }
            else
            {
                EZLog.E(EZLog.Module.UI, "PillCalibrationDialog is not assigned");
            }
        }

        private void UpdateDispenseUI(DispensingProgressInfo info)
        {
            if (info == null)
            {
                return;
            }

            if (totalPillsText != null)
            {
                int remainingPills = Mathf.Max(0, info.TotalPills - info.DispensedPills);
                totalPillsText.text = $"{remainingPills}";
            }

            if (medicineNameText != null)
            {
                medicineNameText.text = $"{info.MedicineName}  {info.TotalPills}粒";
            }

            if (patientNameText != null)
            {
                patientNameText.text = string.IsNullOrEmpty(info.PatientName)
                    ? string.Empty
                    : $"所属患者： {info.PatientName}";
            }

            if (dosageSpecText != null)
            {
                dosageSpecText.text = string.IsNullOrEmpty(info.DosageSpec)
                    ? string.Empty
                    : $"剂量规格： {info.DosageSpec}";
            }

            if (progressPercentText != null)
            {
                var clampedProgress = Mathf.Clamp01(info.Progress);
                var percent = Mathf.RoundToInt(clampedProgress * 100f);
                progressPercentText.text = $"{percent}%";
            }

            // Update progress bar with smooth animation
            if (progressFillImage != null)
            {
                targetProgressValue = info.Progress;
                if (progressAnimRoutine == null)
                {
                    progressAnimRoutine = StartCoroutine(AnimateProgressBar());
                }
            }
            
            // Load and display drug image if available and different from current
            EZLog.V(EZLog.Module.UI, $"UpdateDispenseUI - drugImage={drugImage != null}, ImageResourceId='{info.ImageResourceId}', currentId='{currentDrugImageId}'");
            if (drugImage != null && !string.IsNullOrEmpty(info.ImageResourceId))
            {
                if (currentDrugImageId != info.ImageResourceId)
                {
                    EZLog.D(EZLog.Module.UI, $"Loading new drug image: {info.ImageResourceId}");
                    currentDrugImageId = info.ImageResourceId;
                    LoadDrugImageAsync(info.ImageResourceId);
                }
            }
            else if (drugImage == null)
            {
                EZLog.W(EZLog.Module.UI, "drugImage field is not assigned in Inspector");
            }
            
            // Update next medicine preview info
            UpdateNextMedicinePreview(info);
            
        }
        
        /// <summary>
        /// Updates the next medicine preview text with info about the upcoming medicine
        /// </summary>
        private void UpdateNextMedicinePreview(DispensingProgressInfo info)
        {
            if (nextMedicineText == null) return;
            
            if (!string.IsNullOrEmpty(info.NextMedicineName))
            {
                nextMedicineText.text = $"下一药物：{info.NextMedicineName}，共 {info.NextMedicinePillCount} 粒";
            }
            else
            {
                nextMedicineText.text = "无下一药物";
            }
        }
        
        // Currently loaded drug image ID to avoid reloading same image
        private string currentDrugImageId;
        
        /// <summary>
        /// Load and display drug image from server
        /// </summary>
        private async void LoadDrugImageAsync(string imageResourceId)
        {
            if (drugImage == null || string.IsNullOrEmpty(imageResourceId))
            {
                return;
            }
            
            var serverUrl = AppConfig.Instance?.ServerUrl;
            if (string.IsNullOrEmpty(serverUrl))
            {
                EZLog.W(EZLog.Module.UI, "Cannot load drug image - server URL not configured");
                return;
            }
            
            var texture = await EZDose.UI.PillImageLoader.LoadImageAsync(serverUrl, imageResourceId);
            if (texture != null)
            {
                // Check if this is still the image we want to display
                if (currentDrugImageId == imageResourceId)
                {
                    // Convert Texture2D to Sprite for Image component
                    var sprite = Sprite.Create(
                        texture, 
                        new UnityEngine.Rect(0, 0, texture.width, texture.height), 
                        new Vector2(0.5f, 0.5f));
                    drugImage.sprite = sprite;
                    EZLog.D(EZLog.Module.UI, $"Displayed drug image: {imageResourceId}");
                }
                else
                {
                    // Image changed while loading, destroy this texture
                    Destroy(texture);
                }
            }
        }

        /// <summary>
        /// Smoothly animates the progress bar towards the target value.
        /// </summary>
        private IEnumerator AnimateProgressBar()
        {
            const float smoothSpeed = 5f; // Adjust for faster/slower animation
            
            while (progressFillImage != null)
            {
                float current = progressFillImage.fillAmount;
                float target = targetProgressValue;
                
                // If close enough to target, snap to it
                if (Mathf.Abs(target - current) < 0.001f)
                {
                    progressFillImage.fillAmount = target;
                    yield return null;
                    continue;
                }
                
                // Smoothly interpolate towards target
                progressFillImage.fillAmount = Mathf.Lerp(current, target, Time.deltaTime * smoothSpeed);
                yield return null;
            }
            
            progressAnimRoutine = null;
        }

        private void OnPlateSwitchRequired(int plateNumber)
        {
            var main = MainController.Instance;
            string patientName = "";
            if (main != null && main.GetCurrentPatient() != null)
            {
                patientName = main.GetCurrentPatient().PatientName;
            }

            string plateName = plateNumber == 1 ? "饭前药盒" : "饭后/随餐药盒";

            var targetText = plateSwitchDialogText ?? FindDialogMessageText(plateSwitchDialog, "Message");
            if (targetText != null)
            {
                targetText.text = $"请放入【{patientName}】的【{plateName}】。";
            }

            if (plateSwitchDialog != null)
            {
                plateSwitchDialog.SetActive(true);
            }

            if (plateSwitchConfirmButton != null)
            {
                plateSwitchConfirmButton.onClick.RemoveAllListeners();
                plateSwitchConfirmButton.onClick.AddListener(() =>
                {
                    main?.ConfirmPlateReady();
                    if (plateSwitchDialog != null)
                    {
                        plateSwitchDialog.SetActive(false);
                    }
                });
            }
        }

        private void OnDispenseCompleted()
        {
            FireAndForget(ShowCompletionAsync());
        }

        private async Task ShowCompletionAsync()
        {
            var main = MainController.Instance;
            if (main != null)
            {
                var opened = await main.OpenTrayAsync();
                if (!opened)
                {
                    return;
                }
            }

            if (completeDialog != null)
            {
                completeDialog.SetActive(true);
            }

            if (completeDialogConfirmButton != null)
            {
                completeDialogConfirmButton.onClick.RemoveAllListeners();
                completeDialogConfirmButton.onClick.AddListener(() => FireAndForget(ReturnHomeAsync()));
            }
        }

        private async Task ReturnHomeAsync()
        {
            var main = MainController.Instance;
            if (main != null)
            {
                var closed = await main.CloseTrayAsync();
                if (!closed)
                {
                    return;
                }
            }

            await LoadSceneAsyncSafe(homeSceneName);
        }

        private void ShowDispenseError(string message)
        {
            EZLog.W(EZLog.Module.UI, $"Dispense error: {message}");
            // For now, we show the same completion dialog style with the error message logged.
            if (completeDialog != null)
            {
                completeDialog.SetActive(true);
            }
        }

        #endregion

        #region Helpers

        private bool HandleGlobalDialogShortcuts()
        {
            if (IsActive(deviceLostDialog))
            {
                ShortcutInput.InvokeButtonIfKeyDown(deviceLostConfirmButton, KeyCode.Return, KeyCode.KeypadEnter);
                return true;
            }

            return false;
        }

        private void HandleHomeShortcuts()
        {
            if (deviceManagerUI != null && deviceManagerUI.IsDialogVisible())
            {
                return;
            }

            if (ShortcutInput.InvokeButtonIfKeyDown(refreshButton, KeyCode.R))
            {
                return;
            }

            if (ShortcutInput.InvokeButtonIfKeyDown(manageDevicesButton, KeyCode.D))
            {
                return;
            }

            if (ShortcutInput.InvokeButtonIfKeyDown(cleanTurntableButton, KeyCode.C))
            {
                return;
            }

            if (ShortcutInput.GetAnyKeyDown(KeyCode.Alpha1, KeyCode.Keypad1))
            {
                ShowSubPage(HomeSubPage.PatientCard);
                return;
            }

            if (ShortcutInput.GetAnyKeyDown(KeyCode.Alpha2, KeyCode.Keypad2))
            {
                ShowSubPage(HomeSubPage.Setting);
            }
        }

        private void HandleScanShortcuts()
        {
            if (IsActive(correctBoxDialog))
            {
                ShortcutInput.InvokeButtonIfKeyDown(correctDialogConfirmButton, KeyCode.Return, KeyCode.KeypadEnter);
                return;
            }

            if (IsActive(mismatchDialog))
            {
                ShortcutInput.InvokeButtonIfKeyDown(mismatchRetryButton, KeyCode.Return, KeyCode.KeypadEnter);
                return;
            }

            if (ShortcutInput.InvokeButtonIfKeyDown(backToHomeButton, KeyCode.Escape))
            {
                return;
            }

            ShortcutInput.InvokeButtonIfKeyDown(switchCameraButton, KeyCode.LeftShift, KeyCode.RightShift);
        }

        private void HandleDispenseShortcuts()
        {
            if (IsActive(skipConfirmDialog))
            {
                if (ShortcutInput.InvokeButtonIfKeyDown(skipConfirmButton, KeyCode.Return, KeyCode.KeypadEnter))
                {
                    return;
                }

                ShortcutInput.InvokeButtonIfKeyDown(skipCleanTurntableButton, KeyCode.C);
                return;
            }

            if (IsActive(plateSwitchDialog))
            {
                ShortcutInput.InvokeButtonIfKeyDown(plateSwitchConfirmButton, KeyCode.Return, KeyCode.KeypadEnter);
                return;
            }

            if (IsActive(completeDialog))
            {
                ShortcutInput.InvokeButtonIfKeyDown(completeDialogConfirmButton, KeyCode.Return, KeyCode.KeypadEnter);
                return;
            }

            if (HandleServoKeyboardTuning())
            {
                return;
            }

            if (ShortcutInput.InvokeButtonIfKeyDown(pauseResumeButton, KeyCode.Space))
            {
                return;
            }

            ShortcutInput.InvokeButtonIfKeyDown(skipMedicineButton, KeyCode.P);
        }

        private bool HandleServoKeyboardTuning()
        {
            bool leftHeld = Input.GetKey(KeyCode.LeftArrow);
            bool rightHeld = Input.GetKey(KeyCode.RightArrow);
            bool leftPressed = Input.GetKeyDown(KeyCode.LeftArrow);
            bool rightPressed = Input.GetKeyDown(KeyCode.RightArrow);
            bool released = Input.GetKeyUp(KeyCode.LeftArrow) || Input.GetKeyUp(KeyCode.RightArrow);

            if (!leftHeld && !rightHeld)
            {
                if (released && isServoKeyboardTuning)
                {
                    CommitServoKeyboardTuning();
                    return true;
                }

                isServoKeyboardTuning = false;
                servoKeyboardValueChanged = false;
                return released;
            }

            if (!CanUseServoKeyboardTuning())
            {
                ResetServoKeyboardTuningState();
                return leftPressed || rightPressed || released;
            }

            if (!isServoKeyboardTuning)
            {
                isServoKeyboardTuning = true;
                servoKeyboardValueChanged = false;
                nextServoKeyboardStepTime = 0f;
            }

            bool shouldStep = leftPressed || rightPressed || Time.unscaledTime >= nextServoKeyboardStepTime;
            if (!shouldStep)
            {
                return true;
            }

            int direction = 0;
            if (rightHeld && !leftHeld)
            {
                direction = -1;
            }
            else if (leftHeld && !rightHeld)
            {
                direction = 1;
            }

            if (direction != 0 && ApplyServoKeyboardStep(direction))
            {
                servoKeyboardValueChanged = true;
            }

            nextServoKeyboardStepTime = Time.unscaledTime + Mathf.Max(0.01f, servoKeyboardRepeatInterval);
            return true;
        }

        private bool ApplyServoKeyboardStep(int direction)
        {
            if (!CanUseServoKeyboardTuning())
            {
                return false;
            }

            float currentValue = servoAngleTuningSlider.value;
            float nextValue = Mathf.Clamp(
                currentValue + direction * Mathf.Abs(servoKeyboardStep),
                servoAngleTuningSlider.minValue,
                servoAngleTuningSlider.maxValue);

            if (Mathf.Approximately(currentValue, nextValue))
            {
                return false;
            }

            servoAngleTuningSlider.value = nextValue;
            return true;
        }

        private void CommitServoKeyboardTuning()
        {
            if (servoKeyboardValueChanged)
            {
                OnServoAngleTuningReleased();
            }

            ResetServoKeyboardTuningState();
        }

        private void ResetServoKeyboardTuningState()
        {
            isServoKeyboardTuning = false;
            servoKeyboardValueChanged = false;
            nextServoKeyboardStepTime = 0f;
        }

        private bool CanUseServoKeyboardTuning()
        {
            return servoAngleTuningSlider != null &&
                   servoAngleTuningSlider.isActiveAndEnabled &&
                   servoAngleTuningSlider.gameObject.activeInHierarchy &&
                   servoAngleTuningSlider.interactable;
        }

        private static bool IsActive(GameObject target)
        {
            return target != null && target.activeInHierarchy;
        }

        private void SetupDeviceLostDialog()
        {
            if (deviceLostDialog != null)
            {
                deviceLostDialog.SetActive(false);
            }

            if (deviceLostConfirmButton != null)
            {
                deviceLostConfirmButton.onClick.RemoveAllListeners();
                deviceLostConfirmButton.onClick.AddListener(OnDeviceLostConfirmClicked);
            }
        }

        private void OnDeviceLost(string reason)
        {
            EZLog.W(EZLog.Module.UI, $"Device lost: {reason}");

            if (deviceLostDialogVisible)
            {
                return;
            }

            deviceLostDialogVisible = true;

            if (deviceLostMessageText != null)
            {
                deviceLostMessageText.text = "设备失联，请重启分药机。";
            }

            if (deviceLostDialog != null)
            {
                deviceLostDialog.SetActive(true);
            }
            else
            {
                FireAndForget(ResetAndReturnHomeAfterDeviceLostAsync());
            }
        }

        private void OnDeviceLostConfirmClicked()
        {
            FireAndForget(ResetAndReturnHomeAfterDeviceLostAsync());
        }

        private async Task ResetAndReturnHomeAfterDeviceLostAsync()
        {
            if (deviceLostDialog != null)
            {
                deviceLostDialog.SetActive(false);
            }
            deviceLostDialogVisible = false;

            var main = MainController.Instance;
            if (main != null)
            {
                await main.ResetAfterDeviceLostAsync();
            }

            if (SceneManager.GetActiveScene().name != homeSceneName)
            {
                await LoadSceneAsyncSafe(homeSceneName);
            }
        }

        private void UnsubscribeEvents()
        {
            var main = MainController.Instance;
            if (main != null)
            {
                main.PatientsUpdated -= RenderPatientButtons;
                main.DispensingProgressChanged -= UpdateDispenseUI;
                main.DispensingError -= ShowDispenseError;
                main.DispensingCompleted -= OnDispenseCompleted;
                main.PlateSwitchRequired -= OnPlateSwitchRequired;
                main.PillCalibrationRequired -= OnPillCalibrationRequired;
                main.MedicineSkipped -= OnMedicineSkipped;
                main.SkipConfirmRequired -= OnSkipConfirmRequired;
                main.DeviceLost -= OnDeviceLost;
            }

            if (scanner != null)
            {
                scanner.OnBoxVerified -= OnBoxVerified;
                scanner.OnBoxMismatch -= OnBoxMismatch;
                scanner.OnScanError -= OnScanError;
            }

            var dispenser = FindObjectOfType<DispenserController>();
            if (dispenser != null)
            {
                dispenser.OnConnectionStateChanged -= OnDispenserConnectionChanged;
                dispenser.OnPauseStateChanged -= UpdatePauseButtonUI;
            }

            if (lightBarRoutine != null)
            {
                StopCoroutine(lightBarRoutine);
            }
        }

        private void FireAndForget(Task task)
        {
            async void Wrapper()
            {
                try
                {
                    await task;
                }
                catch (Exception e)
                {
                    EZLog.E(EZLog.Module.UI, $"Async error: {e.Message}");
                }
            }

            Wrapper();
        }

        /// <summary>
        /// Awaitable wrapper for Unity scene loading.
        /// </summary>
        private static async Task LoadSceneAsyncSafe(string sceneName)
        {
            var op = SceneManager.LoadSceneAsync(sceneName);
            if (op == null)
            {
                EZLog.E(EZLog.Module.UI, $"Failed to load scene: {sceneName}");
                return;
            }

            while (!op.isDone)
            {
                await Task.Yield();
            }
        }

        /// <summary>
        /// 判断药品时间是否属于饭后/随餐/任意时间
        /// </summary>
        private bool IsAfterMealTiming(string timing)
        {
            if (string.IsNullOrEmpty(timing)) return false;
            return string.Equals(timing, "after", StringComparison.OrdinalIgnoreCase)
                || string.Equals(timing, "after_meal", StringComparison.OrdinalIgnoreCase)
                || string.Equals(timing, "with_meal", StringComparison.OrdinalIgnoreCase)
                || string.Equals(timing, "anytime", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 寻找弹窗内的 Message 文本组件的辅助方法
        /// </summary>
        private Text FindDialogMessageText(GameObject dialog, string defaultNamePattern)
        {
            if (dialog == null) return null;
            var texts = dialog.GetComponentsInChildren<Text>(true);
            
            // 1. 优先寻找命名中包含特定关键字的组件 (如 "Message", "Content", "Body")
            var matchingByName = texts.FirstOrDefault(t => t.name.IndexOf(defaultNamePattern, StringComparison.OrdinalIgnoreCase) >= 0);
            if (matchingByName != null) return matchingByName;
            
            // 2. 备选方案：寻找第一个不属于 Button 按钮子物体的 Text 组件
            return texts.FirstOrDefault(t => t.GetComponentInParent<Button>() == null);
        }

        #endregion
    }
}
