using System;
using System.Collections.Generic;
using UnityEngine;

namespace EZDose
{
    /// <summary>
    /// Development-only helper to focus EZLog output on a chosen set of modules,
    /// so investigating one flow (e.g. turntable speed) is not drowned out by others.
    ///
    /// Usage: attach to a GameObject in the first-loaded scene, tick the modules you
    /// care about in the Inspector, and press Play. On Awake it lowers MinLevel and
    /// disables every module except the focused ones. Right-click the component for
    /// "Apply Focus" / "Show All Modules" context-menu actions at runtime.
    ///
    /// Active only in Editor / Development builds, so it can never accidentally
    /// suppress Info/Warning/Error logs in a Release build.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class LogFocus : MonoBehaviour
    {
        [Tooltip("Minimum level to show. Debug captures the speed/servo calc & send logs.")]
        [SerializeField] private EZLog.Level minLevel = EZLog.Level.Debug;

        [Tooltip("Only these modules stay enabled. Leave the list empty to show ALL modules.")]
        [SerializeField] private List<EZLog.Module> focusModules = new List<EZLog.Module>
        {
            EZLog.Module.Calibration,
            EZLog.Module.Dispenser,
            EZLog.Module.Main,
        };

        [Tooltip("Re-apply automatically on Awake (needed because domain reload resets EZLog).")]
        [SerializeField] private bool applyOnAwake = true;

        private void Awake()
        {
            if (applyOnAwake)
                Apply();
        }

        /// <summary>
        /// Lower MinLevel and enable only the focused modules (all others muted).
        /// If focusModules is empty, behaves like ShowAll().
        /// </summary>
        [ContextMenu("Apply Focus")]
        public void Apply()
        {
#if !(UNITY_EDITOR || DEVELOPMENT_BUILD)
            // In Release builds, never mute modules: Info/Warning/Error must survive.
            return;
#else
            EZLog.MinLevel = minLevel;

            if (focusModules == null || focusModules.Count == 0)
            {
                ShowAll();
                return;
            }

            var keep = new HashSet<EZLog.Module>(focusModules);
            foreach (EZLog.Module m in Enum.GetValues(typeof(EZLog.Module)))
                EZLog.SetModuleEnabled(m, keep.Contains(m));

            // Bypass EZLog here so this confirmation shows even if Config is muted.
            UnityEngine.Debug.Log(
                $"[LogFocus] MinLevel={minLevel}; showing only: {string.Join(", ", focusModules)}");
#endif
        }

        /// <summary>Re-enable every module (default EZLog state).</summary>
        [ContextMenu("Show All Modules")]
        public void ShowAll()
        {
            foreach (EZLog.Module m in Enum.GetValues(typeof(EZLog.Module)))
                EZLog.SetModuleEnabled(m, true);

            UnityEngine.Debug.Log("[LogFocus] All modules enabled.");
        }
    }
}
