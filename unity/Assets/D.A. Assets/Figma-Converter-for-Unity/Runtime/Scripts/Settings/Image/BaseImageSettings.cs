using DA_Assets.DAI;
using System;
using UnityEngine;

namespace DA_Assets.FCU.Model
{
    [Serializable]
    public class BaseImageSettings : FcuBase
    {
        [SerializeField] UnityEngine.UI.Image.Type type = UnityEngine.UI.Image.Type.Simple;
        public UnityEngine.UI.Image.Type Type { get => type; set => type = value; }

        [SerializeField] bool raycastTarget = true;
        public bool RaycastTarget { get => raycastTarget; set => raycastTarget = value; }

        [SerializeField] bool preserveAspect = true;
        public bool PreserveAspect { get => preserveAspect; set => preserveAspect = value; }

        [SerializeField] Vector4 raycastPadding = new Vector4(0, 0, 0, 0);
        public Vector4 RaycastPadding { get => raycastPadding; set => raycastPadding = value; }

        [SerializeField] bool maskable = true;
        public bool Maskable { get => maskable; set => maskable = value; }
    }
}