using DA_Assets.FCU.Model;
using System;
using UnityEngine;

#pragma warning disable CS0649

namespace DA_Assets.FCU
{
    [Serializable]
    public class SettingsLinker : FcuBase
    {
        public override void Init(FigmaConverterUnity monoBeh)
        {
            base.Init(monoBeh);

            this.MainSettings.Init(monoBeh);
            this.UITK_Settings.Init(monoBeh);
            this.ImageSpritesSettings.Init(monoBeh);
            this.TextureImporterSettings.Init(monoBeh);
            this.ShadowSettings.Init(monoBeh);
            this.ButtonSettings.Init(monoBeh);
            this.LocalizationSettings.Init(monoBeh);
            this.ScriptGeneratorSettings.Init(monoBeh);
            this.NovaSettings.Init(monoBeh);
            this.JoshPuiSettings.Init(monoBeh);
            this.MPUIKitSettings.Init(monoBeh);
            this.DttPuiSettings.Init(monoBeh);
            this.SvgImageSettings.Init(monoBeh);
            this.SVGImporterSettings.Init(monoBeh);
            this.UnityImageSettings.Init(monoBeh);
            this.RawImageSettings.Init(monoBeh);
            this.Shapes2DSettings.Init(monoBeh);
            this.SpriteRendererSettings.Init(monoBeh);
            this.TextMeshSettings.Init(monoBeh);
            this.TextFontsSettings.Init(monoBeh);
            this.UnityTextSettings.Init(monoBeh);
            this.UitkTextSettings.Init(monoBeh);
            this.PrefabSettings.Init(monoBeh);
        }

        [SerializeField] public MainSettings MainSettings = new MainSettings();
        [SerializeField] public UITK_Settings UITK_Settings = new UITK_Settings();
        [SerializeField] public ImageSpritesSettings ImageSpritesSettings = new ImageSpritesSettings();
        [SerializeField] public TextureImporterSettings TextureImporterSettings = new TextureImporterSettings();
        [SerializeField] public ShadowSettings ShadowSettings = new ShadowSettings();
        [SerializeField] public ButtonSettings ButtonSettings = new ButtonSettings();
        [SerializeField] public LocalizationSettings LocalizationSettings = new LocalizationSettings();
        [SerializeField] public ScriptGeneratorSettings ScriptGeneratorSettings = new ScriptGeneratorSettings();
        [SerializeField] public NovaSettings NovaSettings = new NovaSettings();
        [SerializeField] public JoshPuiSettings JoshPuiSettings = new JoshPuiSettings();
        [SerializeField] public MPUIKitSettings MPUIKitSettings = new MPUIKitSettings();
        [SerializeField] public DttPuiSettings DttPuiSettings = new DttPuiSettings();
        [SerializeField] public SvgImageSettings SvgImageSettings = new SvgImageSettings();
        [SerializeField] public SVGImporterSettings SVGImporterSettings = new SVGImporterSettings();
        [SerializeField] public UnityImageSettings UnityImageSettings = new UnityImageSettings();
        [SerializeField] public RawImageSettings RawImageSettings = new RawImageSettings();
        [SerializeField] public Shapes2DSettings Shapes2DSettings = new Shapes2DSettings();
        [SerializeField] public SpriteRendererSettings SpriteRendererSettings = new SpriteRendererSettings();
        [SerializeField] public TextMeshSettings TextMeshSettings = new TextMeshSettings();
        [SerializeField] public TextFontsSettings TextFontsSettings = new TextFontsSettings();
        [SerializeField] public UnityTextSettings UnityTextSettings = new UnityTextSettings();
        [SerializeField] public UitkTextSettings UitkTextSettings = new UitkTextSettings();
        [SerializeField] public PrefabSettings PrefabSettings = new PrefabSettings();
    }
}