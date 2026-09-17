using DA_Assets.DAI;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.CR
{
    [CustomEditor(typeof(CornerRounder)), CanEditMultipleObjects]
    public class CornerRounderEditor : Editor
    {
        [SerializeField] DAInspectorUITK _uitk;
        [Space]
        [SerializeField] Texture2D cornerTopLeftIcon;
        [SerializeField] Texture2D cornerTopRightIcon;
        [SerializeField] Texture2D cornerBottomLeftIcon;
        [SerializeField] Texture2D cornerBottomRightIcon;
        [SerializeField] Texture2D cornerAllIcon;

        public CornerRounder monoBeh;

        private void OnEnable()
        {
            monoBeh = (CornerRounder)target;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = _uitk.CreateRoot(default);    

            var independentContainer = new VisualElement();
            var singleContainer = new VisualElement();

            var independentToggle = new Toggle("Independent") { value = monoBeh.independent };
            independentToggle.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(monoBeh, "Toggle Independent Corners");
                monoBeh.independent = evt.newValue;
                SetContainersVisibility(independentContainer, singleContainer, monoBeh.independent);
                EditorUtility.SetDirty(monoBeh);
            });
            root.Add(independentToggle);
            root.Add(_uitk.Space(15));

            var topRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            var topLeft = CreateCornerField(0, cornerTopLeftIcon, true);
            var topRight = CreateCornerField(1, cornerTopRightIcon, false);
            topRow.Add(topLeft);
            topRow.Add(topRight);
            independentContainer.Add(topRow);
            independentContainer.Add(_uitk.Space(15));

            var bottomRow = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.SpaceBetween } };
            var bottomLeft = CreateCornerField(3, cornerBottomLeftIcon, true);
            var bottomRight = CreateCornerField(2, cornerBottomRightIcon, false);
            bottomRow.Add(bottomLeft);
            bottomRow.Add(bottomRight);
            independentContainer.Add(bottomRow);
            root.Add(independentContainer);

            var allFieldContainer = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            var allField = CreateCornerField(4, cornerAllIcon, true, true);
            allFieldContainer.Add(allField);
            singleContainer.Add(allFieldContainer);
            root.Add(singleContainer);

            SetContainersVisibility(independentContainer, singleContainer, monoBeh.independent);

            return root;
        }

        private void SetContainersVisibility(VisualElement independent, VisualElement single, bool isIndependent)
        {
            independent.style.display = isIndependent ? DisplayStyle.Flex : DisplayStyle.None;
            single.style.display = isIndependent ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private VisualElement CreateCornerField(int index, Texture2D iconTex, bool iconOnLeft, bool isSingle = false)
        {
            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            var intField = new IntegerField
            {
                value = (int)monoBeh.radiiSerialized[isSingle ? 0 : index],
                style = { width = 50 }
            };

            intField.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(monoBeh, "Change Corner Radius");
                int val = evt.newValue < 0 ? 0 : evt.newValue;

                if (isSingle)
                {
                    monoBeh.radiiSerialized[0] = val;
                    monoBeh.radiiSerialized[1] = val;
                    monoBeh.radiiSerialized[2] = val;
                    monoBeh.radiiSerialized[3] = val;
                }
                else
                {
                    monoBeh.radiiSerialized[index] = val;
                }

                monoBeh.Refresh();
                EditorUtility.SetDirty(monoBeh);
            });

            

            if (iconOnLeft)
            {
                container.Add(_uitk.DragZone(intField, iconTex));
                container.Add(intField);
            }
            else
            {
                container.Add(intField);
                container.Add(_uitk.DragZone(intField, iconTex));
            }

            return container;
        }
    }
}