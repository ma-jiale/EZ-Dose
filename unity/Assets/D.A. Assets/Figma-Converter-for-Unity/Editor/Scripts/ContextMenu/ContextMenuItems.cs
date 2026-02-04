using DA_Assets.Constants;
using DA_Assets.Extensions;
using DA_Assets.Logging;
using UnityEditor;
using UnityEngine;

namespace DA_Assets.FCU
{
    internal class ContextMenuItems
    {  
        [MenuItem("Tools/" + DAConstants.Publisher + "/" + FcuConfig.ProductNameShort + ": " + FcuConfig.Create + " " + FcuConfig.ProductName, false, 0)]
        private static void CreateFcu_OnClick()
        {
            EditorEventHandlers.CreateFcu_OnClick();
        }

        [MenuItem("GameObject/Tools/" + DAConstants.Publisher + "/" + FcuConfig.ProductNameShort + ": " + FcuConfig.SetFcuToSyncHelpers, false, 10)]
        private static void SetFcuToSyncHelpers_OnClick()
        {
            if (Selection.activeGameObject.TryGetComponentSafe(out FigmaConverterUnity fcu))
            {
                fcu.EditorEventHandlers.SetFcuToSyncHelpers_OnClick();
            }
            else
            {
                Debug.LogError(FcuLocKey.log_component_not_selected_in_hierarchy.Localize(nameof(FigmaConverterUnity)));
            }
        }

        [MenuItem("GameObject/Tools/" + DAConstants.Publisher + "/" + FcuConfig.ProductNameShort + ": " + FcuConfig.DestroySyncHelpers, false, 14)]
        private static void DestroySyncHelpers_OnClick()
        {
            if (Selection.activeGameObject.TryGetComponentSafe(out FigmaConverterUnity fcu))
            {
                fcu.EditorEventHandlers.DestroySyncHelpers_OnClick();
            }
            else
            {
                Debug.LogError(FcuLocKey.log_component_not_selected_in_hierarchy.Localize(nameof(FigmaConverterUnity)));
            }
        }
    }
}