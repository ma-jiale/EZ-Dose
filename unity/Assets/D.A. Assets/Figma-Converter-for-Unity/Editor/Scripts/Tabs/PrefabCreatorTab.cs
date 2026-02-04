using DA_Assets.DAI;
using UnityEditor;
using UnityEngine.UIElements;

namespace DA_Assets.FCU
{
    internal class PrefabCreatorTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, FigmaConverterUnity>
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
                FcuLocKey.label_prefab_creator.Localize(),
                FcuLocKey.tooltip_prefab_creator.Localize()));
            parent.Add(uitk.Space10());

            var formContainer = uitk.CreateSectionPanel();
            parent.Add(formContainer);

            var settings = monoBeh.Settings.PrefabSettings;

            var folderPathContainer = uitk.CreateFolderInput(
                label: FcuLocKey.label_prefabs_path.Localize(),
                tooltip: FcuLocKey.tooltip_prefabs_path.Localize(),
                initialValue: settings.PrefabsPath,
                onPathChanged: (newValue) => settings.PrefabsPath = newValue,
                onButtonClick: () => EditorUtility.OpenFolderPanel(
                    FcuLocKey.label_select_prefabs_folder.Localize(),
                    settings.PrefabsPath, 
                    ""),
                buttonTooltip: FcuLocKey.tooltip_select_prefabs_folder.Localize()
            );

            formContainer.Add(folderPathContainer);
            formContainer.Add(uitk.ItemSeparator());

            var nameTypeField = uitk.EnumField(FcuLocKey.label_prefab_naming_mode.Localize(), settings.TextPrefabNameType);

            nameTypeField.RegisterValueChangedCallback(evt =>
            {
                var newValue = (TextPrefabNameType)evt.newValue;
                settings.TextPrefabNameType = newValue;
            });

            formContainer.Add(nameTypeField);
            formContainer.Add(uitk.ItemSeparator());
            formContainer.Add(uitk.Space5());

            var createButton = uitk.Button(FcuLocKey.common_button_create.Localize(), () =>
            {
                monoBeh.EditorEventHandlers.CreatePrefabs_OnClick();
            });
            formContainer.Add(createButton);
        }
    }
}
