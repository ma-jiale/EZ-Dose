using System;
using UnityEngine;

namespace DA_Assets.FCU.Model
{
    [Serializable]
    public class SpriteRendererSettings : FcuBase
    {
        [SerializeField] bool flipX = false;
        public bool FlipX { get => flipX; set => flipX = value; }

        [SerializeField] bool flipY = false;
        public bool FlipY { get => flipY; set => flipY = value; }

        [SerializeField] SpriteMaskInteraction maskInteraction = SpriteMaskInteraction.None;
        public SpriteMaskInteraction MaskInteraction { get => maskInteraction; set => maskInteraction = value; }

        [SerializeField] SpriteSortPoint sortPoint = SpriteSortPoint.Center;
        public SpriteSortPoint SortPoint { get => sortPoint; set => sortPoint = value; }

        [SerializeField] string sortingLayer = "Default";
        public string SortingLayer { get => sortingLayer; set => sortingLayer = value; }

        [SerializeField] int nextOrderStep = 10;
        public int NextOrderStep { get => nextOrderStep; set => nextOrderStep = value; }
    }
}