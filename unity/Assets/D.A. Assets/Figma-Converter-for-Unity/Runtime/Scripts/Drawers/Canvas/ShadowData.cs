using DA_Assets.FCU.Model;
using UnityEngine;

namespace DA_Assets.FCU.Drawers.CanvasDrawers
{
    internal struct ShadowData
    {
        public EffectType EffectType { get; set; }
        public Vector2 Offset { get; set; }
        public float Angle { get; set; }
        public float Distance { get; set; }
        public float Spread { get; set; }
        public Color Color { get; set; }
        public float Radius { get; set; }
    }
}