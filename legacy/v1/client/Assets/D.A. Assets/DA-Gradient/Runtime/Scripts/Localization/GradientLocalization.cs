namespace DA_Assets.DAG
{
    public enum GradientLocKey
    {
        label_placeholder
    }

    public static class GradientLocExtensions
    {
        public static string Localize(this GradientLocKey key, params object[] args) =>
            GradientConfig.Instance.Localizator.GetLocalizedText(key, null, args);
    }
}
