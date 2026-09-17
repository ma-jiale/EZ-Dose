using UnityEngine;
using UnityEngine.SceneManagement;
using DA_Assets.Shared.Extensions;

namespace DA_Assets.Extensions
{
    public static class ReflectedExtensions 
    {
        /// <summary>
        /// <para><see href="https://forum.unity.com/threads/how-to-collapse-hierarchy-scene-nodes-via-script.605245/#post-6551890"/></para>
        /// </summary>
        public static void SetExpanded(this Scene scene, bool expand)
        {
#if UNITY_EDITOR
            foreach (var window in Resources.FindObjectsOfTypeAll<UnityEditor.SearchableEditorWindow>())
            {
                if (window.GetType().Name != "SceneHierarchyWindow")
                    continue;

                var method = window.GetType().GetMethod("SetExpandedRecursive",
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance, null,
                    new[] { typeof(int), typeof(bool) }, null);

                if (method == null)
                {
                    Debug.LogError(SharedLocKey.log_scenehierarchy_method_missing.Localize());
                    return;
                }

                var field = scene.GetType().GetField("m_Handle",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (field == null)
                {
                    Debug.LogError(SharedLocKey.log_scenehierarchy_field_missing.Localize());
                    return;
                }

                var sceneHandle = field.GetValue(scene);
                method.Invoke(window, new[] { sceneHandle, expand });
            }
#endif
        }
    }
}
