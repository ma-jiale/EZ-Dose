using UnityEngine;
using DA_Assets.Logging;

namespace DA_Assets.FCU
{
    internal static class PrefabCreatorExtensions
    {
        internal static void SetParentEx(this Transform transform, Transform parent)
        {
            if (transform == null)
            {
                Debug.LogError(FcuLocKey.log_transform_null.Localize());
                return;
            }

            transform.SetParent(parent, false);
        }
    }
}

