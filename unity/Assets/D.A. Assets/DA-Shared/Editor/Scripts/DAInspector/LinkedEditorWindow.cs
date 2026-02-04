using UnityEditor;
using UnityEngine;

namespace DA_Assets.DAI
{
    public class LinkedEditorWindow<T1, T2, T3> : EditorWindow, IDAEditor
        where T1 : LinkedEditorWindow<T1, T2, T3>
        where T2 : Editor
        where T3 : MonoBehaviour
    {
        public static T1 GetInstance(T2 inspector, T3 monoBeh, Vector2 windowSize, bool fixedSize, string title)
        {
            T1[] windows = Resources.FindObjectsOfTypeAll<T1>();
            T1 instance = null;

            foreach (T1 window in windows)
            {
                if (window.monoBeh != null && window.monoBeh.GetInstanceID() == monoBeh.GetInstanceID())
                {
                    instance = window;
                    break;
                }
            }

            if (instance == null)
            {
                instance = CreateInstance<T1>();
                instance.titleContent = new GUIContent(title);
            }

            instance.Inspector = inspector;
            instance.MonoBeh = monoBeh;

            if (instance.SerializedObject == null || instance.SerializedObject.targetObject != monoBeh)
            {
                instance.SerializedObject = new SerializedObject(monoBeh);
            }

            instance.WindowSize = windowSize;
            instance.FixedSize = fixedSize;

            return instance;
        }

        [SerializeField] DAInspector _gui;
        public DAInspector gui { get => _gui; set => _gui = value; }

        [SerializeField] DAInspectorUITK _uitk;
        public DAInspectorUITK uitk { get => _uitk; set => _uitk = value; }

        protected T2 inspector;
        public T2 Inspector { get => inspector; set => inspector = value; }

        [SerializeField] protected T3 monoBeh;
        public T3 MonoBeh { get => monoBeh; set => monoBeh = value; }

        protected SerializedObject serializedObject;
        public SerializedObject SerializedObject { get => serializedObject; set => serializedObject = value; }

        private Vector2 windowSize = new Vector2(800, 600);
        public Vector2 WindowSize { get => windowSize; set => windowSize = value; }

        private bool fixedSize = false;
        public bool FixedSize { get => fixedSize; set => fixedSize = value; }

        protected virtual void OnEnable()
        {
            if (monoBeh != null)
            {
                if (serializedObject == null || serializedObject.targetObject != monoBeh)
                {
                    serializedObject = new SerializedObject(monoBeh);
                }
            }
        }

        protected virtual void OnDisable()
        {

        }

        public new void Show()
        {
            Show(immediateDisplay: false);

            this.position = new Rect(
                (Screen.currentResolution.width - windowSize.x * 2) / 2,
                (Screen.currentResolution.height - windowSize.y * 2) / 2,
                windowSize.x,
                windowSize.y);

            if (fixedSize)
            {
                this.minSize = windowSize;
                this.maxSize = windowSize;
            }

            Focus();
        }
    }
}
