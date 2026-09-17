using DA_Assets.Constants;
using DA_Assets.Extensions;
using DA_Assets.FCU.Extensions;
using System;
using System.Threading;
using UnityEngine;

namespace DA_Assets.FCU
{
    [Serializable]
    public class ProjectImporter : FcuBase
    {
        public CancellationTokenSource ImportTokenSource { get; set; }

        private ProjectImporterBase _importer;
        public ProjectImporterBase Importer => _importer;

        public void StartImport()
        {
            if (monoBeh.IsJsonNetExists() == false)
            {
                Debug.LogError(FcuLocKey.log_cant_find_package.Localize(DAConstants.JsonNetPackageName));
                return;
            }

            if (monoBeh.InspectorDrawer.SelectableDocument.IsProjectEmpty())
            {
                Debug.LogError(FcuLocKey.log_project_empty.Localize());
                return;
            }

            if (!ValidateImportSettings(out string reason))
            {
                Debug.LogError(reason);
                return;
            }

            UIFramework framework = monoBeh.Settings.MainSettings.UIFramework;
            switch (framework)
            {
                case UIFramework.UGUI:
                    _importer = new ProjectImporterUGUI();
                    _importer.Init(monoBeh);
                    break;
                case UIFramework.UITK:
                    _importer = new ProjectImporterUITK();
                    _importer.Init(monoBeh);
                    break;
                case UIFramework.NOVA:
                    _importer = new ProjectImporterNova();
                    _importer.Init(monoBeh);
                    break;
                default:
                    throw new NotSupportedException($"UIFramework {framework} is not supported");
            }

            monoBeh.AssetTools.StopAsset(StopAssetReason.Hidden);

            this.ImportTokenSource = new CancellationTokenSource();
            _ = _importer.StartImportAsync(this.ImportTokenSource.Token);
        }

        private bool ValidateImportSettings(out string reason)
        {
            bool? result = null;
            reason = "null";

            if (monoBeh.IsUITK())
            {
                if (monoBeh.Settings.ImageSpritesSettings.ImageComponent != ImageComponent.UI_Toolkit_Image)
                {
                    reason = FcuLocKey.log_import_failed_incompatible.Localize(
                        $"{nameof(UIFramework)}.{UIFramework.UITK}", $"{nameof(ImageFormat)}.{monoBeh.Settings.ImageSpritesSettings.ImageComponent}");
                    result = false;
                }
                else if (monoBeh.Settings.TextFontsSettings.TextComponent != TextComponent.UI_Toolkit_Text)
                {
                    reason = FcuLocKey.log_import_failed_incompatible.Localize(
                        $"{nameof(UIFramework)}.{UIFramework.UITK}", $"{nameof(TextComponent)}.{monoBeh.Settings.TextFontsSettings.TextComponent}");
                    result = false;
                }
                else if (monoBeh.UsingSVG())
                {
                    reason = FcuLocKey.log_import_failed_incompatible.Localize(
                        $"{nameof(UIFramework)}.{UIFramework.UITK}", $"{nameof(ImageFormat)}.{monoBeh.Settings.ImageSpritesSettings.ImageFormat}");
                    result = false;
                }
                else if (monoBeh.Settings.LocalizationSettings.LocalizationComponent ==
                         LocalizationComponent.I2Localization)
                {
                    reason = FcuLocKey.log_import_failed_incompatible.Localize(
                        $"{nameof(UIFramework)}.{UIFramework.UITK}",
                        $"{nameof(LocalizationComponent)}.{monoBeh.Settings.LocalizationSettings.LocalizationComponent}");
                    result = false;
                }
            }
            else
            {
                if (monoBeh.UsingUI_Toolkit_Image())
                {
                    reason = FcuLocKey.log_import_failed_incompatible.Localize(
                        $"{nameof(UIFramework)}.{monoBeh.Settings.MainSettings.UIFramework}",
                        $"{nameof(LocalizationComponent)}.{monoBeh.Settings.ImageSpritesSettings.ImageComponent}");
                    result = false;
                }
                else if (monoBeh.UsingUI_Toolkit_Text())
                {
                    reason = FcuLocKey.log_import_failed_incompatible.Localize(
                        $"{nameof(UIFramework)}.{monoBeh.Settings.MainSettings.UIFramework}",
                        $"{nameof(LocalizationComponent)}.{monoBeh.Settings.TextFontsSettings.TextComponent}");
                    result = false;
                }
                else if (monoBeh.UsingUIBlock2D())
                {
                    if (!monoBeh.IsNova())
                    {
                        reason = FcuLocKey.log_import_failed_enable_required.Localize(
                            $"{nameof(UIFramework)}.{UIFramework.NOVA}",
                            $"{nameof(ImageComponent)}.{ImageComponent.UIBlock2D}");
                        result = false;
                    }
                    else if (monoBeh.UsingSVG())
                    {
                        reason = FcuLocKey.log_import_failed_incompatible.Localize(
                            $"{nameof(ImageComponent)}.{ImageComponent.UIBlock2D}",
                            $"{nameof(ImageFormat)}.{ImageFormat.SVG}");
                        result = false;
                    }
                }
                else if (monoBeh.UsingSvgImage())
                {
                    if (!monoBeh.IsUGUI())
                    {
                        reason = FcuLocKey.log_import_failed_enable_required.Localize(
                            $"{nameof(UIFramework)}.{UIFramework.UGUI}",
                            $"{nameof(ImageComponent)}.{ImageComponent.SvgImage}");
                        result = false;
                    }
                    else if (!monoBeh.UsingSVG())
                    {
                        reason = FcuLocKey.log_import_failed_enable_required.Localize(
                            $"{nameof(ImageFormat)}.{ImageFormat.SVG}",
                            $"{nameof(ImageComponent)}.{ImageComponent.SvgImage}");
                        result = false;
                    }
                }
                else if (!monoBeh.UsingSvgImage())
                {
                    if (monoBeh.UsingSVG())
                    {
                        reason = FcuLocKey.log_import_failed_unsupported.Localize(
                            $"{nameof(ImageComponent)}.{monoBeh.Settings.ImageSpritesSettings.ImageComponent}",
                            $"{nameof(ImageFormat)}.{ImageFormat.SVG}");
                        result = false;
                    }
                }
            }

            return result.ToBoolNullTrue();
        }
    }
}
