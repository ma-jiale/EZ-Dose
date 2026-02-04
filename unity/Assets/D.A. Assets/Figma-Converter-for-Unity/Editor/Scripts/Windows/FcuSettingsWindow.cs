using DA_Assets.DAI;
using DA_Assets.FCU.Extensions;
using DA_Assets.UpdateChecker;
using DA_Assets.UpdateChecker.Models;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

#pragma warning disable IDE0003
#pragma warning disable CS0649

namespace DA_Assets.FCU
{
    internal class FcuSettingsWindow : LinkedEditorWindow<FcuSettingsWindow, FcuEditor, FigmaConverterUnity>
    {
        internal const string StretchTabClass = "fcu-stretch-tab";

        private List<ITab> _tabs = new List<ITab>();
        private int _selectedTab = 0;

        private AssetVersion? _currentAssetVersion;
        private VisualElement _menuContainer;
        private VisualElement _uitkContent;
        private IMGUIContainer _imguiContent;
        private VisualElement _stretchViewport;
        private VisualElement _stretchContent;
        private EventCallback<GeometryChangedEvent> _stretchViewportCallback;
        private EventCallback<DetachFromPanelEvent> _stretchDetachCallback;
        private bool _changeTrackingRegistered;
        private bool _suppressDirty;

        public interface ITab
        {
            string Title { get; }
            string Tooltip { get; }
            bool Selected { get; set; }
        }

        public sealed class UITKTab : ITab
        {
            public string Title { get; }
            public string Tooltip { get; }
            public bool Selected { get; set; }
            public VisualElement Content { get; }

            public UITKTab(string title, string tooltip, VisualElement content)
            {
                Title = title ?? string.Empty;
                Tooltip = tooltip ?? string.Empty;
                Content = content;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            FcuConfig.Instance.Localizator.OnLanguageChanged += RebuildUI;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            FcuConfig.Instance.Localizator.OnLanguageChanged -= RebuildUI;
        }

        private void RebuildUI()
        {
            rootVisualElement.Clear();
            CreateGUI();
        }

        public void CreateGUI()
        {
            _suppressDirty = true;
            CreateTabs();
            BuildShell();
            BuildMenu();
            MountSelectedTab();
            EnsureChangeTracking();
            _suppressDirty = false;
        }

        private void BuildShell()
        {
            if (_tabs.Count < 1)
            {
                return;
            }

            if (monoBeh.Settings.MainSettings.WindowMode)
            {
                titleContent = new GUIContent(
                    FcuLocKey.label_fcu.Localize(),
                    FcuLocKey.tooltip_fcu.Localize());
            }
            else
            {
                titleContent = new GUIContent(
                    FcuLocKey.label_settings.Localize(),
                    FcuLocKey.tooltip_settings.Localize());
            }

            var windowRoot = rootVisualElement;
            windowRoot.Clear();

            var root = new VisualElement { name = "fcu-root" };
            root.style.flexDirection = FlexDirection.Row;
            root.style.flexGrow = 1f;
            root.style.flexShrink = 1f;
            windowRoot.Add(root);

            _menuContainer = new VisualElement { name = "fcu-menu" };
            _menuContainer.style.width = 200;
            _menuContainer.style.flexShrink = 0;
            _menuContainer.style.flexGrow = 0;
            _menuContainer.style.flexDirection = FlexDirection.Column;
            _menuContainer.style.backgroundColor = uitk.ColorScheme.BG;
            _menuContainer.style.paddingTop = DAI_UitkConstants.MarginPadding / 2;
            root.Add(_menuContainer);

            var separator = new VisualElement { name = "fcu-separator" };
            separator.style.width = 1;
            separator.style.flexShrink = 0;
            separator.style.backgroundColor = uitk.ColorScheme.OUTLINE;
            root.Add(separator);

            var contentHost = new VisualElement { name = "fcu-content" };
            contentHost.style.flexGrow = 1f;
            contentHost.style.flexShrink = 1f;
            contentHost.style.flexDirection = FlexDirection.Column;
            contentHost.style.backgroundColor = uitk.ColorScheme.BG;
            root.Add(contentHost);

            _imguiContent = new IMGUIContainer { name = "fcu-imgui" };
            _imguiContent.style.flexGrow = 1f;
            _imguiContent.style.flexShrink = 1f;
            contentHost.Add(_imguiContent);

            _uitkContent = new VisualElement { name = "fcu-uitk" };
            _uitkContent.style.flexGrow = 1f;
            _uitkContent.style.flexShrink = 1f;
            contentHost.Add(_uitkContent);
        }

