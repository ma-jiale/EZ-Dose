using System;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS0162

namespace DA_Assets.FCU.Extensions
{
    public static class FcuExtensions
    {
        public static bool IsJsonNetExists(this FigmaConverterUnity fcu)
        {
#if JSONNET_EXISTS
            return true;
#endif
            return false;
        }

        public static async Task ReEnableRectTransform(this FigmaConverterUnity fcu, CancellationToken token)
        {
            fcu.gameObject.SetActive(false);
            await Task.Delay(100, token);
            fcu.gameObject.SetActive(true);
        }


        public static Type GetCurrentImageType(this FigmaConverterUnity fcu)
        {
            switch (fcu.Settings.ImageSpritesSettings.ImageComponent)
            {
                case ImageComponent.UnityImage:
                    return typeof(UnityEngine.UI.Image);
                case ImageComponent.RawImage:
                    return typeof(UnityEngine.UI.RawImage);
#if SUBC_SHAPES_EXISTS
                case ImageComponent.SubcShape:
                    return typeof(Shapes2D.Shape);
#endif
#if MPUIKIT_EXISTS
                case ImageComponent.MPImage:
                    return typeof(MPUIKIT.MPImage);
#endif
#if JOSH_PUI_EXISTS
                case ImageComponent.ProceduralImage:
                    return typeof(UnityEngine.UI.ProceduralImage.ProceduralImage);
#endif
            }

            return null;
        }

        public static Type GetCurrentTextType(this FigmaConverterUnity fcu)
        {
            switch (fcu.Settings.TextFontsSettings.TextComponent)
            {
                case TextComponent.UnityEngine_UI_Text:
                    return typeof(UnityEngine.UI.Text);
#if TextMeshPro
                case TextComponent.TextMeshPro:
                    return typeof(TMPro.TextMeshProUGUI);
#endif
            }

            return null;
        }
    }
}