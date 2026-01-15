using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using EZDose.Prescriptions;
using EZDose.Hardware;

namespace EZDose.MainFlow
{
    /// <summary>
    /// Central coordinator for the dispensing flow.
    /// Keeps track of patients, builds dispensing plans, and drives the hardware.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class MainController : MonoBehaviour
    {
        public static MainController Instance { get; private set; }

        [Header("Server")]
        [SerializeField] private string serverUrl = "http://127.0.0.1:5000";
        [SerializeField] private int maxDispensingDays = 7;
        [SerializeField] private int expiryDaysThreshold = 2;

        [Header("Auto Refresh")]
        [Tooltip("Enable automatic refresh of patient list from server.")]
        [SerializeField] private bool enableAutoRefresh = true;

        [Tooltip("Interval in seconds between automatic refreshes. Recommended: 30-60 seconds.")]
        [SerializeField] private float autoRefreshInterval = 30f;

        [Tooltip("Minimum interval allowed (in seconds) to prevent server overload.")]
        [SerializeField] private float minRefreshInterval = 10f;

        [Header("Hardware")]
        [SerializeField] private DispenserController dispenserController;

        // Patient list updates
        public event Action<List<PatientStatus>> PatientsUpdated;
        
        // Progress events for UI binding
        public event Action<DispensingProgressInfo> DispensingProgressChanged;
        public event Action<string> DispensingError;
        public event Action DispensingCompleted;
        public event Action<int> PlateSwitchRequired;

        private PrescriptionManager prescriptionManager;
        private readonly Dictionary<string, PatientStatus> patientStatus = new Dictionary<string, PatientStatus>(StringComparer.OrdinalIgnoreCase);

        // Threshold in days before medicine expiry to trigger dispensing
        // Medicines expiring within this many days will be flagged for dispensing
        

        private PatientStatus currentPatient;
        private DispensingPlan currentPlan;

        private bool isDispensing;
        private TaskCompletionSource<bool> dispenseTcs;
        private TaskCompletionSource<bool> plateReadyTcs;

        // Flag to indicate we're actively waiting for a pill matrix dispensing to complete
        // This prevents false completion triggers from configuration command responses
        private bool isWaitingForDispensingComplete;

        private string currentMedicineName = string.Empty;
        private int currentPlate = 1;
        private int currentMedicineTotal = 0;

        // Auto-refresh coroutine reference for cleanup
        private Coroutine autoRefreshCoroutine;

        // Flag to pause auto-refresh during active dispensing
        private bool isAutoRefreshPaused;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            prescriptionManager = new PrescriptionManager(serverUrl);

            if (dispenserController == null)
            {
                dispenserController = FindObjectOfType<DispenserController>();
            }

            BindDispenserEvents(true);
            ConnectDispenser();

            FireAndForget(RefreshPatientsAsync());

            // Start automatic refresh if enabled
            StartAutoRefresh();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
            BindDispenserEvents(false);

            // Stop auto-refresh coroutine to prevent memory leaks
            StopAutoRefresh();
        }

        private void BindDispenserEvents(bool subscribe)
        {
            if (dispenserController == null)
            {
                return;
            }

            if (subscribe)
            {
                dispenserController.OnDispensingComplete += OnMachineDispensingComplete;
                dispenserController.OnCountError += OnMachineCountError;
                dispenserController.OnPillCountUpdate += OnMachinePillCountUpdate;
            }
            else
            {
                dispenserController.OnDispensingComplete -= OnMachineDispensingComplete;
                dispenserController.OnCountError -= OnMachineCountError;
                dispenserController.OnPillCountUpdate -= OnMachinePillCountUpdate;
            }
        }

        /// <summary>
        /// Try to connect to the dispenser as soon as the app opens.
        /// </summary>
        private void ConnectDispenser()
        {
            if (dispenserController == null)
            {
                Debug.LogWarning("[MainController] DispenserController is missing in the scene");
                return;
            }

            var ok = dispenserController.Initialize();
            if (!ok)
            {
                Debug.LogWarning("[MainController] Failed to initialize dispenser. Will retry on next command.");
            }
        }

