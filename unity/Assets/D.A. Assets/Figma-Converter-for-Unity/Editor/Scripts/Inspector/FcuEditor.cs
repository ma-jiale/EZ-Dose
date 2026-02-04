using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.FCU.Extensions;
using DA_Assets.Singleton;
using DA_Assets.UpdateChecker;
using DA_Assets.UpdateChecker.Models;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using static DA_Assets.DAI.DAInspectorUITK;

#pragma warning disable IDE0003
#pragma warning disable CS0649

namespace DA_Assets.FCU
{
    [CustomEditor(typeof(FigmaConverterUnity)), CanEditMultipleObjects]
    internal class FcuEditor : DAEditor<FcuEditor, FigmaConverterUnity>
    {
        [SerializeField] Texture2D _logoDarkTheme;
        public Texture2D LogoDarkTheme => _logoDarkTheme;

        [SerializeField] Texture2D _logoLightTheme;
        public Texture2D LogoLightTheme => _logoLightTheme;

        private HeaderSection _headerSection;
        internal HeaderSection Header => monoBeh.Link(ref _headerSection, this);

        private FramesSection _frameListSection;
        internal FramesSection FrameList => monoBeh.Link(ref _frameListSection, this);

        internal LayoutUpdaterWindow LayoutUpdaterWindow => LayoutUpdaterWindow.GetInstance(
            this,
            monoBeh,
            new Vector2(900, 600),
            false,
            title: FcuLocKey.layout_updater_title.Localize());

        internal RateLimitWindow RateLimitWindow => RateLimitWindow.GetInstance(
            this,
            monoBeh,
            new Vector2(700, 370),
            false,
            title: FcuLocKey.rate_limit_window_title.Localize());

        internal SpriteDuplicateFinderWindow SpriteDuplicateFinderWindow => SpriteDuplicateFinderWindow.GetInstance(
            this,
            monoBeh,
            new Vector2(900, 600),
            false,
            title: FcuLocKey.layout_updater_button_sprite_duplicate_finder.Localize());

        internal FcuSettingsWindow SettingsWindow => FcuSettingsWindow.GetInstance(
            this,
            monoBeh,
            new Vector2(800, 600),
            false,
            title: FcuLocKey.common_button_settings.Localize());

        private ScrollView _framesScroll;
        private VisualElement _framesHost;
        private SquareIconButton _btnRecent;
        private VisualElement _root;
        private TextField _projectUrlField;

        protected override void OnDisable()
        {
            base.OnDisable();

            FcuConfig.Instance.Localizator.OnLanguageChanged -= RebuildUI;
            monoBeh.InspectorDrawer.OnFramesChanged -= RefreshFrames;
            monoBeh.InspectorDrawer.OnScrollContentUpdated -= this.FrameList.UpdateScrollContent;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            FcuConfig.Instance.Localizator.OnLanguageChanged += RebuildUI;
            monoBeh.InspectorDrawer.OnFramesChanged += RefreshFrames;
            monoBeh.InspectorDrawer.OnScrollContentUpdated += this.FrameList.UpdateScrollContent;

            monoBeh.EditorDelegateHolder.SetSpriteRects = SpriteEditorUtility.SetSpriteRects;
            monoBeh.EditorDelegateHolder.ShowDifferenceChecker = ShowDifferenceChecker;
            monoBeh.EditorDelegateHolder.ShowRateLimitWindow = ShowRateLimitDialog;
            monoBeh.EditorDelegateHolder.ShowSpriteDuplicateFinder = ShowSpriteDuplicateFinder;
            monoBeh.EditorDelegateHolder.SetGameViewSize = GameViewUtils.SetGameViewSize;
            monoBeh.EditorDelegateHolder.StartProgress = (target, category, totalItems, indeterminate) =>
                EditorProgressBarManager.StartProgress(target, category, totalItems, indeterminate);
            monoBeh.EditorDelegateHolder.UpdateProgress = (target, category, itemsDone) =>
                EditorProgressBarManager.UpdateProgress(target, category, itemsDone);
            monoBeh.EditorDelegateHolder.CompleteProgress = (target, category) =>
                EditorProgressBarManager.CompleteProgress(target, category);
            monoBeh.EditorDelegateHolder.StopAllProgress = target =>
                EditorProgressBarManager.StopAllProgress(target);

            _ = monoBeh.Authorizer.TryRestoreSession();
        }

        public override VisualElement CreateInspectorGUI()
        {
            if (monoBeh.Settings.MainSettings.WindowMode)
            {
                return DrawWindowedGUI();
            }
            else
            {
                return DrawGUI();
            }
        }

