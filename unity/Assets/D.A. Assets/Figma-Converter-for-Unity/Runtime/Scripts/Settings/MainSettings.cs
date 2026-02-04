using DA_Assets.Extensions;
using DA_Assets.Logging;
using System;
using UnityEngine;

namespace DA_Assets.FCU.Model
{
    [Serializable]
    public class MainSettings : FcuBase
    {
        [SerializeField] UIFramework uiFramework = UIFramework.UGUI;
        public UIFramework UIFramework
        {
            get => uiFramework;
            set => uiFramework = value;
        }

        [SerializeField] PositioningMode positioningMode = PositioningMode.Absolute;
        public PositioningMode PositioningMode
        {
            get => positioningMode;
            set
            {
                if (value == PositioningMode.GameView && uiFramework != UIFramework.UGUI)
                {
                    Debug.LogError(FcuLocKey.log_main_settings_positioning_not_supported.Localize(value, uiFramework));
                    value = PositioningMode.Absolute;
                }

                positioningMode = value;
            }
        }

        [SerializeField] PivotType pivotType = PivotType.MiddleCenter;
        public PivotType PivotType { get => pivotType; set => pivotType = value; }

        [SerializeField] int goLayer = 5;
        public int GameObjectLayer { get => goLayer; set => goLayer = value; }

        [SerializeField] bool sequentialImport = false;
        public bool SequentialImport { get => sequentialImport; set => sequentialImport = value; }

        [SerializeField] bool useDuplicateFinder = true;
        public bool UseDuplicateFinder { get => useDuplicateFinder; set => useDuplicateFinder = value; }

        [SerializeField] bool rawImport = false;
        public bool RawImport
        {
            get => rawImport;
            set
            {
                if (value && value != rawImport)
                {
                    Debug.LogError(FcuLocKey.log_dev_function_enabled.Localize(FcuLocKey.label_raw_import.Localize()));
                }

                rawImport = value;
            }
        }

        [SerializeField] bool https = true;
        public bool Https { get => https; set => https = value; }

        [SerializeField] int gameObjectNameMaxLength = 32;
        public int GameObjectNameMaxLenght { get => gameObjectNameMaxLength; set => gameObjectNameMaxLength = value; }

        [SerializeField] int textObjectNameMaxLength = 16;
        public int TextObjectNameMaxLenght { get => textObjectNameMaxLength; set => textObjectNameMaxLength = value; }

        [Tooltip(@"Characters, aside from Latin letters and numbers, that may appear in GameObject names.

Some characters will be ignored in certain cases, such as when a backslash is used in a sprite name.

If you add new characters to this list, the stable operation of the asset cannot be guaranteed.")]
        [SerializeField] public char[] AllowedNameChars = new char[] { '_', ' ', '(', ')', '=', '.', '-', '[', ']', '+' };

        [SerializeField] bool windowMode = false;
        public bool WindowMode { get => windowMode; set => windowMode = value; }

        [SerializeField] string projectUrl;

        public string ProjectUrl
        {
            get => projectUrl;
            set
            {
                string _value = value;

                try
                {
                    string fileTag = "/file/";
                    char del = '/';

                    if (_value.IsEmpty())
                    {

                    }
                    else if (_value.Contains(fileTag))
                    {
                        _value = _value.GetBetween(fileTag, del.ToString());
                    }
                    else if (_value.Contains(del.ToString()))
                    {
                        string[] splited = value.Split(del);
                        _value = splited[4];
                    }
                }
                catch
                {
                    Debug.LogError(FcuLocKey.log_incorrent_project_url.Localize());
                }

                projectUrl = _value;
            }
        }
    }
}
