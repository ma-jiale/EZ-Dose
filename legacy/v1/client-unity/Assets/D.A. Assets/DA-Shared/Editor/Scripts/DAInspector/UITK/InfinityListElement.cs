using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace DA_Assets.DAI.UITK
{
    public class InfinityListElement<T> : VisualElement
    {
        private readonly VisualElement _root;
        private readonly VisualElement _searchRow;
        private readonly TextField _searchField;
        private readonly Button _clearBtn;
        private readonly ListView _list;

        private readonly List<T> _items = new();
        private readonly List<T> _view = new();

        private readonly float _itemHeight;
        private int _visibleItemCount;
        private string _filterFieldPath;
        private Func<T, object> _filterGetter;
        private string _query = string.Empty;

        private Func<VisualElement> _makeItem;
        private Action<VisualElement, int, T> _bindItem;
        private Action<VisualElement, int> _unbindItem;

        public int VisibleItemCount
        {
            get => _visibleItemCount;
            set
            {
                _visibleItemCount = Mathf.Max(1, value);
                _list.style.height = _visibleItemCount * _itemHeight;
            }
        }

        public InfinityListElement(float itemHeight, int visibleItemCount = 10, string filterFieldPath = "Name", DAInspectorUITK uitk = null)
        {
            _itemHeight = Mathf.Max(1f, itemHeight);
            _visibleItemCount = Mathf.Max(1, visibleItemCount);
            _filterFieldPath = string.IsNullOrWhiteSpace(filterFieldPath) ? "Name" : filterFieldPath;

            _root = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Column,
                    flexGrow = 1
                }
            };
            Add(_root);

            _searchRow = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    //gap = 6,
                    marginBottom = 4
                }
            };
            _root.Add(_searchRow);

            if (uitk != null)
                _searchField = uitk.TextField("Search");
            else
                _searchField = new TextField("Search");
            _searchField.style.flexGrow = 1;
            _searchField.RegisterValueChangedCallback(OnSearchChanged);
            _searchRow.Add(_searchField);

            _clearBtn = new Button(() =>
            {
                _searchField.SetValueWithoutNotify(string.Empty);
                _query = string.Empty;
                RefilterAndRebind();
                _searchField.Blur();
            })
            {
                text = "✕"
            };
            _searchRow.Add(_clearBtn);

            _list = new ListView
            {
                itemsSource = _view,
                selectionType = SelectionType.None,
                fixedItemHeight = _itemHeight,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                style =
                {
                    height = _visibleItemCount * _itemHeight,
                    flexShrink = 0
                }
            };
            _list.makeItem = MakeRowDefault;
            _list.bindItem = BindRowDefault;
            _list.unbindItem = UnbindRowDefault;
            _root.Add(_list);

            ResolveFilterGetter(typeof(T), _filterFieldPath);
        }

        public void Configure(Func<VisualElement> makeItem, Action<VisualElement, int, T> bindItem, Action<VisualElement, int> unbindItem = null)
        {
            _makeItem = makeItem;
            _bindItem = bindItem;
            _unbindItem = unbindItem;
            _list.makeItem = _makeItem ?? MakeRowDefault;
            _list.bindItem = _bindItem != null ? BindRowUser : BindRowDefault;
            _list.unbindItem = _unbindItem != null ? UnbindRowUser : UnbindRowDefault;
            _list.Rebuild();
        }

        public void SetItems(IEnumerable<T> items)
        {
            _items.Clear();
            if (items != null) _items.AddRange(items);
            RefilterAndRebind();
        }

        public void SetFilterPath(string path)
        {
            _filterFieldPath = string.IsNullOrWhiteSpace(path) ? "Name" : path;
            ResolveFilterGetter(typeof(T), _filterFieldPath);
            RefilterAndRebind();
        }

        public void SetSearch(string query)
        {
            _searchField.SetValueWithoutNotify(query ?? string.Empty);
            _query = _searchField.value;
            RefilterAndRebind();
        }

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            _query = evt.newValue ?? string.Empty;
            RefilterAndRebind();
        }

        private void RefilterAndRebind()
        {
            _view.Clear();
            if (string.IsNullOrWhiteSpace(_query) || _filterGetter == null)
            {
                _view.AddRange(_items);
            }
            else
            {
                var q = _query.Trim();
                for (int i = 0; i < _items.Count; i++)
                {
                    var v = _filterGetter(_items[i]);
                    if (v == null) continue;
                    var s = v.ToString();
                    if (string.IsNullOrEmpty(s)) continue;
                    if (s.IndexOf(q, StringComparison.InvariantCultureIgnoreCase) >= 0)
                        _view.Add(_items[i]);
                }
            }
            _list.Rebuild();
        }

        private VisualElement MakeRowDefault()
        {
            return new Label
            {
                style =
                {
                    unityTextAlign = TextAnchor.MiddleLeft,
                    paddingLeft = 6,
                    paddingRight = 6
                }
            };
        }

        private void BindRowDefault(VisualElement element, int index)
        {
            if ((uint)index >= (uint)_view.Count) return;
            var data = _view[index];
            var label = element as Label;
            var text = _filterGetter?.Invoke(data)?.ToString() ?? data?.ToString() ?? string.Empty;
            label.text = text;
        }

        private void UnbindRowDefault(VisualElement element, int index)
        {
        }

        private void BindRowUser(VisualElement element, int index)
        {
            if ((uint)index >= (uint)_view.Count) return;
            _bindItem(element, index, _view[index]);
        }

        private void UnbindRowUser(VisualElement element, int index)
        {
            _unbindItem(element, index);
        }

        private void ResolveFilterGetter(Type rootType, string path)
        {
            _filterGetter = null;
            if (string.IsNullOrWhiteSpace(path)) return;
            if (TryCreateMemberPathAccessor(rootType, path, out var accessor))
                _filterGetter = (T item) => accessor.GetValue(item);
        }

        private static bool TryCreateMemberPathAccessor(Type rootType, string path, out MemberPathAccessor accessor)
        {
            accessor = null;
            var parts = path.Split('.');
            var members = new List<MemberInfo>();
            var t = rootType;
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.IgnoreCase;
            for (int i = 0; i < parts.Length; i++)
            {
                var p = t.GetProperty(parts[i], flags);
                if (p != null)
                {
                    members.Add(p);
                    t = p.PropertyType;
                    continue;
                }
                var f = t.GetField(parts[i], flags);
                if (f != null)
                {
                    members.Add(f);
                    t = f.FieldType;
                    continue;
                }
                return false;
            }
            accessor = new MemberPathAccessor(members.ToArray());
            return true;
        }

        private sealed class MemberPathAccessor
        {
            private readonly MemberInfo[] _members;

            public MemberPathAccessor(MemberInfo[] members)
            {
                _members = members;
            }

            public object GetValue(object obj)
            {
                var cur = obj;
                for (int i = 0; i < _members.Length; i++)
                {
                    if (cur == null) return null;
                    var m = _members[i];
                    if (m is PropertyInfo pi) cur = pi.GetValue(cur, null);
                    else if (m is FieldInfo fi) cur = fi.GetValue(cur);
                    else return null;
                }
                return cur;
            }
        }
    }
}
