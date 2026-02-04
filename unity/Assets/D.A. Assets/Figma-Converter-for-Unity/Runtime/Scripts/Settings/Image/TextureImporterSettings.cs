using System;
using UnityEngine;

namespace DA_Assets.FCU.Model
{
    [Serializable]
    public class TextureImporterSettings : FcuBase
    {
        [SerializeField] bool crunchedCompression = false;
        public bool CrunchedCompression { get => crunchedCompression; set => crunchedCompression = value; }

        [SerializeField] int compressionQuality = 100;
        public int CompressionQuality { get => compressionQuality; set => compressionQuality = value; }

        [SerializeField] bool isReadable = true;
        public bool IsReadable { get => isReadable; set => isReadable = value; }

        [SerializeField] bool mipmapEnabled = false;
        public bool MipmapEnabled { get => mipmapEnabled; set => mipmapEnabled = value; }

#if UNITY_EDITOR
        [SerializeField] UnityEditor.TextureImporterType textureType = UnityEditor.TextureImporterType.Sprite;
        public UnityEditor.TextureImporterType TextureType { get => textureType; set => textureType = value; }

        [SerializeField] UnityEditor.TextureImporterCompression textureCompression = UnityEditor.TextureImporterCompression.Uncompressed;
        public UnityEditor.TextureImporterCompression TextureCompression { get => textureCompression; set => textureCompression = value; }

        [SerializeField] UnityEditor.SpriteImportMode spriteImportMode = UnityEditor.SpriteImportMode.Single;
        public UnityEditor.SpriteImportMode SpriteImportMode { get => spriteImportMode; set => spriteImportMode = value; }
#endif
    }
}