        #region Auto Refresh

        /// <summary>
        /// Starts the automatic refresh coroutine if enabled.
        /// The coroutine periodically fetches patient data from the server.
        /// </summary>
        public void StartAutoRefresh()
        {
            // Stop any existing coroutine before starting a new one
            StopAutoRefresh();

            if (!enableAutoRefresh)
            {
                Debug.Log("[MainController] Auto-refresh is disabled in settings.");
                return;
            }

            // Ensure interval is not below minimum to prevent server overload
            var interval = Mathf.Max(autoRefreshInterval, minRefreshInterval);
            autoRefreshCoroutine = StartCoroutine(AutoRefreshCoroutine(interval));
            Debug.Log($"[MainController] Auto-refresh started with interval: {interval} seconds");
        }

        /// <summary>
        /// Stops the automatic refresh coroutine if running.
        /// Call this when leaving the Home scene or when manual control is needed.
        /// </summary>
        public void StopAutoRefresh()
        {
            if (autoRefreshCoroutine != null)
            {
                StopCoroutine(autoRefreshCoroutine);
                autoRefreshCoroutine = null;
                Debug.Log("[MainController] Auto-refresh stopped.");
            }
        }

        /// <summary>
        /// Temporarily pauses auto-refresh during active dispensing operations.
        /// This prevents interference during critical hardware communication.
        /// </summary>
        public void PauseAutoRefresh()
        {
            isAutoRefreshPaused = true;
            Debug.Log("[MainController] Auto-refresh paused.");
        }

        /// <summary>
        /// Resumes auto-refresh after dispensing operations complete.
        /// </summary>
        public void ResumeAutoRefresh()
        {
            isAutoRefreshPaused = false;
            Debug.Log("[MainController] Auto-refresh resumed.");
        }

        /// <summary>
        /// Sets whether auto-refresh is enabled and restarts the coroutine if needed.
        /// </summary>
        /// <param name="enabled">True to enable auto-refresh, false to disable.</param>
        public void SetAutoRefreshEnabled(bool enabled)
        {
            enableAutoRefresh = enabled;

            if (enabled)
            {
                StartAutoRefresh();
            }
            else
            {
                StopAutoRefresh();
            }
        }

        /// <summary>
        /// Updates the auto-refresh interval and restarts the coroutine with the new timing.
        /// </summary>
        /// <param name="intervalSeconds">New interval in seconds (will be clamped to minimum).</param>
        public void SetAutoRefreshInterval(float intervalSeconds)
        {
            autoRefreshInterval = Mathf.Max(intervalSeconds, minRefreshInterval);

            // Restart coroutine with new interval if currently running
            if (enableAutoRefresh && autoRefreshCoroutine != null)
            {
                StartAutoRefresh();
            }
        }

        /// <summary>
        /// Coroutine that periodically refreshes patient data from the server.
        /// Automatically skips refresh cycles during active dispensing.
        /// </summary>
        /// <param name="intervalSeconds">Time between refresh attempts.</param>
        private System.Collections.IEnumerator AutoRefreshCoroutine(float intervalSeconds)
        {
            // Wait for first interval before starting (initial refresh is done in Start)
            yield return new WaitForSeconds(intervalSeconds);

            while (true)
            {
                // Skip refresh if paused (during dispensing) or if already dispensing
                if (!isAutoRefreshPaused && !isDispensing)
                {
                    Debug.Log("[MainController] Auto-refresh: Fetching patient data from server...");
                    FireAndForget(RefreshPatientsAsync());
                }
                else
                {
                    Debug.Log("[MainController] Auto-refresh: Skipped (dispensing in progress or paused).");
                }

                yield return new WaitForSeconds(intervalSeconds);
            }
        }

