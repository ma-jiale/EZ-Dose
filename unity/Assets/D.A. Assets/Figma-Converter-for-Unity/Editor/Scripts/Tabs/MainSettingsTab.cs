using DA_Assets.DAI;
using DA_Assets.FCU.Model;
using DA_Assets.Logging;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using DA_Assets.FCU.Extensions;
using DA_Assets.Singleton;
using DA_Assets.Extensions;

namespace DA_Assets.FCU
{
    internal class MainSettingsTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, FigmaConverterUnity>
    {
        public VisualElement Draw()
        {
            VisualElement root = new VisualElement();
            UIHelpers.SetDefaultPadding(root);

            DrawElements(root);

            return root;
        }

        private void DrawElements(VisualElement parent)
        {
            parent.Add(uitk.CreateTitle(
                FcuLocKey.label_main_settings.Localize(),
                FcuLocKey.tooltip_main_settings.Localize()));
            parent.Add(uitk.Space10());

            var formContainer = uitk.CreateSectionPanel();
            parent.Add(formContainer);

            if (scriptableObject.SerializedObject == null)
            {
                formContainer.Add(new HelpBox("SerializedObject is null. Cannot draw MainSettingsTab.", HelpBoxMessageType.Warning));
                return;
            }

            string pathToMainSettings = $"{nameof(monoBeh.Settings)}.{nameof(monoBeh.Settings.MainSettings)}";
            SerializedProperty mainSettingsProp = scriptableObject.SerializedObject.FindProperty(pathToMainSettings);

            if (mainSettingsProp == null)
            {
                formContainer.Add(new HelpBox(FcuLocKey.mainsettings_error_property_not_found.Localize(), HelpBoxMessageType.Error));
                return;
            }

            EnumField uiFrameworkField = uitk.EnumField(FcuLocKey.label_ui_framework.Localize(), monoBeh.Settings.MainSettings.UIFramework);
            uiFrameworkField.tooltip = FcuLocKey.tooltip_ui_framework.Localize();

            void ApplyUiFrameworkSelection(UIFramework requestedValue, bool updateField, bool logErrors, bool refreshTabs)
            {
                var validatedValue = requestedValue;

#if (FCU_EXISTS && FCU_UITK_EXT_EXISTS) == false
                if (requestedValue == UIFramework.UITK)
                {
                    if (logErrors)
                    {
                        Debug.LogError(FcuLocKey.log_asset_not_imported.Localize(nameof(UIFramework.UITK)));
                    }

                    validatedValue = UIFramework.UGUI;
                }
#endif

#if NOVA_UI_EXISTS == false
                if (requestedValue == UIFramework.NOVA)
                {
                    if (logErrors)
                    {
                        Debug.LogError(FcuLocKey.log_asset_not_imported.Localize(nameof(UIFramework.NOVA)));
                    }

                    validatedValue = UIFramework.UGUI;
                }
#endif

                if (updateField)
                {
                    if ((UIFramework)uiFrameworkField.value != validatedValue)
                    {
                        uiFrameworkField.SetValueWithoutNotify(validatedValue);
                    }
                }

                bool changed = monoBeh.Settings.MainSettings.UIFramework != validatedValue;
                monoBeh.Settings.MainSettings.UIFramework = validatedValue;

                if (refreshTabs && (changed || validatedValue != requestedValue))
                {
                    scriptableObject.RefreshTabs();
                }
            }

            uiFrameworkField.RegisterValueChangedCallback(evt =>
            {
                ApplyUiFrameworkSelection((UIFramework)evt.newValue, updateField: true, logErrors: true, refreshTabs: true);
            });
            formContainer.Add(uiFrameworkField);

            ApplyUiFrameworkSelection(monoBeh.Settings.MainSettings.UIFramework, updateField: true, logErrors: false, refreshTabs: false);

            if (monoBeh.IsUGUI() || monoBeh.IsNova() || monoBeh.IsDebug())
            {
                formContainer.Add(uitk.ItemSeparator());
                LayerField gameObjectLayerField = uitk.LayerField(FcuLocKey.label_go_layer.Localize(), monoBeh.Settings.MainSettings.GameObjectLayer);
                gameObjectLayerField.tooltip = FcuLocKey.tooltip_go_layer.Localize();
                gameObjectLayerField.RegisterValueChangedCallback(evt =>
                {
                    monoBeh.Settings.MainSettings.GameObjectLayer = evt.newValue;
                });
                formContainer.Add(gameObjectLayerField);
            }

            if (monoBeh.IsUGUI() || monoBeh.IsDebug())
            {
                formContainer.Add(uitk.ItemSeparator());
                EnumField positioningModeField = uitk.EnumField(FcuLocKey.label_positioning_mode.Localize(), monoBeh.Settings.MainSettings.PositioningMode);
                positioningModeField.tooltip = FcuLocKey.tooltip_positioning_mode.Localize();
                positioningModeField.RegisterValueChangedCallback(evt =>
                {
                    monoBeh.Settings.MainSettings.PositioningMode = (PositioningMode)evt.newValue;
                });
                formContainer.Add(positioningModeField);

                formContainer.Add(uitk.ItemSeparator());
                EnumField pivotTypeField = uitk.EnumField(FcuLocKey.label_pivot_type.Localize(), monoBeh.Settings.MainSettings.PivotType);
                pivotTypeField.tooltip = FcuLocKey.tooltip_pivot_type.Localize();
                pivotTypeField.RegisterValueChangedCallback(evt =>
                {
                    monoBeh.Settings.MainSettings.PivotType = (PivotType)evt.newValue;
                });
                formContainer.Add(pivotTypeField);
            }

            formContainer.Add(uitk.ItemSeparator());
            IntegerField gameObjectNameMaxLenghtField = uitk.IntegerField(FcuLocKey.label_go_name_max_length.Localize());
            gameObjectNameMaxLenghtField.tooltip = FcuLocKey.tooltip_go_name_max_length.Localize();
            gameObjectNameMaxLenghtField.value = monoBeh.Settings.MainSettings.GameObjectNameMaxLenght;
            gameObjectNameMaxLenghtField.RegisterValueChangedCallback(evt =>
            {
                monoBeh.Settings.MainSettings.GameObjectNameMaxLenght = evt.newValue;
            });
            formContainer.Add(gameObjectNameMaxLenghtField);

            formContainer.Add(uitk.ItemSeparator());
            IntegerField textObjectNameMaxLenghtField = uitk.IntegerField(FcuLocKey.label_text_name_max_length.Localize());
            textObjectNameMaxLenghtField.tooltip = FcuLocKey.tooltip_text_name_max_length.Localize();
            textObjectNameMaxLenghtField.value = monoBeh.Settings.MainSettings.TextObjectNameMaxLenght;
            textObjectNameMaxLenghtField.RegisterValueChangedCallback(evt =>
            {
                monoBeh.Settings.MainSettings.TextObjectNameMaxLenght = evt.newValue;
            });
            formContainer.Add(textObjectNameMaxLenghtField);

            formContainer.Add(uitk.ItemSeparator());
            Toggle sequentialImportToggle = uitk.Toggle(FcuLocKey.label_sequential_import.Localize());
            sequentialImportToggle.tooltip = FcuLocKey.tooltip_sequential_import.Localize();
            sequentialImportToggle.value = monoBeh.Settings.MainSettings.SequentialImport;
            sequentialImportToggle.RegisterValueChangedCallback(evt =>
            {
                monoBeh.Settings.MainSettings.SequentialImport = evt.newValue;
            });
            formContainer.Add(sequentialImportToggle);

            formContainer.Add(uitk.ItemSeparator());
            Toggle useDuplicateFinderToggle = uitk.Toggle(FcuLocKey.label_use_duplicate_finder.Localize());
            useDuplicateFinderToggle.tooltip = FcuLocKey.tooltip_use_duplicate_finder.Localize();
            useDuplicateFinderToggle.value = monoBeh.Settings.MainSettings.UseDuplicateFinder;
            useDuplicateFinderToggle.RegisterValueChangedCallback(evt =>
            {
                monoBeh.Settings.MainSettings.UseDuplicateFinder = evt.newValue;
            });
            formContainer.Add(useDuplicateFinderToggle);

            formContainer.Add(uitk.ItemSeparator());
            Toggle rawImportToggle = uitk.Toggle(FcuLocKey.label_raw_import.Localize());
            rawImportToggle.tooltip = FcuLocKey.tooltip_raw_import.Localize();
            rawImportToggle.value = monoBeh.Settings.MainSettings.RawImport;
            rawImportToggle.RegisterValueChangedCallback(evt =>
            {
                monoBeh.Settings.MainSettings.RawImport = evt.newValue;
            });
            formContainer.Add(rawImportToggle);

            formContainer.Add(uitk.ItemSeparator());
            Toggle httpsToggle = uitk.Toggle(FcuLocKey.label_https_setting.Localize());
            httpsToggle.tooltip = FcuLocKey.tooltip_https_setting.Localize();
            httpsToggle.value = monoBeh.Settings.MainSettings.Https;
            httpsToggle.RegisterValueChangedCallback(evt =>
            {
                monoBeh.Settings.MainSettings.Https = evt.newValue;
            });
            formContainer.Add(httpsToggle);

            formContainer.Add(uitk.ItemSeparator());
            var allowedNameCharsField = new IMGUIContainer(() =>
            {
                uitk.Colorize(() =>
                {
                    EditorGUILayout.PropertyField(mainSettingsProp.FindPropertyRelative(nameof(MainSettings.AllowedNameChars)));
                });
            });
            formContainer.Add(allowedNameCharsField);

            formContainer.Add(uitk.ItemSeparator());
            EnumField languageField = uitk.EnumField(FcuLocKey.label_ui_language.Localize(), FcuConfig.Instance.Localizator.Language);
            languageField.tooltip = FcuLocKey.tooltip_ui_language.Localize();
            languageField.RegisterValueChangedCallback(evt =>
            {
                FcuConfig.Instance.Localizator.Language = (DALanguage)evt.newValue;
#if FCU_UITK_EXT_EXISTS
                FuitkConfig.Instance.Localizator.Language = (DALanguage)evt.newValue;
#endif
            });
            formContainer.Add(languageField);
        }
    }
}
