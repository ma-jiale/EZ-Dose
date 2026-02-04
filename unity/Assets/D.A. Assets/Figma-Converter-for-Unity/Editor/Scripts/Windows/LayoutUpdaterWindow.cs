using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.FCU.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.FCU
{
    internal class LayoutUpdaterWindow : LinkedEditorWindow<LayoutUpdaterWindow, FcuEditor, FigmaConverterUnity>
    {
        private LayoutUpdaterInput _diffStruct;
        private Action<LayoutUpdaterOutput> _callback;
        private bool _inited = false;

        // UI Elements
        private Toggle _importNewToggle;
        private Toggle _importUnityChangedToggle;
        private Toggle _importOtherToggle;

        private Label _importNewCountLabel;
        private Label _unitySideChangedCountLabel;
        private Label _importOtherCountLabel;

        private readonly Dictionary<string, Label> _frameHeaderLabels = new Dictionary<string, Label>();
        private readonly Dictionary<string, Toggle> _frameToggles = new Dictionary<string, Toggle>();
        private Label _mainHeaderLabel;
        private Toggle _mainToggle;

        private readonly Dictionary<string, ListView> _removeListViews = new Dictionary<string, ListView>();
        private readonly Dictionary<string, Label> _removeHeaderLabels = new Dictionary<string, Label>();
        private readonly Dictionary<string, Toggle> _removeHeaderToggles = new Dictionary<string, Toggle>();

        private CustomButton _applyBtn;
        private VisualElement _helpBtn;
        private CustomButton _cancelBtn;

        private VisualElement _helpPanel;

        // Search UI & Logic - Import Panel
        private TextField _searchField;
        private string _searchText = "";
        private Func<SelectableObject<DiffInfo>, object> _valueGetter;
        private VisualElement _componentsScrollViewContent;

        // Search UI & Logic - Remove Panel
        private TextField _removeSearchField;
        private string _removeSearchText = "";
        private Func<SelectableObject<SyncData>, object> _removeValueGetter;
        private VisualElement _removeScrollViewContent;

        private readonly FcuLocKey[] _helpSteps =
        {
            FcuLocKey.layout_updater_help_step_1,
            FcuLocKey.layout_updater_help_step_2,
            FcuLocKey.layout_updater_help_step_3,
            FcuLocKey.layout_updater_help_step_4,
            FcuLocKey.layout_updater_help_step_5,
            FcuLocKey.layout_updater_help_step_6,
            FcuLocKey.layout_updater_help_step_7,
            FcuLocKey.layout_updater_help_step_8
        };

        private string LocalizeHelpStep(FcuLocKey step)
        {
            return step switch
            {
                FcuLocKey.layout_updater_help_step_2 => step.Localize(FcuLocKey.layout_updater_title_components_to_import.Localize()),
                FcuLocKey.layout_updater_help_step_3 => step.Localize(FcuLocKey.common_placeholder_search.Localize()),
                FcuLocKey.layout_updater_help_step_4 => 
                    step.Localize(
                    FcuLocKey.common_filter_new.Localize(),
                    FcuLocKey.layout_updater_filter_changed_in_unity.Localize(),
                    FcuLocKey.layout_updater_filter_without_changes.Localize()),
                FcuLocKey.layout_updater_help_step_6 => step.Localize( FcuLocKey.layout_updater_title_remove_from_scene.Localize()),
                FcuLocKey.layout_updater_help_step_8 => step.Localize(FcuLocKey.layout_updater_button_apply_and_continue.Localize()),
                _ => step.Localize()
            };
        }

        private const int ItemBaseHeight = 100;

        private float _scrollViewHeight => ((ItemBaseHeight + DAI_UitkConstants.MarginPadding) * 3f) - DAI_UitkConstants.MarginPadding;

        public void SetData(LayoutUpdaterInput diffStruct, Action<LayoutUpdaterOutput> callback)
        {
            _diffStruct = diffStruct;
            _callback = callback;
            _inited = true;

            // Prepare reflection-based value getter for import search
            if (MemberPathCache.TryGetOrCreate(typeof(SelectableObject<DiffInfo>), "Object.NewData.Data.NameHierarchy", out var importAccessor))
            {
                _valueGetter = (item) => importAccessor.GetValue(item);
            }
            else
            {
                Debug.LogError(FcuLocKey.log_layout_value_getter_failed.Localize(FcuLocKey.layout_updater_title_components_to_import.Localize()));
                _valueGetter = null;
            }

            // Prepare reflection-based value getter for remove search
            if (MemberPathCache.TryGetOrCreate(typeof(SelectableObject<SyncData>), "Object.NameHierarchy", out var removeAccessor))
            {
                _removeValueGetter = (item) => removeAccessor.GetValue(item);
            }
            else
            {
                Debug.LogError(FcuLocKey.log_layout_value_getter_failed.Localize(FcuLocKey.layout_updater_title_remove_from_scene.Localize()));
                _removeValueGetter = null;
            }

            if (rootVisualElement.childCount > 0)
            {
                CreateGUI();
            }
        }

        private void CheckForClose()
        {
            if (_inited && _diffStruct.IsDefault())
            {
                Debug.Log(FcuLocKey.log_layout_window_closed.Localize(this.name));
                this.Close();
            }
        }

        private void Update()
        {
            CheckForClose();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();

            if (!_inited)
            {
                return;
            }

            var root = rootVisualElement;

            UIHelpers.SetDefaultPadding(root);

            root.style.backgroundColor = new StyleColor(uitk.ColorScheme.BG);
            root.style.flexDirection = FlexDirection.Column;

            var topArea = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexGrow = 1,
                    minHeight = 0
                }
            };
            root.Add(topArea);

            _helpPanel = uitk.BuildHelpPanel(FcuConfig.Instance.Localizator.Language, steps: _helpSteps.Select(LocalizeHelpStep).ToArray());
            topArea.Add(_helpPanel);

            var mainContent = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    flexGrow = 1,
                    minHeight = 0
                }
            };
            topArea.Add(mainContent);

            var leftPanel = BuildImportPanel();
            bool hasItemsToRemove = _diffStruct.ToRemove.Childs != null && _diffStruct.ToRemove.Childs.Any();

            if (hasItemsToRemove)
            {
                var splitView = new TwoPaneSplitView(0, 500, TwoPaneSplitViewOrientation.Horizontal)
                {
                    style = { flexGrow = 1 }
                };

                var rightPanel = BuildRemovePanel();

                splitView.Add(leftPanel);
                splitView.Add(rightPanel);
                mainContent.Add(splitView);
            }
            else
            {
                mainContent.Add(leftPanel);
            }

            var footer = BuildFooter();
            root.Add(footer);

            RebuildImportList();

            if (hasItemsToRemove)
            {
                RebuildRemoveList();
            }
        }

        private VisualElement BuildImportPanel()
        {
            var panel = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    paddingRight = DAI_UitkConstants.MarginPadding / 2,
                    flexDirection = FlexDirection.Column
                }
            };
            panel.Add(new Label(FcuLocKey.layout_updater_title_components_to_import.Localize())
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = DAI_UitkConstants.MarginPadding,
                    flexShrink = 0
                }
            });

            var filters = BuildFilters();
            filters.style.flexShrink = 0;
            panel.Add(filters);

            panel.Add(BuildSearchField());

            var scrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                style =
                {
                    flexGrow = 1
                }
            };

            _componentsScrollViewContent = new VisualElement();
            scrollView.Add(_componentsScrollViewContent);

            panel.Add(scrollView);

            return panel;
        }

        private VisualElement BuildSearchField()
        {
            var searchContainer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = DAI_UitkConstants.MarginPadding }
            };

            _searchField = new TextField
            {
                name = "search-field",
                style = { flexGrow = 1 }
            };

            _searchField.RegisterValueChangedCallback(evt =>
            {
                _searchText = evt.newValue;
                RebuildImportList();
            });

            var clearBtn = new Button(() =>
            {
                _searchField.value = string.Empty;
                GUI.FocusControl(null);
            })
            {
                text = "✕",
                style = { width = 22, height = 20, marginLeft = 2 }
            };

            searchContainer.Add(new Label(FcuLocKey.common_placeholder_search.Localize()) { style = { marginRight = 4 } });
            searchContainer.Add(_searchField);
            searchContainer.Add(clearBtn);

            return searchContainer;
        }

        private void RebuildImportList()
        {
            _componentsScrollViewContent.Clear();
            _frameHeaderLabels.Clear();
            _frameToggles.Clear();

            var dataToDisplay = ApplySearchFilter();

            var mainBody = new VisualElement
            {
                style =
                {
                    paddingTop = DAI_UitkConstants.RowMarginBottom
                }
            };

            var mainHeader = MakeHeader(out _mainHeaderLabel, out _mainToggle, out var mainArrow);

            _mainToggle.RegisterValueChangedCallback(evt =>
            {
                _diffStruct.ToImport.SetAllSelected(evt.newValue);
                RefreshState();
            });

            if (dataToDisplay.Childs.Any())
            {
                foreach (var frame in dataToDisplay.Childs)
                {
                    mainBody.Add(BuildFrameFoldout(frame));
                }
            }
            else
            {
                mainBody.Add(new Label(FcuLocKey.common_message_nothing_matches.Localize())
                {
                    style = { unityTextAlign = TextAnchor.MiddleCenter, marginTop = 20, marginBottom = 20 }
                });
            }

            var mainFoldout = new AnimatedFoldout("main_components", mainHeader, mainBody, true, uitk.FoldoutCurve, uitk.FoldoutDuration, uitk.ColorScheme.BG);
            mainFoldout.Toggled += exp => mainArrow.text = exp ? "▾" : "▸";
            mainArrow.text = mainFoldout.Expanded ? "▾" : "▸";

            var mainContainer = NarrowContainer(0, uitk.ColorScheme.BG);
            mainContainer.Add(mainFoldout);
            _componentsScrollViewContent.Add(mainContainer);

            RefreshState();
        }

        private SelectableObject<DiffInfo> ApplySearchFilter()
        {
            if (string.IsNullOrWhiteSpace(_searchText) || _valueGetter == null)
            {
                return _diffStruct.ToImport;
            }

            string query = _searchText.Trim();

            var filteredRoot = new SelectableObject<DiffInfo>
            {
                Object = _diffStruct.ToImport.Object,
                Selected = _diffStruct.ToImport.Selected,
                Childs = new List<SelectableObject<DiffInfo>>()
            };

            foreach (var frame in _diffStruct.ToImport.Childs)
            {
                var matchedComponents = new List<SelectableObject<DiffInfo>>();
                foreach (var component in frame.Childs)
                {
                    var value = _valueGetter(component);
                    if (value != null)
                    {
                        string componentName = value.ToString();
                        if (componentName.IndexOf(query, StringComparison.InvariantCultureIgnoreCase) >= 0)
                        {
                            matchedComponents.Add(component);
                        }
                    }
                }

                if (matchedComponents.Count > 0)
                {
                    var filteredFrame = new SelectableObject<DiffInfo>
                    {
                        Object = frame.Object,
                        Selected = frame.Selected,
                        Childs = matchedComponents
                    };
                    filteredRoot.Childs.Add(filteredFrame);
                }
            }
            return filteredRoot;
        }

        private VisualElement BuildFilters()
        {
            var container = new VisualElement
            {
                style =
                {
                    marginBottom = DAI_UitkConstants.MarginPadding
                }
            };

            container.Add(CreateFilterRow(FcuLocKey.common_filter_new.Localize(), out _importNewCountLabel, out _importNewToggle, FcuLocKey.tooltip_new_components.Localize()));
            container.Add(CreateFilterRow(FcuLocKey.layout_updater_filter_changed_in_unity.Localize(), out _unitySideChangedCountLabel, out _importUnityChangedToggle, FcuLocKey.tooltip_changed_in_unity.Localize()));
            container.Add(CreateFilterRow(FcuLocKey.layout_updater_filter_without_changes.Localize(), out _importOtherCountLabel, out _importOtherToggle, FcuLocKey.tooltip_label_without_changes.Localize()));

            _importNewToggle.RegisterValueChangedCallback(evt =>
            {
                UpdateData(x => x.Object.IsNew, evt.newValue);
                RefreshState();
            });
            _importUnityChangedToggle.RegisterValueChangedCallback(evt =>
            {
                UpdateData(x => x.Object.IsUnitySideChanged(), evt.newValue);
                RefreshState();
            });
            _importOtherToggle.RegisterValueChangedCallback(evt =>
            {
                UpdateData(x => x.Object.IsNotChanged(), evt.newValue);
                RefreshState();
            });

            return container;
        }

        private VisualElement CreateFilterRow(string label, out Label countLabel, out Toggle toggle, string tooltip)
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };
            row.Add(new Label(label)
            {
                style =
                {
                    width = DAI_UitkConstants.ApplyButtonMinWidth
                },
                tooltip = tooltip
            });

            countLabel = new Label
            {
                style =
                {
                    width = 50,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };
            toggle = new Toggle();

            row.Add(countLabel);
            row.Add(toggle);

            return row;
        }

        private VisualElement BuildFrameFoldout(SelectableObject<DiffInfo> frame)
        {
            var frameBody = new VisualElement
            {
                style =
                {
                    paddingTop = DAI_UitkConstants.MarginPadding,
                    paddingBottom = DAI_UitkConstants.MarginPadding
                }
            };

            var frameHeader = MakeHeader(out var frameLabel, out var frameToggle, out var frameArrow);

            _frameHeaderLabels[frame.Object.Id] = frameLabel;
            _frameToggles[frame.Object.Id] = frameToggle;

            frameToggle.RegisterValueChangedCallback(evt =>
            {
                frame.SetAllSelected(evt.newValue);
                RefreshState();
            });

            var listView = new ListView(
                frame.Childs,
                (int)(ItemBaseHeight + DAI_UitkConstants.MarginPadding),
                MakeItem,
                (e, i) => BindItem(e, i, frame))
            {
                selectionType = SelectionType.None,
                style =
                {
                    flexGrow = 1,
                    height = _scrollViewHeight
                }
            };

            frameBody.Add(listView);

            var frameFoldout = new AnimatedFoldout(
                frame.Object.Id, frameHeader, frameBody, true, uitk.FoldoutCurve, uitk.FoldoutDuration,
                uitk.ColorScheme.CLEAR);
            frameFoldout.Toggled += exp => frameArrow.text = exp ? "▾" : "▸";
            frameArrow.text = frameFoldout.Expanded ? "▾" : "▸";

            var container = NarrowContainer(1, uitk.ColorScheme.CLEAR);
            container.Add(frameFoldout);
            container.style.marginBottom = DAI_UitkConstants.RowMarginBottom;
            return container;
        }

        private VisualElement MakeItem()
        {
            var slot = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    height = ItemBaseHeight + DAI_UitkConstants.MarginPadding,
                    backgroundColor = new StyleColor(StyleKeyword.None)
                }
            };

            var card = new VisualElement
            {
                style =
                {
                    height = ItemBaseHeight,
                    flexDirection = FlexDirection.Column,
                    backgroundColor = new StyleColor(uitk.ColorScheme.GROUP),

                    paddingTop = DAI_UitkConstants.MarginPadding,
                    paddingBottom = DAI_UitkConstants.MarginPadding,
                    paddingLeft = 12,
                    paddingRight = 12,

                    marginBottom = DAI_UitkConstants.MarginPadding
                }
            };

            UIHelpers.SetDefaultRadius(card);

            card.RegisterCallback<MouseEnterEvent>(_ =>
                card.style.backgroundColor = new StyleColor(uitk.ColorScheme.BUTTON));
            card.RegisterCallback<MouseLeaveEvent>(_ =>
                card.style.backgroundColor = new StyleColor(uitk.ColorScheme.GROUP));

            var mainRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center
                }
            };

            var toggle = new Toggle
            {
                name = "itemToggle"
            };
            toggle.style.marginRight = DAI_UitkConstants.CornerRadius;
            toggle.style.marginLeft = -4;

            var contentColumn = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    flexDirection = FlexDirection.Column
                }
            };

            var figmaLink = new Button
            {
                name = "figmaLink"
            };
            var unityLink = new Button
            {
                name = "unityLink"
            };

            var figmaRow = CreateLinkRow("F", uitk.ColorScheme.ACCENT_SECOND, figmaLink);
            var unityRow = CreateLinkRow("U", uitk.ColorScheme.BUTTON, unityLink);

            contentColumn.Add(figmaRow);
            contentColumn.Add(unityRow);

            mainRow.Add(toggle);
            mainRow.Add(contentColumn);

            var detailsContainer = new VisualElement
            {
                name = "detailsContainer",
                style =
                {
                    marginLeft = DAI_UitkConstants.ButtonHeight,
                    marginTop = DAI_UitkConstants.CornerRadius,
                    display = DisplayStyle.None
                }
            };

            var colorLabel = new Label
            {
                name = "colorLabel"
            };
            var sizeLabel = new Label
            {
                name = "sizeLabel"
            };

            colorLabel.style.fontSize = 11;
            colorLabel.style.color = new StyleColor(uitk.ColorScheme.ARROW_GRAY);
            sizeLabel.style.fontSize = 11;
            sizeLabel.style.color = new StyleColor(uitk.ColorScheme.ARROW_GRAY);

            detailsContainer.Add(colorLabel);
            detailsContainer.Add(sizeLabel);

            card.Add(mainRow);
            card.Add(detailsContainer);

            slot.Add(card);

            slot.userData = new ItemUI
            {
                FigmaLink = figmaLink,
                UnityLink = unityLink,
                Toggle = toggle,
                ColorLabel = colorLabel,
                SizeLabel = sizeLabel
            };

            figmaLink.RegisterCallback<ClickEvent>(OnFigmaLinkClicked);
            unityLink.RegisterCallback<ClickEvent>(OnUnityLinkClicked);

            return slot;
        }

        private void BindItem(VisualElement element, int index, SelectableObject<DiffInfo> frame)
        {
            var item = frame.Childs[index];
            var ui = element.userData as ItemUI;

            ui.Item = item;
            ui.FigmaLink.userData = item;
            ui.UnityLink.userData = item;
            ui.Toggle.userData = item;

            string figmaHierarchy = GetHierarchyWithoutRootFrame(item.Object.NewData.Data.NameHierarchy);
            ui.FigmaLink.text = figmaHierarchy;

            string unityHierarchy = GetHierarchyWithoutRootFrame(item.Object.OldData?.NameHierarchy);
            ui.UnityLink.text = unityHierarchy ?? FcuLocKey.layout_updater_label_new_component.Localize();
            ui.UnityLink.SetEnabled(!string.IsNullOrEmpty(item.Object.OldData?.NameHierarchy));

            if (string.IsNullOrEmpty(unityHierarchy))
            {
                ui.UnityLink.style.color = new StyleColor(uitk.ColorScheme.ARROW_GRAY);
                ui.UnityLink.style.unityFontStyleAndWeight = FontStyle.Italic;
            }
            else
            {
                ui.UnityLink.style.color = new StyleColor(uitk.ColorScheme.TEXT);
                ui.UnityLink.style.unityFontStyleAndWeight = FontStyle.Normal;
            }

            ui.Toggle.UnregisterValueChangedCallback(OnItemToggleChanged);
            ui.Toggle.SetValueWithoutNotify(item.Selected);
            ui.Toggle.RegisterValueChangedCallback(OnItemToggleChanged);

            var detailsContainer = element.Q("detailsContainer");
            bool hasDetails = item.Object.Color.Enabled || item.Object.Size.Enabled;
            detailsContainer.style.display = hasDetails ? DisplayStyle.Flex : DisplayStyle.None;

            if (item.Object.Color.Enabled)
            {
                ui.ColorLabel.text = 
                    FcuLocKey.layout_updater_label_color_diff.Localize(
                    ColorToHex(item.Object.Color.Value1),
                    ColorToHex(item.Object.Color.Value2));
                ui.ColorLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                ui.ColorLabel.style.display = DisplayStyle.None;
            }

            if (item.Object.Size.Enabled)
            {
                ui.SizeLabel.text = 
                    FcuLocKey.layout_updater_label_size_diff.Localize(
                    item.Object.Size.Value1,
                    item.Object.Size.Value2);
                ui.SizeLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                ui.SizeLabel.style.display = DisplayStyle.None;
            }
        }

        private VisualElement CreateLinkRow(string iconChar, Color iconBgColor, Button linkButton)
        {
            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = DAI_UitkConstants.RowMarginBottom
                }
            };

            var icon = new Label(iconChar)
            {
                style =
                {
                    width = DAI_UitkConstants.GapYAbove,
                    height = DAI_UitkConstants.GapYAbove,
                    unityTextAlign = TextAnchor.MiddleCenter,
                    backgroundColor = new StyleColor(iconBgColor),
                    color = new StyleColor(Color.white),

                    marginRight = DAI_UitkConstants.CornerRadius,
                    fontSize = DAI_UitkConstants.FontSizeNormal,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };

            UIHelpers.SetRadius(icon, DAI_UitkConstants.CornerRadius / 2);

            UIHelpers.SetBorderWidth(linkButton, 0);

            linkButton.style.backgroundColor = new StyleColor(StyleKeyword.None);
            linkButton.style.color = new StyleColor(uitk.ColorScheme.TEXT);
            linkButton.style.unityTextAlign = TextAnchor.MiddleLeft;
            linkButton.style.paddingLeft = 0;
            linkButton.style.paddingRight = 0;
            linkButton.style.flexGrow = 1;

            linkButton.RegisterCallback<MouseEnterEvent>(evt =>
            {
                if (linkButton.enabledSelf)
                    linkButton.style.color = new StyleColor(uitk.ColorScheme.ACCENT_SECOND);
            });
            linkButton.RegisterCallback<MouseLeaveEvent>(evt =>
            {
                if (linkButton.enabledSelf)
                    linkButton.style.color = new StyleColor(uitk.ColorScheme.TEXT);
            });

            row.Add(icon);
            row.Add(linkButton);

            return row;
        }

        private void OnFigmaLinkClicked(ClickEvent evt)
        {
            if ((evt.currentTarget as Button)?.userData is SelectableObject<DiffInfo> item)
            {
                string figmaUrl = $"https://www.figma.com/design/{monoBeh.Settings.MainSettings.ProjectUrl}?node-id={item.Object.NewData.Id.Replace(":", "-")}";
                Application.OpenURL(figmaUrl);
            }
        }

        private void OnUnityLinkClicked(ClickEvent evt)
        {
            if ((evt.currentTarget as Button)?.userData is SelectableObject<DiffInfo> item)
            {
                if (item.Object.OldData?.GameObject != null)
                {
                    Selection.activeTransform = item.Object.OldData.GameObject.transform;
                    EditorGUIUtility.PingObject(item.Object.OldData.GameObject);
                }
            }
        }

        private void OnItemToggleChanged(ChangeEvent<bool> evt)
        {
            if ((evt.target as Toggle)?.userData is SelectableObject<DiffInfo> item)
            {
                item.Selected = evt.newValue;
                RefreshState();
            }
        }

        private VisualElement BuildRemovePanel()
        {
            var panel = new VisualElement
            {
                style =
                {
                    flexGrow = 1,
                    paddingLeft = DAI_UitkConstants.MarginPadding / 2,
                    flexDirection = FlexDirection.Column
                }
            };
            panel.Add(new Label(FcuLocKey.layout_updater_title_remove_from_scene.Localize())
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    marginBottom = DAI_UitkConstants.MarginPadding,
                    flexShrink = 0
                }
            });

            panel.Add(BuildRemoveSearchField());

            var scrollView = new ScrollView(ScrollViewMode.Vertical)
            {
                style =
                {
                    flexGrow = 1
                }
            };

            _removeScrollViewContent = new VisualElement();
            scrollView.Add(_removeScrollViewContent);

            panel.Add(scrollView);
            return panel;
        }

        private VisualElement BuildRemoveSearchField()
        {
            var searchContainer = new VisualElement
            {
                style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, marginBottom = DAI_UitkConstants.MarginPadding }
            };

            _removeSearchField = new TextField
            {
                name = "remove-search-field",
                style = { flexGrow = 1 }
            };

            _removeSearchField.RegisterValueChangedCallback(evt =>
            {
                _removeSearchText = evt.newValue;
                RebuildRemoveList();
            });

            var clearBtn = new Button(() =>
            {
                _removeSearchField.value = string.Empty;
                GUI.FocusControl(null);
            })
            {
                text = "✕",
                style = { width = 22, height = 20, marginLeft = 2 }
            };

            searchContainer.Add(new Label(FcuLocKey.common_placeholder_search.Localize()) { style = { marginRight = 4 } });
            searchContainer.Add(_removeSearchField);
            searchContainer.Add(clearBtn);

            return searchContainer;
        }

        private void RebuildRemoveList()
        {
            _removeScrollViewContent.Clear();
            _removeHeaderLabels.Clear();
            _removeHeaderToggles.Clear();
            _removeListViews.Clear();

            var dataToDisplay = ApplyRemoveSearchFilter();

            if (dataToDisplay.Childs.Any())
            {
                foreach (var item in dataToDisplay.Childs)
                {
                    var header = MakeHeader(out var headerLabel, out var toggle, out var arrow);
                    _removeHeaderLabels[item.Object.Id] = headerLabel;
                    _removeHeaderToggles[item.Object.Id] = toggle;

                    toggle.RegisterValueChangedCallback(evt =>
                    {
                        item.SetAllSelected(evt.newValue);
                        RefreshRemovePanelState();
                    });

                    var listView = new ListView(item.Childs, 22,
                        () => new Toggle(),
                        (element, index) =>
                        {
                            var toggleElement = (Toggle)element;
                            var syncDataItem = item.Childs[index];
                            toggleElement.text = GetHierarchyWithoutRootFrame(syncDataItem.Object.NameHierarchy);
                            toggleElement.SetValueWithoutNotify(syncDataItem.Selected);
                            toggleElement.RegisterValueChangedCallback(e =>
                            {
                                syncDataItem.Selected = e.newValue;
                                RefreshRemovePanelState();
                            });
                        })
                    {
                        selectionType = SelectionType.None
                    };

                    _removeListViews[item.Object.Id] = listView;

                    var foldout = new AnimatedFoldout(
                        item.Object.Id, 
                        header,
                        listView,
                        true, 
                        uitk.FoldoutCurve,
                        uitk.FoldoutDuration,
                        uitk.ColorScheme.CLEAR);

                    foldout.Toggled += exp => arrow.text = exp ? "▾" : "▸";
                    arrow.text = foldout.Expanded ? "▾" : "▸";

                    foldout.style.marginBottom = DAI_UitkConstants.RowMarginBottom;
                    _removeScrollViewContent.Add(foldout);
                }
            }
            else
            {
                _removeScrollViewContent.Add(new Label(FcuLocKey.common_message_nothing_matches.Localize())
                {
                    style = { unityTextAlign = TextAnchor.MiddleCenter, marginTop = 20, marginBottom = 20 }
                });
            }

            RefreshRemovePanelState();
        }

        private SelectableObject<SyncData> ApplyRemoveSearchFilter()
        {
            if (string.IsNullOrWhiteSpace(_removeSearchText) || _removeValueGetter == null)
            {
                return _diffStruct.ToRemove;
            }

            string query = _removeSearchText.Trim();

            var filteredRoot = new SelectableObject<SyncData>
            {
                Object = _diffStruct.ToRemove.Object,
                Selected = _diffStruct.ToRemove.Selected,
                Childs = new List<SelectableObject<SyncData>>()
            };

            foreach (var frame in _diffStruct.ToRemove.Childs)
            {
                var matchedComponents = new List<SelectableObject<SyncData>>();
                foreach (var component in frame.Childs)
                {
                    var value = _removeValueGetter(component);
                    if (value != null)
                    {
                        string componentName = value.ToString();
                        if (componentName.IndexOf(query, StringComparison.InvariantCultureIgnoreCase) >= 0)
                        {
                            matchedComponents.Add(component);
                        }
                    }
                }

                if (matchedComponents.Count > 0)
                {
                    var filteredFrame = new SelectableObject<SyncData>
                    {
                        Object = frame.Object,
                        Selected = frame.Selected,
                        Childs = matchedComponents
                    };
                    filteredRoot.Childs.Add(filteredFrame);
                }
            }
            return filteredRoot;
        }

        private VisualElement BuildFooter()
        {
            var bottomBar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingTop = DAI_UitkConstants.MarginPadding,
                    flexShrink = 0
                }
            };

            _helpBtn = uitk.HelpButton(() => uitk.ToggleHelpPanel(_helpPanel));
            bottomBar.Add(_helpBtn);

            bottomBar.Add(uitk.Space10());

            _applyBtn = uitk.Button(
                FcuLocKey.layout_updater_button_apply_and_continue.Localize(),
                OnApply);
            _applyBtn.color = uitk.ColorScheme.ACCENT_SECOND;
            _applyBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            bottomBar.Add(_applyBtn);

            bottomBar.Add(uitk.Space10());

            _cancelBtn = uitk.Button(
            FcuLocKey.label_stop_import.Localize(),
            () =>
            {
                monoBeh.AssetTools.StopAsset(StopAssetReason.Manual);
                Close();
            });
            _cancelBtn.color = uitk.ColorScheme.RED;
            _cancelBtn.style.maxWidth = 100;
            _cancelBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            bottomBar.Add(_cancelBtn);

            return bottomBar;
        }

        private void RefreshState()
        {
            // Calculate counts for filters
            int importNewCount = _diffStruct.ToImport.CountItems(x => x.Childs.IsEmpty() && x.Object.IsNew);
            int importNewCountSelected = _diffStruct.ToImport.CountItems(x => x.Childs.IsEmpty() && x.Object.IsNew && x.Selected);
            _importNewCountLabel.text = $"{importNewCountSelected}/{importNewCount}";
            SetTriState(_importNewToggle, importNewCountSelected, importNewCount);

            int unityChangedCount = _diffStruct.ToImport.CountItems(x => x.Childs.IsEmpty() && x.Object.IsUnitySideChanged());
            int unityChangedCountSelected = _diffStruct.ToImport.CountItems(x => x.Childs.IsEmpty() && x.Object.IsUnitySideChanged() && x.Selected);
            _unitySideChangedCountLabel.text = $"{unityChangedCountSelected}/{unityChangedCount}";
            SetTriState(_importUnityChangedToggle, unityChangedCountSelected, unityChangedCount);

            int importOtherCount = _diffStruct.ToImport.CountItems(x => x.Childs.IsEmpty() && x.Object.IsNotChanged());
            int importOtherCountSelected = _diffStruct.ToImport.CountItems(x => x.Childs.IsEmpty() && x.Object.IsNotChanged() && x.Selected);
            _importOtherCountLabel.text = $"{importOtherCountSelected}/{importOtherCount}";
            SetTriState(_importOtherToggle, importOtherCountSelected, importOtherCount);

            // Update main header
            GetCounts(_diffStruct.ToImport, out var mainSelected, out var mainAll);
            if (_mainHeaderLabel != null)
            {
                _mainHeaderLabel.text = FcuLocKey.layout_updater_label_main_header.Localize(mainSelected, mainAll);
                SetTriState(_mainToggle, mainSelected, mainAll);
            }

            // Update frame headers
            foreach (var frame in _diffStruct.ToImport.Childs)
            {
                GetCounts(frame, out var frameSelected, out var frameAll);
                if (_frameHeaderLabels.TryGetValue(frame.Object.Id, out var label))
                {
                    label.text = 
                        FcuLocKey.layout_updater_label_name_with_selection.Localize(
                        frame.Object.Name,
                        frameSelected,
                        frameAll);
                }
                if (_frameToggles.TryGetValue(frame.Object.Id, out var toggle))
                {
                    SetTriState(toggle, frameSelected, frameAll);
                }
            }

            RefreshRemovePanelState();
        }

        private void RefreshRemovePanelState()
        {
            if (_diffStruct.ToRemove.Childs.IsEmpty())
                return;

            foreach (var item in _diffStruct.ToRemove.Childs)
            {
                GetCounts(item, out var selected, out var all);

                if (_removeHeaderLabels.TryGetValue(item.Object.Id, out var label))
                {
                    label.text = 
                        FcuLocKey.layout_updater_label_name_with_selection.Localize(
                        item.Object.Names.ObjectName,
                        selected,
                        all);
                }
                if (_removeHeaderToggles.TryGetValue(item.Object.Id, out var toggle))
                {
                    SetTriState(toggle, selected, all);
                }
                if (_removeListViews.TryGetValue(item.Object.Id, out var listView))
                {
                    listView.RefreshItems();
                }
            }
        }

        private void UpdateData(Func<SelectableObject<DiffInfo>, bool> condition, bool value)
        {
            UpdateDataRecursive(_diffStruct.ToImport, condition, value);
        }

        private void UpdateDataRecursive(SelectableObject<DiffInfo> item, Func<SelectableObject<DiffInfo>, bool> condition, bool value)
        {
            if (item.Childs.IsEmpty())
            {
                if (condition(item))
                    item.Selected = value;
            }
            else
            {
                foreach (SelectableObject<DiffInfo> child in item.Childs)
                {
                    UpdateDataRecursive(child, condition, value);
                }
            }
        }

        private void GetCounts<T>(SelectableObject<T> node, out int selected, out int all)
        {
            var leafs = new List<SelectableObject<T>>();
            GetLeafsRecursive(node, leafs);

            all = leafs.Count();
            selected = leafs.Count(x => x.Selected);
        }

        private void GetLeafsRecursive<T>(SelectableObject<T> node, List<SelectableObject<T>> leafs)
        {
            if (node.Childs.IsEmpty())
            {
                leafs.Add(node);
            }
            else
            {
                foreach (var child in node.Childs)
                {
                    GetLeafsRecursive(child, leafs);
                }
            }
        }

        private void SetTriState(Toggle t, int selected, int all)
        {
            bool allOn = all > 0 && selected == all;
            bool noneOn = selected == 0;

            t.showMixedValue = !allOn && !noneOn;
            t.SetValueWithoutNotify(allOn);
        }

        private void OnApply()
        {
            // Use a HashSet to avoid duplicate IDs
            var toImportSelectedIds = new HashSet<string>();

            // Iterate over frame groups in the "Components" panel
            foreach (var frame in _diffStruct.ToImport.Childs)
            {
                bool frameHasSelectedChildren = false;

                // Iterate over components inside each frame
                foreach (var component in frame.Childs)
                {
                    if (component.Selected && component.Object?.Id != null)
                    {
                        // Add the ID of the selected component
                        toImportSelectedIds.Add(component.Object.Id);
                        frameHasSelectedChildren = true;
                    }
                }

                // If the frame has at least one selected component, add the frame's own ID
                if (frameHasSelectedChildren && frame.Object?.Id != null)
                {
                    toImportSelectedIds.Add(frame.Object.Id);
                }
            }

            var toRemoveSelected = new List<SyncData>();

            // Iterate over frame groups in the "Remove" panel
            foreach (var frameGroup in _diffStruct.ToRemove.Childs)
            {
                // Find all selected child elements in this group
                var selectedChildren = frameGroup.Childs
                    .Where(c => c.Selected && c.Object != null)
                    .Select(c => c.Object)
                    .ToList();

                // Add the selected child elements to the final list
                toRemoveSelected.AddRange(selectedChildren);

                // Check if ALL child elements were selected
                bool allChildrenSelected = frameGroup.Childs.Any() && 
                    selectedChildren.Count == frameGroup.Childs.Count;

                // If all children are selected, add the parent frame to the list as well.
                // This indicates the intention to remove the entire frame.
                if (allChildrenSelected && frameGroup.Object != null)
                {
                    toRemoveSelected.Add(frameGroup.Object);
                }
            }

            LayoutUpdaterOutput result = new LayoutUpdaterOutput
            {
                ToImport = toImportSelectedIds.ToList(),
                ToRemove = toRemoveSelected
            };

            _callback?.Invoke(result);
            this.Close();
        }

        private void GetSelectedIdsRecursive(SelectableObject<DiffInfo> node, List<string> selectedIds)
        {
            if (node.Childs.IsEmpty() && node.Selected && node.Object?.Id != null)
            {
                selectedIds.Add(node.Object.Id);
            }

            foreach (var child in node.Childs)
            {
                GetSelectedIdsRecursive(child, selectedIds);
            }
        }

        private void GetSelectedSyncDataRecursive(SelectableObject<SyncData> node, List<SyncData> selectedData)
        {
            if (node.Childs.IsEmpty() && node.Selected && node.Object != null)
            {
                selectedData.Add(node.Object);
            }

            foreach (var child in node.Childs)
            {
                GetSelectedSyncDataRecursive(child, selectedData);
            }
        }

        private string GetHierarchyWithoutRootFrame(string hierarchy)
        {
            if (string.IsNullOrEmpty(hierarchy))
                return hierarchy;

            int index = hierarchy.IndexOf(FcuConfig.HierarchyDelimiter);
            return index != -1 ? ".." + hierarchy.Substring(index) : hierarchy;
        }

        private string ColorToHex(Color color) => "#" + ColorUtility.ToHtmlStringRGB(color);

        private VisualElement MakeHeader(out Label titleLabel, out Toggle rightToggle, out Label arrow)
        {
            var header = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    height = 32,
                    paddingLeft = DAI_UitkConstants.CornerRadius,
                    paddingRight = DAI_UitkConstants.CornerRadius,
                    width = Length.Percent(100)
                }
            };

            UIHelpers.SetBorderColor(header, uitk.ColorScheme.OUTLINE);
            UIHelpers.SetBorderWidth(header, DAI_UitkConstants.BorderWidth);
            UIHelpers.SetRadius(header, DAI_UitkConstants.CornerRadius);

            arrow = new Label("▾")
            {
                style =
                {
                    fontSize = DAI_UitkConstants.FontSizeTitle,
                    width = DAI_UitkConstants.StatusIconSize
                }
            };
            titleLabel = new Label
            {
                style =
                {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    flexGrow = 1
                }
            };
            rightToggle = new Toggle();

            header.Add(arrow);
            header.Add(uitk.Space(5));
            header.Add(titleLabel);
            header.Add(rightToggle);

            return header;
        }

        private VisualElement NarrowContainer(int depth, Color bgColor)
        {
            float p = WidthPercentForDepth(depth);
            return new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    alignSelf = Align.Center,
                    width = new Length(p, LengthUnit.Percent),
                    backgroundColor = bgColor
                }
            };
        }

        private float WidthPercentForDepth(int depth)
        {
            float v = 100f - depth * 4f;
            if (v < 70f)
            {
                v = 70f;
            }

            if (v > 100f)
            {
                v = 100f;
            }

            return v;
        }

        private class ItemUI
        {
            public Button FigmaLink { get; set; }
            public Button UnityLink { get; set; }
            public Toggle Toggle { get; set; }
            public Label ColorLabel { get; set; }
            public Label SizeLabel { get; set; }
            public SelectableObject<DiffInfo> Item { get; set; }
        }
    }

    internal static class DiffCheckerExtensions
    {
        internal static bool IsNotChanged(this DiffInfo diffInfo) => !diffInfo.IsNew && !diffInfo.IsUnitySideChanged();
        internal static bool IsUnitySideChanged(this DiffInfo diffInfo) => diffInfo.Color.Enabled || diffInfo.Size.Enabled;

        internal static int CountItems<T>(this SelectableObject<T> obj, Func<SelectableObject<T>, bool> condition)
        {
            int count = 0;
            if (obj.Childs.IsEmpty())
            {
                if (condition(obj))
                    count++;
            }
            else
            {
                foreach (var child in obj.Childs)
                {
                    count += child.CountItems(condition);
                }
            }
            return count;
        }
    }

    internal sealed class MemberPathAccessor
    {
        private readonly MemberInfo[] _members;

        private MemberPathAccessor(MemberInfo[] members)
        {
            _members = members;
        }

        public static bool TryCreate(Type rootType, string path, out MemberPathAccessor accessor)
        {
            accessor = null;
            if (string.IsNullOrWhiteSpace(path)) return false;

            var parts = path.Split('.');
            var members = new List<MemberInfo>(parts.Length);
            var type = rootType;

            const BindingFlags flags =
                  BindingFlags.Instance |
                  BindingFlags.Public |
                  BindingFlags.NonPublic |
                  BindingFlags.FlattenHierarchy |
                  BindingFlags.IgnoreCase;

            foreach (var part in parts)
            {
                if (string.IsNullOrWhiteSpace(part)) return false;

                var prop = type.GetProperty(part, flags);
                if (prop != null)
                {
                    members.Add(prop);
                    type = prop.PropertyType;
                    continue;
                }

                var field = type.GetField(part, flags);
                if (field != null)
                {
                    members.Add(field);
                    type = field.FieldType;
                    continue;
                }

                return false;
            }

            accessor = new MemberPathAccessor(members.ToArray());
            return true;
        }

        public object GetValue(object root)
        {
            var current = root;

            for (int i = 0; i < _members.Length; i++)
            {
                if (current == null)
                    return null;

                switch (_members[i])
                {
                    case PropertyInfo p:
                        current = p.GetValue(current, null);
                        break;
                    case FieldInfo f:
                        current = f.GetValue(current);
                        break;
                    default:
                        return null;
                }
            }
            return current;
        }
    }

    internal static class MemberPathCache
    {
        private static readonly Dictionary<(Type, string), MemberPathAccessor> Cache =
            new Dictionary<(Type, string), MemberPathAccessor>();

        public static bool TryGetOrCreate(Type rootType, string path, out MemberPathAccessor accessor)
        {
            var key = (rootType, path);
            if (Cache.TryGetValue(key, out accessor))
                return true;

            if (MemberPathAccessor.TryCreate(rootType, path, out accessor))
            {
                Cache[key] = accessor;
                return true;
            }

            return false;
        }
    }
}
