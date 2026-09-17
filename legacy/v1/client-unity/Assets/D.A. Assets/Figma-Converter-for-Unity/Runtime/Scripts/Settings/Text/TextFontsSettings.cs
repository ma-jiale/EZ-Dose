using System;
using UnityEngine;

namespace DA_Assets.FCU.Model
{
    [Serializable]
    public class TextFontsSettings : FcuBase
    {
        [SerializeField] TextComponent textComponent = TextComponent.UnityEngine_UI_Text;
        public TextComponent TextComponent { get => textComponent; set => textComponent = value; }
    }
}