        public VisualElement DrawGUI()
        {
            _root = uitk.CreateRoot(uitk.ColorScheme.FCU_BG);
            BuildContent();
            return _root;
        }

        private void RebuildUI()
        {
            _root.Clear();
            BuildContent();
        }
        private void BuildContent()
        {
            var headerCard = Card();
            headerCard.Add(Header.BuildHeaderUI());
            _root.Add(headerCard);
            _root.Add(uitk.Space10());

            var actionsCard = Card();
            actionsCard.Add(BuildUrlAndActionsRow());
            _root.Add(actionsCard);

            bool hasContent = !monoBeh.InspectorDrawer.SelectableDocument.IsProjectEmpty();
            var framesCard = BuildFramesCard(hasContent);
            _root.Add(uitk.Space5());
            _root.Add(framesCard);

            _root.Add(BuildBottomExtraInfo());
            _root.Add(uitk.Space5());

            var footer = uitk.CreateFooterWithVersionInfo(
                FcuConfig.Instance.Localizator.Language,
                new FooterAssetInfo
                {
                    AssetType = DA_Assets.UpdateChecker.Models.AssetType.FCU,
                    ProductVersion = FcuConfig.Instance.ProductVersion
                }
#if FCU_UITK_EXT_EXISTS
                , new FooterAssetInfo
                {
                    AssetType = AssetType.UITK_CONV,
                    ProductVersion = FuitkConfig.Instance.ProductVersion
                }
#endif
                );

            _root.Add(footer);
        }

        private void ShowDifferenceChecker(LayoutUpdaterInput data, Action<LayoutUpdaterOutput> callback)
        {
            void DelayedShow()
            {
                EditorApplication.delayCall -= DelayedShow;
                this.LayoutUpdaterWindow.SetData(data, callback);
                this.LayoutUpdaterWindow.Show();
            }

            EditorApplication.delayCall += DelayedShow;
        }

        private void ShowRateLimitDialog(RateLimitWindowData data, Action<RateLimitWindowResult> callback)
        {
            void DelayedShow()
            {
                EditorApplication.delayCall -= DelayedShow;
                this.RateLimitWindow.SetData(data, callback);
                this.RateLimitWindow.Show();
            }

            EditorApplication.delayCall += DelayedShow;
        }

        private void ShowSpriteDuplicateFinder(List<List<SpriteUsageFinder.UsedSprite>> groups, Action<List<List<SpriteUsageFinder.UsedSprite>>> callback)
        {
            void DelayedShow()
            {
                EditorApplication.delayCall -= DelayedShow;
                this.SpriteDuplicateFinderWindow.SetData(groups, callback);
                this.SpriteDuplicateFinderWindow.Show();
            }

            EditorApplication.delayCall += DelayedShow;
        }

        private ScrollView CreateScroll()
        {
            var sv = new ScrollView
            {
                style =
                {
                    backgroundColor = uitk.ColorScheme.FCU_BG
                }
            };
#if UNITY_2020_1_OR_NEWER
            sv.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
#else
            sv.showHorizontal = false;
#endif
            return sv;
        }

        private VisualElement BuildFramesCard(bool hasContent)
        {
            var framesCard = Card();

            _framesScroll = CreateScroll();
            _framesHost = new VisualElement();

            if (hasContent)
                _framesHost.Add(FrameList.BuildFramesSectionUI());

            _framesScroll.Add(_framesHost);
            framesCard.Add(_framesScroll);

            return framesCard;
        }

        private void ToggleWindowModeAndRebuild()
        {
            monoBeh.Settings.MainSettings.WindowMode = !monoBeh.Settings.MainSettings.WindowMode;

            if (monoBeh.Settings.MainSettings.WindowMode)
                SettingsWindow.Show();
            else
                SettingsWindow.CreateTabs();

            ForceRebuild();
        }

        private void SetDisplay(VisualElement ve, bool visible)
        {
            ve.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private VisualElement Card()
        {
            var ve = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    backgroundColor = uitk.ColorScheme.FCU_BG
                }
            };

            return ve;
        }

