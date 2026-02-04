using DA_Assets.DAI;
using DA_Assets.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.FCU
{
    internal class SpriteDuplicateFinderWindow : LinkedEditorWindow<
        SpriteDuplicateFinderWindow,
        FcuEditor,
        FigmaConverterUnity>
    {
        private ScrollView _results;
        private CustomButton _applyBtn;
        private CustomButton _cancelBtn;
        private Button _helpBtn;
        private VisualElement _helpPanel;
        private Label _statsLabel;

        private bool _inited;
        private int _dropHighlightIndex = -1;

        private const string DND_KEY = "SDF_DUB";

        internal int GroupCount => _groups.Count;

        private readonly List<List<SpriteUsageFinder.UsedSprite>> _groups = new List<List<SpriteUsageFinder.UsedSprite>>();

        private readonly Dictionary<int, VisualElement> _groupCards = new Dictionary<int, VisualElement>();
        private readonly Dictionary<int, VisualElement> _groupRows = new Dictionary<int, VisualElement>();
        private readonly Dictionary<int, Label> _groupHeaders = new Dictionary<int, Label>();

        private Action<List<List<SpriteUsageFinder.UsedSprite>>> _callback;

        private readonly FcuLocKey[] _helpSteps =
        {
            FcuLocKey.sprite_duplicate_help_step_1,
            FcuLocKey.sprite_duplicate_help_step_2,
            FcuLocKey.sprite_duplicate_help_step_3,
            FcuLocKey.sprite_duplicate_help_step_4,
            FcuLocKey.sprite_duplicate_help_step_5,
            FcuLocKey.sprite_duplicate_help_step_6,
            FcuLocKey.sprite_duplicate_help_step_7,
            FcuLocKey.sprite_duplicate_help_step_8,
            FcuLocKey.sprite_duplicate_help_step_9,
        };


        private void Update()
        {
            if (_groups.IsEmpty())
            {
                Close();
            }
        }

        internal void SetData(List<List<SpriteUsageFinder.UsedSprite>> groups, Action<List<List<SpriteUsageFinder.UsedSprite>>> callback)
        {
            _groups.Clear();
            _groups.AddRange(groups);
            _callback = callback;

            _inited = true;
            BuildAllGroupsUI();
        }

        public void CreateGUI()
        {
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

            _helpPanel = uitk.BuildHelpPanel(
                FcuConfig.Instance.Localizator.Language, 
                steps: _helpSteps.Select(s =>
                s == FcuLocKey.sprite_duplicate_help_step_7
                    ? s.Localize(FcuLocKey.common_button_apply.Localize())
                    : s.Localize()
            ).ToArray());

            topArea.Add(_helpPanel);

            var contentCol = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    flexGrow = 1,
                    minHeight = 0
                }
            };
            topArea.Add(contentCol);

            _results = uitk.ScrollView();
            _results.contentContainer.style.marginRight = DAI_UitkConstants.MarginPadding;
            _results.style.flexGrow = 1;
            _results.style.flexShrink = 1;
            _results.style.minHeight = 0;
            _results.style.marginBottom = 0;
            contentCol.Add(_results);

            var bottomBar = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    paddingTop = DAI_UitkConstants.MarginPadding,
                    //height = DAI_UitkConstants.FooterHeight,
                    flexShrink = 0
                }
            };
            root.Add(bottomBar);

            _helpBtn = uitk.HelpButton(() => uitk.ToggleHelpPanel(_helpPanel));


            bottomBar.Add(_helpBtn);
            bottomBar.Add(uitk.Space5());
            var statsPanel = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    justifyContent = Justify.Center,
                    backgroundColor = uitk.ColorScheme.BUTTON,
                    height = DAI_UitkConstants.ButtonHeight,
                },
            };

            UIHelpers.SetPadding(statsPanel, DAI_UitkConstants.MarginPadding / 2);
            UIHelpers.SetRadius(statsPanel, DAI_UitkConstants.CornerRadius);
            UIHelpers.SetBorderWidth(statsPanel, DAI_UitkConstants.BorderWidth);
            UIHelpers.SetBorderColor(statsPanel, uitk.ColorScheme.OUTLINE);
            bottomBar.Add(statsPanel);
            bottomBar.Add(uitk.Space(8));

            _statsLabel = new Label
            {
                style =
                {
                    unityTextAlign = TextAnchor.MiddleCenter,
                    fontSize = DAI_UitkConstants.FontSizeNormal,
                    whiteSpace = WhiteSpace.Normal
                }
            };
            statsPanel.Add(_statsLabel);
            UpdateStatsLabel();

            _applyBtn = uitk.Button(
                FcuLocKey.common_button_apply.Localize(),
                OnApply);
            _applyBtn.color = EditorGUIUtility.isProSkin ? uitk.ColorScheme.ACCENT_SECOND : uitk.ColorScheme.BUTTON;
            _applyBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
            bottomBar.Add(_applyBtn);

            bottomBar.Add(uitk.Space5());

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

            if (_inited)
                BuildAllGroupsUI();
        }

        private void BuildAllGroupsUI()
        {
            if (_results == null)
            {
                Debug.LogWarning(FcuLocKey.log_sdf_results_scroll_null.Localize());
                return;
            }

            _results.Clear();
            _groupCards.Clear();
            _groupRows.Clear();
            _groupHeaders.Clear();
            _dropHighlightIndex = -1;

            for (int gi = 0; gi < _groups.Count; ++gi)
            {
                var card = CreateGroupCard(gi);
                _results.Add(card);
            }

            UpdateStatsLabel();
        }

        private VisualElement CreateGroupCard(int gi)
        {
            var card = new VisualElement
            {
                name = "card",
                style =
                {
                    flexDirection  = FlexDirection.Column,
                    marginBottom   = DAI_UitkConstants.MarginPadding,
                    backgroundColor= uitk.ColorScheme.GROUP
                },
                userData = gi
            };

            UIHelpers.SetDefaultPadding(card);

            UIHelpers.SetRadius(card, DAI_UitkConstants.CornerRadius);
            UIHelpers.SetBorderWidth(card, DAI_UitkConstants.BorderWidth);
            UIHelpers.SetBorderColor(card, uitk.ColorScheme.OUTLINE);

            var header = new Label();
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.marginBottom = DAI_UitkConstants.MarginPadding;
            _groupHeaders[gi] = header;

            UpdateHeader(gi);

            card.Add(header);

            RegisterDropEvents(card);

            var row = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    flexWrap      = Wrap.Wrap
                }
            };

            row.userData = gi;
            RegisterDropEvents(row);

            _groupRows[gi] = row;
            card.Add(row);

            RenderGroupItems(gi);
            _groupCards[gi] = card;
            return card;
        }

        private void RegisterDropEvents(VisualElement ve)
        {
            ve.RegisterCallback<DragEnterEvent>(e =>
            {
                if (!TryGetDragData(out var payload))
                    return;

                int gi = GetGroupIndexFrom(ve);
                SetDropHighlight(gi, true);
            });

            ve.RegisterCallback<DragUpdatedEvent>(e =>
            {
                if (TryGetDragData(out _))
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    e.StopPropagation();
                }
            });

            ve.RegisterCallback<DragLeaveEvent>(e =>
            {
                if (!TryGetDragData(out _))
                    return;

                int gi = GetGroupIndexFrom(ve);
                if (_dropHighlightIndex == gi)
                    SetDropHighlight(gi, false);
            });

            ve.RegisterCallback<DragExitedEvent>(e =>
            {
                if (_dropHighlightIndex >= 0)
                    SetDropHighlight(_dropHighlightIndex, false);
            });

            ve.RegisterCallback<DragPerformEvent>(e =>
            {
                if (TryGetDragData(out var payload))
                {
                    DragAndDrop.AcceptDrag();
                    int toIndex = GetGroupIndexFrom(ve);
                    if (_dropHighlightIndex >= 0)
                        SetDropHighlight(_dropHighlightIndex, false);
                    MoveSpriteByPath(payload.SourceGroup, toIndex, payload.Path);
                    e.StopPropagation();
                }
            });

        }

        private int GetGroupIndexFrom(VisualElement ve)
        {
            var card = ve;

            while (card != null && !_groupCards.Values.Contains(card))
                card = card.parent;

            if (card == null)
            {
                Debug.LogWarning(FcuLocKey.log_sdf_group_card_missing_for_element.Localize(ve));
                return -1;
            }

            if (card.userData is int gi)
            {
                return gi;
            }
            else
            {
                Debug.LogWarning(FcuLocKey.log_sdf_group_card_userdata_invalid.Localize(card.userData));
                return -1;
            }
        }

        private void SetDropHighlight(int gi, bool on)
        {
            if (!_groupCards.TryGetValue(gi, out var card))
            {
                Debug.LogWarning(FcuLocKey.log_sdf_group_card_missing_for_index.Localize(gi));
                return;
            }

            if (on)
            {
                _dropHighlightIndex = gi;
                UIHelpers.SetBorderColor(card, uitk.ColorScheme.OUTLINE);
                card.style.backgroundColor = uitk.ColorScheme.BUTTON;
            }
            else
            {
                if (_dropHighlightIndex == gi) _dropHighlightIndex = -1;
                UIHelpers.SetBorderColor(card, uitk.ColorScheme.OUTLINE);
                card.style.backgroundColor = uitk.ColorScheme.GROUP;
            }
        }

        private void RenderGroupItems(int gi)
        {
            if (!_groupRows.TryGetValue(gi, out var row))
            {
                Debug.LogWarning(FcuLocKey.log_sdf_group_row_missing_for_index.Localize(gi));
                return;
            }

            row.Clear();

            var group = _groups[gi];

            var ordered = group
                .OrderByDescending(d => Mathf.Max(1, (int)d.Size.x) * Mathf.Max(1, (int)d.Size.y))
                .ToList();

            foreach (SpriteUsageFinder.UsedSprite ds in ordered)
            {
                Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(ds.Path);

                if (tex == null)
                    continue;

                var thumb = MakeThumbPreview(
                    tex.GetPixels32(),
                    tex.width,
                    tex.height,
                    (int)DAI_UitkConstants.ThumbSize,
                    (int)DAI_UitkConstants.ThumbSize);

                var col = new VisualElement
                {
                    style =
                    {
                        flexDirection = FlexDirection.Column,
                        alignItems    = Align.Center,
                        marginRight   = DAI_UitkConstants.MarginPadding,
                        marginBottom  = DAI_UitkConstants.MarginPadding
                    }
                };

                var imgContainer = new VisualElement
                {
                    style =
                    {
                        width = DAI_UitkConstants.ThumbSize,
                        height = DAI_UitkConstants.ThumbSize,
                    }
                };

                var img = new Image
                {
                    image = thumb,
                    scaleMode = ScaleMode.ScaleToFit,
                    style =
                    {
                        width  = DAI_UitkConstants.ThumbSize,
                        height = DAI_UitkConstants.ThumbSize
                    }
                };

                UIHelpers.SetRadius(img, DAI_UitkConstants.CornerRadius);
                UIHelpers.SetBorderWidth(img, DAI_UitkConstants.BorderWidth);
                UIHelpers.SetBorderColor(img, uitk.ColorScheme.OUTLINE);
                imgContainer.Add(img);

                if (ds.Usages != null && ds.Usages.Count > 0)
                {
                    var usageBadge = new Label(ds.Usages.Count.ToString())
                    {
                        style =
                        {
                            position = Position.Absolute,
                            top = 0,
                            right = 0,
                            backgroundColor = uitk.ColorScheme.GREEN,
                            color = Color.white,
                            fontSize = DAI_UitkConstants.FontSizeTiny,
                            unityFontStyleAndWeight = FontStyle.Bold,
                            paddingLeft = 4,
                            paddingRight = 4,
                            borderTopRightRadius = DAI_UitkConstants.CornerRadius,
                            borderBottomLeftRadius = DAI_UitkConstants.CornerRadius
                        }
                    };
                    imgContainer.Add(usageBadge);
                }

                col.Add(imgContainer);

                var tog = new Toggle { value = ds.Selected };
                tog.RegisterValueChangedCallback(e =>
                {
                    ds.Selected = e.newValue;
                    int giNow = GetGroupIndexFrom(col);

                    RenderGroupItems(giNow);
                    UpdateStatsLabel();
                });

                tog.style.marginTop = DAI_UitkConstants.SpacingXXS;
                col.Add(tog);
                col.Add(MakeWrappedNameLabel(System.IO.Path.GetFileName(ds.Path), DAI_UitkConstants.ItemLabelWidth));

                col.Add(new Label($"{(int)ds.Size.x}x{(int)ds.Size.y}")
                {
                    style =
                    {
                        unityTextAlign = TextAnchor.MiddleCenter,
                        fontSize       = DAI_UitkConstants.FontSizeTiny,
                        width          = DAI_UitkConstants.ItemLabelWidth,
                    }
                });

                Vector2 downPos = Vector2.zero;
                bool dragging = false;

                col.RegisterCallback<MouseDownEvent>(e =>
                {
                    if (e.button == 1)
                    {
                        int srcGiNow = GetGroupIndexFrom(col);
                        ShowContextMenuForSprite(srcGiNow, ds.Path);
                        return;
                    }

                    if (e.button != 0)
                        return;

                    downPos = e.mousePosition;
                    dragging = true;

                    int srcIndexNow = GetGroupIndexFrom(col);
                    var payload = new DragData
                    {
                        SourceGroup = srcIndexNow,
                        Path = ds.Path
                    };

                    Texture2D texRef = AssetDatabase.LoadAssetAtPath<Texture2D>(ds.Path);
                    DragAndDrop.PrepareStartDrag();
                    DragAndDrop.SetGenericData(DND_KEY, payload);

                    DragAndDrop.objectReferences = texRef != null ? new UnityEngine.Object[]
                    {
                        texRef
                    } : Array.Empty<UnityEngine.Object>();

                    DragAndDrop.paths = new[] { ds.Path };
                });

                col.RegisterCallback<MouseMoveEvent>(e =>
                {
                    if (!dragging)
                        return;

                    if ((e.mousePosition - downPos).sqrMagnitude > DAI_UitkConstants.DragThresholdSqrMagnitude)
                    {
                        DragAndDrop.StartDrag(System.IO.Path.GetFileName(ds.Path));
                        dragging = false;
                    }
                });

                col.RegisterCallback<MouseUpEvent>(e =>
                {
                    if (e.button == 0 && dragging)
                    {
                        PingAssetByPath(ds.Path, focusProjectWindow: true);
                        e.StopPropagation();
                    }
                    dragging = false;
                });

                col.RegisterCallback<MouseUpEvent>(e => dragging = false);
                row.Add(col);
            }

            UpdateHeader(gi);
            UpdateStatsLabel();
        }

        private static Texture2D MakeThumbPreview(Color32[] pix, int w0, int h0, int outW, int outH)
        {
            var tex = new Texture2D(w0, h0, TextureFormat.RGBA32, false);
            tex.SetPixels32(pix);
            tex.Apply();

            var rt = RenderTexture.GetTemporary(outW, outH);
            var previousRt = RenderTexture.active;
            RenderTexture.active = rt;

            float scale = Mathf.Min(outW / (float)w0, outH / (float)h0);
            int drawW = Mathf.Max(1, Mathf.RoundToInt(w0 * scale));
            int drawH = Mathf.Max(1, Mathf.RoundToInt(h0 * scale));
            float offsetX = (outW - drawW) * 0.5f;
            float offsetY = (outH - drawH) * 0.5f;

            GL.PushMatrix();
            GL.LoadPixelMatrix(0, outW, outH, 0);
            GL.Clear(true, true, Color.clear);
            Graphics.DrawTexture(new Rect(offsetX, offsetY, drawW, drawH), tex);
            GL.PopMatrix();

            var prev = new Texture2D(outW, outH, TextureFormat.RGBA32, false);
            prev.ReadPixels(new Rect(0, 0, outW, outH), 0, 0);
            prev.Apply();

            RenderTexture.active = previousRt;
            RenderTexture.ReleaseTemporary(rt);
            UnityEngine.Object.DestroyImmediate(tex);

            return prev;
        }

        private static Label MakeWrappedNameLabel(string text, float width = 80f)
        {
            var lbl = new Label(text);
            lbl.style.width = width;
            lbl.style.maxWidth = width;
            lbl.style.whiteSpace = WhiteSpace.Normal;
            lbl.style.overflow = Overflow.Hidden;
            lbl.style.unityTextAlign = TextAnchor.UpperCenter;
            lbl.style.fontSize = DAI_UitkConstants.FontSizeTiny;
            lbl.style.marginTop = DAI_UitkConstants.SpacingXXS;
            return lbl;
        }

        private void ShowContextMenuForSprite(int sourceGroupIndex, string path)
        {
            var group = _groups[sourceGroupIndex];
            var ds = group.FirstOrDefault(s => s.Path == path);

            if (ds == null)
            {
                Debug.LogError(FcuLocKey.log_sdf_sprite_data_missing.Localize(path, sourceGroupIndex));
                return;
            }

            var menu = new GenericMenu();

            if (ds.Usages != null && ds.Usages.Count > 0)
            {
                menu.AddItem(new GUIContent($"Show {ds.Usages.Count} Usages..."), false, () =>
                {
                    SpriteUsagePopup.Show(ds.Usages, uitk);
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Show Usages (None)"));
            }

            menu.AddSeparator("");

            menu.AddItem(new GUIContent(FcuLocKey.common_button_create_group.Localize()), false, () =>
            {
                CreateGroupFromSprite(sourceGroupIndex, path);
            });

            menu.AddItem(new GUIContent(FcuLocKey.sprite_duplicate_context_move_to_group.Localize()), false, () =>
            {
                MoveToGroupPopup.Show(sourceGroupIndex, path, this);
            });

            menu.ShowAsContext();
        }

        private void CreateGroupFromSprite(int sourceGroup, string path)
        {
            if (sourceGroup < 0 || sourceGroup >= _groups.Count)
            {
                EditorUtility.DisplayDialog("Create group", $"Invalid source group index: {sourceGroup}", "OK");
                return;
            }

            var src = _groups[sourceGroup];

            int idx = src.FindIndex(x => x.Path == path);
            if (idx < 0)
            {
                EditorUtility.DisplayDialog("Create group", "Source group does not contain this sprite.", "OK");
                return;
            }

            var ds = src[idx];
            src.RemoveAt(idx);
            UpdateSelectionsForGroup(src);

            int insertIndex = Mathf.Min(sourceGroup + 1, _groups.Count);
            _groups.Insert(insertIndex, new List<DA_Assets.SpriteUsageFinder.UsedSprite> { ds });

            ShiftUiDictionariesOnInsert(insertIndex);
            var newCard = CreateGroupCard(insertIndex);
            _results.Insert(insertIndex, newCard);

            RenderGroupItems(sourceGroup);
            RenderGroupItems(insertIndex);

            for (int i = insertIndex; i < _groups.Count; i++)
                UpdateHeader(i);

            UpdateStatsLabel();
        }

        private void ShiftUiDictionariesOnInsert(int insertIndex)
        {
            for (int i = _groups.Count - 2; i >= insertIndex; i--)
            {
                if (_groupCards.TryGetValue(i, out var card))
                {
                    _groupCards.Remove(i);
                    _groupCards[i + 1] = card;
                    card.userData = i + 1;
                }

                if (_groupRows.TryGetValue(i, out var row))
                {
                    _groupRows.Remove(i);
                    _groupRows[i + 1] = row;
                }

                if (_groupHeaders.TryGetValue(i, out var header))
                {
                    _groupHeaders.Remove(i);
                    _groupHeaders[i + 1] = header;
                }
            }
        }

        internal void MoveSpriteByPath(int from, int to, string path)
        {
            if (from == to)
            {
                EditorUtility.DisplayDialog(
                    FcuLocKey.move_to_group_title.Localize(),
                    "Source and target groups are the same.",
                    FcuLocKey.common_button_ok.Localize());
                return;
            }

            if (from < 0 || from >= _groups.Count)
            {
                EditorUtility.DisplayDialog(
                    FcuLocKey.move_to_group_title.Localize(),
                    $"Invalid source index: {from}",
                    FcuLocKey.common_button_ok.Localize());
                return;
            }

            if (to < 0 || to >= _groups.Count)
            {
                EditorUtility.DisplayDialog(
                    FcuLocKey.move_to_group_title.Localize(),
                    $"Target group {to} does not exist.",
                    FcuLocKey.common_button_ok.Localize());
                return;
            }

            if (_groupCards.TryGetValue(from, out var fromCard) && fromCard.userData is int fUI)
                from = fUI;

            if (_groupCards.TryGetValue(to, out var toCard) && toCard.userData is int tUI)
                to = tUI;

            if (from == to)
            {
                EditorUtility.DisplayDialog(
                    FcuLocKey.move_to_group_title.Localize(),
                    "Already in this group.",
                    FcuLocKey.common_button_ok.Localize());
                return;
            }

            var src = _groups[from];
            var dst = _groups[to];

            int idx = src.FindIndex(x => x.Path == path);
            if (idx < 0)
            {
                EditorUtility.DisplayDialog(
                    FcuLocKey.move_to_group_title.Localize(),
                    "Source group does not contain this sprite anymore.",
                    FcuLocKey.common_button_ok.Localize());
                return;
            }

            var ds = src[idx];
            src.RemoveAt(idx);
            dst.Add(ds);

            UpdateSelectionsForGroup(src);
            UpdateSelectionsForGroup(dst);

            RenderGroupItems(from);
            RenderGroupItems(to);
            UpdateHeader(from);
            UpdateHeader(to);
            UpdateStatsLabel();
        }

        private void UpdateSelectionsForGroup(List<SpriteUsageFinder.UsedSprite> g)
        {
            if (g == null || g.Count == 0)
                return;

            var keep = g.OrderByDescending(d => d.Size.x * d.Size.y).First();
            foreach (var d in g) d.Selected = d != keep;
        }

        private void UpdateHeader(int gi)
        {
            if (_groupHeaders.TryGetValue(gi, out var label))
            {
                int count = _groups[gi].Count;
                label.text = FcuLocKey.sprite_duplicate_label_group_count.Localize(gi, count);
            }
            else
            {
                Debug.LogWarning(FcuLocKey.log_sdf_group_header_missing.Localize(gi));
            }

            if (_groupCards.TryGetValue(gi, out var card))
            {
                card.userData = gi;
            }
            else
            {
                Debug.LogWarning(FcuLocKey.log_sdf_group_card_missing_for_index.Localize(gi));
            }
        }

        private void OnApply()
        {
            _callback.Invoke(_groups);
            Close();
        }

        private void UpdateStatsLabel()
        {
            int total = _groups.Sum(g => g.Count);
            int selected = _groups.Sum(g => g.Count(d => d.Selected));
            _statsLabel.text = FcuLocKey.sprite_duplicate_label_selected_for_deletion.Localize(selected, total);
        }

        private static bool TryGetDragData(out DragData data)
        {
            object obj = DragAndDrop.GetGenericData(DND_KEY);
            if (obj is DragData dd)
            {
                data = dd;
                return true;
            }
            else
            {
                Debug.LogWarning(FcuLocKey.log_sdf_drag_data_invalid.Localize(
                    obj?.GetType()?.ToString() ?? "null",
                    typeof(DragData).Name,
                    DND_KEY));
            }
            data = default;
            return false;
        }

        private static void PingAssetByPath(string assetPath, bool focusProjectWindow = true)
        {
            var obj = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (obj == null)
            {
                Debug.LogError(FcuLocKey.log_sdf_asset_not_found.Localize(assetPath));
                return;
            }

            Selection.activeObject = obj;
            EditorGUIUtility.PingObject(obj);

            if (focusProjectWindow)
                EditorUtility.FocusProjectWindow();
        }
    }
}
