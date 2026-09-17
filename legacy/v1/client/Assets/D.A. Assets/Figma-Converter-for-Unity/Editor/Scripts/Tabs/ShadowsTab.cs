using DA_Assets.DAI;
using UnityEngine;
using UnityEngine.UIElements;
using DA_Assets.Logging;

#pragma warning disable CS0649

namespace DA_Assets.FCU
{
    internal class ShadowsTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, FigmaConverterUnity>
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
            Label title = new Label(FcuLocKey.label_shadows_tab.Localize());
            title.tooltip = FcuLocKey.tooltip_shadows_tab.Localize();
            title.style.fontSize = DAI_UitkConstants.FontSizeTitle;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            parent.Add(title);
            parent.Add(uitk.Space10());

            var formContainer = new VisualElement();
            formContainer.style.backgroundColor = uitk.ColorScheme.GROUP;
            UIHelpers.SetDefaultRadius(formContainer);
            UIHelpers.SetDefaultPadding(formContainer);
            parent.Add(formContainer);

            var shadowComponentField = uitk.EnumField(FcuLocKey.label_shadow_type.Localize(), monoBeh.Settings.ShadowSettings.ShadowComponent);
            shadowComponentField.tooltip = FcuLocKey.tooltip_shadow_type.Localize();

            void ApplyShadowComponentSelection(ShadowComponent requestedValue, bool updateField, bool logErrors)
            {
                var validatedValue = requestedValue;

#if TRUESHADOW_EXISTS == false
                if (requestedValue == ShadowComponent.TrueShadow)
                {
                    if (logErrors)
                    {
                        Debug.LogError(FcuLocKey.log_asset_not_imported.Localize(nameof(ShadowComponent.TrueShadow)));
                    }

                    validatedValue = ShadowComponent.Figma;
                }
#endif

                if (updateField)
                {
                    if ((ShadowComponent)shadowComponentField.value != validatedValue)
                    {
                        shadowComponentField.SetValueWithoutNotify(validatedValue);
                    }
                }

                monoBeh.Settings.ShadowSettings.ShadowComponent = validatedValue;
            }

            shadowComponentField.RegisterValueChangedCallback(evt =>
            {
                ApplyShadowComponentSelection((ShadowComponent)evt.newValue, updateField: true, logErrors: true);
            });
            formContainer.Add(shadowComponentField);

            ApplyShadowComponentSelection(monoBeh.Settings.ShadowSettings.ShadowComponent, updateField: true, logErrors: false);
        }
    }
}
