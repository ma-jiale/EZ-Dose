using DA_Assets.Constants;
using DA_Assets.Extensions;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DA_Assets.Shared.Extensions;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;
using Image = UnityEngine.UI.Image;

namespace DA_Assets.Tools
{
    internal class SpriteRemoverWindow : EditorWindow
    {
        public const string RemoveUnusedSprites = "Remove unused sprites";

        [SerializeField] private string spritesPath = Path.Combine("Assets", "Sprites");

        [MenuItem("Tools/" + DAConstants.Publisher + "/" + nameof(DA_Assets.Tools) + ": " + RemoveUnusedSprites, false, 90)]
        public static void ShowWindow()
        {
            SpriteRemoverWindow win = GetWindow<SpriteRemoverWindow>(RemoveUnusedSprites);
            win.minSize = new Vector2(500, 130);
            win.maxSize = new Vector2(500, 130);
        }

        public void CreateGUI()
        {
            VisualElement root = rootVisualElement;
            root.style.paddingTop = 10;
            root.style.paddingRight = 10;
            root.style.paddingBottom = 10;
            root.style.paddingLeft = 10;

            Label descriptionLabel = new Label
            {
                text = "Remove sprites from the selected folder that are not used by Image components in the current open scene."
            };
            descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
            descriptionLabel.style.marginBottom = 15;
            root.Add(descriptionLabel);

            VisualElement folderFieldContainer = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };
            root.Add(folderFieldContainer);

            TextField pathField = new TextField("Sprites Path")
            {
                value = spritesPath
            };
            pathField.style.flexGrow = 1;
            pathField.RegisterValueChangedCallback(evt =>
            {
                spritesPath = evt.newValue;
            });
            folderFieldContainer.Add(pathField);

            Button browseButton = new Button(() =>
            {
                string path = EditorUtility.OpenFolderPanel("Select Sprites Folder", spritesPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                    {
                        spritesPath = "Assets" + path.Substring(Application.dataPath.Length);
                        pathField.value = spritesPath;
                    }
                    else
                    {
                        Debug.LogWarning(SharedLocKey.log_select_folder_inside_assets.Localize());
                    }
                }
            })
            {
                text = "…"
            };
            browseButton.style.width = 30;
            browseButton.style.marginLeft = 5;
            folderFieldContainer.Add(browseButton);

            root.Add(new VisualElement { style = { flexGrow = 1 } });

            Button removeButton = new Button(RemoveCurrentSceneUnusedSprites)
            {
                text = "Remove"
            };
            removeButton.style.marginTop = 15;
            root.Add(removeButton);
        }

        public void RemoveCurrentSceneUnusedSprites()
        {
#if UNITY_EDITOR
            Image[] images;

#if UNITY_2023_3_OR_NEWER
            images = MonoBehaviour.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#elif UNITY_2021_3_OR_NEWER
            images = MonoBehaviour.FindObjectsOfType<Image>(true);
#else
            images = MonoBehaviour.FindObjectsOfType<Image>();
#endif

            var sceneSpritePathes = images
                .Where(x => x.sprite != null)
                .Select(x => AssetDatabase.GetAssetPath(x.sprite));

            var assetSpritePathes = AssetDatabase.FindAssets($"t:{typeof(Sprite).Name}", new string[]
            {
                spritesPath
            }).Select(x => AssetDatabase.GUIDToAssetPath(x));

            var result = assetSpritePathes.Where(x1 => sceneSpritePathes.All(x2 => x2 != x1));

            foreach (var filePath in result)
            {
                File.Delete(filePath.GetFullAssetPath());
            }

            Debug.Log(SharedLocKey.log_sprites_removed.Localize(result.Count()));

            AssetDatabase.Refresh();
#endif
        }
    }
}
