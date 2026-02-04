using System;
using UnityEngine;

namespace DA_Assets.FCU.Model
{
    [Serializable]
    public class SvgImageSettings : FcuBase
    {
        [SerializeField] bool raycastTarget = true;
        public bool RaycastTarget { get => raycastTarget; set => raycastTarget = value; }

        [SerializeField] bool preserveAspect = true;
        public bool PreserveAspect { get => preserveAspect; set => preserveAspect = value; }

        [SerializeField] Vector4 raycastPadding = new Vector4(0, 0, 0, 0);
        public Vector4 RaycastPadding { get => raycastPadding; set => raycastPadding = value; }
    }
}