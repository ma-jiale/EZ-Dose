using DA_Assets.Extensions;
using DA_Assets.FCU.Model;

namespace DA_Assets.FCU.Extensions
{
    public static class FObjectExtensions
    {
        public static void SetData(this FObject fobject, SyncHelper syncHelper, FigmaConverterUnity fcu)
        {
            fobject.Data.FigmaConverterUnity = fcu;
            fobject.Data.GameObject = syncHelper.gameObject;
            syncHelper.Data = fobject.Data;
        }

        public static bool CanUseUnityImage(this FObject fobject, FigmaConverterUnity fcu)
        {
            bool? result = null;
            int reason = 0;

            if (fcu.IsUGUI())
            {
                if (fcu.UsingAnyProceduralImage())
                {
                    if (result == null && fcu.Settings.ImageSpritesSettings.ProceduralCondition.HasFlag(ProceduralCondition.Sprite))
                    {
                        if (fobject.IsDownloadableType())
                        {
                            reason = 1;
                            result = true;
                        }
                    }

                    if (result == null && fcu.Settings.ImageSpritesSettings.ProceduralCondition.HasFlag(ProceduralCondition.RectangleNoRoundedCorners))
                    {
                        bool b1 = fobject.Type == NodeType.RECTANGLE || fobject.Type == NodeType.FRAME;
                        bool b2 = !fobject.ContainsRoundedCorners();
                        bool b3 = !fcu.GraphicHelpers.IsDownloadableByFills(fobject, out string _reason).ToBoolNullFalse();
                        bool b4 = !fobject.Data.Graphic.HasStroke;

                        FcuLogger.Debug($"{nameof(CanUseUnityImage)} | {fobject.Data.NameHierarchy} | {b1} | {b2} | {b3} | {b4}", FcuDebugSettingsFlags.LogComponentDrawer);

                        reason = 2;
                        result = b1 && b2 && b3 && b4;
                    }
                }
                else if (fobject.IsSvgExtension())
                {
                    reason = 3;
                    result = false;
                }
                else if (fcu.UsingSvgImage())
                {
                    if (result == null && fcu.Settings.ImageSpritesSettings.SvgCondition.HasFlag(SvgCondition.ImageOrVideo))
                    {
                        if (fobject.IsAnyImageOrVideoOrEmojiTypeInChildren())
                        {
                            reason = 4;
                            result = true;
                        }
                    }

                    if (result == null && fcu.Settings.ImageSpritesSettings.SvgCondition.HasFlag(SvgCondition.AnyEffect))
                    {
                        if (fobject.IsAnyEffectInChildren())
                        {
                            reason = 5;
                            result = true;
                        }
                    }
                }
            }

            FcuLogger.Debug($"{nameof(CanUseUnityImage)} | {fobject.Data.NameHierarchy} | {result} | {reason}", FcuDebugSettingsFlags.LogComponentDrawer);

            return result.ToBoolNullFalse();
        }
    }
}