        private void BuildMenu()
        {
            _menuContainer.Clear();

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1f;
            _menuContainer.Add(scroll);

            var listContainer = new VisualElement();
            listContainer.style.flexDirection = FlexDirection.Column;
            scroll.Add(listContainer);

            var items = new List<TabItem>(_tabs.Count);

            for (int i = 0; i < _tabs.Count; i++)
            {
                int index = i;
                var item = uitk.TabItem(_tabs[i].Title, () =>
                {
                    _selectedTab = index;
                    _tabs[index].Selected = true;
                    for (int j = 0; j < _tabs.Count; j++)
                    {
                        if (index != j)
                        {
                            _tabs[j].Selected = false;
                        }
                    }
                    for (int k = 0; k < items.Count; k++)
                    {
                        items[k].SetSelected(k == index);
                    }
                    MountSelectedTab();
                });
                item.SetSelected(_tabs[i].Selected || i == _selectedTab);
                item.tooltip = _tabs[i].Tooltip;
                listContainer.Add(item);
                items.Add(item);
            }

            var bottom = new VisualElement();
            bottom.style.flexDirection = FlexDirection.Row;
            bottom.style.marginTop = DAI_UitkConstants.GapYBelow;
            _menuContainer.Add(bottom);

            var leftSpace = new VisualElement();
            leftSpace.style.width = DAI_UitkConstants.MarginPadding;
            bottom.Add(leftSpace);

            if (_currentAssetVersion == null)
            {
                _currentAssetVersion = UpdateService.GetCurrentAssetVersion(AssetType.FCU, FcuConfig.Instance.ProductVersion);
            }

            var versionLabel = new Label
            {
                style =
                {
                    whiteSpace = WhiteSpace.Normal,
                    fontSize = DAI_UitkConstants.FontSizeNormal
                }
            };

            UIHelpers.SetPadding(versionLabel, DAI_UitkConstants.MarginPadding);

            if (_currentAssetVersion != null)
            {
                switch (_currentAssetVersion.Value.VersionType)
                {
                    case VersionType.stable:
                        versionLabel.text = FcuLocKey.label_stable_version.Localize();
                        versionLabel.tooltip = FcuLocKey.tooltip_stable_version.Localize();
                        break;
                    case VersionType.beta:
                        versionLabel.text = FcuLocKey.label_beta_version.Localize();
                        versionLabel.tooltip = FcuLocKey.tooltip_beta_version.Localize();
                        break;
                    case VersionType.buggy:
                        versionLabel.text = FcuLocKey.label_buggy_version.Localize();
                        versionLabel.tooltip = FcuLocKey.tooltip_buggy_version.Localize();
                        versionLabel.style.color = uitk.ColorScheme.RED;
                        break;
                }
            }

            bottom.Add(versionLabel);
        }


        private void MountSelectedTab()
        {
            if (_tabs.Count == 0)
            {
                return;
            }

            ClearStretchTracking();

            var tab = _tabs[_selectedTab] as UITKTab;

            _uitkContent.Clear();

            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.flexGrow = 1f;
            scrollView.contentContainer.style.flexDirection = FlexDirection.Column;

            var tabContent = tab.Content;
            ApplyStretchLayout(scrollView, tabContent);
            scrollView.Add(tabContent);

            _imguiContent.style.display = DisplayStyle.None;
            _uitkContent.style.display = DisplayStyle.Flex;
            _uitkContent.Add(scrollView);
        }

        public void RefreshTabs()
        {
            if (_menuContainer == null || _uitkContent == null)
            {
                CreateGUI();
                return;
            }

            CreateTabs();
            BuildMenu();
            MountSelectedTab();
        }

