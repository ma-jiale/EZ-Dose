using DA_Assets.DAI;
using DA_Assets.FCU.Extensions;
using DA_Assets.Logging;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System.Linq;
using System.Collections.Generic;
using DA_Assets.Tools;


#if DALOC_EXISTS
using DA_Assets.DAL;
#endif

#pragma warning disable CS0649

namespace DA_Assets.FCU
{
    internal class LocalizationTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, FigmaConverterUnity>
    {
        public VisualElement Draw()
        {
            var root = new VisualElement();
            UIHelpers.SetDefaultPadding(root);

            DrawElements(root);

            return root;
        }

        private void DrawElements(VisualElement parent)
        {
            parent.Add(uitk.CreateTitle(
                FcuLocKey.label_localization_settings.Localize(),
                FcuLocKey.tooltip_localization_settings.Localize()
            ));
            parent.Add(uitk.Space10());

            var formContainer = uitk.CreateSectionPanel();
            parent.Add(formContainer);

            var settings = monoBeh.Settings.LocalizationSettings;
            var settingsContainer = new VisualElement();

#if DALOC_EXISTS
            VisualElement daLocContainer = null;
#endif

            var locComponentField = uitk.EnumField(FcuLocKey.label_loc_component.Localize(), settings.LocalizationComponent);
            locComponentField.tooltip = FcuLocKey.tooltip_loc_component.Localize();

            void ApplyLocalizationSelection(LocalizationComponent requestedValue, bool updateField, bool logErrors)
            {
                var validatedValue = requestedValue;

#if DALOC_EXISTS == false
                if (requestedValue == LocalizationComponent.DALocalizator)
                {
                    if (logErrors)
                    {
                        Debug.LogError(FcuLocKey.log_asset_not_imported.Localize(nameof(LocalizationComponent.DALocalizator)));
                    }

                    validatedValue = LocalizationComponent.None;
                }
#endif

#if I2LOC_EXISTS == false
                if (requestedValue == LocalizationComponent.I2Localization)
                {
                    if (logErrors)
                    {
                        Debug.LogError(FcuLocKey.log_asset_not_imported.Localize(nameof(LocalizationComponent.I2Localization)));
                    }

                    validatedValue = LocalizationComponent.None;
                }
#endif

                if (updateField && validatedValue != requestedValue)
                {
                    locComponentField.SetValueWithoutNotify(validatedValue);
                }

                settings.LocalizationComponent = validatedValue;
                settingsContainer.style.display = validatedValue == LocalizationComponent.None ? DisplayStyle.None : DisplayStyle.Flex;

#if DALOC_EXISTS
                if (daLocContainer != null)
                {
                    daLocContainer.style.display = validatedValue == LocalizationComponent.DALocalizator || monoBeh.IsDebug() ? DisplayStyle.Flex : DisplayStyle.None;
                }
#endif
            }

            locComponentField.RegisterValueChangedCallback(evt =>
            {
                ApplyLocalizationSelection((LocalizationComponent)evt.newValue, updateField: true, logErrors: true);
            });
            formContainer.Add(locComponentField);

            formContainer.Add(settingsContainer);

#if DALOC_EXISTS
            daLocContainer = new VisualElement();
            daLocContainer.style.display = settings.LocalizationComponent == LocalizationComponent.DALocalizator || monoBeh.IsDebug() ? DisplayStyle.Flex : DisplayStyle.None;
            settingsContainer.Add(daLocContainer);

            daLocContainer.Add(uitk.Space10());

            var helpBox = uitk.HelpBox(new HelpBoxData
            {
                Message = FcuLocKey.localization_warning_serialize_first.Localize(nameof(settings.Localizator)),
                MessageType = MessageType.Warning
            });
            helpBox.style.display = settings.Localizator == null ? DisplayStyle.Flex : DisplayStyle.None;

            var localizatorField = new ObjectField(FcuLocKey.label_localizator.Localize());
            localizatorField.tooltip = FcuLocKey.tooltip_localizator.Localize();
            localizatorField.objectType = typeof(LocalizatorBase);
            localizatorField.allowSceneObjects = false;
            localizatorField.value = settings.Localizator;
            localizatorField.RegisterValueChangedCallback(evt =>
            {
                settings.Localizator = (ScriptableObject)evt.newValue;
                helpBox.style.display = settings.Localizator == null ? DisplayStyle.Flex : DisplayStyle.None;
            });

            daLocContainer.Add(localizatorField);
            daLocContainer.Add(uitk.Space10());
            daLocContainer.Add(helpBox);
#endif
            settingsContainer.Add(uitk.Space10());

            var keyMaxLengthField = uitk.IntegerField(FcuLocKey.label_loc_key_max_lenght.Localize());
            keyMaxLengthField.tooltip = FcuLocKey.tooltip_loc_key_max_lenght.Localize();
            keyMaxLengthField.value = settings.LocKeyMaxLenght;
            keyMaxLengthField.RegisterValueChangedCallback(evt => settings.LocKeyMaxLenght = evt.newValue);
            settingsContainer.Add(keyMaxLengthField);
            settingsContainer.Add(uitk.Space10());

            var caseTypeField = uitk.EnumField(FcuLocKey.label_loc_case_type.Localize(), settings.LocKeyCaseType);
            caseTypeField.tooltip = FcuLocKey.tooltip_loc_case_type.Localize();
            caseTypeField.RegisterValueChangedCallback(evt => settings.LocKeyCaseType = (LocalizationKeyCaseType)evt.newValue);
            settingsContainer.Add(caseTypeField);
            settingsContainer.Add(uitk.Space10());

#if DALOC_EXISTS
            IMGUIContainer culturePopupContainer = null;

            GenericMenu selectLangMenu = LanguageCollector.CreateLanguageMenu((cultureCode) =>
            {
                monoBeh.Settings.LocalizationSettings.CurrentFigmaLayoutCulture = cultureCode;
                culturePopupContainer?.MarkDirtyRepaint();
            });

            culturePopupContainer = new IMGUIContainer(() =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(new GUIContent(
                    FcuLocKey.label_figma_layout_culture.Localize(),
                    FcuLocKey.tooltip_figma_layout_culture.Localize()));
                uitk.Colorize(() =>
                {
                    if (EditorGUILayout.DropdownButton(
                   new GUIContent(monoBeh.Settings.LocalizationSettings.CurrentFigmaLayoutCulture),
                   FocusType.Keyboard))
                    {
                        selectLangMenu.ShowAsContext();
                    }
                });
                EditorGUILayout.EndHorizontal();
            });

