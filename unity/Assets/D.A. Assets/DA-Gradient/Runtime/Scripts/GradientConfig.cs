using DA_Assets.Constants;
using DA_Assets.Singleton;
using UnityEngine;

namespace DA_Assets.DAG
{
    [CreateAssetMenu(menuName = DAConstants.Publisher + "/" + GradientConfigName)]
    [ResourcePath("")]
    public class GradientConfig : AssetConfig<GradientConfig>
    {
        public const string GradientConfigName = "GradientConfig";
    }
}
