using UnityEngine;

namespace DA_Assets.DM
{
    [CreateAssetMenu(fileName = "New Dependency", menuName = "D.A. Assets/Dependency Item")]
    public class DependencyItem : ScriptableObject
    {
        [Tooltip("Full type name, comma, assembly name.\nExample: TMPro.TextMeshPro, Unity.TextMeshPro")]
        public string TypeAndAssembly;

        [Tooltip("The symbol that will be added or removed.\nExample: ASSET_TMPRO_PRESENT")]
        public string ScriptingDefineSymbol;

        [SerializeField, HideInInspector]
        private bool isEnabled;

        [SerializeField, HideInInspector]
        private string scriptPath;

        [SerializeField, HideInInspector]
        private bool disabledManually;

        public bool IsEnabled { get => isEnabled; internal set => isEnabled = value; }

        public string ScriptPath { get => scriptPath; internal set => scriptPath = value; }

        public bool DisabledManually { get => disabledManually; set => disabledManually = value; }
    }
}