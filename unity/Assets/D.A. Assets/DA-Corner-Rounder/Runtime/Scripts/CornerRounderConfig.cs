using DA_Assets.Constants;
using DA_Assets.Singleton;
using UnityEngine;

namespace DA_Assets.CR
{
    [CreateAssetMenu(menuName = DAConstants.Publisher + "/" + CornerRounderConfigName)]
    [ResourcePath("")]
    public class CornerRounderConfig : AssetConfig<CornerRounderConfig>
    {
        public const string CornerRounderConfigName = "CornerRounderConfig";
    }
}
