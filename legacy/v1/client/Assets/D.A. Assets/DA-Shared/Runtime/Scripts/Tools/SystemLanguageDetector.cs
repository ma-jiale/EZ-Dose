using DA_Assets.Singleton;
using UnityEngine;

namespace DA_Assets.Tools
{
    public static class SystemLanguageDetector
    {
        public static DALanguage GetSystemLanguage()
        {
            SystemLanguage systemLang = Application.systemLanguage;

            switch (systemLang)
            {
                case SystemLanguage.Japanese:
                    return DALanguage.ja;
                case SystemLanguage.Korean:
                    return DALanguage.ko;
                case SystemLanguage.Chinese:
                case SystemLanguage.ChineseSimplified:
                case SystemLanguage.ChineseTraditional:
                    return DALanguage.zh;
                case SystemLanguage.Spanish:
                    return DALanguage.es;
                case SystemLanguage.German:
                    return DALanguage.de;
                case SystemLanguage.French:
                    return DALanguage.fr;

                case SystemLanguage.English:
                default:
                    return DALanguage.en;
            }
        }
    }
}