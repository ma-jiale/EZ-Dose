namespace DA_Assets.CR
{
    public enum CornerRounderLocKey
    {
        log_shader_load_failed
    }

    public static class CornerRounderLocExtensions
    {
        public static string Localize(this CornerRounderLocKey key, params object[] args) =>
            CornerRounderConfig.Instance.Localizator.GetLocalizedText(key, null, args);
    }
}
