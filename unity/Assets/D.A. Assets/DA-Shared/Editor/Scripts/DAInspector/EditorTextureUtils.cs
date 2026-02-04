using UnityEditor;
using UnityEngine;

namespace DA_Assets.DAI
{
    public static class EditorTextureUtils
    {
        public static Texture2D RecolorToEditorSkin(Texture2D source)
        {
            if (source == null)
                throw new System.ArgumentNullException(nameof(source));

            bool isPro = EditorGUIUtility.isProSkin;
            Color32 baseCol32 = isPro ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 255);

            Texture2D readable = source.isReadable ? source : MakeReadableCopy(source);
            var pixels = readable.GetPixels32();

            for (int i = 0; i < pixels.Length; i++)
            {
                byte a = pixels[i].a;
                pixels[i] = new Color32(baseCol32.r, baseCol32.g, baseCol32.b, a);
            }

            var result = new Texture2D(readable.width, readable.height, TextureFormat.RGBA32, false, false)
            {
                name = source.name + (isPro ? "_White" : "_Black"),
                filterMode = source.filterMode,
                wrapMode = source.wrapMode,
                anisoLevel = source.anisoLevel
            };

            result.SetPixels32(pixels);
            result.Apply(false, false);

            if (!ReferenceEquals(readable, source))
                Object.DestroyImmediate(readable);

            return result;
        }

        private static Texture2D MakeReadableCopy(Texture2D source)
        {
            var rt = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            try
            {
                Graphics.Blit(source, rt);
                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false);
                copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0, false);
                copy.Apply(false, false);
                RenderTexture.active = prev;
                return copy;
            }
            finally
            {
                RenderTexture.ReleaseTemporary(rt);
            }
        }
    }
}