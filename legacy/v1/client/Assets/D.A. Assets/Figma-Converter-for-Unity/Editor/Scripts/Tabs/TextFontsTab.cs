using DA_Assets.DAI;
using DA_Assets.FCU.Extensions;
using DA_Assets.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.FCU
{
    [Serializable]
    internal class TextFontsTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, FigmaConverterUnity>
    {
        private VisualElement _dynamicSettingsContainer;

        public VisualElement Draw()
        {
            VisualElement root = new VisualElement();
            UIHelpers.SetDefaultPadding(root);

            root.Add(uitk.CreateTitle(
                FcuLocKey.label_text_and_fonts.Localize(),
                FcuLocKey.tooltip_text_and_fonts.Localize()));
            root.Add(uitk.Space10());

            DrawGeneralSettings(root);
            root.Add(uitk.Space10());

            _dynamicSettingsContainer = new VisualElement();
            root.Add(_dynamicSettingsContainer);
            UpdateDynamicSettings();
            root.Add(uitk.Space10());

            DrawPathSettings(root);
            root.Add(uitk.Space10());

            DrawGoogleFontsSettings(root);

#if TextMeshPro
            root.Add(uitk.Space10());
            DrawFontGenerationSettings(root);
#endif

            return root;
        }

        private void DrawGeneralSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var textComponentField = uitk.EnumField(FcuLocKey.label_text_component.Localize(), monoBeh.Settings.TextFontsSettings.TextComponent);
            textComponentField.tooltip = FcuLocKey.tooltip_text_component.Localize();
            textComponentField.RegisterValueChangedCallback(evt =>
            {
                var requestedValue = (TextComponent)evt.newValue;
                var validatedValue = requestedValue;

                switch (requestedValue)
                {
                    case TextComponent.UnityEngine_UI_Text:
                        break;

                    case TextComponent.TextMeshPro:
#if TextMeshPro == false
                        Debug.LogError(FcuLocKey.log_asset_not_imported.Localize(nameof(TextComponent.TextMeshPro)));
                        validatedValue = TextComponent.UnityEngine_UI_Text;
#endif
                        break;

                    case TextComponent.RTL_TextMeshPro:
                        break;

                    case TextComponent.UI_Toolkit_Text:
                        break;
                }

                if (monoBeh.Settings.MainSettings.UIFramework == UIFramework.UITK &&
                    validatedValue != TextComponent.UI_Toolkit_Text)
                {
                    Debug.LogError(FcuLocKey.label_cannot_select_setting.Localize(validatedValue, monoBeh.Settings.MainSettings.UIFramework));
                    validatedValue = TextComponent.UI_Toolkit_Text;
                }

                if (validatedValue != requestedValue)
                {
                    textComponentField.SetValueWithoutNotify(validatedValue);
                }

                monoBeh.Settings.TextFontsSettings.TextComponent = validatedValue;
                UpdateDynamicSettings();
            });
            panel.Add(textComponentField);
        }

        private void UpdateDynamicSettings()
        {
            _dynamicSettingsContainer.Clear();

            switch (monoBeh.Settings.TextFontsSettings.TextComponent)
            {
                case TextComponent.UnityEngine_UI_Text:
                    DrawDefaultTextSettings(_dynamicSettingsContainer);
                    break;
                case TextComponent.TextMeshPro:
                case TextComponent.RTL_TextMeshPro:
                    DrawTextMeshSettingsSection(_dynamicSettingsContainer);
                    break;
                case TextComponent.UI_Toolkit_Text:
                    DrawUitkTextSettings(_dynamicSettingsContainer);
                    break;
            }
        }

        public void DrawDefaultTextSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel();
            parent.Add(panel);

            Label header = new Label(FcuLocKey.label_unity_text_settings.Localize());
            header.tooltip = FcuLocKey.tooltip_unity_text_settings.Localize();
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(header);
            panel.Add(uitk.ItemSeparator());

            var settings = monoBeh.Settings.UnityTextSettings;

            var bestFitToggle = uitk.Toggle(FcuLocKey.label_best_fit.Localize());
            bestFitToggle.tooltip = FcuLocKey.tooltip_best_fit.Localize();
            bestFitToggle.value = settings.BestFit;
            bestFitToggle.RegisterValueChangedCallback(evt =>
            {
                settings.BestFit = evt.newValue;
                if (settings.VerticalWrapMode == VerticalWrapMode.Overflow)
                {
                    settings.BestFit = false;
                    bestFitToggle.SetValueWithoutNotify(false);
                }
            });
            panel.Add(bestFitToggle);
            panel.Add(uitk.ItemSeparator());

            var lineSpacingField = uitk.FloatField(FcuLocKey.label_line_spacing.Localize());
            lineSpacingField.tooltip = FcuLocKey.tooltip_line_spacing.Localize();
            lineSpacingField.value = settings.FontLineSpacing;
            lineSpacingField.RegisterValueChangedCallback(evt => settings.FontLineSpacing = evt.newValue);
            panel.Add(lineSpacingField);
            panel.Add(uitk.ItemSeparator());

            var hOverflowField = uitk.EnumField(FcuLocKey.label_horizontal_overflow.Localize(), settings.HorizontalWrapMode);
            hOverflowField.tooltip = FcuLocKey.tooltip_horizontal_overflow.Localize();
            hOverflowField.RegisterValueChangedCallback(evt => settings.HorizontalWrapMode = (HorizontalWrapMode)evt.newValue);
            panel.Add(hOverflowField);
            panel.Add(uitk.ItemSeparator());

            var vOverflowField = uitk.EnumField(FcuLocKey.label_vertical_overflow.Localize(), settings.VerticalWrapMode);
            vOverflowField.tooltip = FcuLocKey.tooltip_vertical_overflow.Localize();
            vOverflowField.RegisterValueChangedCallback(evt =>
            {
                settings.VerticalWrapMode = (VerticalWrapMode)evt.newValue;
                if (settings.VerticalWrapMode == VerticalWrapMode.Overflow)
                {
                    settings.BestFit = false;
                    bestFitToggle.SetValueWithoutNotify(false);
                }
            });
            panel.Add(vOverflowField);
        }

        private void DrawTextMeshSettingsSection(VisualElement parent)
        {
#if TextMeshPro
            VisualElement panel = uitk.CreateSectionPanel();
            parent.Add(panel);

            Label header = new Label(FcuLocKey.label_textmeshpro_settings.Localize());
            header.tooltip = FcuLocKey.tooltip_textmeshpro_settings.Localize();
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(header);
            panel.Add(uitk.ItemSeparator());

            var settings = monoBeh.Settings.TextMeshSettings;

            var autoSizeToggle = uitk.Toggle(FcuLocKey.label_auto_size.Localize());
            autoSizeToggle.tooltip = FcuLocKey.tooltip_auto_size.Localize();
            autoSizeToggle.value = settings.AutoSize;
            autoSizeToggle.RegisterValueChangedCallback(evt => settings.AutoSize = evt.newValue);
            panel.Add(autoSizeToggle);
            panel.Add(uitk.ItemSeparator());

            var overrideTagsToggle = uitk.Toggle(FcuLocKey.label_override_tags.Localize());
            overrideTagsToggle.tooltip = FcuLocKey.tooltip_override_tags.Localize();
            overrideTagsToggle.value = settings.OverrideTags;
            overrideTagsToggle.RegisterValueChangedCallback(evt => settings.OverrideTags = evt.newValue);
            panel.Add(overrideTagsToggle);
            panel.Add(uitk.ItemSeparator());

            var wrappingToggle = uitk.Toggle(FcuLocKey.label_wrapping.Localize());
            wrappingToggle.tooltip = FcuLocKey.tooltip_wrapping.Localize();
            wrappingToggle.value = settings.Wrapping;
            wrappingToggle.RegisterValueChangedCallback(evt => settings.Wrapping = evt.newValue);
            panel.Add(wrappingToggle);
            panel.Add(uitk.ItemSeparator());

            if (monoBeh.IsNova() || monoBeh.IsDebug())
            {
                var orthoModeToggle = uitk.Toggle(FcuLocKey.label_orthographic_mode.Localize());
                orthoModeToggle.tooltip = FcuLocKey.tooltip_orthographic_mode.Localize();
                orthoModeToggle.value = settings.OrthographicMode;
                orthoModeToggle.RegisterValueChangedCallback(evt => settings.OrthographicMode = evt.newValue);
                panel.Add(orthoModeToggle);
                panel.Add(uitk.ItemSeparator());
            }

            var richTextToggle = uitk.Toggle(FcuLocKey.label_rich_text.Localize());
            richTextToggle.tooltip = FcuLocKey.tooltip_rich_text.Localize();
            richTextToggle.value = settings.RichText;
            richTextToggle.RegisterValueChangedCallback(evt => settings.RichText = evt.newValue);
            panel.Add(richTextToggle);
            panel.Add(uitk.ItemSeparator());

            var raycastTargetToggle = uitk.Toggle(FcuLocKey.label_raycast_target.Localize());
            raycastTargetToggle.tooltip = FcuLocKey.tooltip_raycast_target.Localize();
            raycastTargetToggle.value = settings.RaycastTarget;
            raycastTargetToggle.RegisterValueChangedCallback(evt => settings.RaycastTarget = evt.newValue);
            panel.Add(raycastTargetToggle);
            panel.Add(uitk.ItemSeparator());

            var parseEscCharsToggle = uitk.Toggle(FcuLocKey.label_parse_escape_characters.Localize());
            parseEscCharsToggle.tooltip = FcuLocKey.tooltip_parse_escape_characters.Localize();
            parseEscCharsToggle.value = settings.ParseEscapeCharacters;
            parseEscCharsToggle.RegisterValueChangedCallback(evt => settings.ParseEscapeCharacters = evt.newValue);
            panel.Add(parseEscCharsToggle);
            panel.Add(uitk.ItemSeparator());

            var visibleDescenderToggle = uitk.Toggle(FcuLocKey.label_visible_descender.Localize());
            visibleDescenderToggle.tooltip = FcuLocKey.tooltip_visible_descender.Localize();
            visibleDescenderToggle.value = settings.VisibleDescender;
            visibleDescenderToggle.RegisterValueChangedCallback(evt => settings.VisibleDescender = evt.newValue);
            panel.Add(visibleDescenderToggle);
            panel.Add(uitk.ItemSeparator());

            var kerningToggle = uitk.Toggle(FcuLocKey.label_kerning.Localize());
            kerningToggle.tooltip = FcuLocKey.tooltip_kerning.Localize();
            kerningToggle.value = settings.Kerning;
            kerningToggle.RegisterValueChangedCallback(evt => settings.Kerning = evt.newValue);
            panel.Add(kerningToggle);
            panel.Add(uitk.ItemSeparator());

            var extraPaddingToggle = uitk.Toggle(FcuLocKey.label_extra_padding.Localize());
            extraPaddingToggle.tooltip = FcuLocKey.tooltip_extra_padding.Localize();
            extraPaddingToggle.value = settings.ExtraPadding;
            extraPaddingToggle.RegisterValueChangedCallback(evt => settings.ExtraPadding = evt.newValue);
            panel.Add(extraPaddingToggle);
            panel.Add(uitk.ItemSeparator());

            var overflowField = uitk.EnumField(FcuLocKey.label_overflow.Localize(), settings.Overflow);
            overflowField.tooltip = FcuLocKey.tooltip_overflow.Localize();
            overflowField.value = settings.Overflow;
            overflowField.RegisterValueChangedCallback(evt => settings.Overflow = (TMPro.TextOverflowModes)evt.newValue);
            panel.Add(overflowField);
            panel.Add(uitk.ItemSeparator());

            var hMappingField = uitk.EnumField(FcuLocKey.label_horizontal_mapping.Localize(), settings.HorizontalMapping);
            hMappingField.tooltip = FcuLocKey.tooltip_horizontal_mapping.Localize();
            hMappingField.value = settings.HorizontalMapping;
            hMappingField.RegisterValueChangedCallback(evt => settings.HorizontalMapping = (TMPro.TextureMappingOptions)evt.newValue);
            panel.Add(hMappingField);
            panel.Add(uitk.ItemSeparator());

            var vMappingField = uitk.EnumField(FcuLocKey.label_vertical_mapping.Localize(), settings.VerticalMapping);
            vMappingField.tooltip = FcuLocKey.tooltip_vertical_mapping.Localize();
            vMappingField.value = settings.VerticalMapping;
            vMappingField.RegisterValueChangedCallback(evt => settings.VerticalMapping = (TMPro.TextureMappingOptions)evt.newValue);
            panel.Add(vMappingField);
            panel.Add(uitk.ItemSeparator());

            var geoSortingField = uitk.EnumField(FcuLocKey.label_geometry_sorting.Localize(), settings.GeometrySorting);
            geoSortingField.tooltip = FcuLocKey.tooltip_geometry_sorting.Localize();
            geoSortingField.value = settings.GeometrySorting;
            geoSortingField.RegisterValueChangedCallback(evt => settings.GeometrySorting = (TMPro.VertexSortingOrder)evt.newValue);
            panel.Add(geoSortingField);
            panel.Add(uitk.ItemSeparator());

            List<string> shaderNames = ShaderUtil.GetAllShaderInfo().Select(info => info.name).ToList();
            var shaderDropdown = new PopupField<string>(FcuLocKey.label_shader.Localize(), shaderNames, 0);
            shaderDropdown.tooltip = FcuLocKey.tooltip_shader.Localize();
            shaderDropdown.RegisterValueChangedCallback(evt => settings.Shader = Shader.Find(evt.newValue));
            panel.Add(shaderDropdown);

#if RTLTMP_EXISTS
            if (monoBeh.UsingRTLTextMeshPro() || monoBeh.IsDebug())
            {
                panel.Add(uitk.ItemSeparator());

                var farsiToggle = uitk.Toggle(FcuLocKey.label_farsi.Localize());
                farsiToggle.tooltip = FcuLocKey.tooltip_farsi.Localize();
                farsiToggle.value = settings.Farsi;
                farsiToggle.RegisterValueChangedCallback(evt => settings.Farsi = evt.newValue);
                panel.Add(farsiToggle);
                panel.Add(uitk.ItemSeparator());

                var forceFixToggle = uitk.Toggle(FcuLocKey.label_force_fix.Localize());
                forceFixToggle.tooltip = FcuLocKey.tooltip_force_fix.Localize();
                forceFixToggle.value = settings.ForceFix;
                forceFixToggle.RegisterValueChangedCallback(evt => settings.ForceFix = evt.newValue);
                panel.Add(forceFixToggle);
                panel.Add(uitk.ItemSeparator());

                var preserveNumsToggle = uitk.Toggle(FcuLocKey.label_preserve_numbers.Localize());
                preserveNumsToggle.tooltip = FcuLocKey.tooltip_preserve_numbers.Localize();
                preserveNumsToggle.value = settings.PreserveNumbers;
                preserveNumsToggle.RegisterValueChangedCallback(evt => settings.PreserveNumbers = evt.newValue);
                panel.Add(preserveNumsToggle);
                panel.Add(uitk.ItemSeparator());

                var fixTagsToggle = uitk.Toggle(FcuLocKey.label_fix_tags.Localize());
                fixTagsToggle.tooltip = FcuLocKey.tooltip_fix_tags.Localize();
                fixTagsToggle.value = settings.FixTags;
                fixTagsToggle.RegisterValueChangedCallback(evt => settings.FixTags = evt.newValue);
                panel.Add(fixTagsToggle);
            }
#endif
#endif
        }

        private void DrawUitkTextSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel();
            parent.Add(panel);

            Label header = new Label(FcuLocKey.label_uitk_text_settings.Localize());
            header.tooltip = FcuLocKey.tooltip_uitk_text_settings.Localize();
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(header);
            panel.Add(uitk.ItemSeparator());

            var settings = monoBeh.Settings.UitkTextSettings;

            var whiteSpaceField = uitk.EnumField(FcuLocKey.label_white_space.Localize(), settings.WhiteSpace);
            whiteSpaceField.tooltip = FcuLocKey.tooltip_white_space.Localize();
            whiteSpaceField.RegisterValueChangedCallback(evt => settings.WhiteSpace = (WhiteSpace)evt.newValue);
            panel.Add(whiteSpaceField);
            panel.Add(uitk.ItemSeparator());

            var textOverflowField = uitk.EnumField(FcuLocKey.label_text_overflow.Localize(), settings.TextOverflow);
            textOverflowField.tooltip = FcuLocKey.tooltip_text_overflow.Localize();
            textOverflowField.RegisterValueChangedCallback(evt => settings.TextOverflow = (TextOverflow)evt.newValue);
            panel.Add(textOverflowField);
            panel.Add(uitk.ItemSeparator());

