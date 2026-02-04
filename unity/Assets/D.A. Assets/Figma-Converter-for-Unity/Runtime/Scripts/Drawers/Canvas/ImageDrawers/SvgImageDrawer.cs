#if VECTOR_GRAPHICS_EXISTS
using DA_Assets.Extensions;
using DA_Assets.FCU.Model;
using System;
using Unity.VectorGraphics;
using UnityEngine;

namespace DA_Assets.FCU.Drawers.CanvasDrawers
{
    [Serializable]
    public class SvgImageDrawer : FcuBase
    {
        public void Draw(FObject fobject, Sprite sprite, GameObject target)
        {
            target.TryAddGraphic(out SVGImage img);

            img.sprite = sprite;
            img.material = FcuConfig.VectorMaterials.UnlitVectorGradientUI;
            img.raycastTarget = monoBeh.Settings.SvgImageSettings.RaycastTarget;
            img.preserveAspect = monoBeh.Settings.SvgImageSettings.PreserveAspect;
            img.raycastPadding = monoBeh.Settings.SvgImageSettings.RaycastPadding;

            monoBeh.CanvasDrawer.ImageDrawer.UnityImageDrawer.SetColor(fobject, img);
            monoBeh.CanvasDrawer.ImageDrawer.TryAddCornerRounder(fobject, target);
        }
    }
}
#endif