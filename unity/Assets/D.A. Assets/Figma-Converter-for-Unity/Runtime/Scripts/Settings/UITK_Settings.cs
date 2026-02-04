using DA_Assets.DAI;
using System;
using UnityEngine;
using System.IO;
using DA_Assets.Logging;

#if ULB_EXISTS
using DA_Assets.ULB;
#endif

namespace DA_Assets.FCU.Model
{
    [Serializable]
    public class UITK_Settings : FcuBase
    {    
#if ULB_EXISTS
        [SerializeField] UitkLinkingMode uitkLinkingMode = UitkLinkingMode.IndexNames;
        public UitkLinkingMode UitkLinkingMode
        {
            get => uitkLinkingMode;
            set
            {
                if (value != uitkLinkingMode)
                {
                    switch (value)
                    {
                        case UitkLinkingMode.Name:
                            Debug.LogError(FcuLocKey.log_name_linking_not_recommended.Localize(FcuLocKey.label_uitk_linking_mode.Localize(), nameof(UitkLinkingMode.Name)));
                            break;
                    }
                }

                uitkLinkingMode = value;
            }
        }
#endif

        [SerializeField] string uitkOutputPath = Path.Combine("Assets", "UITK Output");
        public string UitkOutputPath { get => uitkOutputPath; set => uitkOutputPath = value; }
    }
}