        #endregion

        /// <summary>
        /// Refresh patient list from the server.
        /// Only includes patients who need dispensing today (medicines expiring within threshold).
        /// </summary>
        public async Task RefreshPatientsAsync(bool keepCompleted = true)
        {
            if (prescriptionManager == null)
            {
                return;
            }

            var success = await prescriptionManager.RefreshFromServerAsync();
            if (!success)
            {
                Debug.LogWarning("[MainController] Failed to pull patients from server.");
            }

            Debug.Log($"[MainController] Received {prescriptionManager.CachedRecords.Count} prescription records from server.");
            foreach (var rx in prescriptionManager.CachedRecords)
            {
                Debug.Log($"[MainController] RX: patient_id={rx.patient_id}, patient_name={rx.patient_name}, medicine_name={rx.medicine_name}, last_dispensed_expiry_date={rx.last_dispensed_expiry_date}, is_active={rx.is_active}");
            }

            patientStatus.Clear();

            // Group prescription records by patient_id to get unique patients
            var grouped = prescriptionManager.CachedRecords
                .GroupBy(r => r.patient_id)
                .Select(g => new PatientInfo
                {
                    PatientId = g.Key,
                    PatientName = g.First().patient_name
                })
                .OrderBy(p => p.PatientName);

            int addedCount = 0;
            foreach (var patient in grouped)
            {
                // Count how many medicines need dispensing today
                var medicineCount = CountMedicinesNeedingDispensing(patient.PatientId, expiryDaysThreshold);
                Debug.Log($"[MainController] Patient {patient.PatientId} ({patient.PatientName}) needs dispensing for {medicineCount} medicines.");

                // Only include patients who have medicines needing dispensing today
                if (medicineCount > 0)
                {
                    patientStatus[patient.PatientId] = new PatientStatus(patient, false, medicineCount);
                    addedCount++;
                }
            }

            Debug.Log($"[MainController] Added {addedCount} patients needing dispensing to UI.");
            PatientsUpdated?.Invoke(GetPatientsSnapshot());
        }

