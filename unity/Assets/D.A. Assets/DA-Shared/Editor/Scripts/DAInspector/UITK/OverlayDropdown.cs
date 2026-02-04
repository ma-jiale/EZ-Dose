using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.DAI
{
    /// <summary>
    /// Internal utility class to render an overlay drop‑down.  All magic numbers used for styling have been
    /// extracted into constants at the top of the class to aid maintainability.
    /// </summary>
    public sealed class OverlayDropdown : VisualElement
    {
        [SerializeField] DAInspectorUITK gui;

        private static OverlayDropdown _active;
        private static VisualElement _activeRoot;
        private static VisualElement _activeAnchor;

        private readonly List<string> _options;
        private int _selected;
        private readonly Action<int> _onPick;

        private VisualElement _overlay;
        private ScrollView _scroll;
        private readonly List<VisualElement> _rows = new List<VisualElement>();

        private EventCallback<GeometryChangedEvent> _onRootGeometryChanged;
        private EventCallback<DetachFromPanelEvent> _onPopupDetached;

        private int _hoverIndex = -1;

        private OverlayDropdown(DAInspectorUITK gui, List<string> options, int selected, Action<int> onPick)
        {
            this.gui = gui;

            _options = options ?? new List<string>();
            _selected = Mathf.Clamp(selected, -1, _options.Count - 1);
            _onPick = onPick;

            pickingMode = PickingMode.Position;
            focusable = true;

            style.position = Position.Absolute;
            style.flexDirection = FlexDirection.Column;
            style.backgroundColor = gui.ColorScheme.GROUP;
            style.paddingTop = DAI_UitkConstants.MarginPadding;
            style.paddingBottom = DAI_UitkConstants.MarginPadding;
            style.maxHeight = DAI_UitkConstants.MaxHeight;
            style.unityTextAlign = TextAnchor.MiddleLeft;

            UIHelpers.SetRadius(this, DAI_UitkConstants.CornerRadius);
            UIHelpers.SetBorderWidth(this, DAI_UitkConstants.BorderWidth);
            UIHelpers.SetBorderColor(this, gui.ColorScheme.OUTLINE);

            _scroll = new ScrollView(ScrollViewMode.Vertical)
            {
                pickingMode = PickingMode.Position,
                style =
                {
                    flexGrow = 1f,
                    paddingLeft = 0f,
                    paddingRight = 0f,
                    marginLeft = 0f,
                    marginRight = 0f
                }
            };

            // Build a row for each option and add it to the scroll view.
            for (int i = 0; i < _options.Count; i++)
            {
                int index = i;
                _rows.Add(BuildItem(_options[i], index == _selected, () => { Pick(index); }));
            }
            foreach (var row in _rows)
            {
                _scroll.Add(row);
            }
            Add(_scroll);
            RegisterCallback<KeyDownEvent>(OnKeyDown);
            RegisterCallback<FocusOutEvent>((FocusOutEvent e) => Close());
        }

        // Creates a single selectable row in the drop‑down.
        private VisualElement BuildItem(string text, bool selected, Action pick)
        {
            VisualElement row = new VisualElement
            {
                focusable = true,
                tabIndex = 0,
                style =
                {
                    flexDirection = FlexDirection.Row,
                    width = Length.Percent(100),
                    height = DAI_UitkConstants.ButtonHeight,
                    alignItems = Align.Center,
                    paddingLeft = DAI_UitkConstants.MarginPadding,
                    paddingRight = DAI_UitkConstants.MarginPadding,
                    marginBottom = DAI_UitkConstants.RowMarginBottom
                }
            };
            Label label = new Label(text)
            {
                style =
                {
                    flexGrow = 1f,
                    fontSize = DAI_UitkConstants.FontSizeNormal,
                    unityFontStyleAndWeight = selected ? FontStyle.Bold : FontStyle.Normal,
                    color = gui.ColorScheme.TEXT
                }
            };
            DropdownFontUtil.ApplyTo(label);
            row.Add(label);
            row.RegisterCallback<PointerEnterEvent>((PointerEnterEvent _) =>
            {
                _hoverIndex = _rows.IndexOf(row);
                row.style.backgroundColor = new StyleColor(gui.ColorScheme.HOVER_WHITE);
            });
            row.RegisterCallback<PointerLeaveEvent>((PointerLeaveEvent _) =>
            {
                row.style.backgroundColor = StyleKeyword.Null;
            });
            row.RegisterCallback<PointerDownEvent>((PointerDownEvent e) =>
            {
                pick();
                e.StopPropagation();
            });
            return row;
        }

        // Handles keyboard navigation within the overlay.
        private void OnKeyDown(KeyDownEvent e)
        {
            if (_rows.Count == 0)
            {
                return;
            }
            if (e.keyCode == KeyCode.Escape)
            {
                Close();
                e.StopPropagation();
                return;
            }
            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
            {
                int pickIndex = _hoverIndex >= 0 ? _hoverIndex : (_selected >= 0 ? _selected : 0);
                pickIndex = Mathf.Clamp(pickIndex, 0, _rows.Count - 1);
                Pick(pickIndex);
                e.StopPropagation();
                return;
            }
            if (e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.DownArrow)
            {
                if (_hoverIndex < 0)
                {
                    _hoverIndex = Mathf.Max(0, _selected);
                }
                _hoverIndex += (e.keyCode == KeyCode.DownArrow) ? 1 : -1;
                _hoverIndex = Mathf.Clamp(_hoverIndex, 0, _rows.Count - 1);
                UpdateHoverVisuals();
                EnsureVisible(_hoverIndex);
                e.StopPropagation();
                return;
            }
        }

        // Updates the row backgrounds based on hover state.
        private void UpdateHoverVisuals()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                row.style.backgroundColor = (i == _hoverIndex) ? new StyleColor(gui.ColorScheme.HOVER_WHITE) : StyleKeyword.Null;
            }
        }

        // Ensures the hovered item is visible within the scroll view by adjusting the scroll offset.
        private void EnsureVisible(int index)
        {
            if (index < 0 || index >= _rows.Count)
            {
                return;
            }
            var row = _rows[index];
            var rowY = row.worldBound.yMin;
            var rowH = row.worldBound.height;
            var scrollY = _scroll.worldBound.yMin;
            var scrollH = _scroll.worldBound.height;
            if (rowY < scrollY)
            {
                _scroll.scrollOffset = new Vector2(0, _scroll.scrollOffset.y - (scrollY - rowY));
            }
            else if (rowY + rowH > scrollY + scrollH)
            {
                _scroll.scrollOffset = new Vector2(0, _scroll.scrollOffset.y + (rowY + rowH - (scrollY + scrollH)));
            }
        }

        // Invokes the pick callback and closes the overlay.
        private void Pick(int index)
        {
            _onPick?.Invoke(index);
            Close();
        }

        // Positions the overlay relative to the anchor and ensures it fits within the root.
        private void Reposition()
        {
            if (_activeRoot == null || _activeAnchor == null)
            {
                return;
            }
            style.minWidth = _activeAnchor.worldBound.width;
            Vector2 worldTopLeft = new Vector2(_activeAnchor.worldBound.xMin, _activeAnchor.worldBound.yMin);
            Vector2 local = _activeRoot.WorldToLocal(worldTopLeft);
            float popupHeight = resolvedStyle.height;
            if (float.IsNaN(popupHeight) || popupHeight <= 0f)
            {
                popupHeight = DAI_UitkConstants.MarginPadding + DAI_UitkConstants.MarginPadding + DAI_UitkConstants.ButtonHeight * _rows.Count;
                popupHeight = Mathf.Min(popupHeight, DAI_UitkConstants.MaxHeight);
            }
            float anchorH = _activeAnchor.resolvedStyle.height;
            float desiredBelowTop = local.y + anchorH + DAI_UitkConstants.GapYBelow;
            float desiredAboveTop = local.y - popupHeight - DAI_UitkConstants.GapYAbove;
            float rootH = _activeRoot.resolvedStyle.height;
            bool canShowBelow = desiredBelowTop + popupHeight <= rootH + DAI_UitkConstants.HoverOffset;
            float top = canShowBelow ? desiredBelowTop : Mathf.Max(0f, desiredAboveTop);
            style.left = local.x;
            style.top = top;
        }

        // Closes the overlay and cleans up event handlers.
        private void Close()
        {
            if (_activeRoot != null)
            {
                if (_overlay != null)
                {
                    try
                    {
                        _activeRoot.Remove(_overlay);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex); 
                    }                 
                }

                if (_onRootGeometryChanged != null)
                {
                    _activeRoot.UnregisterCallback(_onRootGeometryChanged);
                }
            }

            UnregisterCallback<DetachFromPanelEvent>(_onPopupDetached);
            RemoveFromHierarchy();

            if (_active == this)
            {
                _active = null;
                _activeRoot = null;
                _activeAnchor = null;
            }
        }

        public static void Show(
            VisualElement root,
            VisualElement anchor,
            List<string> options,
            int selected,
            Action<int> onPick,
            DAInspectorUITK gui,
            DropdownPlacement placement = DropdownPlacement.Auto)
        {
            if (_active != null && _activeAnchor == anchor)
            {
                _active.Close();
                return;
            }
            if (_active != null)
            {
                _active.Close();
            }

            var popup = new OverlayDropdown(gui, options, selected, onPick);
            _active = popup;
            _activeRoot = root;
            _activeAnchor = anchor;

            popup._overlay = new VisualElement
            {
                pickingMode = PickingMode.Position,
                style =
                {
                    position = Position.Absolute,
                    left = 0f,
                    top = 0f,
                    right = 0f,
                    bottom = 0f,
                    backgroundColor = gui.ColorScheme.CLEAR
                }
            };

            popup._overlay.RegisterCallback<PointerDownEvent>((PointerDownEvent e) =>
            {
                popup.Close();
                e.StopPropagation();
            });

            popup._onRootGeometryChanged = (GeometryChangedEvent _) => popup.Reposition(placement);
            root.RegisterCallback(popup._onRootGeometryChanged);

            popup._onPopupDetached = (DetachFromPanelEvent _) => popup.Close();
            popup.RegisterCallback(popup._onPopupDetached);

            root.Add(popup._overlay);
            root.Add(popup);

            void PositionPopup()
            {
                popup.style.display = DisplayStyle.Flex;
                popup.Reposition(placement);
            }

            popup.style.display = DisplayStyle.None;
            root.schedule.Execute(PositionPopup).ExecuteLater(1);
        }

        private void Reposition(DropdownPlacement placement)
        {
            if (_activeRoot == null || _activeAnchor == null)
                return;

            style.minWidth = _activeAnchor.worldBound.width;

            Vector2 worldTopLeft = new Vector2(_activeAnchor.worldBound.xMin, _activeAnchor.worldBound.yMin);
            Vector2 local = _activeRoot.WorldToLocal(worldTopLeft);

            float popupHeight = resolvedStyle.height;
            if (float.IsNaN(popupHeight) || popupHeight <= 0f)
            {
                popupHeight = DAI_UitkConstants.MarginPadding + DAI_UitkConstants.MarginPadding +
                              DAI_UitkConstants.ButtonHeight * _rows.Count;
                popupHeight = Mathf.Min(popupHeight, DAI_UitkConstants.MaxHeight);
            }

            float popupWidth = resolvedStyle.width;
            if (float.IsNaN(popupWidth) || popupWidth <= 0f)
            {
                popupWidth = Mathf.Max(_activeAnchor.worldBound.width, 0f);
            }

            float anchorW = _activeAnchor.worldBound.width;
            float anchorH = _activeAnchor.worldBound.height;

            if (placement == DropdownPlacement.Auto)
            {
                float desiredBelowTop = local.y + anchorH + DAI_UitkConstants.GapYBelow;
                float desiredAboveTop = local.y - popupHeight - DAI_UitkConstants.GapYAbove;
                float rootH = _activeRoot.resolvedStyle.height;

                bool canShowBelow = desiredBelowTop + popupHeight <= rootH + DAI_UitkConstants.HoverOffset;
                float top = canShowBelow ? desiredBelowTop : Mathf.Max(0f, desiredAboveTop);

                style.left = local.x;
                style.top = top;
                return;
            }

            // Explicit placement schemes:
            switch (placement)
            {
                case DropdownPlacement.TopLeft:
                    // The bottom-right corner of the popup touches the top-left corner of the anchor
                    style.left = local.x - popupWidth;
                    style.top = local.y - popupHeight;
                    break;

                case DropdownPlacement.TopRight:
                    // The bottom-left corner of the popup touches the top-right corner of the anchor
                    style.left = local.x + anchorW;
                    style.top = local.y - popupHeight;
                    break;

                case DropdownPlacement.BottomLeft:
                    // The top-right corner of the popup touches the bottom-left corner of the anchor
                    style.left = local.x - popupWidth;
                    style.top = local.y + anchorH;
                    break;

                case DropdownPlacement.BottomRight:
                    // The top-left corner of the popup touches the bottom-right corner of the anchor
                    style.left = local.x + anchorW;
                    style.top = local.y + anchorH;
                    break;
            }
        }
    }

    public enum DropdownPlacement { Auto, TopLeft, TopRight, BottomLeft, BottomRight }
}