using DA_Assets.DAI;
using UnityEditor;
using UnityEngine.UIElements;

#if ULB_EXISTS
using DA_Assets.ULB;
#endif

namespace DA_Assets.FCU
{
    internal class UITK_Tab : MonoBehaviourLinkerEditor<FcuSettingsWindow, FigmaConverterUnity>
    {
        public VisualElement Draw()
        {
            var root = new VisualElement();
            UIHelpers.SetDefaultPadding(root);

            root.Add(uitk.CreateTitle(
                FcuLocKey.label_ui_toolkit_tab.Localize(),
                FcuLocKey.tooltip_ui_toolkit_tab.Localize()
            ));
            root.Add(uitk.Space10());

            DrawUITKSettings(root);

            return root;
        }

        private void DrawUITKSettings(VisualElement parent)
        {
            VisualElement panel = uitk.CreateSectionPanel(withBorder: true);
            parent.Add(panel);

            var settings = monoBeh.Settings.UITK_Settings;

#if ULB_EXISTS
            var linkingModeField = uitk.EnumField(FcuLocKey.label_uitk_linking_mode.Localize(), settings.UitkLinkingMode);
            linkingModeField.tooltip = FcuLocKey.tooltip_uitk_linking_mode.Localize();
            linkingModeField.RegisterValueChangedCallback(evt => settings.UitkLinkingMode = (UitkLinkingMode)evt.newValue);
            panel.Add(linkingModeField);
            panel.Add(uitk.ItemSeparator());
#endif

            var folderPathContainer = uitk.CreateFolderInput(
                label: FcuLocKey.label_uitk_output_path.Localize(),
                tooltip: FcuLocKey.tooltip_uitk_output_path.Localize(),
                initialValue: settings.UitkOutputPath,
                onPathChanged: (newValue) => settings.UitkOutputPath = newValue,
                onButtonClick: () => EditorUtility.OpenFolderPanel(
                    FcuLocKey.label_select_folder.Localize(),
                    settings.UitkOutputPath,
                    ""),
                buttonTooltip: FcuLocKey.tooltip_select_folder.Localize());
            panel.Add(folderPathContainer);
        }
    }
}
