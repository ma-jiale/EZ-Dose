using UnityEditor;
using UnityEngine;

namespace DA_Assets.DM
{
    [CustomEditor(typeof(DependencyItem))]
    public class DependencyItemEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DependencyItem item = (DependencyItem)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Current Status", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("Is Enabled", item.IsEnabled);
                EditorGUILayout.TextField("Script Path", item.ScriptPath ?? "Not found");
                EditorGUILayout.Toggle("Removed Manually", item.DisabledManually);
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Check Dependency Now"))
            {
                DependencyManager.CheckSingleItem(item);
            }
        }
    }
}
