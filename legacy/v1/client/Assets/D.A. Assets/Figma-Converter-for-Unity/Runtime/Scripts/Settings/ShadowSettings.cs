using DA_Assets.DAI;
using DA_Assets.Logging;
using System;
using UnityEngine;

#pragma warning disable CS0162

namespace DA_Assets.FCU.Model
{
    [Serializable]
    public class ShadowSettings : FcuBase
    {

        [SerializeField] ShadowComponent shadowComponent = ShadowComponent.Figma;
        public ShadowComponent ShadowComponent
        {
            get => shadowComponent;
            set => shadowComponent = value;
        }

    }
}