            settingsContainer.Add(culturePopupContainer);
            settingsContainer.Add(uitk.Space10());
#endif

            var separatorField = uitk.EnumField(FcuLocKey.label_csv_separator.Localize(), settings.CsvSeparator);
            separatorField.tooltip = FcuLocKey.tooltip_csv_separator.Localize();
            separatorField.RegisterValueChangedCallback(evt => settings.CsvSeparator = (CsvSeparator)evt.newValue);
            settingsContainer.Add(separatorField);
            settingsContainer.Add(uitk.Space10());

            var folderPathContainer = uitk.CreateFolderInput(
                label: FcuLocKey.label_loc_folder_path.Localize(),
                tooltip: FcuLocKey.tooltip_loc_folder_path.Localize(),
                initialValue: settings.LocFolderPath,
                onPathChanged: (newValue) => settings.LocFolderPath = newValue,
                onButtonClick: () => EditorUtility.OpenFolderPanel(
                    FcuLocKey.label_select_folder.Localize(), 
                    settings.LocFolderPath, 
                    ""),
                buttonTooltip: FcuLocKey.tooltip_select_folder.Localize()
            );
            settingsContainer.Add(folderPathContainer);
            settingsContainer.Add(uitk.Space10());

            var fileNameField = new TextField(FcuLocKey.label_loc_file_name.Localize());
            fileNameField.tooltip = FcuLocKey.tooltip_loc_file_name.Localize();
            fileNameField.value = settings.LocFileName;
            fileNameField.RegisterValueChangedCallback(evt => settings.LocFileName = evt.newValue);
            settingsContainer.Add(fileNameField);

            ApplyLocalizationSelection(settings.LocalizationComponent, updateField: true, logErrors: false);
        }
    }
}