        /// <summary>
        /// Count how many medicines need dispensing today for a patient.
        /// A medicine needs dispensing if:
        /// 1. last_dispensed_expiry_date is empty/null (never dispensed), OR
        /// 2. last_dispensed_expiry_date is within threshold days from today
        /// </summary>
        private int CountMedicinesNeedingDispensing(string patientId, int thresholdDays)
        {
            if (prescriptionManager == null)
            {
                return 0;
            }

            if (!prescriptionManager.TryGetPatientPrescription(patientId, out var prescription))
            {
                return 0;
            }

            if (prescription.Medicines == null || prescription.Medicines.Count == 0)
            {
                return 0;
            }

            var today = DateTime.Today;
            int count = 0;

            foreach (var medicine in prescription.Medicines)
            {
                if (!medicine.IsActive)
                {
                    continue;
                }

                // If last_dispensed_expiry_date is empty/null, the medicine has never been dispensed
                // and definitely needs dispensing now
                if (string.IsNullOrWhiteSpace(medicine.LastDispensedExpiryDate))
                {
                    count++;
                    continue;
                }

                // If we can parse the expiry date, check if it's within threshold
                if (DateTime.TryParse(medicine.LastDispensedExpiryDate, out var expiryDate))
                {
                    var daysUntilExpiry = (expiryDate.Date - today).Days;

                    if (daysUntilExpiry <= thresholdDays)
                    {
                        count++;
                    }
                }
                else
                {
                    // If date parsing fails, treat as needing dispensing (data issue, be safe)
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Check if a patient needs dispensing today.
        /// A patient needs dispensing if any active medicine expires within the threshold days.
        /// Matches Python's get_patients_for_today logic: days_until_expiry <= threshold.
        /// </summary>
        private bool CheckIfPatientNeedsDispensingToday(string patientId, int thresholdDays)
        {
            if (prescriptionManager == null)
            {
                return false;
            }

            // Try to get patient prescription data
            if (!prescriptionManager.TryGetPatientPrescription(patientId, out var prescription))
            {
                return false;
            }

            // If patient has no medicines, they don't need dispensing
            if (prescription.Medicines == null || prescription.Medicines.Count == 0)
            {
                return false;
            }

            var today = DateTime.Today;

            // Check if any active medicine expires within threshold
            foreach (var medicine in prescription.Medicines)
            {
                if (!medicine.IsActive)
                {
                    continue; // Skip inactive medicines
                }

                // Parse expiry date
                if (DateTime.TryParse(medicine.LastDispensedExpiryDate, out var expiryDate))
                {
                    var daysUntilExpiry = (expiryDate.Date - today).Days;

                    // If medicine expires within threshold, patient needs dispensing
                    if (daysUntilExpiry <= thresholdDays)
                    {
                        return true;
                    }
                }
            }

            // No medicines expiring within threshold
            return false;
        }

        /// <summary>
        /// Expose a snapshot to UI without sharing internal references.
        /// </summary>
        public List<PatientStatus> GetPatients()
        {
            return GetPatientsSnapshot();
        }

        private List<PatientStatus> GetPatientsSnapshot()
        {
            return patientStatus.Values
                .OrderBy(p => p.PatientName)
                .Select(p => p.Clone())
                .ToList();
        }

        /// <summary>
        /// Set the current patient before scanning and dispensing.
        /// </summary>
        public bool TrySelectPatient(string patientId, out PatientStatus status)
        {
            status = null;
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return false;
            }

            if (!patientStatus.TryGetValue(patientId, out var record))
            {
                return false;
            }

            currentPatient = record;
            status = record;
            return true;
        }

        public PatientStatus GetCurrentPatient()
        {
            return currentPatient;
        }

        /// <summary>
        /// Build a dispensing plan for the selected patient.
        /// </summary>
        public Task<bool> PreparePlanAsync()
        {
            if (currentPatient == null)
            {
                DispensingError?.Invoke("尚未选择患者");
                return Task.FromResult(false);
            }

            if (!prescriptionManager.TryGenerateDispensingPlan(currentPatient.PatientId, maxDispensingDays, out var plan))
            {
                DispensingError?.Invoke("无法生成分药计划，请检查处方数据");
                return Task.FromResult(false);
            }

            currentPlan = plan;
            return Task.FromResult(true);
        }

        /// <summary>
        /// Start the dispensing routine. This runs plate by plate.
        /// </summary>
        public async Task<bool> StartDispensingAsync()
        {
            if (isDispensing)
            {
                return false;
            }

            if (dispenserController == null)
            {
                DispensingError?.Invoke("分药机未连接");
                return false;
            }

            if (currentPlan == null)
            {
                var ok = await PreparePlanAsync();
                if (!ok)
                {
                    return false;
                }
            }

            isDispensing = true;

            var plates = new List<(List<DispensingMedicine> meds, int plate)>
            {
                (currentPlan.MedicinesPlate1, 1),
                (currentPlan.MedicinesPlate2, 2)
            };

            foreach (var entry in plates)
            {
                if (entry.meds == null || entry.meds.Count == 0)
                {
                    continue;
                }

                if (entry.plate == 2 && currentPlan.MedicinesPlate1.Count > 0)
                {
                    // Open tray so user can remove old plate
                    var openTcs = new TaskCompletionSource<bool>();
                    dispenserController.OpenTray(success => openTcs.TrySetResult(success));
                    await openTcs.Task;
                    
                    // Ask the user to swap the plate before continuing.
                    PlateSwitchRequired?.Invoke(entry.plate);
                    plateReadyTcs = new TaskCompletionSource<bool>();
                    await plateReadyTcs.Task;
                    
                    // Close tray after new plate is inserted
                    var closeTcs = new TaskCompletionSource<bool>();
                    dispenserController.CloseTray(success => closeTcs.TrySetResult(success));
                    await closeTcs.Task;
                }

                foreach (var med in entry.meds)
                {
                    var ok = await DispenseMedicineAsync(med, entry.plate);
                    if (!ok)
                    {
                        isDispensing = false;
                        return false;
                    }
                }
            }

            // All medicines dispensed successfully
            // Pause the turntable motor to stop rotation
            Debug.Log("[MainController] All medicines dispensed, pausing turntable motor");
            await PauseAsync();
            
            // Open tray for user to collect pills
            Debug.Log("[MainController] Opening tray for pill collection");
            await OpenTrayAsync();
            
            // Update server and mark patient complete
            await prescriptionManager.PushAllChangesAsync();
            MarkPatientCompleted(currentPatient.PatientId);

            isDispensing = false;
            
            // Fire completion event
            DispensingCompleted?.Invoke();
            return true;
        }

        /// <summary>
        /// Called by UI after the user has replaced the plate for plate 2.
        /// </summary>
        public void ConfirmPlateReady()
        {
            plateReadyTcs?.TrySetResult(true);
        }

        private async Task<bool> DispenseMedicineAsync(DispensingMedicine med, int plate)
        {
            if (med == null)
            {
                return false;
            }

            // Configure dispenser hardware based on pill size before dispensing
            // This adjusts servo angle and motor speed to match pill dimensions
            Debug.Log($"[MainController] Configuring dispenser for pill size: {med.PillSize}");
            var configured = await ConfigureDispenserForPillSize(med.PillSize);
            if (!configured)
            {
                Debug.LogWarning($"[MainController] Failed to configure for pill size {med.PillSize}, continuing anyway...");
            }

            var matrix = ToByteMatrix(med.PillMatrix);
            currentMedicineTotal = CountPills(matrix);
            currentMedicineName = med.MedicineName;
            currentPlate = plate;

            var progress = new DispensingProgressInfo
            {
                PatientName = currentPatient?.PatientName ?? string.Empty,
                MedicineName = currentMedicineName,
                PlateNumber = plate,
                TotalPills = currentMedicineTotal,
                DispensedPills = 0,
                Progress = 0f
            };
            DispensingProgressChanged?.Invoke(progress);

            var success = await SendMatrixAndWaitAsync(matrix);
            if (success)
            {
                prescriptionManager.ApplyDispensingResult(med.MedicineName);
            }

            return success;
        }

        /// <summary>
        /// Configure dispenser motor speed and servo angle for different pill sizes.
        /// Small pills need slower speed and smaller opening.
        /// Large pills need faster speed and bigger opening.
        /// </summary>
        private async Task<bool> ConfigureDispenserForPillSize(string pillSize)
        {
            if (dispenserController == null)
            {
                return false;
            }

            // Default to Medium if pill size is not recognized
            float motorSpeed;
            float servoAngle;

            switch (pillSize?.ToUpper())
            {
                case "S": // Small pills: gentle and slow
                    motorSpeed = 0.3f;
                    servoAngle = 0.8f;
                    break;
                    
                case "L": // Large pills: fast with wide opening
                    motorSpeed = 0.8f;
                    servoAngle = 0.2f;
                    break;
                    
                case "M": // Medium pills: balanced settings
                default:
                    motorSpeed = 0.5f;
                    servoAngle = 0.5f;
                    break;
            }

            Debug.Log($"[MainController] Configuring dispenser for pill size: {pillSize}");

            // CRITICAL: Temporarily unsubscribe from completion event during configuration
            // Configuration commands also send machine_state:FINISH which should NOT trigger dispensing completion
            dispenserController.OnDispensingComplete -= OnMachineDispensingComplete;

            try
            {
                Debug.Log($"[MainController] Setting motor speed: {motorSpeed}, servo angle: {servoAngle}");

                // Set turntable motor speed first (controls how fast pills rotate)
                var speedResult = await RunDispenserAction(callback => 
                    dispenserController.SetTurntableSpeed(motorSpeed, callback));
                
                if (!speedResult)
                {
                    Debug.LogWarning("[MainController] Failed to set motor speed");
                    return false;
                }

                // Then set servo angle (controls opening size for pills to drop through)
                // Must wait for previous command to finish before sending next one
                var angleResult = await RunDispenserAction(callback => 
                    dispenserController.SetServoAngle(servoAngle, callback));

                if (!angleResult)
                {
                    Debug.LogWarning("[MainController] Failed to set servo angle");
                    return false;
                }
                
                Debug.Log("[MainController] Dispenser configuration completed successfully");
                return true;
            }
            finally
            {
                // Re-subscribe to completion event after configuration is done
                dispenserController.OnDispensingComplete += OnMachineDispensingComplete;
            }
        }

        private static int CountPills(byte[,] matrix)
        {
            var total = 0;
            foreach (var value in matrix)
            {
                total += value;
            }
            return total;
        }

        private byte[,] ToByteMatrix(int[,] source)
        {
            var result = new byte[4, 7];
            if (source == null)
            {
                return result;
            }

            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    var value = source[i, j];
                    result[i, j] = (byte)Mathf.Clamp(value, 0, byte.MaxValue);
                }
            }

            return result;
        }

