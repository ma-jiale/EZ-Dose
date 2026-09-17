using DA_Assets.DAI;
using DA_Assets.FCU.Extensions;
using DA_Assets.Logging;
using UnityEditor;
using UnityEngine;
using DA_Assets.FCU.Model;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace DA_Assets.FCU
{
    internal class ImageSpritesTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, FigmaConverterUnity>
    {
        private VisualElement dynamicSettingsContainer;

        public VisualElement Draw()
        {
            VisualElement root = new VisualElement();
            UIHelpers.SetDefaultPadding(root);

            root.Add(uitk.CreateTitle(
                FcuLocKey.label_images_and_sprites_tab.Localize(),
                FcuLocKey.tooltip_images_and_sprites_tab.Localize()
            ));
            root.Add(uitk.Space10());

            string pathToImageSpritesSettings = $"{nameof(monoBeh.Settings)}.{nameof(monoBeh.Settings.ImageSpritesSettings)}";
            SerializedProperty imageSpritesSettingsProp = scriptableObject.SerializedObject.FindProperty(pathToImageSpritesSettings);

            if (imageSpritesSettingsProp == null)
            {
                root.Add(new HelpBox(FcuLocKey.imagesprites_error_settings_not_found.Localize(), HelpBoxMessageType.Error));
                return root;
            }

            DrawImageComponentPanel(root);
            root.Add(uitk.Space10());

            DrawGeneralSettings(root, imageSpritesSettingsProp);
            root.Add(uitk.Space10());

            dynamicSettingsContainer = new VisualElement();
            root.Add(dynamicSettingsContainer);
            UpdateDynamicSettings();

            root.Add(uitk.Space10());

            DrawTextureImporterSettings(root);

            return root;
        }

        private void DrawImageComponentPanel(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var settings = monoBeh.Settings.ImageSpritesSettings;
            var imageComponentField = uitk.EnumField(FcuLocKey.label_image_component.Localize(), settings.ImageComponent);
            imageComponentField.tooltip = FcuLocKey.tooltip_image_component.Localize();

            void ApplyImageComponentSelection(ImageComponent requestedValue, bool updateField, bool logErrors)
            {
                var validatedValue = requestedValue;
#if SUBC_SHAPES_EXISTS == false
                if (requestedValue == ImageComponent.SubcShape)
                {
                    if (logErrors)
                    {
                        Debug.LogError(FcuLocKey.log_asset_not_imported.Localize(nameof(ImageComponent.SubcShape)));
                    }

                    validatedValue = ImageComponent.UnityImage;
                }
#endif

#if MPUIKIT_EXISTS == false
                if (requestedValue == ImageComponent.MPImage)
                {
                    if (logErrors)
                    {
                        Debug.LogError(FcuLocKey.log_asset_not_imported.Localize(nameof(ImageComponent.MPImage)));
                    }

                    validatedValue = ImageComponent.UnityImage;
                }
#endif

#if JOSH_PUI_EXISTS == false
                if (requestedValue == ImageComponent.ProceduralImage)
                {
                    if (logErrors)
                    {
                        Debug.LogError(FcuLocKey.log_asset_not_imported.Localize(nameof(ImageComponent.ProceduralImage)));
                    }

                    validatedValue = ImageComponent.UnityImage;
                }
#endif

#if PROCEDURAL_UI_ASSET_STORE_RELEASE == false
                if (requestedValue == ImageComponent.RoundedImage)
                {
                    if (logErrors)
                    {
                        Debug.LogError(FcuLocKey.log_asset_not_imported.Localize(nameof(ImageComponent.RoundedImage)));
                    }

                    validatedValue = ImageComponent.UnityImage;
                }
#endif

#if VECTOR_GRAPHICS_EXISTS == false
                if (requestedValue == ImageComponent.SvgImage)
                {
                    if (logErrors)
                    {
                        Debug.LogError(FcuLocKey.log_asset_not_imported.Localize(nameof(ImageComponent.SvgImage)));
                    }

                    validatedValue = ImageComponent.UnityImage;
                }
#endif

                if (monoBeh.Settings.MainSettings.UIFramework == UIFramework.UITK &&
                    validatedValue != ImageComponent.UI_Toolkit_Image)
                {
                    Debug.LogError(FcuLocKey.label_cannot_select_setting.Localize(validatedValue, monoBeh.Settings.MainSettings.UIFramework));
                    validatedValue = ImageComponent.UI_Toolkit_Image;
                }

                if (updateField)
                {
                    if ((ImageComponent)imageComponentField.value != validatedValue)
                    {
                        imageComponentField.SetValueWithoutNotify(validatedValue);
                    }
                }

                settings.ImageComponent = validatedValue;

                if (dynamicSettingsContainer != null)
                {
                    UpdateDynamicSettings();
                }
            }

            imageComponentField.RegisterValueChangedCallback(evt =>
            {
                ApplyImageComponentSelection((ImageComponent)evt.newValue, updateField: true, logErrors: true);
            });
            panel.Add(imageComponentField);

            ApplyImageComponentSelection(settings.ImageComponent, updateField: true, logErrors: false);
        }

        private void DrawGeneralSettings(VisualElement parent, SerializedProperty imageSpritesSettingsProp)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            if (monoBeh.IsUGUI())
            {
                if (monoBeh.UsingAnyProceduralImage() || monoBeh.IsDebug())
                {
                    var procConditionProp = imageSpritesSettingsProp.FindPropertyRelative(nameof(ImageSpritesSettings.ProceduralCondition));
                    if (procConditionProp != null && procConditionProp.hasVisibleChildren)
                    {
                        var procConditionField = new PropertyField(procConditionProp);
                        panel.Add(procConditionField);
                        panel.Add(uitk.ItemSeparator());
                    }
                }

                if (monoBeh.UsingSvgImage() || monoBeh.IsDebug())
                {
                    var svgConditionProp = imageSpritesSettingsProp.FindPropertyRelative(nameof(ImageSpritesSettings.SvgCondition));
                    if (svgConditionProp != null && svgConditionProp.hasVisibleChildren)
                    {
                        var svgConditionField = new PropertyField(svgConditionProp);
                        panel.Add(svgConditionField);
                        panel.Add(uitk.ItemSeparator());
                    }
                }

                if (monoBeh.UsingUnityImage() || monoBeh.UsingRawImage() || monoBeh.IsDebug())
                {
                    if (PlayerSettings.colorSpace == ColorSpace.Linear || monoBeh.IsDebug())
                    {
                        var linearMatToggle = uitk.Toggle(FcuLocKey.label_use_image_linear_material.Localize());
                        linearMatToggle.tooltip = FcuLocKey.tooltip_use_image_linear_material.Localize();
                        linearMatToggle.value = monoBeh.Settings.ImageSpritesSettings.UseImageLinearMaterial;
                        linearMatToggle.RegisterValueChangedCallback(evt => monoBeh.Settings.ImageSpritesSettings.UseImageLinearMaterial = evt.newValue);
                        panel.Add(linearMatToggle);
                        panel.Add(uitk.ItemSeparator());
                    }
                }
            }

            var imageFormatField = uitk.EnumField(FcuLocKey.label_images_format.Localize(), monoBeh.Settings.ImageSpritesSettings.ImageFormat);
            imageFormatField.tooltip = FcuLocKey.tooltip_images_format.Localize();

            void ApplyImageFormatSelection(ImageFormat requestedValue, bool updateField, bool logErrors)
            {
                var validatedValue = requestedValue;

#if VECTOR_GRAPHICS_EXISTS == false
                if (requestedValue == ImageFormat.SVG)
                {
                    if (logErrors)
                    {
                        Debug.LogError(FcuLocKey.log_asset_not_imported.Localize(nameof(ImageFormat.SVG)));
                    }

                    validatedValue = ImageFormat.PNG;
                }
#endif

                if (updateField)
                {
                    if ((ImageFormat)imageFormatField.value != validatedValue)
                    {
                        imageFormatField.SetValueWithoutNotify(validatedValue);
                    }
                }

                monoBeh.Settings.ImageSpritesSettings.ImageFormat = validatedValue;
            }

            imageFormatField.RegisterValueChangedCallback(evt =>
            {
                ApplyImageFormatSelection((ImageFormat)evt.newValue, updateField: true, logErrors: true);
            });
            panel.Add(imageFormatField);
            panel.Add(uitk.ItemSeparator());

            ApplyImageFormatSelection(monoBeh.Settings.ImageSpritesSettings.ImageFormat, updateField: true, logErrors: false);

            const float imageScaleStep = 0.25f;
            var imageScaleSlider = new Slider(FcuLocKey.label_images_scale.Localize(), 0.25f, 4.0f);
            imageScaleSlider.tooltip = FcuLocKey.tooltip_images_scale.Localize();
            imageScaleSlider.showInputField = true;

            float SnapImageScale(float value)
            {
                float snapped = Mathf.Round(value / imageScaleStep) * imageScaleStep;
                return Mathf.Clamp(snapped, imageScaleSlider.lowValue, imageScaleSlider.highValue);
            }

            float initialScale = SnapImageScale(monoBeh.Settings.ImageSpritesSettings.ImageScale);
            imageScaleSlider.SetValueWithoutNotify(initialScale);
            monoBeh.Settings.ImageSpritesSettings.ImageScale = initialScale;

            imageScaleSlider.RegisterValueChangedCallback(evt =>
            {
                float snappedValue = SnapImageScale(evt.newValue);
                monoBeh.Settings.ImageSpritesSettings.ImageScale = snappedValue;

                if (!Mathf.Approximately(snappedValue, evt.newValue))
                {
                    imageScaleSlider.SetValueWithoutNotify(snappedValue);
                }
            });
            panel.Add(imageScaleSlider);
            panel.Add(uitk.ItemSeparator());

            var maxSpriteSizeField = uitk.Vector2IntField(FcuLocKey.label_max_sprite_size.Localize());
            maxSpriteSizeField.tooltip = FcuLocKey.tooltip_max_sprite_size.Localize();
            maxSpriteSizeField.value = monoBeh.Settings.ImageSpritesSettings.MaxSpriteSize;
            maxSpriteSizeField.RegisterValueChangedCallback(evt => monoBeh.Settings.ImageSpritesSettings.MaxSpriteSize = evt.newValue);
            panel.Add(maxSpriteSizeField);
            panel.Add(uitk.ItemSeparator());

            var ppuField = uitk.FloatField(FcuLocKey.label_pixels_per_unit.Localize());
            ppuField.tooltip = FcuLocKey.tooltip_pixels_per_unit.Localize();
            ppuField.value = monoBeh.Settings.ImageSpritesSettings.PixelsPerUnit;
            ppuField.RegisterValueChangedCallback(evt => monoBeh.Settings.ImageSpritesSettings.PixelsPerUnit = evt.newValue);
            panel.Add(ppuField);
            panel.Add(uitk.ItemSeparator());

            var redownloadToggle = uitk.Toggle(FcuLocKey.label_redownload_sprites.Localize());
            redownloadToggle.tooltip = FcuLocKey.tooltip_redownload_sprites.Localize();
            redownloadToggle.value = monoBeh.Settings.ImageSpritesSettings.RedownloadSprites;
            redownloadToggle.RegisterValueChangedCallback(evt => monoBeh.Settings.ImageSpritesSettings.RedownloadSprites = evt.newValue);
            panel.Add(redownloadToggle);
            panel.Add(uitk.ItemSeparator());

            var downloadOptionsField = uitk.EnumFlagsField("Download Options", monoBeh.Settings.ImageSpritesSettings.DownloadOptions);
            downloadOptionsField.RegisterValueChangedCallback(evt =>
            {
                monoBeh.Settings.ImageSpritesSettings.DownloadOptions = (SpriteDownloadOptions)evt.newValue;
            });
            panel.Add(downloadOptionsField);
            panel.Add(uitk.ItemSeparator());

            var preserveRatioField = uitk.EnumField(FcuLocKey.label_preserve_ratio_mode.Localize(), monoBeh.Settings.ImageSpritesSettings.PreserveRatioMode);
            preserveRatioField.tooltip = FcuLocKey.tooltip_preserve_ratio_mode.Localize();
            preserveRatioField.RegisterValueChangedCallback(evt => monoBeh.Settings.ImageSpritesSettings.PreserveRatioMode = (PreserveRatioMode)evt.newValue);
            panel.Add(preserveRatioField);
            panel.Add(uitk.ItemSeparator());

            {
                var spritesPathContainer = uitk.CreateFolderInput(
                    label: FcuLocKey.label_sprites_path.Localize(),
                    tooltip: FcuLocKey.tooltip_sprites_path.Localize(),
                    initialValue: monoBeh.Settings.ImageSpritesSettings.SpritesPath,
                    onPathChanged: (newValue) => monoBeh.Settings.ImageSpritesSettings.SpritesPath = newValue,
                    onButtonClick: () => EditorUtility.OpenFolderPanel(
                        FcuLocKey.label_select_folder.Localize(), 
                        monoBeh.Settings.ImageSpritesSettings.SpritesPath, 
                        ""),
                    buttonTooltip: FcuLocKey.tooltip_select_folder.Localize());
                panel.Add(spritesPathContainer);
            }
        }

        private void UpdateDynamicSettings()
        {
            dynamicSettingsContainer.Clear();

            if (monoBeh.IsUGUI() == false)
            {
                return;
            }

            switch (monoBeh.Settings.ImageSpritesSettings.ImageComponent)
            {
                case ImageComponent.UnityImage:
                    DrawUnityImageSettings(dynamicSettingsContainer);
                    break;
                case ImageComponent.RawImage:
                    DrawRawImageSettings(dynamicSettingsContainer);
                    break;
                case ImageComponent.ProceduralImage:
                    DrawProceduralUIImageSettings(dynamicSettingsContainer);
                    break;
                case ImageComponent.SubcShape:
                    DrawShapes2DSettings(dynamicSettingsContainer);
                    break;
                case ImageComponent.RoundedImage:
                    DrawDttPuiSettings(dynamicSettingsContainer);
                    break;
                case ImageComponent.MPImage:
                    DrawMPUIKitSettings(dynamicSettingsContainer);
                    break;
                case ImageComponent.SpriteRenderer:
                    DrawSpriteRendererSettings(dynamicSettingsContainer);
                    break;
                case ImageComponent.SvgImage:
                    DrawSvgImageSettings(dynamicSettingsContainer);
                    break;
            }
        }

        private void DrawBaseImageSettingsFields(VisualElement parent, BaseImageSettings settings)
        {
            var typeField = uitk.EnumField(FcuLocKey.label_image_type.Localize(), settings.Type);
            typeField.tooltip = FcuLocKey.tooltip_image_type.Localize();
            typeField.RegisterValueChangedCallback(evt => settings.Type = (UnityEngine.UI.Image.Type)evt.newValue);
            parent.Add(typeField);
            parent.Add(uitk.ItemSeparator());

            var raycastToggle = uitk.Toggle(FcuLocKey.label_raycast_target.Localize());
            raycastToggle.tooltip = FcuLocKey.tooltip_raycast_target.Localize();
            raycastToggle.value = settings.RaycastTarget;
            raycastToggle.RegisterValueChangedCallback(evt => settings.RaycastTarget = evt.newValue);
            parent.Add(raycastToggle);
            parent.Add(uitk.ItemSeparator());

            var aspectToggle = uitk.Toggle(FcuLocKey.label_preserve_aspect.Localize());
            aspectToggle.tooltip = FcuLocKey.tooltip_preserve_aspect.Localize();
            aspectToggle.value = settings.PreserveAspect;
            aspectToggle.RegisterValueChangedCallback(evt => settings.PreserveAspect = evt.newValue);
            parent.Add(aspectToggle);
            parent.Add(uitk.ItemSeparator());

            var paddingField = uitk.Vector4Field(FcuLocKey.label_raycast_padding.Localize());
            paddingField.tooltip = FcuLocKey.tooltip_raycast_padding.Localize();
            paddingField.value = settings.RaycastPadding;
            paddingField.RegisterValueChangedCallback(evt => settings.RaycastPadding = evt.newValue);
            parent.Add(paddingField);
            parent.Add(uitk.ItemSeparator());

            var maskableToggle = uitk.Toggle(FcuLocKey.label_maskable.Localize());
            maskableToggle.tooltip = FcuLocKey.tooltip_maskable.Localize();
            maskableToggle.value = settings.Maskable;
            maskableToggle.RegisterValueChangedCallback(evt => settings.Maskable = evt.newValue);
            parent.Add(maskableToggle);
        }

        private void DrawUnityImageSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var title = new Label(FcuLocKey.label_unity_image_settings.Localize());
            title.tooltip = FcuLocKey.tooltip_unity_image_settings.Localize();
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            panel.Add(uitk.ItemSeparator());

            DrawBaseImageSettingsFields(panel, monoBeh.Settings.UnityImageSettings);
        }

        private void DrawRawImageSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var title = new Label(FcuLocKey.label_raw_image_settings.Localize());
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            panel.Add(uitk.ItemSeparator());

            DrawBaseImageSettingsFields(panel, monoBeh.Settings.RawImageSettings);
        }

        private void DrawShapes2DSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var title = new Label(FcuLocKey.label_shapes2d_settings.Localize());
            title.tooltip = FcuLocKey.tooltip_shapes2d_settings.Localize();
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            panel.Add(uitk.ItemSeparator());

            DrawBaseImageSettingsFields(panel, monoBeh.Settings.Shapes2DSettings);
        }

        private void DrawDttPuiSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var title = new Label(FcuLocKey.label_procedural_ui_settings.Localize());
            title.tooltip = FcuLocKey.tooltip_procedural_ui_settings.Localize();
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            panel.Add(uitk.ItemSeparator());

            var settings = monoBeh.Settings.DttPuiSettings;

            var falloffField = uitk.FloatField(FcuLocKey.label_pui_falloff_distance.Localize());
            falloffField.tooltip = FcuLocKey.tooltip_pui_falloff_distance.Localize();
            falloffField.value = settings.FalloffDistance;
            falloffField.RegisterValueChangedCallback(evt => settings.FalloffDistance = evt.newValue);
            panel.Add(falloffField);
            panel.Add(uitk.ItemSeparator());

            DrawBaseImageSettingsFields(panel, settings);
        }

        private void DrawMPUIKitSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var title = new Label(FcuLocKey.label_mpuikit_settings.Localize());
            title.tooltip = FcuLocKey.tooltip_mpuikit_settings.Localize();
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            panel.Add(uitk.ItemSeparator());

            var settings = monoBeh.Settings.MPUIKitSettings;

            var falloffField = uitk.FloatField(FcuLocKey.label_pui_falloff_distance.Localize());
            falloffField.tooltip = FcuLocKey.tooltip_pui_falloff_distance.Localize();
            falloffField.value = settings.FalloffDistance;
            falloffField.RegisterValueChangedCallback(evt => settings.FalloffDistance = evt.newValue);
            panel.Add(falloffField);
            panel.Add(uitk.ItemSeparator());

            DrawBaseImageSettingsFields(panel, settings);
        }

        private void DrawSpriteRendererSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var title = new Label(FcuLocKey.label_sr_settings.Localize());
            title.tooltip = FcuLocKey.tooltip_sr_settings.Localize();
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            panel.Add(uitk.ItemSeparator());

            var settings = monoBeh.Settings.SpriteRendererSettings;

            var flipXToggle = uitk.Toggle(FcuLocKey.label_flip_x.Localize());
            flipXToggle.tooltip = FcuLocKey.tooltip_flip_x.Localize();
            flipXToggle.value = settings.FlipX;
            flipXToggle.RegisterValueChangedCallback(evt => settings.FlipX = evt.newValue);
            panel.Add(flipXToggle);
            panel.Add(uitk.ItemSeparator());

            var flipYToggle = uitk.Toggle(FcuLocKey.label_flip_y.Localize());
            flipYToggle.tooltip = FcuLocKey.tooltip_flip_y.Localize();
            flipYToggle.value = settings.FlipY;
            flipYToggle.RegisterValueChangedCallback(evt => settings.FlipY = evt.newValue);
            panel.Add(flipYToggle);
            panel.Add(uitk.ItemSeparator());

            var maskInteractionField = uitk.EnumField(FcuLocKey.label_mask_interaction.Localize(), settings.MaskInteraction);
            maskInteractionField.tooltip = FcuLocKey.tooltip_mask_interaction.Localize();
            maskInteractionField.RegisterValueChangedCallback(evt => settings.MaskInteraction = (SpriteMaskInteraction)evt.newValue);
            panel.Add(maskInteractionField);
            panel.Add(uitk.ItemSeparator());

            var sortPointField = uitk.EnumField(FcuLocKey.label_sort_point.Localize(), settings.SortPoint);
            sortPointField.tooltip = FcuLocKey.tooltip_sort_point.Localize();
            sortPointField.RegisterValueChangedCallback(evt => settings.SortPoint = (SpriteSortPoint)evt.newValue);
            panel.Add(sortPointField);
            panel.Add(uitk.ItemSeparator());

            var sortingLayerField = uitk.TextField(FcuLocKey.label_sorting_layer.Localize());
            sortingLayerField.tooltip = FcuLocKey.tooltip_sorting_layer.Localize();
            sortingLayerField.value = settings.SortingLayer;
            sortingLayerField.RegisterValueChangedCallback(evt => settings.SortingLayer = evt.newValue);
            panel.Add(sortingLayerField);
            panel.Add(uitk.ItemSeparator());

            var nextOrderStepField = uitk.IntegerField(FcuLocKey.label_next_order_step.Localize());
            nextOrderStepField.tooltip = FcuLocKey.tooltip_next_order_step.Localize();
            nextOrderStepField.value = settings.NextOrderStep;
            nextOrderStepField.RegisterValueChangedCallback(evt => settings.NextOrderStep = evt.newValue);
            panel.Add(nextOrderStepField);
        }

        private void DrawProceduralUIImageSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var title = new Label(FcuLocKey.label_pui_settings.Localize());
            title.tooltip = FcuLocKey.tooltip_pui_settings.Localize();
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            panel.Add(uitk.ItemSeparator());

            var settings = monoBeh.Settings.JoshPuiSettings;

            var falloffField = uitk.FloatField(FcuLocKey.label_pui_falloff_distance.Localize());
            falloffField.tooltip = FcuLocKey.tooltip_pui_falloff_distance.Localize();
            falloffField.value = settings.FalloffDistance;
            falloffField.RegisterValueChangedCallback(evt => settings.FalloffDistance = evt.newValue);
            panel.Add(falloffField);
            panel.Add(uitk.ItemSeparator());

            DrawBaseImageSettingsFields(panel, settings);
        }

        private void DrawTextureImporterSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var title = new Label(FcuLocKey.label_texture_importer_settings.Localize());
            title.tooltip = FcuLocKey.tooltip_texture_importer_settings.Localize();
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            panel.Add(uitk.ItemSeparator());

            var settings = monoBeh.Settings.TextureImporterSettings;

            var crunchToggle = uitk.Toggle(FcuLocKey.label_crunched_compression.Localize());
            crunchToggle.tooltip = FcuLocKey.tooltip_crunched_compression.Localize();
            crunchToggle.value = settings.CrunchedCompression;

            var qualitySlider = uitk.SliderInt(FcuLocKey.label_compression_quality.Localize(), 0, 100);
            qualitySlider.tooltip = FcuLocKey.tooltip_compression_quality.Localize();
            qualitySlider.showInputField = true;
            qualitySlider.value = settings.CompressionQuality;
            qualitySlider.RegisterValueChangedCallback(evt => settings.CompressionQuality = evt.newValue);
            qualitySlider.SetEnabled(settings.CrunchedCompression);

            crunchToggle.RegisterValueChangedCallback(evt =>
            {
                settings.CrunchedCompression = evt.newValue;
                qualitySlider.SetEnabled(evt.newValue);
            });

            panel.Add(crunchToggle);
            panel.Add(uitk.ItemSeparator());
            panel.Add(qualitySlider);
            panel.Add(uitk.ItemSeparator());

            var readableToggle = uitk.Toggle(FcuLocKey.label_is_readable.Localize());
            readableToggle.tooltip = FcuLocKey.tooltip_is_readable.Localize();
            readableToggle.value = settings.IsReadable;
            readableToggle.RegisterValueChangedCallback(evt => settings.IsReadable = evt.newValue);
            panel.Add(readableToggle);
            panel.Add(uitk.ItemSeparator());

            var mipmapToggle = uitk.Toggle(FcuLocKey.label_mipmap_enabled.Localize());
            mipmapToggle.tooltip = FcuLocKey.tooltip_mipmap_enabled.Localize();
            mipmapToggle.value = settings.MipmapEnabled;
            mipmapToggle.RegisterValueChangedCallback(evt => settings.MipmapEnabled = evt.newValue);
            panel.Add(mipmapToggle);
            panel.Add(uitk.ItemSeparator());

            var typeField = uitk.EnumField(FcuLocKey.label_texture_type.Localize(), settings.TextureType);
            typeField.tooltip = FcuLocKey.tooltip_texture_type.Localize();
            typeField.RegisterValueChangedCallback(evt => settings.TextureType = (TextureImporterType)evt.newValue);
            panel.Add(typeField);
            panel.Add(uitk.ItemSeparator());

            var compressionField = uitk.EnumField(FcuLocKey.label_texture_compression.Localize(), settings.TextureCompression);
            compressionField.tooltip = FcuLocKey.tooltip_texture_compression.Localize();
            compressionField.RegisterValueChangedCallback(evt => settings.TextureCompression = (TextureImporterCompression)evt.newValue);
            panel.Add(compressionField);
            panel.Add(uitk.ItemSeparator());

            var spriteModeField = uitk.EnumField(FcuLocKey.label_sprite_import_mode.Localize(), settings.SpriteImportMode);
            spriteModeField.tooltip = FcuLocKey.tooltip_sprite_import_mode.Localize();
            spriteModeField.RegisterValueChangedCallback(evt => settings.SpriteImportMode = (SpriteImportMode)evt.newValue);
            panel.Add(spriteModeField);
        }

        private void DrawSvgImageSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var title = new Label(FcuLocKey.label_svg_image_settings.Localize());
            title.tooltip = FcuLocKey.tooltip_svg_image_settings.Localize();
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            panel.Add(uitk.ItemSeparator());

            var settings = monoBeh.Settings.SvgImageSettings;

            var raycastToggle = uitk.Toggle(FcuLocKey.label_raycast_target.Localize());
            raycastToggle.tooltip = FcuLocKey.tooltip_raycast_target.Localize();
            raycastToggle.value = settings.RaycastTarget;
            raycastToggle.RegisterValueChangedCallback(evt => settings.RaycastTarget = evt.newValue);
            panel.Add(raycastToggle);
            panel.Add(uitk.ItemSeparator());

            var aspectToggle = uitk.Toggle(FcuLocKey.label_preserve_aspect.Localize());
            aspectToggle.tooltip = FcuLocKey.tooltip_preserve_aspect.Localize();
            aspectToggle.value = settings.PreserveAspect;
            aspectToggle.RegisterValueChangedCallback(evt => settings.PreserveAspect = evt.newValue);
            panel.Add(aspectToggle);
            panel.Add(uitk.ItemSeparator());

            var paddingField = uitk.Vector4Field(FcuLocKey.label_raycast_padding.Localize());
            paddingField.tooltip = FcuLocKey.tooltip_raycast_padding.Localize();
            paddingField.value = settings.RaycastPadding;
            paddingField.RegisterValueChangedCallback(evt => settings.RaycastPadding = evt.newValue);
            panel.Add(paddingField);
        }
    }
}
