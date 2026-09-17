using DA_Assets.DAI;
using DA_Assets.FCU.Extensions;
using DA_Assets.FCU.Model;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace DA_Assets.FCU
{
    internal class ButtonsTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, FigmaConverterUnity>
    {
        public VisualElement Draw()
        {
            VisualElement root = new VisualElement();
            UIHelpers.SetDefaultPadding(root);

            root.Add(uitk.CreateTitle(
                 FcuLocKey.label_buttons_tab.Localize(),
                 FcuLocKey.tooltip_buttons_tab.Localize()));
            root.Add(uitk.Space10());

            DrawGeneralSettings(root);
            root.Add(uitk.Space10());

            DrawUnityButtonSettings(root);

#if DABUTTON_EXISTS
            if (monoBeh.UsingDAButton())
            {
                root.Add(uitk.Space10());
                DrawDABSection(root);
            }
#endif

            return root;
        }

        private void DrawGeneralSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var buttonComponentField = uitk.EnumField(FcuLocKey.label_button_type.Localize(), monoBeh.Settings.ButtonSettings.ButtonComponent);
            buttonComponentField.tooltip = FcuLocKey.tooltip_button_type.Localize();
            buttonComponentField.RegisterValueChangedCallback(evt => monoBeh.Settings.ButtonSettings.ButtonComponent = (ButtonComponent)evt.newValue);
            panel.Add(buttonComponentField);
            panel.Add(uitk.ItemSeparator());

            var transitionField = uitk.EnumField(FcuLocKey.label_transition_type.Localize(), monoBeh.Settings.ButtonSettings.TransitionType);
            transitionField.tooltip = FcuLocKey.tooltip_transition_type.Localize();
            transitionField.RegisterValueChangedCallback(evt => monoBeh.Settings.ButtonSettings.TransitionType = (ButtonTransitionType)evt.newValue);
            panel.Add(transitionField);
        }

        private void DrawUnityButtonSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel();
            parent.Add(panel);

            var settings = monoBeh.Settings.ButtonSettings.UnityButtonSettings;

            Label header = new Label(FcuLocKey.label_button_settings.Localize());
            header.tooltip = FcuLocKey.tooltip_button_settings.Localize();
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(header);
            panel.Add(uitk.ItemSeparator());

            var normalColorField = uitk.ColorField(FcuLocKey.label_normal_color.Localize());
            normalColorField.tooltip = FcuLocKey.tooltip_normal_color.Localize();
            normalColorField.value = settings.NormalColor;
            normalColorField.RegisterValueChangedCallback(evt => settings.NormalColor = evt.newValue);
            panel.Add(normalColorField);
            panel.Add(uitk.ItemSeparator());

            var highlightedColorField = uitk.ColorField(FcuLocKey.label_highlighted_color.Localize());
            highlightedColorField.tooltip = FcuLocKey.tooltip_highlighted_color.Localize();
            highlightedColorField.value = settings.HighlightedColor;
            highlightedColorField.RegisterValueChangedCallback(evt => settings.HighlightedColor = evt.newValue);
            panel.Add(highlightedColorField);
            panel.Add(uitk.ItemSeparator());

            var pressedColorField = uitk.ColorField(FcuLocKey.label_pressed_color.Localize());
            pressedColorField.tooltip = FcuLocKey.tooltip_pressed_color.Localize();
            pressedColorField.value = settings.PressedColor;
            pressedColorField.RegisterValueChangedCallback(evt => settings.PressedColor = evt.newValue);
            panel.Add(pressedColorField);
            panel.Add(uitk.ItemSeparator());

            var selectedColorField = uitk.ColorField(FcuLocKey.label_selected_color.Localize());
            selectedColorField.tooltip = FcuLocKey.tooltip_selected_color.Localize();
            selectedColorField.value = settings.SelectedColor;
            selectedColorField.RegisterValueChangedCallback(evt => settings.SelectedColor = evt.newValue);
            panel.Add(selectedColorField);
            panel.Add(uitk.ItemSeparator());

            var disabledColorField = uitk.ColorField(FcuLocKey.label_disabled_color.Localize());
            disabledColorField.tooltip = FcuLocKey.tooltip_disabled_color.Localize();
            disabledColorField.value = settings.DisabledColor;
            disabledColorField.RegisterValueChangedCallback(evt => settings.DisabledColor = evt.newValue);
            panel.Add(disabledColorField);
            panel.Add(uitk.ItemSeparator());

            var colorMultiplierField = uitk.FloatField(FcuLocKey.label_color_multiplier.Localize());
            colorMultiplierField.tooltip = FcuLocKey.tooltip_color_multiplier.Localize();
            colorMultiplierField.value = settings.ColorMultiplier;
            colorMultiplierField.RegisterValueChangedCallback(evt => settings.ColorMultiplier = evt.newValue);
            panel.Add(colorMultiplierField);
            panel.Add(uitk.ItemSeparator());

            var fadeDurationField = uitk.FloatField(FcuLocKey.label_fade_duration.Localize());
            fadeDurationField.tooltip = FcuLocKey.tooltip_fade_duration.Localize();
            fadeDurationField.value = settings.FadeDuration;
            fadeDurationField.RegisterValueChangedCallback(evt => settings.FadeDuration = evt.newValue);
            panel.Add(fadeDurationField);
        }

#if DABUTTON_EXISTS
        private void DrawDABSection(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel();
            parent.Add(panel);

            Label title = new Label(FcuLocKey.label_dabutton_settings.Localize());
            title.tooltip = FcuLocKey.tooltip_dabutton_settings.Localize();
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(title);
            panel.Add(uitk.ItemSeparator());

            string pathToDabSettings = $"{nameof(monoBeh.Settings)}.{nameof(monoBeh.Settings.ButtonSettings)}.{nameof(monoBeh.Settings.ButtonSettings.DAB_Settings)}";
            SerializedProperty dabSettingsProp = scriptableObject.SerializedObject.FindProperty(pathToDabSettings);

            if (dabSettingsProp == null)
            {
                panel.Add(new HelpBox("Could not find DAB_Settings property. Check the path.", HelpBoxMessageType.Error));
                return;
            }

            var scalePropsField = new IMGUIContainer(() =>
            {
                EditorGUILayout.PropertyField(dabSettingsProp.FindPropertyRelative(nameof(DAB_Settings.ScaleProperties)));
            });
            panel.Add(scalePropsField);
            panel.Add(uitk.ItemSeparator());

            var scaleAnimsField = new IMGUIContainer(() =>
            {
                EditorGUILayout.PropertyField(dabSettingsProp.FindPropertyRelative(nameof(DAB_Settings.ScaleAnimations)));
            });
            panel.Add(scaleAnimsField);
            panel.Add(uitk.ItemSeparator());

            var colorAnimsField = new IMGUIContainer(() =>
            {
                EditorGUILayout.PropertyField(dabSettingsProp.FindPropertyRelative(nameof(DAB_Settings.ColorAnimations)));
            });
            panel.Add(colorAnimsField);
            panel.Add(uitk.ItemSeparator());

            var spriteAnimsField = new IMGUIContainer(() =>
            {
                EditorGUILayout.PropertyField(dabSettingsProp.FindPropertyRelative(nameof(DAB_Settings.SpriteAnimations)));
            });
            panel.Add(spriteAnimsField);
        }
#endif
    }
}