#if UNITY_2022_3_OR_NEWER
            var languageDirectionField = uitk.EnumField(FcuLocKey.label_language_direction.Localize(), settings.LanguageDirection);
            languageDirectionField.tooltip = FcuLocKey.tooltip_language_direction.Localize();
            languageDirectionField.RegisterValueChangedCallback(evt => settings.LanguageDirection = (LanguageDirection)evt.newValue);
            panel.Add(languageDirectionField);
            panel.Add(uitk.ItemSeparator());
#endif
            var autoSizeToggle = uitk.Toggle(FcuLocKey.label_auto_size.Localize());
            autoSizeToggle.tooltip = FcuLocKey.tooltip_auto_size.Localize();
            autoSizeToggle.value = settings.AutoSize;
            autoSizeToggle.RegisterValueChangedCallback(evt => settings.AutoSize = evt.newValue);
            panel.Add(autoSizeToggle);
            panel.Add(uitk.ItemSeparator());

            var focusableToggle = uitk.Toggle(FcuLocKey.label_focusable.Localize());
            focusableToggle.tooltip = FcuLocKey.tooltip_focusable.Localize();
            focusableToggle.value = settings.Focusable;
            focusableToggle.RegisterValueChangedCallback(evt => settings.Focusable = evt.newValue);
            panel.Add(focusableToggle);
            panel.Add(uitk.ItemSeparator());

            var richTextToggle = uitk.Toggle(FcuLocKey.label_rich_text.Localize());
            richTextToggle.tooltip = FcuLocKey.tooltip_rich_text.Localize();
            richTextToggle.value = settings.EnableRichText;
            richTextToggle.RegisterValueChangedCallback(evt => settings.EnableRichText = evt.newValue);
            panel.Add(richTextToggle);
            panel.Add(uitk.ItemSeparator());

            var emojiFallbackToggle = uitk.Toggle(FcuLocKey.label_emoji_fallback_support.Localize());
            emojiFallbackToggle.tooltip = FcuLocKey.tooltip_emoji_fallback_support.Localize();
            emojiFallbackToggle.value = settings.EmojiFallbackSupport;
            emojiFallbackToggle.RegisterValueChangedCallback(evt => settings.EmojiFallbackSupport = evt.newValue);
            panel.Add(emojiFallbackToggle);
            panel.Add(uitk.ItemSeparator());

            var parseEscapeToggle = uitk.Toggle(FcuLocKey.label_parse_escape_characters.Localize());
            parseEscapeToggle.tooltip = FcuLocKey.tooltip_parse_escape_characters.Localize();
            parseEscapeToggle.value = settings.ParseEscapeSequences;
            parseEscapeToggle.RegisterValueChangedCallback(evt => settings.ParseEscapeSequences = evt.newValue);
            panel.Add(parseEscapeToggle);
            panel.Add(uitk.ItemSeparator());

            var selectableToggle = uitk.Toggle(FcuLocKey.label_selectable.Localize());
            selectableToggle.tooltip = FcuLocKey.tooltip_selectable.Localize();
            selectableToggle.value = settings.Selectable;
            selectableToggle.RegisterValueChangedCallback(evt => settings.Selectable = evt.newValue);
            panel.Add(selectableToggle);
            panel.Add(uitk.ItemSeparator());

            var doubleClickToggle = uitk.Toggle(FcuLocKey.label_double_click_selects_word.Localize());
            doubleClickToggle.tooltip = FcuLocKey.tooltip_double_click_selects_word.Localize();
            doubleClickToggle.value = settings.DoubleClickSelectsWord;
            doubleClickToggle.RegisterValueChangedCallback(evt => settings.DoubleClickSelectsWord = evt.newValue);
            panel.Add(doubleClickToggle);
            panel.Add(uitk.ItemSeparator());

            var tripleClickToggle = uitk.Toggle(FcuLocKey.label_triple_click_selects_line.Localize());
            tripleClickToggle.tooltip = FcuLocKey.tooltip_triple_click_selects_line.Localize();
            tripleClickToggle.value = settings.TripleClickSelectsLine;
            tripleClickToggle.RegisterValueChangedCallback(evt => settings.TripleClickSelectsLine = evt.newValue);
            panel.Add(tripleClickToggle);
            panel.Add(uitk.ItemSeparator());

            var tooltipWhenElidedToggle = uitk.Toggle(FcuLocKey.label_display_tooltip_when_elided.Localize());
            tooltipWhenElidedToggle.tooltip = FcuLocKey.tooltip_display_tooltip_when_elided.Localize();
            tooltipWhenElidedToggle.value = settings.DisplayTooltipWhenElided;
            tooltipWhenElidedToggle.RegisterValueChangedCallback(evt => settings.DisplayTooltipWhenElided = evt.newValue);
            panel.Add(tooltipWhenElidedToggle);
        }

        private void DrawFontGenerationSettings(VisualElement parent)
        {
#if TextMeshPro
            VisualElement panel = uitk.CreateSectionPanel();
            parent.Add(panel);

            Label header = new Label(FcuLocKey.label_asset_creator_settings.Localize());
            header.tooltip = FcuLocKey.tooltip_asset_creator_settings.Localize();
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(header);
            panel.Add(uitk.ItemSeparator());

            var tmpDownloader = monoBeh.FontDownloader.TmpDownloader;

            var samplingField = uitk.IntegerField(FcuLocKey.label_sampling_point_size.Localize());
            samplingField.tooltip = FcuLocKey.tooltip_sampling_point_size.Localize();
            samplingField.value = tmpDownloader.SamplingPointSize;
            samplingField.RegisterValueChangedCallback(evt => tmpDownloader.SamplingPointSize = evt.newValue);
            panel.Add(samplingField);
            panel.Add(uitk.ItemSeparator());

            var paddingField = uitk.IntegerField(FcuLocKey.label_atlas_padding.Localize());
            paddingField.tooltip = FcuLocKey.tooltip_atlas_padding.Localize();
            paddingField.value = tmpDownloader.AtlasPadding;
            paddingField.RegisterValueChangedCallback(evt => tmpDownloader.AtlasPadding = evt.newValue);
            panel.Add(paddingField);
            panel.Add(uitk.ItemSeparator());

            var renderModeField = uitk.EnumField(FcuLocKey.label_render_mode.Localize(), tmpDownloader.RenderMode);
            renderModeField.tooltip = FcuLocKey.tooltip_render_mode.Localize();
            renderModeField.value = tmpDownloader.RenderMode;
            renderModeField.RegisterValueChangedCallback(evt => tmpDownloader.RenderMode = (UnityEngine.TextCore.LowLevel.GlyphRenderMode)evt.newValue);
            panel.Add(renderModeField);
            panel.Add(uitk.ItemSeparator());

            var atlasResField = uitk.Vector2IntField(FcuLocKey.label_atlas_resolution.Localize());
            atlasResField.tooltip = FcuLocKey.tooltip_atlas_resolution.Localize();
            atlasResField.value = new Vector2Int(tmpDownloader.AtlasWidth, tmpDownloader.AtlasHeight);
            atlasResField.RegisterValueChangedCallback(evt =>
            {
                tmpDownloader.AtlasWidth = evt.newValue.x;
                tmpDownloader.AtlasHeight = evt.newValue.y;
            });
            panel.Add(atlasResField);
            panel.Add(uitk.ItemSeparator());

            var populationModeField = uitk.EnumField(FcuLocKey.label_atlas_population_mode.Localize(), tmpDownloader.AtlasPopulationMode);
            populationModeField.tooltip = FcuLocKey.tooltip_atlas_population_mode.Localize();
            populationModeField.RegisterValueChangedCallback(evt => tmpDownloader.AtlasPopulationMode = (TMPro.AtlasPopulationMode)evt.newValue);
            panel.Add(populationModeField);
            panel.Add(uitk.ItemSeparator());

            var multiAtlasToggle = uitk.Toggle(FcuLocKey.label_enable_multi_atlas_support.Localize());
            multiAtlasToggle.tooltip = FcuLocKey.tooltip_enable_multi_atlas_support.Localize();
            multiAtlasToggle.value = tmpDownloader.EnableMultiAtlasSupport;
            multiAtlasToggle.RegisterValueChangedCallback(evt => tmpDownloader.EnableMultiAtlasSupport = evt.newValue);
            panel.Add(multiAtlasToggle);
            panel.Add(uitk.ItemSeparator());
            panel.Add(uitk.Space5());

            var downloadButton = uitk.Button(FcuLocKey.label_download_fonts_from_project.Localize(monoBeh.Settings.TextFontsSettings.TextComponent), () =>
            {
                monoBeh.FontDownloader.DownloadFontsCts?.Cancel();
                monoBeh.FontDownloader.DownloadFontsCts?.Dispose();
                monoBeh.FontDownloader.DownloadFontsCts = new CancellationTokenSource();
                _ = monoBeh.FontDownloader.DownloadAllProjectFonts(monoBeh.FontDownloader.DownloadFontsCts.Token);
            });
            downloadButton.tooltip = FcuLocKey.tooltip_download_fonts_from_project.Localize(monoBeh.Settings.TextFontsSettings.TextComponent);
            panel.Add(downloadButton);
#endif
        }

        private void DrawGoogleFontsSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel();
            parent.Add(panel);

            Label header = new Label(FcuLocKey.label_google_fonts_settings.Localize());
            header.tooltip = FcuLocKey.tooltip_google_fonts_settings.Localize();
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(header);
            panel.Add(uitk.ItemSeparator());

            {
                var apiKeyContainer = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Center
                    }
                };

                var apiKeyField = uitk.TextField(FcuLocKey.label_google_fonts_api_key.Localize());
                apiKeyField.tooltip = FcuLocKey.tooltip_google_fonts_api_key.Localize(FcuLocKey.label_google_fonts_api_key.Localize());
                apiKeyField.value = FcuConfig.GoogleFontsApiKey;
                apiKeyField.style.flexGrow = 1;

                apiKeyField.RegisterValueChangedCallback(evt => FcuConfig.GoogleFontsApiKey = evt.newValue);
                apiKeyContainer.Add(apiKeyField);

                var getApiKeyButton = uitk.Button(FcuLocKey.label_get_google_api_key.Localize(), () =>
                {
                    Application.OpenURL("https://developers.google.com/fonts/docs/developer_api#identifying_your_application_to_google");
                });
                getApiKeyButton.tooltip = FcuLocKey.tooltip_get_google_api_key.Localize();

                getApiKeyButton.style.maxWidth = 100;
                getApiKeyButton.style.maxHeight = 18;
                getApiKeyButton.style.marginTop = 1;

                UIHelpers.SetRadius(getApiKeyButton, 3);
                apiKeyContainer.Add(getApiKeyButton);
                panel.Add(apiKeyContainer);
            }

            panel.Add(uitk.ItemSeparator());

            if (monoBeh.FontDownloader.GFontsApi != null)
            {
                monoBeh.FontDownloader.GFontsApi.FontSubsets |= FontSubset.Latin;

                EnumFlagsField subsetsField = uitk.EnumFlagsField(nameof(DaGoogleFontsApi.FontSubsets), monoBeh.FontDownloader.GFontsApi.FontSubsets);
                subsetsField.RegisterValueChangedCallback(evt =>
                {
                    monoBeh.FontDownloader.GFontsApi.FontSubsets = (FontSubset)evt.newValue;
                });

                panel.Add(subsetsField);
            }
            else
            {
                panel.Add(new HelpBox(FcuLocKey.textfonts_error_gfontsapi_not_found.Localize(nameof(monoBeh.FontDownloader.GFontsApi)), HelpBoxMessageType.Error));
            }
        }

        private void DrawPathSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel();
            parent.Add(panel);

            Label fontSettingsHeader = new Label(FcuLocKey.label_font_settings.Localize());
            fontSettingsHeader.tooltip = FcuLocKey.tooltip_font_settings.Localize();
            fontSettingsHeader.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(fontSettingsHeader);
            panel.Add(uitk.ItemSeparator());

            SerializedProperty fontLoaderProp = scriptableObject.SerializedObject.FindProperty(nameof(FigmaConverterUnity.FontLoader));

            if (fontLoaderProp == null)
            {
                panel.Add(new HelpBox(FcuLocKey.textfonts_error_fontloader_not_found.Localize(nameof(FigmaConverterUnity.FontLoader)), HelpBoxMessageType.Error));
                return;
            }

            {
                var ttfPathContainer = uitk.CreateFolderInput(
                    label: FcuLocKey.label_ttf_path.Localize(),
                    tooltip: FcuLocKey.tooltip_ttf_path.Localize(),
                    initialValue: monoBeh.FontLoader.TtfFontsPath,
                    onPathChanged: (newValue) => monoBeh.FontLoader.TtfFontsPath = newValue,
                    onButtonClick: () => EditorUtility.OpenFolderPanel(
                        FcuLocKey.label_select_fonts_folder.Localize(),
                        monoBeh.FontLoader.TtfFontsPath,
                        ""),
                    buttonTooltip: FcuLocKey.tooltip_select_fonts_folder.Localize());

                panel.Add(ttfPathContainer);
                panel.Add(uitk.ItemSeparator());

                var addTtfButton = uitk.Button(FcuLocKey.label_add_ttf_fonts_from_folder.Localize(), () =>
                {
                    monoBeh.FontDownloader.AddTtfFontsCts?.Cancel();
                    monoBeh.FontDownloader.AddTtfFontsCts?.Dispose();
                    monoBeh.FontDownloader.AddTtfFontsCts = new CancellationTokenSource();
                    _ = monoBeh.FontLoader.AddToTtfFontsList(monoBeh.FontDownloader.AddTtfFontsCts.Token);
                });
                addTtfButton.tooltip = FcuLocKey.tooltip_add_ttf_fonts_from_folder.Localize();
                panel.Add(addTtfButton);
            }

            panel.Add(uitk.ItemSeparator());

            var ttfFontsList = new IMGUIContainer(() =>
            {
                uitk.Colorize(() =>
                {
                    EditorGUILayout.PropertyField(fontLoaderProp.FindPropertyRelative(nameof(FontLoader.TtfFonts)));
                });
            });
            panel.Add(ttfFontsList);

