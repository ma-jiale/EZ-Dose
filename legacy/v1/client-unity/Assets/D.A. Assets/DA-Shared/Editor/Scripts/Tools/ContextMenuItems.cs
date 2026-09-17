using DA_Assets.Constants;
using DA_Assets.Extensions;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using DA_Assets.Shared.Extensions;

namespace DA_Assets.Tools
{
    public class ContextMenuItems
    {
        public const string ResetToPrefabState = "Reset to prefab state";
        public const string ResetAllComponents = "Reset all components to prefab state";
        public const string DestroyChilds = "Destroy Childs";
        public const string TryBackupActiveScene = "Try Backup Active Scene";
        public const string SimplifyHierarchy = "Simplify the hierarchy";

        [MenuItem("GameObject/Tools/" + DAConstants.Publisher + "/" + nameof(DA_Assets.Tools) + ": " + DestroyChilds, false, 1)]
        private static void DestroyChilds_OnClick()
        {
            bool backuped = SceneBackuper.TryBackupActiveScene();

            if (!backuped)
            {
                Debug.LogError(SharedLocKey.log_cant_execute_no_backup.Localize());
                return;
            }

            if (Selection.activeGameObject != null)
            {
                Selection.activeGameObject.DestroyChilds();
            }
            else
            {
                Debug.LogError(SharedLocKey.log_selection_null.Localize());
            }
        }

        [MenuItem("Tools/" + DAConstants.Publisher + "/" + nameof(DA_Assets.Tools) + ": " + TryBackupActiveScene, false, 2)]
        private static void TryBackupActiveScene_OnClick()
        {
            SceneBackuper.TryBackupActiveScene();
        }

        [MenuItem("GameObject/Tools/" + DAConstants.Publisher + "/" + nameof(DA_Assets.Tools) + ": " + SimplifyHierarchy, false, 92)]
        private static void SetSelectedAsParentForAllChilds_OnClick()
        {
            GameObject selectedGameObject = Selection.activeGameObject;

            if (selectedGameObject == null)
            {
                Debug.LogError(SharedLocKey.log_object_not_selected.Localize(nameof(GameObject)));
                return;
            }

            List<Transform> childs = new List<Transform>();
            SetSelectedAsParentForAllChild(selectedGameObject);
            foreach (Transform child in childs)
            {
                child.SetParent(selectedGameObject.transform);
            }

            void SetSelectedAsParentForAllChild(GameObject @object)
            {
                if (@object == null)
                    return;

                foreach (Transform child in @object.transform)
                {
                    if (child == null)
                        continue;

                    childs.Add(child);

                    SetSelectedAsParentForAllChild(child.gameObject);
                }
            }
        }

        [MenuItem("GameObject/Tools/" + DAConstants.Publisher + "/" + nameof(DA_Assets.Tools) + ": " + ResetToPrefabState, false, 93)]
        private static void ResetToPrefabState_OnClick()
        {
            GameObject selectedGameObject = Selection.activeGameObject;

            if (selectedGameObject == null)
            {
                Debug.LogError(SharedLocKey.log_object_not_selected.Localize(nameof(GameObject)));
                return;
            }

            PrefabUtility.RevertPrefabInstance(Selection.activeGameObject, InteractionMode.AutomatedAction);

            Debug.Log(SharedLocKey.log_prefab_reset.Localize(selectedGameObject.name));
        }

        [MenuItem("GameObject/Tools/" + DAConstants.Publisher + "/" + nameof(DA_Assets.Tools) + ": " + ResetAllComponents, false, 94)]
        private static void ResetAllComponents_OnClick()
        {
            GameObject selectedGameObject = Selection.activeGameObject;

            if (selectedGameObject == null)
            {
                Debug.LogError(SharedLocKey.log_object_not_selected.Localize(nameof(GameObject)));
                return;
            }

            Component[] components = selectedGameObject.GetComponents<Component>();

            if (components.IsEmpty())
            {
                Debug.LogError(SharedLocKey.log_no_components.Localize(selectedGameObject.name));
                return;
            }

            int count = 0;

            foreach (var item in components)
            {
                SerializedObject serializedObject = new SerializedObject(item);
                SerializedProperty propertyIterator = serializedObject.GetIterator();

                while (propertyIterator.NextVisible(true))
                {
                    PrefabUtility.RevertPropertyOverride(propertyIterator, InteractionMode.AutomatedAction);
                    count++;
                }
            }

            Debug.Log(SharedLocKey.log_properties_reset.Localize(count));
        }
    }
}
