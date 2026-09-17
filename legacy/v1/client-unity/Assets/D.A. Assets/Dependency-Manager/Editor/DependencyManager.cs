using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DA_Assets.DM
{
    public class DependencyManager : AssetPostprocessor
    {
        private static bool _debug = false;

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            bool assetsChanged = importedAssets.Any(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) ||
                                 deletedAssets.Any(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)) ||
                                 movedAssets.Any(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));

            if (assetsChanged)
            {
                if (_debug) Debug.Log(DependencyManagerLocKey.log_script_change_detected.Localize());
                CheckAllDependencies();
            }
        }

        public static void CheckSingleItem(DependencyItem item)
        {
            if (_debug) Debug.Log(DependencyManagerLocKey.log_manual_check_started.Localize(item.name));

            if (ProcessSingleItem(item))
            {
                ApplyDefines(GetAllItems());
                AssetDatabase.SaveAssets();
            }
            else
            {
                if (_debug) Debug.Log(DependencyManagerLocKey.log_no_status_change.Localize(item.name));
            }
        }

        public static void CheckAllDependencies()
        {
            List<DependencyItem> allItems = GetAllItems();

            if (allItems.Count == 0)
            {
                return;
            }

            if (_debug) Debug.Log(DependencyManagerLocKey.log_dependency_items_found.Localize(allItems.Count));

            bool hasChanges = false;
            foreach (DependencyItem item in allItems)
            {
                if (ProcessSingleItem(item))
                {
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                if (_debug) Debug.Log(DependencyManagerLocKey.log_changes_detected.Localize());
                ApplyDefines(allItems);
                AssetDatabase.SaveAssets();
            }
            else
            {
                if (_debug) Debug.Log(DependencyManagerLocKey.log_dependencies_up_to_date.Localize());
            }
        }

        public static IReadOnlyList<DependencyItem> GetDependencyItems()
        {
            return GetAllItems();
        }

        public static void ForceEnableDependency(DependencyItem item)
        {
            SetDependencyState(item, true);
        }

        public static void ForceDisableDependency(DependencyItem item)
        {
            SetDependencyState(item, false);
        }

        private static bool ProcessSingleItem(DependencyItem item)
        {
            if (string.IsNullOrWhiteSpace(item.TypeAndAssembly) || string.IsNullOrWhiteSpace(item.ScriptingDefineSymbol))
            {
                return false;
            }

            bool oldStatus = item.IsEnabled;
            string oldPath = item.ScriptPath;

            bool newStatus = false;
            string newPath = "Not found";

            if (_debug) Debug.Log(DependencyManagerLocKey.log_processing_check_type.Localize(item.name, item.TypeAndAssembly));
            Type type = Type.GetType(item.TypeAndAssembly, false, true);

            if (type != null)
            {
                if (_debug) Debug.Log(DependencyManagerLocKey.log_processing_type_found.Localize(item.name, type.FullName, type.Assembly.GetName().Name));
                newStatus = true;
                newPath = FindSourcePathForType(type);
            }
            else
            {
                if (_debug) Debug.Log(DependencyManagerLocKey.log_processing_type_not_found.Localize(item.name, item.TypeAndAssembly));
            }

            if (item.DisabledManually && newStatus)
            {
                if (_debug) Debug.Log(DependencyManagerLocKey.log_manual_removal_protection.Localize(item.name));
                newStatus = false;
            }

            if (oldStatus != newStatus || oldPath != newPath)
            {
                string statusString = newStatus ? "ENABLED" : "DISABLED";
                if (_debug) Debug.Log(DependencyManagerLocKey.log_status_changed.Localize(item.name, item.ScriptingDefineSymbol, statusString, newPath));

                item.IsEnabled = newStatus;
                item.ScriptPath = newPath;
                EditorUtility.SetDirty(item);
                return true;
            }

            return false;
        }

        private static void ApplyDefines(List<DependencyItem> items)
        {
            var currentDefines = GetDefines()
                .Select(d => d?.Trim())
                .Where(d => !string.IsNullOrEmpty(d))
                .ToList();

            var desiredDefines = new HashSet<string>(currentDefines, StringComparer.Ordinal);

            var allManagedSymbols = items
                .Select(i => i.ScriptingDefineSymbol?.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct(StringComparer.Ordinal);

            foreach (string symbol in allManagedSymbols)
            {
                bool shouldBeEnabled = items.Any(i =>
                    string.Equals(i.ScriptingDefineSymbol?.Trim(), symbol, StringComparison.Ordinal) && i.IsEnabled);

                if (shouldBeEnabled)
                    desiredDefines.Add(symbol);
                else
                    desiredDefines.Remove(symbol);
            }

            string desiredList = desiredDefines.Count > 0 ? string.Join(", ", desiredDefines) : "None";
            if (_debug) Debug.Log(DependencyManagerLocKey.log_final_desired_defines.Localize(desiredList));
            SetDefines(desiredDefines.ToList());
        }

        private static List<DependencyItem> GetAllItems()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(DependencyItem)}");
            List<DependencyItem> items = new List<DependencyItem>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var item = AssetDatabase.LoadAssetAtPath<DependencyItem>(path);

                if (item != null)
                {
                    items.Add(item);
                }
            }

            return items;
        }

        private static string FindSourcePathForType(Type type)
        {
            if (type == null)
            {
                return "Not found";
            }

            string[] guids = AssetDatabase.FindAssets($"{type.Name} t:MonoScript");
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                MonoScript monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);

                if (monoScript != null && monoScript.GetClass() == type)
                {
                    return assetPath;
                }
            }

            try
            {
                string assemblyLocation = type.Assembly.Location;
                if (!string.IsNullOrEmpty(assemblyLocation))
                {
                    string projectPath = Path.GetFullPath(".").Replace('\\', '/');
                    string locationNormalized = assemblyLocation.Replace('\\', '/');

                    if (locationNormalized.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return locationNormalized.Substring(projectPath.Length + 1);
                    }
                    else
                    {
                        return assemblyLocation;
                    }
                }
            }
            catch (Exception)
            {
            }

            return "Source not found (likely built-in or in-memory)";
        }

        private static List<string> GetDefines()
        {
            string rawDefs;

#if UNITY_2023_1_OR_NEWER
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
            UnityEditor.Build.NamedBuildTarget namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
            rawDefs = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
#else
            rawDefs = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
#endif

            if (string.IsNullOrWhiteSpace(rawDefs))
            {
                return new List<string>();
            }

            return rawDefs.Split(';')
                .Select(d => d.Trim())
                .Where(d => !string.IsNullOrEmpty(d))
                .ToList();
        }

        private static void SetDefines(List<string> defines)
        {
            var cleaned = defines
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            string joinedDefs = string.Join(";", cleaned);

#if UNITY_2023_1_OR_NEWER
            BuildTarget target = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup group = BuildPipeline.GetBuildTargetGroup(target);
            UnityEditor.Build.NamedBuildTarget namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
            string currentDefs = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);

            if (currentDefs != joinedDefs)
            {
                if (_debug) Debug.Log(DependencyManagerLocKey.log_final_defines_named_target.Localize(namedBuildTarget, joinedDefs));
                PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, joinedDefs);
            }
