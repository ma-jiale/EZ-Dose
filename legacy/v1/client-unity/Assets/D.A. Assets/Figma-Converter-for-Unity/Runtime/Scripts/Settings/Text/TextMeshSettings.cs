using UnityEngine;
using System;
using DA_Assets.DAI;

#if TextMeshPro
using TMPro;
#endif

namespace DA_Assets.FCU.Model
{
    [Serializable]
    public class TextMeshSettings : FcuBase
    {
        [SerializeField] bool autoSize = true;
        public bool AutoSize { get => autoSize; set => autoSize = value; }

        [SerializeField] bool overrideTags = false;
        public bool OverrideTags { get => overrideTags; set => overrideTags = value; }

        [SerializeField] bool wrapping = true;
        public bool Wrapping { get => wrapping; set => wrapping = value; }

        [SerializeField] bool orthographicMode = true;
        /// <summary>
        /// For NOVA only.
        /// </summary>
        public bool OrthographicMode { get => orthographicMode; set => orthographicMode = value; }

        [SerializeField] bool richText = true;
        public bool RichText { get => richText; set => richText = value; }

        [SerializeField] bool raycastTarget = true;
        public bool RaycastTarget { get => raycastTarget; set => raycastTarget = value; }

        [SerializeField] bool parseEscapeCharacters = true;
        public bool ParseEscapeCharacters { get => parseEscapeCharacters; set => parseEscapeCharacters = value; }

        [SerializeField] bool visibleDescender = true;
        public bool VisibleDescender { get => visibleDescender; set => visibleDescender = value; }

        [SerializeField] bool kerning = true;
        public bool Kerning { get => kerning; set => kerning = value; }

        [SerializeField] bool extraPadding = false;
        public bool ExtraPadding { get => extraPadding; set => extraPadding = value; }

#if TextMeshPro
        [SerializeField] TextOverflowModes overflow = TextOverflowModes.Overflow;
        public TextOverflowModes Overflow { get => overflow; set => overflow = value; }

        [SerializeField] TextureMappingOptions horizontalMapping = TextureMappingOptions.Character;
        public TextureMappingOptions HorizontalMapping { get => horizontalMapping; set => horizontalMapping = value; }

        [SerializeField] TextureMappingOptions verticalMapping = TextureMappingOptions.Character;
        public TextureMappingOptions VerticalMapping { get => verticalMapping; set => verticalMapping = value; }

        [SerializeField] VertexSortingOrder geometrySorting = VertexSortingOrder.Normal;
        public VertexSortingOrder GeometrySorting { get => geometrySorting; set => geometrySorting = value; }

#if RTLTMP_EXISTS
        [SerializeField] bool farsi = true;
        public bool Farsi { get => farsi; set => farsi = value; }

        [SerializeField] bool forceFix = false;
        public bool ForceFix { get => forceFix; set => forceFix = value; }

        [SerializeField] bool preserveNumbers = false;
        public bool PreserveNumbers { get => preserveNumbers; set => preserveNumbers = value; }

        [SerializeField] bool fixTags = true;
        public bool FixTags { get => fixTags; set => fixTags = value; }
#endif
#endif

        [SerializeField] Shader shader;
        public Shader Shader { get => shader; set => shader = value; }
    }
}