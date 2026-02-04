using DA_Assets.DAI;
using System;
using UnityEngine;

namespace DA_Assets.FCU.Model
{
    [Serializable]
    public class ButtonSettings : FcuBase
    {
        [SerializeField] ButtonComponent buttonComponent = ButtonComponent.UnityButton;
        public ButtonComponent ButtonComponent { get => buttonComponent; set => buttonComponent = value; }

        [SerializeField] public UnityButtonSettings UnityButtonSettings;

        [SerializeField] public ButtonTransitionType TransitionType;

#if DABUTTON_EXISTS
        [SerializeField] public DAB_Settings DAB_Settings = new DAB_Settings();
#endif
    }
}