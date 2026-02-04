// Generic Unity Editor window that displays a scrollable list with search/filter support.
// Optimized for large datasets using virtualized rendering.
// Allows filtering items by a nested field/property path like "Object.Name".

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace DA_Assets.DAI
{
    public delegate void DrawItem<T>(T item);

    public class InfinityScrollRectWindow<T>
    {
        // Custom inspector styling/utilities
        [SerializeField] DAInspector gui;

        // Cached item array to display
        private T[] _items;

        // Current scroll offset
        private Vector2 _scrollPosition;

        // Number of items visible at once
        protected int _visibleItemCount;

        // Height of each item
        protected float _itemHeight;

        // Total scrollable height based on item count
        private float _totalScrollHeight;

        // Height of the scroll viewport
        private float _visibleAreaHeight;

        // Callback to render individual items
        private DrawItem<T> _drawItem;

        // Dot-delimited field/property path for filtering
        private readonly string _filterPath;

        // Getter function for extracting the filter value
        private Func<T, object> _valueGetter;

        // Current search input
        private string _searchText = string.Empty;

        // Min items before search UI is shown
        private int _searchAppearItemsCount = 12;

        // Constructor sets layout parameters and resolves filter accessor
        public InfinityScrollRectWindow(
            int visibleItemCount, 
            float itemHeight, 
            DAInspector gui, 
            string filterFieldPath = "name")
        {
            this.gui = gui;
            _visibleItemCount = visibleItemCount;
            _itemHeight = itemHeight;
            _filterPath = filterFieldPath;

            // Tries to create a value getter for the target path — disables search if path is invalid
            if (MemberPathCache.TryGetOrCreate(typeof(T), _filterPath, out var accessor))
            {
                _valueGetter = (T item) => accessor.GetValue(item);
            }
            else
            {
                _valueGetter = null;
            }
        }

        // Assigns item data and drawing logic — recalculates layout
        public void SetData(IEnumerable<T> items, DrawItem<T> drawItem)
        {
            _drawItem = drawItem;
            _items = items?.ToArray() ?? Array.Empty<T>();

            if (_items.Length < _visibleItemCount)
                _visibleItemCount = _items.Length;

            _visibleAreaHeight = _visibleItemCount * _itemHeight;
            _totalScrollHeight = _items.Length * _itemHeight;
        }

        // Renders the window content including optional search bar and scroll virtualization
        public void OnGUI()
        {
            if (_items == null || _items.Length < 1)
            {
                GUILayout.Label("No data.");
                return;
            }

            if (_drawItem == null)
            {
                GUILayout.Label("DrawItem is missing.");
                return;
            }

            bool canSearch = _valueGetter != null;

            // Show search bar only if accessor is valid and item count exceeds threshold
            if (canSearch && _items.Length >= _searchAppearItemsCount)
            {
                gui.Colorize(() =>
                {
                    gui.Space10();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();

                    _searchText = EditorGUILayout.TextField("Search", _searchText);
                    if (GUILayout.Button("✕", GUILayout.Width(22)))
                    {
                        _searchText = string.Empty;
                        GUI.FocusControl(null);
                        _scrollPosition = Vector2.zero;
                    }

                    if (EditorGUI.EndChangeCheck())
                        _scrollPosition = Vector2.zero;

                    EditorGUILayout.EndHorizontal();
                    gui.Space5();
                });
            }

            T[] targetItems = _items;

            // Apply filtering if search is active and accessor is valid
            if (canSearch && !string.IsNullOrWhiteSpace(_searchText))
            {
                string q = _searchText.Trim();

                targetItems = _items.Where(item =>
                {
                    var v = _valueGetter(item);

                    if (v == null) 
                        return false;

                    string s = v.ToString();

                    if (string.IsNullOrEmpty(s))
                        return false;

                    return s.IndexOf(q, StringComparison.InvariantCultureIgnoreCase) >= 0;
                }).ToArray();
            }

            if (targetItems.Length == 0)
            {
                GUILayout.Label("Nothing matches.");
                return;
            }

            _totalScrollHeight = targetItems.Length * _itemHeight;

            gui.Colorize(() =>
            {
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(_visibleAreaHeight));
            });

            // Calculate which items to draw based on scroll position
            float currentScrollPos = _scrollPosition.y;
            int startIndex = Mathf.Max(0, (int)(currentScrollPos / _itemHeight));
            int endIndex = Mathf.Min(targetItems.Length, startIndex + _visibleItemCount + 3); // extra padding

            GUILayout.BeginVertical();
            GUILayout.Space(startIndex * _itemHeight); // offset before first visible item

            for (int i = startIndex; i < endIndex; i++)
                _drawItem(targetItems[i]);

            GUILayout.Space(_totalScrollHeight - endIndex * _itemHeight); // bottom padding
            GUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
        }
    }

    // Utility that resolves a nested property/field access chain from a dot-path like "Object.Name".
    // Supports any depth and mix of fields/properties. Uses reflection.
    internal sealed class MemberPathAccessor
    {
        private readonly MemberInfo[] _members; // Sequence of field/property infos to walk

        private MemberPathAccessor(MemberInfo[] members)
        {
            _members = members;
        }

        // Attempts to parse a dot-delimited path (e.g. "Object.Name") from a given root type
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

            // Walk through each path segment and resolve field/property info
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

                return false; // Segment not found
            }

            accessor = new MemberPathAccessor(members.ToArray());
            return true;
        }

        // Retrieves the final value from the root object by traversing all members
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

    // Simple static cache for storing resolved MemberPathAccessor instances.
    // Reduces overhead by avoiding redundant reflection lookups for the same path/type.
    internal static class MemberPathCache
    {
        private static readonly Dictionary<(Type, string), MemberPathAccessor> Cache =
            new Dictionary<(Type, string), MemberPathAccessor>();

        // Retrieves existing accessor or builds one if not cached
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

            return false; // Could not resolve path
        }
    }
}