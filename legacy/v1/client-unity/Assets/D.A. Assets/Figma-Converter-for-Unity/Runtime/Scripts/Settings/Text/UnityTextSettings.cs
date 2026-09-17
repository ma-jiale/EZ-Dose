using System;
using UnityEngine;

namespace DA_Assets.FCU.Model
{
    [Serializable]
    public class UnityTextSettings : FcuBase
    {
        [SerializeField] bool bestFit = true;
        public bool BestFit { get => bestFit; set => bestFit = value; }

        [SerializeField] float fontLineSpacing = 1.0f;
        public float FontLineSpacing { get => fontLineSpacing; set => fontLineSpacing = value; }

        [SerializeField] HorizontalWrapMode horizontalWrapMode = HorizontalWrapMode.Wrap;
        public HorizontalWrapMode HorizontalWrapMode { get => horizontalWrapMode; set => horizontalWrapMode = value; }

        [SerializeField] VerticalWrapMode verticalWrapMode = VerticalWrapMode.Truncate;
        public VerticalWrapMode VerticalWrapMode { get => verticalWrapMode; set => verticalWrapMode = value; }
    }
}