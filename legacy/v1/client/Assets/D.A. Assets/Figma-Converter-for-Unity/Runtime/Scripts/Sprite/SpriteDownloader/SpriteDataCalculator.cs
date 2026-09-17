using DA_Assets.FCU.Extensions;
using DA_Assets.FCU.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.FCU
{
    public static class SpriteDataCalculator
    {
        public static async Task CalculateAndSetSpriteData(IEnumerable<FObject> fobjects, FigmaConverterUnity monoBeh, CancellationToken token)
        {
            await Task.Run(() =>
            {
                Parallel.ForEach(fobjects.Where(x => x.IsSprite()), fobject =>
                {
                    fobject.Data.MaxSpriteSize = GetMaxSpriteSize(fobject);
                    fobject.Data.Scale = GetMaxAllowedScale(fobject.Data.MaxSpriteSize, monoBeh);
                });
            }, token);
        }

        private static Vector2 GetMaxSpriteSize(FObject fobject)
        {
            fobject.GetBoundingSize(out Vector2 bSize);
            fobject.GetRenderSize(out Vector2 rSize);

            float maxX = Mathf.Max(bSize.x, rSize.x, fobject.Size.x);
            float maxY = Mathf.Max(bSize.y, rSize.y, fobject.Size.y);

            return new Vector2(maxX, maxY);
        }

        private static float GetMaxAllowedScale(Vector2 imageSize, FigmaConverterUnity monoBeh)
        {
            if (monoBeh.UsingSVG())
            {
                return monoBeh.Settings.ImageSpritesSettings.ImageScale;
            }

            Vector2 maxSpriteSizeSettings = monoBeh.Settings.ImageSpritesSettings.MaxSpriteSize;
            float scaleX = imageSize.x > 0 ? maxSpriteSizeSettings.x / imageSize.x : float.MaxValue;
            float scaleY = imageSize.y > 0 ? maxSpriteSizeSettings.y / imageSize.y : float.MaxValue;

            float maxScaleBySpriteSize = Mathf.Min(scaleX, scaleY);
            maxScaleBySpriteSize = Mathf.Max(1f, maxScaleBySpriteSize);

            float clampedScale = Mathf.Clamp(maxScaleBySpriteSize, FcuConfig.IMAGE_SCALE_MIN, FcuConfig.IMAGE_SCALE_MAX);
            float roundedScale = (float)System.Math.Round(clampedScale, FcuConfig.Rounding.MaxAllowedScale);

            return Mathf.Min(roundedScale, monoBeh.Settings.ImageSpritesSettings.ImageScale);
        }
    }

}