        public void CreateTabs()
        {
            _tabs.Clear();

            if (monoBeh.Settings.MainSettings.WindowMode)
            {
                _tabs.Add(new UITKTab(FcuLocKey.label_asset.Localize(), FcuLocKey.tooltip_asset.Localize(), this.MainTab.Draw()));
            }

            _tabs.Add(new UITKTab(FcuLocKey.label_main_settings.Localize(), FcuLocKey.tooltip_main_settings.Localize(), this.MainSettingsTab.Draw()));
            _tabs.Add(new UITKTab(FcuLocKey.label_figma_auth.Localize(), FcuLocKey.tooltip_figma_auth.Localize(), this.AuthorizerTab.Draw()));
            _tabs.Add(new UITKTab(FcuLocKey.label_images_and_sprites_tab.Localize(), FcuLocKey.tooltip_images_and_sprites_tab.Localize(), this.ImageSpritesTab.Draw()));
            _tabs.Add(new UITKTab(FcuLocKey.label_text_and_fonts.Localize(), FcuLocKey.tooltip_text_and_fonts.Localize(), this.TextFontsTab.Draw()));

            if (monoBeh.IsUGUI() || monoBeh.IsNova() || monoBeh.IsDebug())
            {
                _tabs.Add(new UITKTab(FcuLocKey.label_buttons_tab.Localize(), FcuLocKey.tooltip_buttons_tab.Localize(), this.ButtonsTab.Draw()));
            }

            _tabs.Add(new UITKTab(FcuLocKey.label_localization_settings.Localize(), FcuLocKey.tooltip_localization_settings.Localize(), this.LocalizationTab.Draw()));

            if (monoBeh.IsUGUI() || monoBeh.IsDebug())
            {
                _tabs.Add(new UITKTab(FcuLocKey.label_shadows_tab.Localize(), FcuLocKey.tooltip_shadows_tab.Localize(), this.ShadowsTab.Draw()));
            }

            if (monoBeh.IsUGUI() || monoBeh.IsNova() || monoBeh.IsDebug())
            {
                _tabs.Add(new UITKTab(FcuLocKey.label_prefab_creator.Localize(), FcuLocKey.tooltip_prefab_creator.Localize(), this.PrefabCreatorTab.Draw()));
            }

            if (monoBeh.Settings.MainSettings.UIFramework == UIFramework.UITK || monoBeh.IsDebug())
            {
                _tabs.Add(new UITKTab(FcuLocKey.label_ui_toolkit_tab.Localize(), FcuLocKey.tooltip_ui_toolkit_tab.Localize(), this.UITK_Tab.Draw()));
            }

            _tabs.Add(new UITKTab(FcuLocKey.label_script_generator.Localize(), FcuLocKey.tooltip_script_generator.Localize(), this.ScriptGeneratorTab.Draw()));
            _tabs.Add(new UITKTab(FcuLocKey.label_import_events.Localize(), FcuLocKey.tooltip_import_events.Localize(), this.ImportEventsTab.Draw()));
            _tabs.Add(new UITKTab(FcuLocKey.label_debug.Localize(), FcuLocKey.tooltip_debug_tools.Localize(), this.DebugTab.Draw()));

            _tabs[_selectedTab].Selected = true;
        }

        private MainTab mainTab;
        internal MainTab MainTab => monoBeh.Link(ref mainTab, this);

        private LocalizationTab localizationTab;
        internal LocalizationTab LocalizationTab => monoBeh.Link(ref localizationTab, this);

        private ButtonsTab buttonsTab;
        internal ButtonsTab ButtonsTab => monoBeh.Link(ref buttonsTab, this);

        private MainSettingsTab mainSettingsTab;
        internal MainSettingsTab MainSettingsTab => monoBeh.Link(ref mainSettingsTab, this);

        private ScriptGeneratorTab scriptGeneratorTab;
        internal ScriptGeneratorTab ScriptGeneratorTab => monoBeh.Link(ref scriptGeneratorTab, this);

        private AuthTab authorizerTab;
        internal AuthTab AuthorizerTab => monoBeh.Link(ref authorizerTab, this);

        private TextFontsTab textFontsTab;
        internal TextFontsTab TextFontsTab => monoBeh.Link(ref textFontsTab, this);

        private UITK_Tab uitkTab;
        internal UITK_Tab UITK_Tab => monoBeh.Link(ref uitkTab, this);

        private ImageSpritesTab imageSpritesTab;
        internal ImageSpritesTab ImageSpritesTab => monoBeh.Link(ref imageSpritesTab, this);

