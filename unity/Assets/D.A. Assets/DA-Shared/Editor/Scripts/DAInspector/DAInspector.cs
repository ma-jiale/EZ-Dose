using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using UnityEditor;
using UnityEngine;
using DA_Assets.Shared.Extensions;
using Debug = UnityEngine.Debug;

#pragma warning disable CS0649

namespace DA_Assets.DAI
{
    [CreateAssetMenu(fileName = "DAInspector", menuName = "Custom/DAInspector")]
    [Serializable]
    public class DAInspector : ScriptableObject
    {
        private Dictionary<string, GroupData> _groupDatas = new Dictionary<string, GroupData>();

        [SerializeField] DAI.InspectorResources _resources;
        public DAI.InspectorResources Resources => _resources;

        [SerializeField] DaiStyle _style;
        [SerializeField] ColorScheme _colorScheme;

        public DaiStyle ColoredStyle => _style;

        public void DrawGroup(Group group)
        {
            if (group.GroupType == GroupType.Horizontal)
            {
                if (group.Style != null)
                {
                    GUILayout.BeginHorizontal(group.Style, group.Options);
                }
                else
                {
                    GUILayout.BeginHorizontal(group.Options);
                }

                if (group.Flexible)
                    FlexibleSpace();

                group.Body.Invoke();

                if (group.Flexible)
                    FlexibleSpace();

                GUILayout.EndHorizontal();
            }
            else if (group.GroupType == GroupType.Vertical)
            {
                if (group.Style != null)
                {
                    GUILayout.BeginVertical(group.Style, group.Options);
                }
                else
                {
                    GUILayout.BeginVertical(group.Options);
                }

                if (group.Scroll)
                {
                    StackFrame sf = new StackFrame(1, true);
                    BeginScroll(group, sf);
                }

                if (group.Flexible)
                    FlexibleSpace();

                group.Body.Invoke();

                if (group.Flexible)
                    FlexibleSpace();

                if (group.Scroll)
                {
                    EndScroll();
                }

                GUILayout.EndVertical();
            }
            else if (group.GroupType == GroupType.Fade)
            {
                if (EditorGUILayout.BeginFadeGroup(group.Fade.faded))
                {
                    if (group.Flexible)
                        FlexibleSpace();

                    group.Body.Invoke();

                    if (group.Flexible)
                        FlexibleSpace();
                }

                EditorGUILayout.EndFadeGroup();
            }
            else
            {
                                Debug.LogError(SharedLocKey.log_unknown_group_type.Localize());
            }
        }

        public bool CheckBox(GUIContent label, bool value, bool rightSide = true, Action onClick = null, Action onValueChange = null, bool autoDarkColorize = true)
        {
            bool _value = false;

            DrawGroup(new Group
            {
                Style = _style.CheckBoxField,
                GroupType = GroupType.Horizontal,
                Body = () =>
                {
                    if (rightSide)
                    {
                        Btn();
                    }

                    Rect rect = GUILayoutUtility.GetRect(width: 25, height: 25);

                    if (autoDarkColorize)
                    {
                        Colorize(() =>
                        {
                            _value = EditorGUI.Toggle(
                                rect,
                                value,
                                EditorStyles.toggle);
                        });
                    }
                    else
                    {
                        _value = EditorGUI.Toggle(
                            rect,
                            value,
                            EditorStyles.toggle);
                    }

                    if (!rightSide)
                    {
                        Btn();
                    }
                }
            });

            void Btn()
            {
                GUIStyle lblStyle = _style.CheckBoxLabel;

                if (EditorGUIUtility.isProSkin)
                {
                    lblStyle.normal.textColor = Color.white;        
                }
                else
                {
                    lblStyle.normal.textColor = Color.black;
                }

                if (GUILayout.Button(label, lblStyle))
                {
                    if (onClick == null)
                    {
                        value = !value;
                        if (onValueChange != null)
                        {
                            onValueChange.Invoke();
                        }
                    }
                    else
                    {
                        onClick.Invoke();
                    }
                }

                GUILayout.FlexibleSpace();
            }

            return _value;
        }

        public void BeginScroll(Group group, StackFrame sf)
        {
            string methodPath = GetMethodPath(sf);
            string unicumId = $"{methodPath}-{group.InstanceId}";

            if (_groupDatas.TryGetValue(unicumId, out GroupData gd) == false)
            {
                gd = new GroupData();
                _groupDatas.Add(unicumId, gd);
            }

            Colorize(() =>
            {
                gd.ScrollPosition = EditorGUILayout.BeginScrollView(gd.ScrollPosition, false, false);
            });
        }

        public void EndScroll()
        {
            EditorGUILayout.EndScrollView();
        }

        public string GetMethodPath(StackFrame frame)
        {
            var method = frame.GetMethod();
            string className = method.DeclaringType.Name;
            int lineNumber = frame.GetFileLineNumber();
            return $"{className}-{lineNumber}";
        }

        public void Space10() => GUILayout.Space(10);
        public void Space5() => GUILayout.Space(5);
        public void Space(float pixels) => GUILayout.Space(pixels);
        public void FlexibleSpace() => GUILayout.FlexibleSpace();

        private SerializedProperty GetPropertyRecursive(string[] names, int index, SerializedProperty property)
        {
            if (index >= names.Length)
            {
                return property;
            }
            else
            {
                string fieldName = names[index];
                SerializedProperty rprop = property.FindPropertyRelative(fieldName);
                return GetPropertyRecursive(names, index + 1, rprop);
            }
        }

        public static string GetFieldName<T>(Expression<Func<T, object>> pathExpression)
        {
            string[] fields = pathExpression.GetFieldsArray();
            return fields.Last();
        }

        public void SerializedPropertyField<T>(SerializedObject so, Expression<Func<T, object>> pathExpression, bool? isExpanded = null, Color? overrideColor = null)
        {
            string[] fields = pathExpression.GetFieldsArray();

            DrawGroup(new Group
            {
                GroupType = GroupType.Horizontal,
                Body = () =>
                {
                    Space(14);

                    DrawGroup(new Group
                    {
                        GroupType = GroupType.Vertical,
                        Body = () =>
                        {
                            SerializedProperty rootProperty = null;

                            try
                            {
                                rootProperty = so.FindProperty(fields[0]);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning(ex.Message);
                                return;
                            }

                            SerializedProperty lastProperty = null;

                            try
                            {
                                lastProperty = GetPropertyRecursive(fields, 1, rootProperty);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning(ex.Message);
                                return;
                            }

                            if (isExpanded != null)
                            {
                                lastProperty.isExpanded = (bool)isExpanded;
                            }

                            so.Update();

                            if (overrideColor == null)
                            {
                                GUI.backgroundColor = _colorScheme.UnityGuiColor;
                            }
                            else
                            {
                                GUI.backgroundColor = overrideColor.Value;
                            }

                            EditorGUI.indentLevel--;
                            EditorGUILayout.PropertyField(lastProperty, true);
                            EditorGUI.indentLevel++;
                            GUI.backgroundColor = Color.white;

                            so.ApplyModifiedProperties();
                        }
                    });
                }
            });
        }

        public void Colorize(Action action)
        {
            if (EditorGUIUtility.isProSkin)
            {
                GUI.backgroundColor = _colorScheme.UnityGuiColor;
                action.Invoke();
                GUI.backgroundColor = Color.white;
            }
            else
            {
                action.Invoke();
            }
        }
    }
}
