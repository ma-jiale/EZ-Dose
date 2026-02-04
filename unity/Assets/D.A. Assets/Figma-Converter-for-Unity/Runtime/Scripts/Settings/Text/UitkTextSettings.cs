using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.FCU.Model
{
    [Serializable]
    public class UitkTextSettings : FcuBase
    {
        [SerializeField] WhiteSpace whiteSpace = WhiteSpace.Normal;
        public WhiteSpace WhiteSpace { get => whiteSpace; set => whiteSpace = value; }

        [SerializeField] TextOverflow textOverflow = TextOverflow.Clip;
        public TextOverflow TextOverflow { get => textOverflow; set => textOverflow = value; }

#if UNITY_2022_3_OR_NEWER
        [SerializeField] LanguageDirection languageDirection = LanguageDirection.Inherit;
        public LanguageDirection LanguageDirection { get => languageDirection; set => languageDirection = value; }
#endif
        [SerializeField] bool autoSize = false;
        public bool AutoSize { get => autoSize; set => autoSize = value; }

        [SerializeField] bool focusable = false;
        public bool Focusable { get => focusable; set => focusable = value; }

        [SerializeField] bool enableRichText = true;
        public bool EnableRichText { get => enableRichText; set => enableRichText = value; }

        [SerializeField] bool emojiFallbackSupport = true;
        public bool EmojiFallbackSupport { get => emojiFallbackSupport; set => emojiFallbackSupport = value; }

        [SerializeField] bool parseEscapeSequences = false;
        public bool ParseEscapeSequences { get => parseEscapeSequences; set => parseEscapeSequences = value; }

        [SerializeField] bool selectable = false;
        public bool Selectable { get => selectable; set => selectable = value; }

        [SerializeField] bool doubleClickSelectsWord = true;
        public bool DoubleClickSelectsWord { get => doubleClickSelectsWord; set => doubleClickSelectsWord = value; }

        [SerializeField] bool tripleClickSelectsLine = true;
        public bool TripleClickSelectsLine { get => tripleClickSelectsLine; set => tripleClickSelectsLine = value; }

        [SerializeField] bool displayTooltipWhenElided = true;
        public bool DisplayTooltipWhenElided { get => displayTooltipWhenElided; set => displayTooltipWhenElided = value; }
    }
}
