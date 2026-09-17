using System;
using UnityEngine;

namespace DA_Assets.FCU
{
    /// <summary>
    /// Stores decimal precision settings (digits after the decimal point) 
    /// for rounding.
    /// </summary>
    [Serializable]
    public class RoundingConfig
    {
        [SerializeField] int isCircle = 0;
        public int IsCircle => isCircle;

        [SerializeField] int boundingSize = 2;
        public int BoundingSize => boundingSize;

        [SerializeField] int position = 2;
        public int Position => position;

        [SerializeField] int strokeWeight = 1;
        public int StrokeWeight => strokeWeight;

        [SerializeField] int rotation = 3;
        public int Rotation => rotation;

        [SerializeField] int angleFromMatrix = 2;
        public int AngleFromMatrix => angleFromMatrix;

        [SerializeField] int diffCheckerSize = 2;
        public int DiffCheckerSize => diffCheckerSize;

        [SerializeField] int padding = 0;
        public int Padding => padding;

        [SerializeField] int fontParams = 2;
        public int FontParams => fontParams;

        [SerializeField] int maxAllowedScale = 2;
        public int MaxAllowedScale => maxAllowedScale;
    }
}