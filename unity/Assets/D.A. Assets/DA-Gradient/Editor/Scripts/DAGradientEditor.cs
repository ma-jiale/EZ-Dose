using DA_Assets.DAI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.DAG
{
    [CustomEditor(typeof(DAGradient)), CanEditMultipleObjects]
    public class DAGradientEditor : Editor
    {
        [SerializeField] DAInspector gui;
        [SerializeField] DAInspectorUITK uitk;

        public DAGradient monoBeh;

        private void OnEnable()
        {
            monoBeh = (DAGradient)target;
        }

        public override VisualElement CreateInspectorGUI()
        {
            var root = uitk.CreateRoot(default);

            var gradientField = new IMGUIContainer(() =>
            {
                gui.SerializedPropertyField<DAGradient>(serializedObject, x => x.Gradient, false);
            });
            root.Add(gradientField);
            root.Add(uitk.Space5());

            var blendModeField = new IMGUIContainer(() =>
            {
                gui.SerializedPropertyField<DAGradient>(serializedObject, x => x.BlendMode);
            });
            root.Add(blendModeField);
            root.Add(uitk.Space5());

            var intensityField = new IMGUIContainer(() =>
            {
                gui.SerializedPropertyField<DAGradient>(serializedObject, x => x.Intensity);
            });
            root.Add(intensityField);
            root.Add(uitk.Space5());

            var angleField = new IMGUIContainer(() =>
            {
                gui.SerializedPropertyField<DAGradient>(serializedObject, x => x.Angle);
            });
            root.Add(angleField);

            root.Add(uitk.Footer()); 

            return root;
        }
    }
}