        private async Task<bool> SendMatrixAndWaitAsync(byte[,] matrix)
        {
            // Create a fresh TaskCompletionSource for this specific dispensing operation
            // This ensures we only respond to FINISH messages for THIS matrix send
            dispenseTcs = new TaskCompletionSource<bool>();
            
            // CRITICAL: Reset the waiting flag before sending
            // This prevents stale FINISH messages from completing the wrong operation
            isWaitingForDispensingComplete = false;

            var sendTcs = new TaskCompletionSource<bool>();
            dispenserController.SendPillMatrix(matrix, success =>
            {
                sendTcs.TrySetResult(success);
            });

            // Wait for send confirmation
            var sendSuccess = await sendTcs.Task;
            if (!sendSuccess)
            {
                Debug.LogWarning("[MainController] Failed to send pill matrix");
                return false;
            }

            // Only start waiting for completion AFTER the matrix is successfully sent
            // This prevents configuration command FINISH responses from triggering completion
            isWaitingForDispensingComplete = true;
            Debug.Log("[MainController] Matrix sent successfully, now waiting for dispensing completion");

            return await WaitWithTimeout(dispenseTcs.Task, TimeSpan.FromSeconds(600));
        }

        private async Task<bool> WaitWithTimeout(Task<bool> task, TimeSpan timeout)
        {
            var delay = Task.Delay(timeout);
            var finished = await Task.WhenAny(task, delay);

            if (finished == delay)
            {
                DispensingError?.Invoke("分药超时，请检查设备连接");
                return false;
            }

            return await task;
        }

