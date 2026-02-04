using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.DAI
{
    public enum DropdownWidthMode
    {
        Fixed,
        Stretch
    }

    public sealed class DA_Dropdown<T> : VisualElement, INotifyValueChanged<T>
    {
        private DAInspectorUITK gui;

        private readonly Label _labelElement;
        private readonly Button _button;
        private readonly Label _selectedLabel;
        private readonly Label _arrowLabel;

        private readonly DropdownWidthMode _labelWidthMode;
        private readonly float _labelFixedWidthPx;
        private readonly DropdownWidthMode _dropdownWidthMode;
        private readonly float _dropdownFixedWidthPx;

        private List<T> _choices = new List<T>();
        private int _index = -1;
        private T _value;

        private Func<T, string> _toString = v => v?.ToString() ?? string.Empty;
        private IEqualityComparer<T> _comparer = EqualityComparer<T>.Default;

        public event Action<T> ValueChanged;

        public bool AllowCustomValue { get; set; } = false;

        public DA_Dropdown(
            DAInspectorUITK gui,
            string labelText,
            DropdownWidthMode labelWidthMode,
            float labelFixedWidthPx,
            DropdownWidthMode dropdownWidthMode,
            float dropdownFixedWidthPx)
        {
            this.gui = gui;

            _labelWidthMode = labelWidthMode;
            _labelFixedWidthPx = Mathf.Max(0f, labelFixedWidthPx);
            _dropdownWidthMode = dropdownWidthMode;
            _dropdownFixedWidthPx = Mathf.Max(0f, dropdownFixedWidthPx);

            style.flexDirection = FlexDirection.Row;
            style.alignItems = Align.Center;

            if (!string.IsNullOrEmpty(labelText))
            {
                _labelElement = new Label(labelText)
                {
                    style =
                    {
                        marginRight = DAI_UitkConstants.MarginPadding / 2f
                    }
                };
                ApplyLabelWidthMode(_labelElement);
                Add(_labelElement);
            }

            _button = new Button
            {
                style =
                {
                    height = DAI_UitkConstants.ButtonHeight,
                    paddingLeft = DAI_UitkConstants.MarginPadding,
                    paddingRight = DAI_UitkConstants.MarginPadding,
                    unityTextAlign = TextAnchor.MiddleLeft,
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    backgroundColor = gui.ColorScheme.GROUP,
                    marginLeft = 0f,
                    marginRight = 0f
                }
            };

            UIHelpers.SetRadius(_button, DAI_UitkConstants.CornerRadius);
            UIHelpers.SetBorderWidth(_button, DAI_UitkConstants.BorderWidth);
            UIHelpers.SetBorderColor(_button, gui.ColorScheme.OUTLINE);

            _selectedLabel = new Label("—")
            {
                style =
                {
                    flexGrow = 1f,
                    flexShrink = 1f,
                    fontSize = DAI_UitkConstants.FontSizeNormal,
                    unityFontStyleAndWeight = FontStyle.Normal,
                    marginRight = DAI_UitkConstants.MarginPadding / 2f
                }
            };
            DropdownFontUtil.ApplyTo(_selectedLabel);

            _arrowLabel = new Label("▼")
            {
                style =
                {
                    flexShrink = 0f,
                    color = new StyleColor(gui.ColorScheme.ARROW_GRAY),
                    fontSize = DAI_UitkConstants.FontSizeNormal,
                    marginLeft = DAI_UitkConstants.MarginPadding / 2f
                }
            };
            DropdownFontUtil.ApplyTo(_arrowLabel);

            _button.Add(_selectedLabel);
            _button.Add(_arrowLabel);

            _button.tooltip = "Open dropdown";
            _button.focusable = true;
            _button.clicked += OnClicked;

            Add(_button);

            ApplyDropdownWidthMode();

            SetValueWithoutNotify(default);
            UpdateSelectedLabel();
        }

        public List<T> choices
        {
            get => _choices;
            set
            {
                _choices = value ?? new List<T>();

                if (!_choices.Any())
                {
                    SetValueWithoutNotify(default);
                    _index = -1;
                    UpdateSelectedLabel();
                    return;
                }

                SyncIndexFromValue();

                if (_index == -1 && !AllowCustomValue)
                {
                    _index = -1;
                    SetValueWithoutNotify(default);
                }

                UpdateSelectedLabel();
            }
        }

        public int index
        {
            get => _index;
            set
            {
                int clamped = Mathf.Clamp(value, -1, _choices.Count - 1);
                if (clamped == -1)
                {
                    SetValueCore(default, true);
                    return;
                }
                SetValueCore(_choices[clamped], true);
            }
        }

        public T value
        {
            get => _value;
            set => SetValueCore(value, true);
        }

        public void SetValueWithoutNotify(T newValue)
        {
            SetValueCore(newValue, false);
        }

        public void SetToString(Func<T, string> toString)
        {
            _toString = toString ?? (v => v?.ToString() ?? string.Empty);
            UpdateSelectedLabel();
        }

        public void SetComparer(IEqualityComparer<T> comparer)
        {
            _comparer = comparer ?? EqualityComparer<T>.Default;
            SyncIndexFromValue();
            UpdateSelectedLabel();
        }

        public void RegisterValueChangedCallback(EventCallback<ChangeEvent<T>> callback)
        {
            RegisterCallback(callback);
        }

        public void UnregisterValueChangedCallback(EventCallback<ChangeEvent<T>> callback)
        {
            UnregisterCallback(callback);
        }

        private void ApplyLabelWidthMode(Label label)
        {
            label.style.alignSelf = Align.Center;
            if (_labelWidthMode == DropdownWidthMode.Stretch)
            {
                label.style.flexGrow = 1f;
                label.style.flexShrink = 1f;
                label.style.width = StyleKeyword.Auto;
            }
            else
            {
                label.style.flexGrow = 0f;
                label.style.flexShrink = 0f;
                label.style.width = PxOrAuto(_labelFixedWidthPx);
            }
        }

        private static StyleLength PxOrAuto(float px)
        {
            return (px > 0f)
                ? new StyleLength(new Length(px, LengthUnit.Pixel))
                : new StyleLength(StyleKeyword.Auto);
        }

        private void ApplyDropdownWidthMode()
        {
            if (_dropdownWidthMode == DropdownWidthMode.Stretch)
            {
                style.alignSelf = Align.Stretch;
                style.flexGrow = 1f;
                style.flexShrink = 1f;
                style.width = StyleKeyword.Auto;

                _button.style.flexGrow = 1f;
                _button.style.flexShrink = 1f;
                _button.style.width = Length.Percent(100);
            }
            else
            {
                style.alignSelf = Align.FlexStart;
                style.flexGrow = 0f;
                style.flexShrink = 0f;          
                style.width = PxOrAuto(_dropdownFixedWidthPx);
                _button.style.flexGrow = 0f;
                _button.style.flexShrink = 0f;
                _button.style.width = Length.Percent(100);
            }
        }

        private void SetValueCore(T newValue, bool notify)
        {
            if (_comparer.Equals(_value, newValue))
            {
                return;
            }

            var previous = _value;
            _value = newValue;
            SyncIndexFromValue();
            UpdateSelectedLabel();

            if (notify)
            {
                using (var evt = ChangeEvent<T>.GetPooled(previous, _value))
                {
                    evt.target = this;
                    SendEvent(evt);
                }
                ValueChanged?.Invoke(_value);
            }
        }

        private void SyncIndexFromValue()
        {
            if (_choices == null || _choices.Count == 0)
            {
                _index = -1;
                return;
            }
            int found = _choices.FindIndex(c => _comparer.Equals(c, _value));
            _index = found;
        }

        private void UpdateSelectedLabel()
        {
            bool hasSelection = (uint)_index < (uint)_choices.Count;
            bool hasCustom = AllowCustomValue && !EqualityComparer<T>.Default.Equals(_value, default);

            string candidate = hasSelection
                ? _toString(_choices[_index])
                : hasCustom ? _toString(_value) : null;

            _selectedLabel.text = (hasSelection || hasCustom)
                ? (string.IsNullOrEmpty(candidate) ? "—" : candidate)
                : "Select…";
        }

        private void OnClicked()
        {
            if (_choices == null || _choices.Count == 0)
            {
                return;
            }
            VisualElement root = _button.panel?.visualTree;
            if (root == null)
            {
                return;
            }
            var options = new List<string>(_choices.Count);
            for (int i = 0; i < _choices.Count; i++)
            {
                options.Add(_toString(_choices[i]));
            }
            int selected = _index;
            OverlayDropdown.Show(
                root,
                _button,
                options,
                selected,
                idx =>
                {
                    if (idx < 0 || idx >= _choices.Count)
                    {
                        SetValueCore(default, true);
                        return;
                    }
                    SetValueCore(_choices[idx], true);
                },
                gui);
        }
    }

    static class DropdownFontUtil
    {
        private static Font GetDefaultFont()
        {
            var inter = UnityEditor.EditorGUIUtility.Load("Fonts/Inter/Inter-Regular.ttf") as Font;

            if (inter != null)
                return inter;

#if UNITY_2022_3_OR_NEWER
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
#else
            return Resources.GetBuiltinResource<Font>("Arial.ttf");
#endif
        }

        public static void ApplyTo(TextElement el)
        {
            var font = GetDefaultFont();
            el.style.unityFont = font;
#if UNITY_2022_2_OR_NEWER
            el.style.unityFontDefinition = new FontDefinition 
            { 
                font = font 
            };
#endif
        }
    }
}