using DA_Assets.DAI;
using UnityEngine.UIElements;

namespace DA_Assets.FCU
{
    internal class MainTab : MonoBehaviourLinkerEditor<FcuSettingsWindow, FigmaConverterUnity>
    {
        public VisualElement Draw()
        {
            if (scriptableObject.Inspector.Header.MonoBeh == null)
            {
                scriptableObject.Close();
                return null;
            }
            else
            {
                var root = scriptableObject.Inspector.DrawGUI();
                root.RemoveFromHierarchy();
                return root;
            }          
        }
    }
}