        private void OnMachineDispensingComplete()
        {
            // Only respond to FINISH messages if we're actively waiting for dispensing completion
            // This prevents false triggers from configuration commands or stale messages
            if (!isWaitingForDispensingComplete)
            {
                Debug.Log("[MainController] Ignoring FINISH message - not waiting for dispensing completion");
                return;
            }

            Debug.Log("[MainController] Dispensing complete for current medicine");
            isWaitingForDispensingComplete = false;
            dispenseTcs?.TrySetResult(true);
        }

        private void OnMachineCountError()
        {
            // Only respond to error messages if we're actively waiting for dispensing completion
            if (!isWaitingForDispensingComplete)
            {
                Debug.Log("[MainController] Ignoring count error - not waiting for dispensing completion");
                return;
            }

            isWaitingForDispensingComplete = false;
            dispenseTcs?.TrySetResult(false);
            DispensingError?.Invoke("计数错误，请重新检查药品放置");
        }

        private void OnMachinePillCountUpdate(int dispensed)
        {
            var progress = new DispensingProgressInfo
            {
                PatientName = currentPatient?.PatientName ?? string.Empty,
                MedicineName = currentMedicineName,
                PlateNumber = currentPlate,
                TotalPills = currentMedicineTotal,
                DispensedPills = dispensed,
                Progress = currentMedicineTotal > 0 ? (float)dispensed / currentMedicineTotal : 0f
            };

            DispensingProgressChanged?.Invoke(progress);
        }