#else
            BuildTargetGroup group = EditorUserBuildSettings.selectedBuildTargetGroup;
            string currentDefs = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);

            if (currentDefs != joinedDefs)
            {
                 if (_debug) Debug.Log(DependencyManagerLocKey.log_final_defines_group.Localize(group, joinedDefs));
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, joinedDefs);
            }
#endif
        }

        private static void SetDependencyState(DependencyItem item, bool isEnabled)
        {
            if (item == null)
            {
                return;
            }

            bool shouldSave = false;

            if (isEnabled)
            {
                if (item.DisabledManually || !item.IsEnabled)
                {
                    item.DisabledManually = false;
                    item.IsEnabled = true;
                    shouldSave = true;
                }

                if (string.IsNullOrEmpty(item.ScriptPath))
                {
                    item.ScriptPath = "Enabled manually";
                    shouldSave = true;
                }
            }
            else
            {
                if (!item.DisabledManually || item.IsEnabled)
                {
                    item.DisabledManually = true;
                    item.IsEnabled = false;
                    shouldSave = true;
                }

                if (string.IsNullOrEmpty(item.ScriptPath) || item.ScriptPath == "Enabled manually")
                {
                    item.ScriptPath = "Disabled manually";
                    shouldSave = true;
                }
            }

            if (!shouldSave)
            {
                return;
            }

            EditorUtility.SetDirty(item);
            ApplyDefines(GetAllItems());
            AssetDatabase.SaveAssets();
        }
    }
}