#if TextMeshPro
            panel.Add(uitk.ItemSeparator());

            {
                var tmpPathContainer = uitk.CreateFolderInput(
                    label: FcuLocKey.label_tmp_path.Localize(),
                    tooltip: FcuLocKey.tooltip_tmp_path.Localize(),
                    initialValue: monoBeh.FontLoader.TmpFontsPath,
                    onPathChanged: (newValue) => monoBeh.FontLoader.TmpFontsPath = newValue,
                    onButtonClick: () => EditorUtility.OpenFolderPanel(
                        FcuLocKey.label_select_fonts_folder.Localize(),
                        monoBeh.FontLoader.TmpFontsPath, 
                        ""),
                    buttonTooltip: FcuLocKey.tooltip_select_fonts_folder.Localize());

                panel.Add(tmpPathContainer);
                panel.Add(uitk.ItemSeparator());

                var addTmpButton = uitk.Button(FcuLocKey.label_add_tmp_fonts_from_folder.Localize(), () =>
                {
                    monoBeh.FontDownloader.AddTmpFontsCts?.Cancel();
                    monoBeh.FontDownloader.AddTmpFontsCts?.Dispose();
                    monoBeh.FontDownloader.AddTmpFontsCts = new CancellationTokenSource();
                    _ = monoBeh.FontLoader.AddToTmpMeshFontsList(monoBeh.FontDownloader.AddTmpFontsCts.Token);
                });
                addTmpButton.tooltip = FcuLocKey.tooltip_add_fonts_from_folder.Localize();
                panel.Add(addTmpButton);
            }

            panel.Add(uitk.ItemSeparator());

            var tmpFontsList = new IMGUIContainer(() =>
            {
                uitk.Colorize(() =>
                {
                    EditorGUILayout.PropertyField(fontLoaderProp.FindPropertyRelative(nameof(FontLoader.TmpFonts)));
                });
            });
            panel.Add(tmpFontsList);
#endif
        }
    }
}
