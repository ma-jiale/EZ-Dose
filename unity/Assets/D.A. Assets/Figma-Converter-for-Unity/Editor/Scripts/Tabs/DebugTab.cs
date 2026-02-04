using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.FCU.Extensions;
using DA_Assets.Tools;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.FCU
{
    internal class DebugTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, FigmaConverterUnity>
    {
        public VisualElement Draw()
        {
            var root = new VisualElement
            {
                style =
                {
                    paddingTop = 10,
                    paddingBottom = 10,
                    paddingLeft = 10,
                    paddingRight = 10
                }
            };

            DrawElements(root);

            return root;
        }

        private void DrawElements(VisualElement parent)
        {
            Label title = new Label(FcuLocKey.label_debug_tools.Localize())
            {
                style =
                {
                    fontSize = DAI_UitkConstants.FontSizeTitle,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };

            parent.Add(title);
            parent.Add(uitk.Space10());

            var formContainer = uitk.CreateSectionPanel();

            parent.Add(formContainer);

            var debugFlagsField = uitk.EnumFlagsField(FcuLocKey.debug_label_settings.Localize(), FcuDebugSettings.Settings);
            debugFlagsField.RegisterValueChangedCallback(evt =>
            {
                FcuDebugSettings.Settings = (FcuDebugSettingsFlags)evt.newValue;
            });
            formContainer.Add(debugFlagsField);
            formContainer.Add(uitk.ItemSeparator());
            formContainer.Add(uitk.Space5());

            var buttonsContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap = Wrap.Wrap,
                    alignItems = Align.FlexStart
                }
            };

            var openLogsButton = uitk.Button(
                FcuLocKey.debug_button_open_logs.Localize(),
                () => FcuConfig.LogPath.OpenFolderInOS());

            openLogsButton.style.flexShrink = 0;
            buttonsContainer.Add(openLogsButton);

            buttonsContainer.Add(uitk.Space10());

            var openCacheButton = uitk.Button(
                FcuLocKey.debug_button_open_cache.Localize(),
                () => FcuConfig.CachePath.OpenFolderInOS());

            openCacheButton.style.flexShrink = 0;
            buttonsContainer.Add(openCacheButton);

            buttonsContainer.Add(uitk.Space10());

            var openBackupButton = uitk.Button(
                FcuLocKey.debug_button_open_backup.Localize(),
                () => SceneBackuper.GetBackupsPath().OpenFolderInOS());

            openBackupButton.style.flexShrink = 0;
            buttonsContainer.Add(openBackupButton);

            buttonsContainer.Add(uitk.Space10());

            var testButton = uitk.Button(
                FcuLocKey.debug_button_test.Localize(),
                TestButton_OnClick);

            testButton.style.flexShrink = 0;
            buttonsContainer.Add(testButton);

            formContainer.Add(buttonsContainer);

            if (monoBeh.IsDebug())
            {
                formContainer.Add(uitk.ItemSeparator());

                var fcuConfigInspector = new InspectorElement(FcuConfig.Instance);
                formContainer.Add(fcuConfigInspector);

                if (scriptableObject.Inspector != null)
                {
                    formContainer.Add(uitk.ItemSeparator());
                    var inspector = new InspectorElement(scriptableObject.Inspector);
                    formContainer.Add(inspector);
                }
            }
        }

        private void TestButton_OnClick()
        {

        }
    }
}