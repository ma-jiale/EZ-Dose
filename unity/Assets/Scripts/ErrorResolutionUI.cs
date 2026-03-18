using System;
using UnityEngine;
using UnityEngine.UI;
using EZDose.MainFlow;

namespace EZDose.UI
{
    /// <summary>
    /// Handles the user interaction for resolving dispensing count errors.
    /// Manages the diagonal that prompts the user to manual fix pills.
    /// </summary>
    public class ErrorResolutionUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Dialog shown when a count error occurs and manual intervention is needed.")]
        [SerializeField] private GameObject errorResolutionDialog;
        
        [Tooltip("Button to confirm the error has been resolved.")]
        [SerializeField] private Button errorResolutionConfirmButton;
        
        [Tooltip("Text component to display the error message.")]
        [SerializeField] private Text errorResolutionText;

        private void Start()
        {
            // Subscribe to the global controller events
            var main = MainController.Instance;
            if (main != null)
            {
                main.ErrorResolutionRequired += OnErrorResolutionRequired;
            }
            
            // Ensure dialog is hidden on start
            if (errorResolutionDialog != null)
            {
                errorResolutionDialog.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe to prevent memory leaks
            if (MainController.Instance != null)
            {
                MainController.Instance.ErrorResolutionRequired -= OnErrorResolutionRequired;
            }
        }

        private void OnErrorResolutionRequired(string message)
        {
            if (errorResolutionDialog != null)
            {
                errorResolutionDialog.SetActive(true);
            }
            
            if (errorResolutionText != null)
            {
                errorResolutionText.text = message;
            }

            if (errorResolutionConfirmButton != null)
            {
                // Clear previous listeners to avoid stacking them
                errorResolutionConfirmButton.onClick.RemoveAllListeners();
                
                // Add new listener for this specific error instance
                errorResolutionConfirmButton.onClick.AddListener(OnConfirmClicked);
            }
        }

        private void OnConfirmClicked()
        {
            EZLog.I(EZLog.Module.UI, "User confirmed error resolution");
            
            var main = MainController.Instance;
            if (main != null)
            {
                // Inform MainController that user has fixed the issue
                main.ConfirmErrorResolution();
            }
            
            if (errorResolutionDialog != null)
            {
                errorResolutionDialog.SetActive(false);
            }
        }
    }
}


