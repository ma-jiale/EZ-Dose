using DA_Assets.FCU.Model;
using System;
using UnityEngine;

namespace DA_Assets.FCU
{
    [Serializable]
    public struct FStroke
    {
        [SerializeField] StrokeAlign align;
        public StrokeAlign Align { get => align; set => align = value; }

        [SerializeField] float weight;
        public float Weight { get => weight; set => weight = value; }

        [SerializeField] bool hasSolid;
        public bool HasSolid { get => hasSolid; set => hasSolid = value; }

        [SerializeField] bool hasGradient;
        public bool HasGradient { get => hasGradient; set => hasGradient = value; }

        [SerializeField] Paint solidPaint;
        public Paint SolidPaint { get => solidPaint; set => solidPaint = value; }

        [SerializeField] Paint gradientPaint;
        public Paint GradientPaint { get => gradientPaint; set => gradientPaint = value; }

        [SerializeField] Color singleColor;
        public Color SingleColor { get => singleColor; set => singleColor = value; }
    }
}

