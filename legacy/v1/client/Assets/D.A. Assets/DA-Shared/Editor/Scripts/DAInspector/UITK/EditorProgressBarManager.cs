using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.DAI
{
    [InitializeOnLoad]
    public static class EditorProgressBarManager
    {
        private const float AUTO_MAX_PROGRESS = 0.985f;
        private const float AUTO_BASE_SPEED = 0.3f;
        private const float SLOW_DOWN_POWER = 3.0f;
        private const float MAX_LEAD = 0.05f;
        private const float LEAD_LAMBDA = 1.2f;

        private const float INDETERMINATE_CYCLE_DURATION = 2.0f;

        private static readonly Dictionary<object, InstanceInfo> activeInstances = new();
        private static readonly ConcurrentQueue<ProgressAction> actionQueue = new();
        private static readonly object _lock = new();
        private static double lastUpdateTime;

        static EditorProgressBarManager()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;
            lastUpdateTime = EditorApplication.timeSinceStartup;
        }

        public static void RegisterContainer(object key, VisualElement container, DAInspectorUITK uitk)
        {
            actionQueue.Enqueue(new ProgressAction
            {
                Type = ProgressActionType.Register,
                Key = key,
                Container = container,
                Uitk = uitk
            });
        }

        public static void StartProgress(object key, ProgressBarCategory category, int totalItems, bool indeterminate = false)
        {
            actionQueue.Enqueue(new ProgressAction
            {
                Type = indeterminate ? ProgressActionType.StartIndeterminate : ProgressActionType.Start,
                Key = key,
                Category = category,
                TotalItems = totalItems
            });
        }

        public static void UpdateProgress(object key, ProgressBarCategory category, int itemsDone)
        {
            actionQueue.Enqueue(new ProgressAction
            {
                Type = ProgressActionType.Update,
                Key = key,
                Category = category,
                ItemsDone = itemsDone
            });
        }

        public static void CompleteProgress(object key, ProgressBarCategory category)
        {
            actionQueue.Enqueue(new ProgressAction
            {
                Type = ProgressActionType.Complete,
                Key = key,
                Category = category
            });
        }

        public static void StopAllProgress(object key)
        {
            actionQueue.Enqueue(new ProgressAction
            {
                Type = ProgressActionType.StopAll,
                Key = key
            });
        }

        private static void ProcessActions()
        {
            while (actionQueue.TryDequeue(out var action))
            {
                lock (_lock)
                {
                    switch (action.Type)
                    {
                        case ProgressActionType.Register:
                            HandleRegister(action.Key, action.Container, action.Uitk);
                            break;
                        case ProgressActionType.Start:
                            HandleStart(action.Key, action.Category, action.TotalItems);
                            break;
                        case ProgressActionType.Update:
                            HandleUpdate(action.Key, action.Category, action.ItemsDone);
                            break;
                        case ProgressActionType.Complete:
                            HandleComplete(action.Key, action.Category);
                            break;
                        case ProgressActionType.StartIndeterminate:
                            HandleStartIndeterminate(action.Key, action.Category);
                            break;
                        case ProgressActionType.StopAll:
                            HandleStopAll(action.Key);
                            break;
                    }
                }
            }
        }

        private static void OnEditorUpdate()
        {
            ProcessActions();

            if (activeInstances.Count == 0)
                return;

            DisplayProgress();
        }

        private static void DisplayProgress()
        {
            double currentTime = EditorApplication.timeSinceStartup;
            float deltaTime = (float)(currentTime - lastUpdateTime);
            lastUpdateTime = currentTime;

            lock (_lock)
            {
                foreach (var instanceInfo in activeInstances.Values.ToList())
                {
                    instanceInfo.VisualElements.RemoveAll(item => item == null);
                    if (instanceInfo.VisualElements.Count == 0 && instanceInfo.Containers.Count == 0)
                    {
                        activeInstances.Remove(instanceInfo.Key);
                        continue;
                    }

                    if (instanceInfo.VisualElements.Count == 0) continue;

                    if (instanceInfo.IsIndeterminate)
                    {
                        var elapsedTime = (float)(currentTime - instanceInfo.IndeterminateStartTime);
                        var value = (elapsedTime % INDETERMINATE_CYCLE_DURATION) / INDETERMINATE_CYCLE_DURATION;
                        foreach (var pb in instanceInfo.VisualElements)
                        {
                            pb.value = value * 100;
                            pb.title = $"{instanceInfo.IndeterminateCategory}...";
                        }
                    }
                    else
                    {
                        if (!instanceInfo.ActiveProcesses.Any())
                        {
                            SetProgressBarVisibility(instanceInfo, false);
                            continue;
                        }

                        float target = instanceInfo.TargetValue;
                        float displayed = instanceInfo.CurrentVisualValue;

                        float desired = (target > displayed + 0.0001f) ? target : AUTO_MAX_PROGRESS;

                        double timeSinceChange = currentTime - instanceInfo.LastTargetChangeTime;
                        float lead = MAX_LEAD * (1f - Mathf.Exp(-LEAD_LAMBDA * (float)timeSinceChange));
                        desired = Mathf.Min(desired, target + lead, AUTO_MAX_PROGRESS);

                        if (displayed < desired)
                        {
                            float easeFactor = Mathf.Pow(1f - displayed, SLOW_DOWN_POWER);
                            float step = AUTO_BASE_SPEED * easeFactor * deltaTime;
                            displayed = Mathf.Min(displayed + step, desired);
                        }

                        instanceInfo.CurrentVisualValue = displayed;

                        foreach (var pb in instanceInfo.VisualElements)
                        {
                            pb.value = instanceInfo.CurrentVisualValue * 100;
                            string activeTasks = string.Join(", ", instanceInfo.ActiveProcesses.Keys);
                            pb.title = $"{activeTasks} ({instanceInfo.CompletedWork}/{instanceInfo.TotalWork}) - {pb.value:F1}%";
                        }
                    }
                }
            }
        }


        private static void HandleRegister(object key, VisualElement container, DAInspectorUITK uitk)
        {
            if (!activeInstances.TryGetValue(key, out var instanceInfo))
            {
                instanceInfo = new InstanceInfo { Key = key };
                activeInstances[key] = instanceInfo;
            }

            if (container != null && !instanceInfo.Containers.Contains(container))
            {
                container.Clear();
                var progressBar = CreateProgressBarVisualElement(uitk);
                container.Add(progressBar);
                instanceInfo.VisualElements.Add(progressBar);
                instanceInfo.Containers.Add(container);
            }
        }

        private static void HandleStart(object key, ProgressBarCategory category, int totalItems)
        {
            if (activeInstances.TryGetValue(key, out var instanceInfo) && !instanceInfo.ActiveProcesses.ContainsKey(category))
            {
                instanceInfo.ActiveProcesses[category] = new ProcessInfo { TotalItems = totalItems, CompletedItems = 0 };
                instanceInfo.RecalculateTotals();
                instanceInfo.TargetValue = instanceInfo.TotalWork > 0 ? (float)instanceInfo.CompletedWork / instanceInfo.TotalWork : 0;
                instanceInfo.LastTargetChangeTime = EditorApplication.timeSinceStartup;
                SetProgressBarVisibility(instanceInfo, true);
            }
        }

        private static void HandleUpdate(object key, ProgressBarCategory category, int itemsDone)
        {
            if (!activeInstances.TryGetValue(key, out var instanceInfo))
                return;

            if (instanceInfo.IsIndeterminate)
                return;

            if (!instanceInfo.ActiveProcesses.TryGetValue(category, out var processInfo))
                return;

            if (itemsDone > processInfo.CompletedItems)
            {
                processInfo.CompletedItems = itemsDone;
                instanceInfo.ActiveProcesses[category] = processInfo;
                instanceInfo.RecalculateTotals();

                float newTarget = instanceInfo.TotalWork > 0 ? (float)instanceInfo.CompletedWork / instanceInfo.TotalWork : 0;
                if (newTarget > instanceInfo.TargetValue)
                {
                    instanceInfo.TargetValue = newTarget;
                    instanceInfo.LastTargetChangeTime = EditorApplication.timeSinceStartup;
                }
            }
        }

        private static void HandleComplete(object key, ProgressBarCategory category)
        {
            if (activeInstances.TryGetValue(key, out var instanceInfo))
            {
                if (instanceInfo.IsIndeterminate && instanceInfo.IndeterminateCategory == category)
                {
                    instanceInfo.IsIndeterminate = false;
                    instanceInfo.IndeterminateCategory = null;
                    instanceInfo.RestoreState();

                    if (!instanceInfo.ActiveProcesses.Any())
                    {
                        SetProgressBarVisibility(instanceInfo, false);
                    }
                }
                else if (instanceInfo.ActiveProcesses.Remove(category))
                {
                    instanceInfo.RecalculateTotals();

                    if (instanceInfo.ActiveProcesses.Any())
                    {
                        instanceInfo.TargetValue = instanceInfo.TotalWork > 0 ? (float)instanceInfo.CompletedWork / instanceInfo.TotalWork : 0;
                    }
                    else
                    {
                        SetProgressBarVisibility(instanceInfo, false);
                        instanceInfo.CurrentVisualValue = 0f;
                        instanceInfo.TargetValue = 0f;
                    }
                }
            }
        }

        private static void HandleStartIndeterminate(object key, ProgressBarCategory category)
        {
            if (activeInstances.TryGetValue(key, out var instanceInfo) && !instanceInfo.IsIndeterminate)
            {
                instanceInfo.CacheState();
                instanceInfo.IsIndeterminate = true;
                instanceInfo.IndeterminateCategory = category;
                instanceInfo.IndeterminateStartTime = EditorApplication.timeSinceStartup;
                SetProgressBarVisibility(instanceInfo, true);
            }
        }

        private static void HandleStopAll(object key)
        {
            if (activeInstances.TryGetValue(key, out var instanceInfo))
            {
                instanceInfo.ActiveProcesses.Clear();
                instanceInfo.IsIndeterminate = false;
                instanceInfo.IndeterminateCategory = null;

                instanceInfo.RecalculateTotals();
                instanceInfo.CurrentVisualValue = 0f;
                instanceInfo.TargetValue = 0f;

                SetProgressBarVisibility(instanceInfo, false);
            }
        }

        private static void SetProgressBarVisibility(InstanceInfo instanceInfo, bool isVisible)
        {
            var newVisibility = isVisible ? Visibility.Visible : Visibility.Hidden;
            foreach (var pb in instanceInfo.VisualElements)
            {
                if (pb.style.visibility != newVisibility)
                {
                    pb.style.visibility = newVisibility;
                }
            }
        }


        private static ProgressBar CreateProgressBarVisualElement(DAInspectorUITK uitk)
        {
            var pb = new ProgressBar
            {
                lowValue = 0f,
                highValue = 100f,
                value = 0,
            };

            pb.style.position = Position.Relative;
            pb.style.left = 0;
            pb.style.right = 0;
            pb.style.top = 0;

            pb.style.height = DAI_UitkConstants.PbarHeight;

            UIHelpers.SetZeroMarginPadding(pb);

            pb.style.borderBottomWidth = 0;
            pb.style.borderLeftWidth = 0;
            pb.style.borderRightWidth = 0;
            pb.style.borderTopWidth = 0;

            pb.style.visibility = Visibility.Hidden;

            var titleLabel = pb.Q<Label>(className: "unity-progress-bar__title");
            if (titleLabel != null)
            {
                titleLabel.style.color = new StyleColor(Color.white);
                titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
                titleLabel.style.fontSize = DAI_UitkConstants.FontSizeNormal;
                titleLabel.style.unityFontStyleAndWeight = FontStyle.Normal;
            }

            VisualElement progressBarBackground = pb.Q<VisualElement>(className: "unity-progress-bar__background");
            if (progressBarBackground != null)
            {
                progressBarBackground.style.backgroundColor = new StyleColor(new Color32(255, 255, 255, 32));
            }

            return pb;
        }

        private enum ProgressActionType
        {
            Register,
            Start,
            Update,
            Complete,
            StartIndeterminate,
            StopAll
        }

        private struct ProgressAction
        {
            public ProgressActionType Type { get; set; }
            public object Key { get; set; }
            public ProgressBarCategory Category { get; set; }
            public int TotalItems { get; set; }
            public int ItemsDone { get; set; }
            public VisualElement Container { get; set; }
            public DAInspectorUITK Uitk { get; set; }
        }

        private struct ProcessInfo
        {
            public int TotalItems;
            public int CompletedItems;
        }

        private class InstanceInfo
        {
            public object Key;
            public readonly List<ProgressBar> VisualElements = new();
            public readonly List<VisualElement> Containers = new();
            public readonly Dictionary<ProgressBarCategory, ProcessInfo> ActiveProcesses = new();

            private Dictionary<ProgressBarCategory, ProcessInfo> _cachedProcesses = new();

            public int TotalWork { get; private set; }
            public int CompletedWork { get; private set; }

            public float CurrentVisualValue { get; set; }
            public float TargetValue { get; set; }
            public double LastTargetChangeTime { get; set; }

            public bool IsIndeterminate { get; set; }
            public ProgressBarCategory? IndeterminateCategory { get; set; }
            public double IndeterminateStartTime { get; set; }

            public void RecalculateTotals()
            {
                TotalWork = ActiveProcesses.Values.Sum(p => p.TotalItems);
                CompletedWork = ActiveProcesses.Values.Sum(p => p.CompletedItems);
            }

            public void CacheState()
            {
                _cachedProcesses = new Dictionary<ProgressBarCategory, ProcessInfo>(ActiveProcesses);
            }

            public void RestoreState()
            {
                ActiveProcesses.Clear();
                foreach (var pair in _cachedProcesses)
                {
                    ActiveProcesses.Add(pair.Key, pair.Value);
                }
                RecalculateTotals();
                _cachedProcesses.Clear();
            }
        }
    }
}
