using DA_Assets.DAI;
using DA_Assets.Extensions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace DA_Assets.FCU
{
    [CustomEditor(typeof(SyncHelper)), CanEditMultipleObjects]
    internal class SyncHelperEditor : Editor
    {
        [SerializeField] DAInspectorUITK _uitk;
        private FigmaConverterUnity fcu;
        private SyncHelper syncHelper;

        private void OnEnable()
        {
            syncHelper = (SyncHelper)target;

            if (syncHelper.Data != null)
            {
                fcu = syncHelper.Data.FigmaConverterUnity as FigmaConverterUnity;
            }
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = _uitk.CreateRoot(default);

            var debugToggle = new Toggle(FcuLocKey.common_label_debug.Localize()) { value = syncHelper.Debug };
            root.Add(debugToggle);

            if (fcu == null)
            {
                var fcuWarningLabel = new Label(FcuLocKey.label_fcu_is_null.Localize(nameof(FigmaConverterUnity), FcuConfig.CreatePrefabs, FcuConfig.SetFcuToSyncHelpers));
                fcuWarningLabel.tooltip = FcuLocKey.tooltip_fcu_is_null.Localize(nameof(FigmaConverterUnity), FcuConfig.CreatePrefabs, FcuConfig.SetFcuToSyncHelpers);
                fcuWarningLabel.style.whiteSpace = WhiteSpace.Normal;
                fcuWarningLabel.style.fontSize = 10;
                fcuWarningLabel.style.marginTop = 10;
                root.Add(fcuWarningLabel);
            }

            if (syncHelper.Data != null)
            {
                if (!syncHelper.Data.NameHierarchy.IsEmpty())
                {
                    var hierarchyField = new TextField { value = syncHelper.Data.NameHierarchy, multiline = true, isReadOnly = true };
                    hierarchyField.style.marginTop = 10;
                    root.Add(hierarchyField);
                }

                if (!syncHelper.Data.Names.FigmaName.IsEmpty())
                {
                    var figmaNameField = new TextField { value = syncHelper.Data.Names.FigmaName, multiline = true, isReadOnly = true };
                    figmaNameField.style.marginTop = 10;
                    root.Add(figmaNameField);
                }

                if (!syncHelper.Data.ProjectId.IsEmpty() && !syncHelper.Data.Id.IsEmpty())
                {
                    var figmaLinkButton = new Button(() =>
                    {
                        string figmaUrl = $"https://www.figma.com/design/{syncHelper.Data.ProjectId}?node-id={syncHelper.Data.Id.Replace(":", "-")}";
                        Application.OpenURL(figmaUrl);
                    })
                    { text = FcuLocKey.sync_helper_link_view_in_figma.Localize() };
                    figmaLinkButton.style.marginTop = 10;
                    root.Add(figmaLinkButton);
                }
            }

            var infoLabel1 = new Label(FcuLocKey.label_dont_remove_fcu_meta.Localize());
            infoLabel1.tooltip = FcuLocKey.tooltip_dont_remove_fcu_meta.Localize();
            infoLabel1.style.whiteSpace = WhiteSpace.Normal;
            infoLabel1.style.fontSize = 12;
            infoLabel1.style.marginTop = 10;
            root.Add(infoLabel1);

            var infoLabel2 = new Label(FcuLocKey.label_more_about_layout_updating.Localize());
            infoLabel2.tooltip = FcuLocKey.tooltip_more_about_layout_updating.Localize();
            infoLabel2.style.whiteSpace = WhiteSpace.Normal;
            infoLabel2.style.fontSize = 10;
            infoLabel2.style.marginTop = 5;
            root.Add(infoLabel2);

            var defaultInspectorContainer = new VisualElement();
            defaultInspectorContainer.style.marginTop = 10;

            SerializedProperty property = serializedObject.GetIterator();
            if (property.NextVisible(true))
            {
                do
                {
                    if (property.name == "m_Script")
                    {
                        continue;
                    }

                    var propertyField = new PropertyField(property.Copy());
                    defaultInspectorContainer.Add(propertyField);
                }
                while (property.NextVisible(false));
            }

            defaultInspectorContainer.style.display = syncHelper.Debug ? DisplayStyle.Flex : DisplayStyle.None;
            root.Add(defaultInspectorContainer);

            debugToggle.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(syncHelper, "Toggle Debug");
                syncHelper.Debug = evt.newValue;
                defaultInspectorContainer.style.display = syncHelper.Debug ? DisplayStyle.Flex : DisplayStyle.None;
                EditorUtility.SetDirty(syncHelper);
            });

            return root;
        }
    }
}
