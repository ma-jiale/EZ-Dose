namespace DA_Assets.FCU
{
    public static class FcuLocExtensions
    {
        public static string Localize(this FcuLocKey key, params object[] args) =>
            FcuConfig.Instance.Localizator.GetLocalizedText(key, null, args);
    }
}