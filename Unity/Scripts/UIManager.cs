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
        [SerializeField] private GameObject mismatchDialog;
        [SerializeField] private Button mismatchHomeButton;
        [SerializeField] private Button mismatchRetryButton;

        [Header("Dispense UI")]
        [SerializeField] private Text totalPillsText;
        [SerializeField] private Text medicineNameText;
        [SerializeField] private Text patientNameText;
        [SerializeField] private Text progressPercentText;
        [Tooltip("进度条填充图片，需设置 Image Type 为 Filled")]
        [SerializeField] private Image progressFillImage;
        [SerializeField] private RawImage pillPreview;
        [SerializeField] private Button captureBackgroundButton;
        [SerializeField] private GameObject plateSwitchDialog;
        [SerializeField] private Button plateSwitchConfirmButton;
        [SerializeField] private GameObject completeDialog;
        [SerializeField] private Button completeDialogConfirmButton;
        [SerializeField] private PillCounterController pillCounterController;
        
        [Tooltip("药片校准对话框")]
        [SerializeField] private PillCalibrationDialog pillCalibrationDialog;

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

                Debug.Log($"[UIManager] Switched to sub-page: {targetPage}");
            }
            else
            {
                Debug.LogWarning($"[UIManager] Sub-page not found or not assigned: {targetPage}");
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
                Debug.Log("[UIManager] Device management dialog opened");
            }
            else
            {
                Debug.LogWarning("[UIManager] DeviceManagerUI not assigned or found");
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
                Debug.Log("[UIManager] Device management dialog closed");
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
                Debug.LogWarning("[UIManager] Dispenser not connected. Showing prompt.");
                if (connectDispenserDialog != null)
                {
                    connectDispenserDialog.SetActive(true);
                }
                return;
            }

            if (!main.TrySelectPatient(patientId, out _))
            {
                Debug.LogWarning("[UIManager] Failed to select patient");
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
                backToHomeButton.onClick.AddListener(() => SceneManager.LoadScene(homeSceneName));
            }

            if (scanner != null && patient != null)
            {
                scanner.OnBoxVerified += OnBoxVerified;
                scanner.OnBoxMismatch += OnBoxMismatch;
                scanner.OnScanError += OnScanError;
                scanner.StartScanner(patient.PatientId);
            }

            if (lightBar != null)
            {
                lightBarRoutine = StartCoroutine(AnimateLightBar());
            }
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
            if (main != null)
            {
                await main.OpenTrayAsync();
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
            Debug.LogWarning($"[UIManager] Scan error: {error}");
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
                mismatchHomeButton.onClick.AddListener(() => SceneManager.LoadScene(homeSceneName));
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

            var main = MainController.Instance;
            if (main != null)
            {
                await main.PreparePlanAsync();
                // await main.CloseTrayAsync();
            }

            await LoadSceneAsyncSafe(dispenseSceneName);
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
            }

            if (captureBackgroundButton != null && pillCounterController != null)
            {
                captureBackgroundButton.onClick.AddListener(() => pillCounterController.CaptureBackground());
            }

            if (main != null)
            {
                FireAndForget(main.StartDispensingAsync());
            }
        }

        /// <summary>
        /// MainController 触发校准事件时，直接调用对话框
        /// </summary>
        private void OnPillCalibrationRequired(EZDose.Prescriptions.DispensingMedicine medicine)
        {
            Debug.Log($"[UIManager] Calibration required for: {medicine.MedicineName}");
            
            if (pillCalibrationDialog != null)
            {
                pillCalibrationDialog.Show(medicine.MedicineName, medicine.PatientName, medicine.BedNumber);
            }
            else
            {
                Debug.LogError("[UIManager] PillCalibrationDialog is not assigned!");
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
                totalPillsText.text = $"{info.TotalPills}";
            }

            if (medicineNameText != null)
            {
                medicineNameText.text = info.MedicineName;
            }

            if (patientNameText != null)
            {
                patientNameText.text = string.IsNullOrEmpty(info.PatientName)
                    ? string.Empty
                    : $"所属患者： {info.PatientName}";
            }

            if (progressPercentText != null)
            {
                var percent = Mathf.RoundToInt(info.Progress * 100f);
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
            if (plateSwitchDialog != null)
            {
                plateSwitchDialog.SetActive(true);
            }

            if (plateSwitchConfirmButton != null)
            {
                plateSwitchConfirmButton.onClick.RemoveAllListeners();
                plateSwitchConfirmButton.onClick.AddListener(() =>
                {
                    var main = MainController.Instance;
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
                await main.OpenTrayAsync();
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
                await main.CloseTrayAsync();
            }

            await LoadSceneAsyncSafe(homeSceneName);
        }

        private void ShowDispenseError(string message)
        {
            Debug.LogWarning($"[UIManager] Dispense error: {message}");
            // For now, we show the same completion dialog style with the error message logged.
            if (completeDialog != null)
            {
                completeDialog.SetActive(true);
            }
        }

        #endregion

        #region Helpers

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
                    Debug.LogError($"[UIManager] Async error: {e.Message}");
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
                Debug.LogError($"[UIManager] Failed to load scene: {sceneName}");
                return;
            }

            while (!op.isDone)
            {
                await Task.Yield();
            }
        }

        #endregion
    }
}
