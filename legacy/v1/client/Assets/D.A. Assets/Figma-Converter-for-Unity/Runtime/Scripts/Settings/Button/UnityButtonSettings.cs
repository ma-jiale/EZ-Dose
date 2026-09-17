using System;
using UnityEngine;

namespace DA_Assets.FCU.Model
{
    [Serializable]
    public class UnityButtonSettings
    {
        [SerializeField] Color normalColor = new Color32(255, 255, 255, 255);
        public Color NormalColor { get => normalColor; set => normalColor = value; }

        [SerializeField] Color highlightedColor = new Color32(245, 245, 245, 255);
        public Color HighlightedColor { get => highlightedColor; set => highlightedColor = value; }

        [SerializeField] Color pressedColor = new Color32(200, 200, 200, 255);
        public Color PressedColor { get => pressedColor; set => pressedColor = value; }

        [SerializeField] Color selectedColor = new Color32(245, 245, 245, 255);
        public Color SelectedColor { get => selectedColor; set => selectedColor = value; }

        [SerializeField] Color disabledColor = new Color32(200, 200, 200, 128);
        public Color DisabledColor { get => disabledColor; set => disabledColor = value; }

        [SerializeField] float colorMultiplier = 1f;
        public float ColorMultiplier { get => colorMultiplier; set => colorMultiplier = value; }

        [SerializeField] float fadeDuration = 0.1f;
        public float FadeDuration { get => fadeDuration; set => fadeDuration = value; }
    }
}