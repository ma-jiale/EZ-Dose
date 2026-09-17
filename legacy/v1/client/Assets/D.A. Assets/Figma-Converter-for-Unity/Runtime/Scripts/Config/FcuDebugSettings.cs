using DA_Assets.Tools;
using System.Linq;

namespace DA_Assets.FCU
{
    public class FcuDebugSettings
    {
        private const string FCU_DEBUG_PREFS_KEY = "FCU_DEBUG_FLAGS";
        private static FcuDebugSettingsFlags flags;

        static FcuDebugSettings()
        {
            FcuDebugSettingsFlags[] debugFlags = new FcuDebugSettingsFlags[]
            {
                FcuDebugSettingsFlags.LogSetTag,
                FcuDebugSettingsFlags.LogIsDownloadable,
                FcuDebugSettingsFlags.LogTransform,
                FcuDebugSettingsFlags.LogGameObjectDrawer,
                FcuDebugSettingsFlags.LogComponentDrawer,
                FcuDebugSettingsFlags.LogHashGenerator,
                FcuDebugSettingsFlags.LogFontLoader,
                FcuDebugSettingsFlags.LogNameSetter,
                FcuDebugSettingsFlags.LogAutoLayoutExtensions,
                FcuDebugSettingsFlags.LogSpriteGenerator
            };

            flags = (FcuDebugSettingsFlags)LocalPrefs.GetInt(FCU_DEBUG_PREFS_KEY, (int)debugFlags.Aggregate((acc, flag) => acc | flag));
        }

        public static FcuDebugSettingsFlags Settings
        {
            get
            {
                return flags;
            }
            set
            {
                if (flags != value)
                {
                    flags = value;
                    LocalPrefs.SetInt(FCU_DEBUG_PREFS_KEY, (int)flags);
                }
            }
        }
    }
}