        private VisualElement BuildUrlAndActionsRow()
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.FlexStart,
                    backgroundColor = uitk.ColorScheme.FCU_BG,
                    flexWrap = Wrap.Wrap
                }
            };

            string val = monoBeh?.Settings?.MainSettings?.ProjectUrl == null ? "" : monoBeh?.Settings?.MainSettings?.ProjectUrl;

            _projectUrlField = new TextField
            {
                value = val,
                multiline = false,
                style =
                {
                    flexGrow = 1,
                    flexShrink = 1,
                    minWidth = 100,
                    height = 32,
                    backgroundColor = uitk.ColorScheme.FCU_BG,
                    overflow = Overflow.Hidden,
                    whiteSpace = WhiteSpace.NoWrap
                }
            };

            _projectUrlField.ClearClassList();
            var input = _projectUrlField.Q<VisualElement>(null, "unity-text-field__input") ?? _projectUrlField.Q<VisualElement>(null, "unity-text-input");
            input?.ClearClassList();
            if (input != null)
            {
                input.style.backgroundColor = uitk.ColorScheme.FCU_BG;
                input.style.height = Length.Percent(100);
                input.style.unityTextAlign = TextAnchor.MiddleLeft;
                input.style.paddingLeft = 8;
                input.style.paddingRight = 8;
                input.style.marginLeft = 0;
                input.style.marginRight = 0;
                input.style.overflow = Overflow.Hidden;
            }

            UIHelpers.SetRadius(_projectUrlField, 0);
            UIHelpers.SetBorderWidth(_projectUrlField, DAI_UitkConstants.BorderWidth);
            UIHelpers.SetBorderColor(_projectUrlField, uitk.ColorScheme.OUTLINE);
            UIHelpers.SetZeroMarginPadding(_projectUrlField);

            _projectUrlField.RegisterValueChangedCallback(e =>
            {
                monoBeh.Settings.MainSettings.ProjectUrl = e.newValue;
            });

            var btnRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    backgroundColor = uitk.ColorScheme.FCU_BG,
                    flexShrink = 0,
                    flexWrap = Wrap.Wrap
                }
            };

            _btnRecent = uitk.SquareIconButton(
                ShowRecentProjectsPopup_OnClick,
                EditorTextureUtils.RecolorToEditorSkin(gui.Resources.ImgViewRecent),
                FcuLocKey.tooltip_recent_projects.Localize());

            var btnDownload = uitk.SquareIconButton(
                () =>
                {
                    if (!monoBeh.Authorizer.IsAuthed())
                    {
                        Debug.Log(FcuLocKey.log_not_authorized.Localize());
                        return;
                    }

                    if (string.IsNullOrEmpty(monoBeh.Settings.MainSettings.ProjectUrl))
                    {
                        Debug.Log(FcuLocKey.log_incorrent_project_url.Localize());
                        return;
                    }

                    monoBeh.EditorEventHandlers.DownloadProject_OnClick();
                },
                EditorTextureUtils.RecolorToEditorSkin(gui.Resources.IconDownload),
                FcuLocKey.tooltip_download_project.Localize());

            var btnImport = uitk.SquareIconButton(
                monoBeh.EditorEventHandlers.ImportSelectedFrames_OnClick,
                EditorTextureUtils.RecolorToEditorSkin(gui.Resources.IconImport),
                FcuLocKey.tooltip_import_frames.Localize());

            var btnStop = uitk.SquareIconButton(
                monoBeh.EditorEventHandlers.StopImport_OnClick,
                gui.Resources.IconStop,
                FcuLocKey.tooltip_stop_import.Localize());

            var btnSettings = uitk.SquareIconButton(
                () => EditorApplication.delayCall += SettingsWindow.Show,
                EditorTextureUtils.RecolorToEditorSkin(gui.Resources.IconSettings),
                FcuLocKey.tooltip_open_settings_window.Localize());

            void UpdateSettingsVisibility()
            {
                bool show = !monoBeh.Settings.MainSettings.WindowMode;
                SetDisplay(btnSettings, show);
            }

            var btnToggle = uitk.SquareIconButton(
                ToggleWindowModeAndRebuild,
                EditorTextureUtils.RecolorToEditorSkin(gui.Resources.IconExpandWindow),
                FcuLocKey.tooltip_change_window_mode.Localize());

            btnRow.Add(_btnRecent);
            btnRow.Add(uitk.Space5());
            btnRow.Add(btnDownload);
            btnRow.Add(uitk.Space5());
            btnRow.Add(btnImport);
            btnRow.Add(uitk.Space5());
            btnRow.Add(btnStop);
            btnRow.Add(uitk.Space5());
            btnRow.Add(btnSettings);

            UpdateSettingsVisibility();

            btnRow.Add(uitk.Space5());
            btnRow.Add(btnToggle);

            row.Add(_projectUrlField);
            row.Add(uitk.Space5());
            row.Add(btnRow);
            return row;
        }

        private void ShowRecentProjectsPopup_OnClick()
        {
            List<RecentProject> recentProjects = monoBeh.ProjectCacher.GetRecentProjects();

            List<GUIContent> options = new List<GUIContent>();

            if (recentProjects.IsEmpty())
            {
                options.Add(new GUIContent(
                    FcuLocKey.label_no_recent_projects.Localize(),
                    FcuLocKey.tooltip_no_recent_projects.Localize()));
            }
            else
            {
                foreach (RecentProject project in recentProjects)
                {
                    options.Add(new GUIContent(project.Name));
                }
            }

            Rect anchor = _btnRecent.worldBound;
            Rect pos = new Rect(anchor.xMin, anchor.yMax, 0, 0);
            EditorUtility.DisplayCustomMenu(pos, options.ToArray(), -1, (userData, ops, selected) =>
            {
                RecentProject recentProject = recentProjects[selected];

                monoBeh.Settings.MainSettings.ProjectUrl = recentProject.Url;
                _projectUrlField.value = recentProject.Url;

                monoBeh.EditorEventHandlers.DownloadProject_OnClick();
            }, null);
        }

        public UnityEngine.UIElements.VisualElement BuildBottomExtraInfo()
        {
            var root = new UnityEngine.UIElements.VisualElement();

            root.Add(new DeveloperMessagesElement(AssetType.FCU, FcuConfig.Instance.ProductVersion, uitk));

            if (monoBeh.IsJsonNetExists() == false)
            {
                var helpBoxData = new HelpBoxData
                {
                    Message = FcuLocKey.helpbox_install_json_net.Localize(),
                    MessageType = UnityEditor.MessageType.Error,
                    OnClick = () => Application.OpenURL("https://da-assets.gitbook.io/docs/fcu-for-developers/json.net"),
                };

                var customHelpBox = new CustomHelpBox(uitk, helpBoxData);
                root.Add(uitk.Space10());
                root.Add(customHelpBox);
            }

            if (monoBeh.AssetTools.NeedShowRateMe)
                root.Add(BuildRateMe());

            return root;
        }

        private VisualElement BuildRateMe()
        {
            int packageId;
            string packageLink;

            if (FcuConfig.Instance.Localizator.Language == DALanguage.zh)
            {
                if (monoBeh.IsUGUI())
                {
                    packageId = -1;
                }
                else
                {
                    packageId = -1;
                }

                packageLink = "";
            }
            else
            {
                if (monoBeh.IsUGUI())
                {
                    packageId = 198134;
                }
                else
                {
                    packageId = 272042;
                }

                packageLink = "https://assetstore.unity.com/packages/tools/utilities/" + packageId + "#reviews";
            }

            Func<string> descriptionProvider = () =>
            {
                int dc = UpdateService.GetFirstVersionDaysCount(AssetType.FCU);
                var desc = FcuLocKey.label_rateme_desc.Localize(dc);
                return desc;
            };

            return uitk.BuildRateMe(
                packageLink,
                descriptionProvider,
                FcuConfig.RATEME_PREFS_KEY,
                () => FcuLocKey.tooltip_rateme_desc.Localize());
        }


        private void RefreshFrames()
        {
            _framesHost.Clear();
            _framesHost.Add(this.FrameList.BuildFramesSectionUI());
            _framesScroll.ScrollTo(_framesHost);

            Repaint();
        }

        void ForceRebuild() => ActiveEditorTracker.sharedTracker.ForceRebuild();

        public VisualElement DrawWindowedGUI()
        {
            var root = uitk.CreateRoot(uitk.ColorScheme.FCU_BG);

            var progressBarsContainer = new VisualElement { name = "ProgressBarsContainer" };
            progressBarsContainer.style.marginTop = -15;
            progressBarsContainer.style.marginLeft = -15;
            progressBarsContainer.style.marginRight = -15;
            root.Add(progressBarsContainer);

            EditorProgressBarManager.RegisterContainer(monoBeh, progressBarsContainer, uitk);

            var toolbar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    backgroundColor = uitk.ColorScheme.FCU_BG
                }
            };

            var btnOpen = uitk.SquareIconButton(
                SettingsWindow.Show,
                gui.Resources.IconOpen,
                FcuLocKey.tooltip_open_fcu_window.Localize());

            var btnToggle = uitk.SquareIconButton(
                ToggleWindowModeAndRebuild,
                gui.Resources.IconExpandWindow,
                FcuLocKey.tooltip_change_window_mode.Localize());

            toolbar.Add(btnOpen);
            toolbar.Add(uitk.Space5());
            toolbar.Add(btnToggle);
            root.Add(toolbar);

            return root;
        }
    }
}
