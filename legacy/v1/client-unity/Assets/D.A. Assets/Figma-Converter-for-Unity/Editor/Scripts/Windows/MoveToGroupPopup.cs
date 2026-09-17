using DA_Assets.DAI;
using UnityEditor;
using UnityEngine;

namespace DA_Assets.FCU
{
    internal class MoveToGroupPopup : EditorWindow
    {
        [SerializeField] DAInspector gui;

        private int _sourceIndex;
        private string _path;
        private SpriteDuplicateFinderWindow _owner;
        private int _targetIndex;

        public static void Show(int sourceIndex, string path, SpriteDuplicateFinderWindow owner)
        {
            var wnd = CreateInstance<MoveToGroupPopup>();
            wnd.titleContent = new GUIContent(FcuLocKey.move_to_group_title.Localize());
            wnd._sourceIndex = sourceIndex;
            wnd._path = path;
            wnd._owner = owner;
            wnd.minSize = new Vector2(300, 300);
            wnd.maxSize = new Vector2(300, 300);
            wnd.ShowUtility();
            wnd.Focus();
        }

        private void OnGUI()
        {
            gui.Space5();
            EditorGUILayout.LabelField(FcuLocKey.move_to_group_label_target_index.Localize());
            _targetIndex = EditorGUILayout.IntField(_targetIndex);
            gui.Space10();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(FcuLocKey.common_button_cancel.Localize(), GUILayout.Height(DAI_UitkConstants.ButtonHeight)))
                {
                    Close();
                    GUIUtility.ExitGUI();
                }

                if (GUILayout.Button(FcuLocKey.common_button_ok.Localize(), GUILayout.Height(DAI_UitkConstants.ButtonHeight)))
                {
                    if (_owner == null)
                    {
                        EditorUtility.DisplayDialog(FcuLocKey.move_to_group_title.Localize(), "Owner window is missing.", FcuLocKey.common_button_ok.Localize());
                        Close();
                        GUIUtility.ExitGUI();
                        return;
                    }

                    if (_targetIndex < 0 || _targetIndex >= _owner.GroupCount)
                    {
                        EditorUtility.DisplayDialog(FcuLocKey.move_to_group_title.Localize(), $"Target group {_targetIndex} does not exist.", FcuLocKey.common_button_ok.Localize());
                        return;
                    }

                    if (_targetIndex == _sourceIndex)
                    {
                        EditorUtility.DisplayDialog(FcuLocKey.move_to_group_title.Localize(), "Sprite is already in this group.", FcuLocKey.common_button_ok.Localize());
                        return;
                    }

                    _owner.MoveSpriteByPath(_sourceIndex, _targetIndex, _path);
                    Close();
                    GUIUtility.ExitGUI();
                }
            }

            gui.Space5();
        }
    }
}