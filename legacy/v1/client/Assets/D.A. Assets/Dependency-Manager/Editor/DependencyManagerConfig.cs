using DA_Assets.Constants;
using DA_Assets.Singleton;
using UnityEngine;

namespace DA_Assets.DM
{
    [CreateAssetMenu(menuName = DAConstants.Publisher + "/" + DependencyManagerConfigName)]
    [ResourcePath("")]
    public class DependencyManagerConfig : AssetConfig<DependencyManagerConfig>
    {
        public const string DependencyManagerConfigName = "DependencyManagerConfig";
    }
}