        private ShadowsTab shadowsTab;
        internal ShadowsTab ShadowsTab => monoBeh.Link(ref shadowsTab, this);

        private ImportEventsTab importEventsTab;
        internal ImportEventsTab ImportEventsTab => monoBeh.Link(ref importEventsTab, this);

        private DebugTab debugTab;
        internal DebugTab DebugTab => monoBeh.Link(ref debugTab, this);

        private PrefabCreatorTab prefabCreatorTab;
        internal PrefabCreatorTab PrefabCreatorTab => monoBeh.Link(ref prefabCreatorTab, this);

        private void ApplyStretchLayout(ScrollView scrollView, VisualElement tabContent)
        {
            if (tabContent == null)
            {
                return;
            }

            tabContent.style.flexGrow = 1f;
            tabContent.style.flexShrink = 1f;

            var viewport = scrollView.contentViewport;
            if (viewport == null)
            {
                return;
            }

            _stretchViewport = viewport;
            _stretchContent = tabContent;

            tabContent.style.minHeight = viewport.resolvedStyle.height;

            _stretchViewportCallback = evt =>
            {
                if (_stretchContent != null)
                {
                    _stretchContent.style.minHeight = evt.newRect.height;
                }
            };

            _stretchDetachCallback = evt =>
            {
                ClearStretchTracking();
            };

            _stretchViewport.RegisterCallback(_stretchViewportCallback);
            _stretchContent.RegisterCallback(_stretchDetachCallback);
        }

        private void ClearStretchTracking()
        {
            if (_stretchViewport != null && _stretchViewportCallback != null)
            {
                _stretchViewport.UnregisterCallback(_stretchViewportCallback);
            }

            if (_stretchContent != null)
            {
                if (_stretchDetachCallback != null)
                {
                    _stretchContent.UnregisterCallback(_stretchDetachCallback);
                }

                _stretchContent.style.minHeight = StyleKeyword.Auto;
            }

            _stretchViewport = null;
            _stretchContent = null;
            _stretchViewportCallback = null;
            _stretchDetachCallback = null;
        }

        private void EnsureChangeTracking()
        {
            if (_changeTrackingRegistered)
            {
                return;
            }

            RegisterChangeHandler<bool>();
            RegisterChangeHandler<int>();
            RegisterChangeHandler<long>();
            RegisterChangeHandler<double>();
            RegisterChangeHandler<float>();
            RegisterChangeHandler<string>();
            RegisterChangeHandler<Color>();
            RegisterChangeHandler<Vector2>();
            RegisterChangeHandler<Vector3>();
            RegisterChangeHandler<Vector4>();
            RegisterChangeHandler<Vector2Int>();
            RegisterChangeHandler<Vector3Int>();
            RegisterChangeHandler<Rect>();
            RegisterChangeHandler<RectInt>();
            RegisterChangeHandler<Quaternion>();
            RegisterChangeHandler<Bounds>();
            RegisterChangeHandler<BoundsInt>();
            RegisterChangeHandler<AnimationCurve>();
            RegisterChangeHandler<Gradient>();
            RegisterChangeHandler<Hash128>();
            RegisterChangeHandler<Object>();
            RegisterChangeHandler<System.Enum>();

            _changeTrackingRegistered = true;
        }

        private void RegisterChangeHandler<T>()
        {
            rootVisualElement.RegisterCallback<ChangeEvent<T>>(OnValueChanged, TrickleDown.TrickleDown);
            rootVisualElement.RegisterCallback<ChangeEvent<T>>(OnValueChanged, TrickleDown.NoTrickleDown);
        }

        private void OnValueChanged<T>(ChangeEvent<T> evt)
        {
            if (_suppressDirty)
            {
                return;
            }

            if (EqualityComparer<T>.Default.Equals(evt.previousValue, evt.newValue))
            {
                return;
            }

            MarkDirty();
        }

        private void MarkDirty()
        {
            if (monoBeh == null)
            {
                return;
            }

            Undo.RecordObject(monoBeh, "FCU Settings Change");
            EditorUtility.SetDirty(monoBeh);

            if (monoBeh.gameObject != null && monoBeh.gameObject.scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(monoBeh.gameObject.scene);
            }
        }
    }
}
