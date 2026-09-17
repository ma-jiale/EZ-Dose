using DA_Assets.DAI;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using DA_Assets.Logging;

namespace DA_Assets.FCU
{
    internal class SpriteUsagePopup : EditorWindow
    {
        private List<SpriteUsageFinder.UsageRef> _usages;
        private DAInspectorUITK _uitk;

        public static void Show(List<SpriteUsageFinder.UsageRef> usages, DAInspectorUITK uitk)
        {
            var window = CreateInstance<SpriteUsagePopup>();
            window._uitk = uitk;
            window.titleContent = new GUIContent("Sprite Usages");
            window._usages = usages.ToList();

            var size = new Vector2(300, 200);
            window.minSize = size;

            window.ShowUtility();
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            UIHelpers.SetDefaultPadding(root);
            root.style.backgroundColor = new StyleColor(_uitk.ColorScheme.BG);

            if (_usages == null || _usages.Count == 0)
            {
                root.Add(new Label("No usages found."));
                return;
            }

            var listView = new ListView
            {
                itemsSource = _usages,
                selectionType = SelectionType.Single,
                style =
                {
                    flexGrow = 1
                }
            };

            listView.makeItem = () =>
            {
                var itemRoot = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Row,
                        alignItems = Align.Center,
                        paddingTop = 2,
                        paddingBottom = 2
                    }
                };

                var icon = new Image
                {
                    style =
                    {
                        width = 16,
                        height = 16,
                        marginRight = 5
                    }
                };

                var label = new Label
                {
                    style =
                    {
                        flexGrow = 1,
                        whiteSpace = WhiteSpace.Normal
                    }
                };

                itemRoot.Add(icon);
                itemRoot.Add(label);
                itemRoot.userData = new { Icon = icon, Label = label };
                return itemRoot;
            };

            listView.bindItem = (element, i) =>
            {
                var usage = _usages[i];
                var cache = (dynamic)element.userData;
                var icon = (Image)cache.Icon;
                var label = (Label)cache.Label;

                var assetIcon = AssetDatabase.GetCachedIcon(usage.AssetPath);
                icon.image = assetIcon ? assetIcon : EditorGUIUtility.IconContent("ScriptableObject Icon").image;

                string fileName = Path.GetFileName(usage.AssetPath);
                label.text = $"[{usage.Kind}] {fileName}";
                label.tooltip = usage.AssetPath;
            };

#if UNITY_2022_2_OR_NEWER
            listView.selectionChanged += (items) =>
#else
            listView.onSelectionChange += (items) =>
#endif
            {
                if (items.FirstOrDefault() is SpriteUsageFinder.UsageRef selectedUsage)
                {
                    PingAssetByPath(selectedUsage.AssetPath);
                }
            };

            root.Add(listView);
        }

        private static void PingAssetByPath(string assetPath, bool focusProjectWindow = true)
        {
            var obj = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (obj == null)
            {
                Debug.LogError(FcuLocKey.log_sprite_usage_asset_not_found.Localize(assetPath));
                return;
            }

            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);

            if (focusProjectWindow)
                EditorUtility.FocusProjectWindow();
        }
    }
}
