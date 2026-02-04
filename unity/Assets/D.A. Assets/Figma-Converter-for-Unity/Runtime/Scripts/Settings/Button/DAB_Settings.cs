#if DABUTTON_EXISTS
using DA_Assets.DAB;
using DA_Assets.DAI;
using System;
using UnityEngine;

namespace DA_Assets.FCU.Model
{
    [Serializable]
    public class DAB_Settings : FcuBase
    {
        [SerializeField] public AnimatedProperty<Vector2> ScaleProperties;

        [SerializeField] public EventAnimations ScaleAnimations;

        //////////////////////

        [SerializeField] public EventAnimations ColorAnimations;

        [SerializeField] public EventAnimations SpriteAnimations;

        public void Reset()
        {
            var defaultEventAnims = DabConfig.Instance.DefaultEventAnimations;

            ScaleProperties = DabConfig.Instance.DefaultScaleProps;
            ScaleAnimations = defaultEventAnims;
            ColorAnimations = defaultEventAnims;
            SpriteAnimations = defaultEventAnims;
        }
    }
}
#endif