namespace DA_Assets.Tools
{
    public class EngineDetector
    {
        public static bool IsTuanjie
        {
            get
            {
#if TUANJIE_1_1_OR_NEWER
                return true;
#else
                return false;
#endif
            }
        }
    }
}