        public Task<bool> OpenTrayAsync()
        {
            if (dispenserController == null || !dispenserController.EnsureConnected())
            {
                Debug.LogError("[MainController] Cannot open tray - dispenser not connected");
                return Task.FromResult(false);
            }

            return RunDispenserAction(dispenserController.OpenTray);
        }

        public Task<bool> CloseTrayAsync()
        {
            if (dispenserController == null || !dispenserController.EnsureConnected())
            {
                Debug.LogError("[MainController] Cannot close tray - dispenser not connected");
                return Task.FromResult(false);
            }

            return RunDispenserAction(dispenserController.CloseTray);
        }

        public Task<bool> PauseAsync()
        {
            if (dispenserController == null)
            {
                return Task.FromResult(false);
            }

            return RunDispenserAction(dispenserController.PauseDispenser);
        }

        public Task<bool> ResetDispenserAsync()
        {
            if (dispenserController == null)
            {
                return Task.FromResult(false);
            }

            return RunDispenserAction(dispenserController.ResetDispenser);
        }

        private Task<bool> RunDispenserAction(Action<Action<bool>> action)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (action == null)
            {
                tcs.TrySetResult(false);
                return tcs.Task;
            }

            action(success =>
            {
                tcs.TrySetResult(success);
            });

            return WaitWithTimeout(tcs.Task, TimeSpan.FromSeconds(10));
        }

        private void MarkPatientCompleted(string patientId)
        {
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return;
            }

            // Remove patient from list entirely (button will disappear)
            // Patient no longer needs dispensing today since all medicines are dispensed
            if (patientStatus.ContainsKey(patientId))
            {
                patientStatus.Remove(patientId);
                Debug.Log($"[MainController] Patient {patientId} completed and removed from today's list");
                
                // Notify UI to update patient list (button will be removed)
                PatientsUpdated?.Invoke(GetPatientsSnapshot());
            }
        }

        /// <summary>
        /// Helper to run async work from non-async Unity callbacks.
        /// </summary>
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
                    Debug.LogError($"[MainController] Async error: {e.Message}");
                }
            }

            Wrapper();
        }
    }

    [Serializable]
    public class PatientStatus
    {
        // Patient identification and dispensing status
        // Note: RFID removed - patients identified by 6-digit ID only
        public string PatientName;
        public string PatientId;        // 6-digit zero-padded format (e.g., "000001")
        public bool IsCompleted;        // Whether dispensing is done for this patient
        public int MedicineCount;       // Number of medicines needing dispensing

        public PatientStatus() { }

        public PatientStatus(PatientInfo info, bool completed, int medicineCount = 0)
        {
            PatientName = info.PatientName;
            PatientId = info.PatientId;
            IsCompleted = completed;
            MedicineCount = medicineCount;
        }

        /// <summary>
        /// Create a deep copy to avoid sharing references with UI.
        /// </summary>
        public PatientStatus Clone()
        {
            return new PatientStatus
            {
                PatientName = PatientName,
                PatientId = PatientId,
                IsCompleted = IsCompleted,
                MedicineCount = MedicineCount
            };
        }
    }

    [Serializable]
    public class DispensingProgressInfo
    {
        public string PatientName;
        public string MedicineName;
        public int PlateNumber;
        public int TotalPills;
        public int DispensedPills;
        public float Progress;
    }
}
