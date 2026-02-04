using DA_Assets.FCU.Extensions;
using DA_Assets.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Reflection;
using DA_Assets.Logging;
using DA_Assets.Tools;
using DA_Assets.FCU.Model;
using DA_Assets.FCU.Attributes;
using UnityEngine.Events;

#if ULB_EXISTS
using DA_Assets.ULB;
#endif

#if TextMeshPro
using TMPro;
#endif

#if DABUTTON_EXISTS
using DA_Assets.DAB;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace DA_Assets.FCU
{
    [Serializable]
    public class ScriptGenerator : FcuBase
    {
        public void GenerateScripts()
        {
            _ = GenerateScriptsAsync(null);
        }

        public void GenerateScripts(ScriptGeneratorSelectionContext context)
        {
            _ = GenerateScriptsAsync(context);
        }

        internal void Serialize()
        {
            _ = SerializeAsync(CreateSelectionContext());
        }

        internal void Serialize(ScriptGeneratorSelectionContext context)
        {
            _ = SerializeAsync(context);
        }

        public ScriptGeneratorSelectionContext CreateSelectionContext()
        {
            var context = new ScriptGeneratorSelectionContext();
            List<GroupedSyncHelpers> rootFrameGroups = BuildRootFrameGroups(null, out SyncHelper[] syncHelpers);

            if (rootFrameGroups == null || rootFrameGroups.Count == 0)
            {
                context.SetFrames(Array.Empty<ScriptGeneratorFrameSelection>());
                return context;
            }

            List<ScriptGeneratorFrameSelection> frames = new List<ScriptGeneratorFrameSelection>(rootFrameGroups.Count);

            foreach (GroupedSyncHelpers group in rootFrameGroups)
            {
                if (group.RootFrame == null)
                {
                    continue;
                }

                var frameSelection = new ScriptGeneratorFrameSelection(group.RootFrame)
                {
                    SearchSignature = BuildFrameSearchSignature(group.RootFrame)
                };

                foreach (SyncHelper helper in group.SyncHelpers)
                {
                    Type componentType = DetermineComponentType(helper);
                    string componentName = componentType?.Name ?? nameof(GameObject);

                    var fieldSelection = new ScriptGeneratorFieldSelection(helper)
                    {
                        ComponentTypeName = componentName,
                        ComponentType = componentType ?? typeof(GameObject),
                        SearchSignature = BuildFieldSearchSignature(helper, componentName)
                    };

                    if (helper.ContainsTag(FcuTag.Button))
                    {
                        fieldSelection.MethodSelection = new ScriptGeneratorMethodSelection(helper);
                    }

                    frameSelection.Fields.Add(fieldSelection);
                }

                frames.Add(frameSelection);
            }

            context.SetFrames(frames);
            return context;
        }

        private async Task SerializeAsync(ScriptGeneratorSelectionContext context)
        {
            context ??= CreateSelectionContext();
            bool backuped = SceneBackuper.TryBackupActiveScene();

            if (!backuped)
            {
                Debug.LogError(FcuLocKey.log_cant_execute_because_no_backup.Localize());
                return;
            }

            SyncHelper[] syncHelpers;
            List<GroupedSyncHelpers> rootFrameGroups = BuildRootFrameGroups(context, out syncHelpers);

            if (syncHelpers == null || syncHelpers.Length == 0)
            {
                Debug.LogError(FcuLocKey.log_script_generator_no_sync_helpers.Localize());
                return;
            }

            Type[] screenTypes = GetScriptTypes();

            await SerializeObjectsAsync(syncHelpers, rootFrameGroups, screenTypes, context);
            await SerializeOnClickToMethodsAsync(syncHelpers, rootFrameGroups, screenTypes, context);
        }

        private async Task SerializeOnClickToMethodsAsync(
            SyncHelper[] syncHelpers,
            IEnumerable<GroupedSyncHelpers> rootFrameGroups,
            Type[] screenTypes,
            ScriptGeneratorSelectionContext context)
        {
            GameObject rootFrameGO = null;
            MonoBehaviour rootComponent = null;
            string frameName = null;
            string screenTypeName = null;

            try
            {
                foreach (Type screenType in screenTypes)
                {
                    foreach (GroupedSyncHelpers rootFrameGroup in rootFrameGroups)
                    {
                        if (rootFrameGroup.RootFrame == null)
                        {
                            Debug.LogError(FcuLocKey.log_script_generator_root_frame_null.Localize());
                            continue;
                        }

                        rootFrameGO = rootFrameGroup.RootFrame.GameObject;

                        if (rootFrameGO == null)
                        {
                            Debug.LogError(FcuLocKey.log_script_generator_root_frame_gameobject_null.Localize(rootFrameGroup.RootFrame.Id));
                            continue;
                        }

                        frameName = ResolveClassName(rootFrameGroup.RootFrame, context);
                        if (frameName.IsEmpty())
                        {
                            frameName = rootFrameGroup.RootFrame.Names?.ClassName;
                        }
                        screenTypeName = screenType.Name;

                        if (frameName.IsEmpty() || screenTypeName.IsEmpty() || frameName != screenTypeName)
                            continue;

                        rootComponent = rootFrameGO.GetComponent(screenType) as MonoBehaviour;

                        MethodInfo[] methods = screenType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                        foreach (SyncHelper syncHelper in rootFrameGroup.SyncHelpers)
                        {
                            if (syncHelper == null || syncHelper.gameObject == null)
                                continue;

#if ULB_EXISTS && UNITY_2021_3_OR_NEWER
                            UitkButton uitkButton = syncHelper.gameObject.GetComponent<UitkButton>();

                            if (uitkButton == null)
                                continue;

                            string resolvedMethod = ResolveMethodName(syncHelper, context);
                            if (resolvedMethod.IsEmpty())
                                continue;

                            string methodName = resolvedMethod + "_OnClick";

                            if (methodName.IsEmpty())
                                continue;

                            MethodInfo targetMethod = methods.FirstOrDefault(m =>
                                m.Name == methodName &&
                                m.GetParameters().Length == 0);

                            if (targetMethod == null)
                            {
                                Debug.LogWarning(FcuLocKey.log_script_generator_method_not_found.Localize(methodName, screenType.Name, syncHelper.gameObject.name));
                                continue;
                            }

                            SerializeOnClickInspector(uitkButton, rootComponent, methodName);

                            rootFrameGO.SetDirtyExt();
#endif
                        }

                        await Task.Yield();
                    }
                }

                Debug.Log(FcuLocKey.log_script_generator_methods_serialized.Localize());
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void SerializeOnClickInspector(
#if ULB_EXISTS
            UitkButton uitkButton,
#else
            GameObject uitkButton,
#endif
            MonoBehaviour rootComponent,
            string methodName)
        {
#if UNITY_EDITOR
            SerializedObject so = new SerializedObject(uitkButton);
            SerializedProperty onClickProp = so.FindProperty("_onClick");

            if (onClickProp != null)
            {
                SerializedProperty callsProp = onClickProp.FindPropertyRelative("m_PersistentCalls.m_Calls");
                int callIndex = callsProp.arraySize;
                callsProp.arraySize++;
                SerializedProperty call = callsProp.GetArrayElementAtIndex(callIndex);

                call.FindPropertyRelative("m_Target").objectReferenceValue = rootComponent;
                call.FindPropertyRelative("m_MethodName").stringValue = methodName;
                call.FindPropertyRelative("m_Mode").enumValueIndex = (int)PersistentListenerMode.Void;
                call.FindPropertyRelative("m_CallState").enumValueIndex = (int)UnityEventCallState.RuntimeOnly;

                SerializedProperty argsProp = call.FindPropertyRelative("m_Arguments");
                argsProp.FindPropertyRelative("m_ObjectArgument").objectReferenceValue = null;
                argsProp.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName").stringValue = typeof(UnityEngine.Object).AssemblyQualifiedName;
                argsProp.FindPropertyRelative("m_IntArgument").intValue = 0;
                argsProp.FindPropertyRelative("m_FloatArgument").floatValue = 0f;
                argsProp.FindPropertyRelative("m_StringArgument").stringValue = string.Empty;
                argsProp.FindPropertyRelative("m_BoolArgument").boolValue = false;

                so.ApplyModifiedProperties();

                EditorUtility.SetDirty(uitkButton);
                EditorUtility.SetDirty(rootComponent.gameObject);

                Debug.Log(FcuLocKey.log_script_generator_persistent_method_assigned.Localize(methodName, uitkButton.gameObject.name));
            }
#endif
        }

        private async Task SerializeObjectsAsync(
            SyncHelper[] syncHelpers,
            IEnumerable<GroupedSyncHelpers> rootFrameGroups,
            Type[] screenTypes,
            ScriptGeneratorSelectionContext context)
        {
            string frameName1 = null;
            string frameName2 = null;

            string objName1 = null;
            string objName2 = null;

            GameObject rootFrameGO = null;
            FObjectAttribute attribute = null;
            MonoBehaviour rootComponent = null;
            Type rootType = null;
            FieldInfo[] fields = null;

            try
            {
                foreach (Type screenType in screenTypes)
                {
                    foreach (GroupedSyncHelpers rootFrameGroup in rootFrameGroups)
                    {
                        if (rootFrameGroup.RootFrame == null)
                        {
                            Debug.LogError(FcuLocKey.log_script_generator_root_frame_null.Localize());
                            continue;
                        }

                        rootFrameGO = rootFrameGroup.RootFrame.GameObject;

                        if (rootFrameGO == null)
                        {
                            Debug.LogError(FcuLocKey.log_script_generator_root_frame_gameobject_null.Localize(rootFrameGroup.RootFrame.Id));
                            continue;
                        }

                        frameName1 = null;
                        frameName2 = null;

                        switch (monoBeh.Settings.ScriptGeneratorSettings.SerializationMode)
                        {
                            case FieldSerializationMode.SyncHelpers:
                                {
                                    frameName1 = ResolveClassName(rootFrameGroup.RootFrame, context);
                                    if (frameName1.IsEmpty())
                                    {
                                        frameName1 = rootFrameGroup.RootFrame.Names?.ClassName;
                                    }
                                    frameName2 = screenType.Name;
                                }
                                break;
                            case FieldSerializationMode.Attributes:
                                {
                                    attribute = screenType.GetCustomAttribute<FObjectAttribute>();

                                    if (attribute != null)
                                    {
                                        frameName1 = rootFrameGroup.RootFrame.Names.FigmaName;
                                        frameName2 = attribute.Name;
                                    }
                                }
                                break;
                            case FieldSerializationMode.GameObjectNames:
                                {
                                    frameName1 = rootFrameGO.name;
                                    frameName2 = screenType.Name;
                                }
                                break;
                            default:
                                throw new NotImplementedException();
                        }

                        if (!frameName1.IsEmpty() && !frameName2.IsEmpty() && frameName1 == frameName2)
                        {
                            rootComponent = rootFrameGO.GetComponent(screenType) as MonoBehaviour;

                            if (rootComponent == null)
                            {
                                rootComponent = rootFrameGO.AddComponent(screenType) as MonoBehaviour;
                                rootFrameGO.SetDirtyExt();
                                Debug.Log(FcuLocKey.log_script_generator_component_added.Localize(screenType.Name, rootFrameGO.name));
                            }

                            rootType = rootComponent.GetType();
                            fields = rootType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

                            foreach (FieldInfo fieldInfo in fields)
                            {
                                foreach (SyncHelper fieldSH in rootFrameGroup.SyncHelpers)
                                {
                                    if (fieldSH.gameObject == null)
                                        continue;

                                    objName1 = null;
                                    objName2 = null;

                                    switch (monoBeh.Settings.ScriptGeneratorSettings.SerializationMode)
                                    {
                                        case FieldSerializationMode.SyncHelpers:
                                            {
                                                objName1 = ResolveFieldName(fieldSH, context);
                                                objName2 = fieldInfo.Name;
                                            }
                                            break;
                                        case FieldSerializationMode.Attributes:
                                            {
                                                attribute = fieldInfo.GetCustomAttribute<FObjectAttribute>();

                                                if (attribute != null)
                                                {
                                                    objName1 = fieldSH.Data.Names.FigmaName;
                                                    objName2 = attribute.Name;
                                                }
                                            }
                                            break;
                                        case FieldSerializationMode.GameObjectNames:
                                            {
                                                objName1 = fieldSH.gameObject.name;
                                                objName2 = fieldInfo.Name;
                                            }
                                            break;
                                        default:
                                            throw new NotImplementedException();
                                    }

                                    if (!objName1.IsEmpty() && !objName2.IsEmpty() && objName1 == objName2)
                                    {
                                        AssignValue(rootComponent, fieldInfo, fieldSH);
                                        break;
                                    }
                                }
                            }

                            rootFrameGO.SetDirtyExt();

                            await Task.Yield();
                        }
                    }
                }

#if UNITY_EDITOR
                AssetDatabase.SaveAssets();
#endif

                Debug.Log(FcuLocKey.log_script_generator_names_serialized.Localize());
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        private void AssignValue(MonoBehaviour rootComponent, FieldInfo fieldInfo, SyncHelper fieldSyncHelper)
        {
            GameObject fieldGameObject = fieldSyncHelper.gameObject;

            if (fieldGameObject == null)
            {
                Debug.LogError(FcuLocKey.log_script_generator_sync_helper_gameobject_null.Localize(fieldSyncHelper.name));
                return;
            }

            Type fieldType = fieldInfo.FieldType;

            if (typeof(Component).IsAssignableFrom(fieldType))
            {
                Component component = fieldGameObject.GetComponent(fieldType);

                if (component == null)
                {
                    Debug.LogWarning(FcuLocKey.log_script_generator_component_null.Localize(fieldInfo.Name));
                    return;
                }

                fieldInfo.SetValue(rootComponent, component);
                rootComponent.SetDirtyExt();

                Debug.Log(FcuLocKey.log_script_generator_component_assigned.Localize(component.GetType().Name, fieldInfo.Name));
            }
            else if (fieldType == typeof(GameObject))
            {
                fieldInfo.SetValue(rootComponent, fieldGameObject);
                rootComponent.SetDirtyExt();

                Debug.Log(FcuLocKey.log_script_generator_gameobject_assigned.Localize(fieldGameObject.name, fieldInfo.Name));
            }
            else
            {
                Debug.LogWarning(FcuLocKey.log_script_generator_field_type_unsupported.Localize(fieldType.Name, fieldInfo.Name));
            }
        }

        private string ResolveFieldName(SyncHelper syncHelper, ScriptGeneratorSelectionContext context)
        {
            if (syncHelper == null)
            {
                return null;
            }

            if (context == null)
            {
                return syncHelper.Data?.Names?.FieldName;
            }

            string resolved = context.GetResolvedFieldName(syncHelper);

            if (resolved.IsEmpty())
            {
                return syncHelper.Data?.Names?.FieldName;
            }

            return resolved;
        }

        private string ResolveClassName(SyncData rootFrame, ScriptGeneratorSelectionContext context)
        {
            if (rootFrame == null)
            {
                return null;
            }

            if (context == null)
            {
                return rootFrame.Names?.ClassName;
            }

            string resolved = context.GetResolvedClassName(rootFrame);
            if (resolved.IsEmpty())
            {
                return rootFrame.Names?.ClassName;
            }

            return resolved;
        }

        private string ResolveMethodName(SyncHelper syncHelper, ScriptGeneratorSelectionContext context)
        {
            if (syncHelper == null)
            {
                return null;
            }

            if (context == null)
            {
                return syncHelper.Data?.Names?.MethodName;
            }

            string resolved = context.GetResolvedMethodName(syncHelper);
            if (resolved.IsEmpty())
            {
                return syncHelper.Data?.Names?.MethodName;
            }

            return resolved;
        }

        private async Task GenerateScriptsAsync(ScriptGeneratorSelectionContext context)
        {
            bool backuped = SceneBackuper.TryBackupActiveScene();

            if (!backuped)
            {
                Debug.LogError(FcuLocKey.log_cant_execute_because_no_backup.Localize());
                return;
            }

            try
            {
                SyncHelper[] syncHelpers;
                List<GroupedSyncHelpers> grouped = BuildRootFrameGroups(context, out syncHelpers);

                if (syncHelpers == null || syncHelpers.Length == 0 || grouped == null || grouped.Count == 0)
                {
                    Debug.LogError(FcuLocKey.log_script_generator_no_sync_helpers.Localize());
                    return;
                }

                foreach (GroupedSyncHelpers group in grouped)
                {
                    string script = GenerateScript(group, context);
                    Debug.Log(script);
                    string className = ResolveClassName(group.RootFrame, context);
                    if (className.IsEmpty())
                    {
                        className = group.RootFrame?.Names?.ClassName;
                    }

                    if (className.IsEmpty())
                    {
                        className = group.RootFrame?.GameObject != null
                            ? group.RootFrame.GameObject.name
                            : "FcuScreen";
                    }
                    string folderPath = monoBeh.Settings.ScriptGeneratorSettings.OutputPath;
                    Directory.CreateDirectory(folderPath);
                    string filePath = Path.Combine(folderPath, $"{className}.cs");
                    File.WriteAllText(filePath, script.ToString());
                    await Task.Yield();
                }

#if UNITY_EDITOR
                AssetDatabase.Refresh();
#endif
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public string GenerateUsings()
        {
            List<string> usings = new List<string>
            {

            };

            if (monoBeh.IsUGUI())
            {
                usings.Add("using UnityEngine.UI;");

                if (monoBeh.UsingTextMesh())
                {
                    usings.Add("#if TextMeshPro");
                    usings.Add("using TMPro;");
                    usings.Add("#endif");
                }
            }
            else
            {
                usings.Add("using UnityEngine.UIElements;");

                usings.Add("#if ULB_EXISTS");
                usings.Add("using DA_Assets.UEL;");
                usings.Add("#endif");
            }

            return string.Join(Environment.NewLine, usings);
        }

        private string GenerateScript(GroupedSyncHelpers group, ScriptGeneratorSelectionContext context)
        {
            string className = ResolveClassName(group.RootFrame, context);
            if (className.IsEmpty())
            {
                className = group.RootFrame?.Names?.ClassName;
            }

            if (className.IsEmpty())
            {
                className = group.RootFrame?.GameObject != null
                    ? group.RootFrame.GameObject.name
                    : "FcuScreen";
            }

            string usings = GenerateUsings();
            string baseClass = FcuConfig.BaseClass.text;
            string fields = GetFields(group.SyncHelpers, context);
            string methods = GetMethods(group.SyncHelpers, context);

            string script = string.Format(baseClass,
                usings,
                monoBeh.Settings.ScriptGeneratorSettings.Namespace,
                className,
                monoBeh.Settings.ScriptGeneratorSettings.BaseClass,
                fields,
                methods);

            return script;
        }

        private string GetFields(List<SyncHelper> syncHelpers, ScriptGeneratorSelectionContext context)
        {
            StringBuilder elemsSb = new StringBuilder();
            StringBuilder labelsSb = new StringBuilder();

            string sfAtt = $"[{nameof(SerializeField)}]";
            string tab = "        ";

            var syncHelpersWithComponents = syncHelpers.Select(syncHelper =>
            {
                Type componentType = DetermineComponentType(syncHelper);
                string componentName = componentType?.Name ?? nameof(GameObject);
                return new { SyncHelper = syncHelper, ComponentType = componentType, ComponentName = componentName };
            });

            var sortedSyncHelpers = syncHelpersWithComponents
                .OrderBy(item => item.ComponentName)
                .ThenBy(item => item.SyncHelper.Data.Names.FieldName)
                .ToList();

            foreach (var item in sortedSyncHelpers)
            {
                var syncHelper = item.SyncHelper;
                string fieldName = ResolveFieldName(syncHelper, context);

                if (fieldName.IsEmpty())
                {
                    continue;
                }

                string componentName = item.ComponentName;
                labelsSb.AppendLine($"{tab}{sfAtt} {componentName} {fieldName};");
            }

            return $"{elemsSb}\n{labelsSb}";
        }

        private string GetMethods(List<SyncHelper> syncHelpers, ScriptGeneratorSelectionContext context)
        {
            StringBuilder elemsSb = new StringBuilder();
            StringBuilder labelsSb = new StringBuilder();

            string tab = "        ";

            var syncHelpersWithComponents = syncHelpers
                .Where(x => x.ContainsTag(FcuTag.Button))
                .Select(syncHelper =>
                {
                    Type componentType = DetermineComponentType(syncHelper);
                    string componentName = componentType?.Name ?? nameof(GameObject);
                    string methodName = ResolveMethodName(syncHelper, context);
                    return new { SyncHelper = syncHelper, ComponentName = componentName, MethodName = methodName };
                })
                .Where(item => item.MethodName.IsEmpty() == false)
                .OrderBy(item => item.ComponentName)
                .ThenBy(item => item.MethodName)
                .ToList();

            foreach (var item in syncHelpersWithComponents)
            {
                string methodName = item.MethodName;
                string componentName = item.ComponentName;
                labelsSb.AppendLine($"{tab}public void {methodName}_OnClick()\n{tab}{{\n\n{tab}}}");
                labelsSb.AppendLine();
            }

            return $"{elemsSb}\n{labelsSb}";
        }

        private Type DetermineComponentType(SyncHelper syncHelper)
        {
            if (monoBeh.IsUGUI())
            {
                if (syncHelper.gameObject.TryGetComponentSafe(out Text c1))
                {
                    return typeof(Text);
                }
#if TextMeshPro
                else if (syncHelper.gameObject.TryGetComponentSafe(out TMP_Text c2))
                {
                    return typeof(TMP_Text);
                }
                else if (syncHelper.gameObject.TryGetComponentSafe(out TMP_InputField c7))
                {
                    return typeof(TMP_InputField);
                }
#endif
                else if (syncHelper.gameObject.TryGetComponentSafe(out Button c3))
                {
                    return typeof(Button);
                }
#if DABUTTON_EXISTS
                else if (syncHelper.gameObject.TryGetComponentSafe(out DAButton c4))
                {
                    return typeof(DAButton);
                }
#endif
                else if (syncHelper.gameObject.TryGetComponentSafe(out InputField c6))
                {
                    return typeof(InputField);
                }
                else
                {
                    return typeof(GameObject);
                }
            }
            else
            {
#if ULB_EXISTS && UNITY_2021_3_OR_NEWER
                if (syncHelper.gameObject.TryGetComponentSafe(out UitkLabel c1))
                {
                    return typeof(UitkLabel);
                }
                else if (syncHelper.gameObject.TryGetComponentSafe(out UitkButton c2))
                {
                    return typeof(UitkButton);
                }
                else if (syncHelper.gameObject.TryGetComponentSafe(out UitkVisualElement c7))
                {
                    return typeof(UitkVisualElement);
                }
                else
                {
                    return typeof(GameObject);
                }
#else
                return typeof(GameObject);
#endif
            }
        }

        private List<GroupedSyncHelpers> BuildRootFrameGroups(ScriptGeneratorSelectionContext context, out SyncHelper[] syncHelpers)
        {
            if (context == null)
            {
                syncHelpers = monoBeh.SyncHelpers.GetAllSyncHelpers() ?? Array.Empty<SyncHelper>();
                monoBeh.SyncHelpers.RestoreRootFrames(syncHelpers);

                return syncHelpers
                    .Where(IsValidHelper)
                    .GroupBy(helper => helper.Data.RootFrame)
                    .Select(group => new GroupedSyncHelpers
                    {
                        RootFrame = group.Key,
                        SyncHelpers = group.Where(IsValidHelper).ToList()
                    })
                    .Where(group => group.RootFrame != null && group.SyncHelpers.Any())
                    .ToList();
            }

            var allHelpers = new List<SyncHelper>();
            var groups = new List<GroupedSyncHelpers>();

            foreach (ScriptGeneratorFrameSelection frameSelection in context.GetEnabledFrames())
            {
                if (frameSelection?.RootFrame == null)
                {
                    continue;
                }

                List<SyncHelper> helpers = frameSelection.EnabledSyncHelpers
                    .Where(IsValidHelper)
                    .ToList();

                if (helpers.Count == 0)
                {
                    continue;
                }

                groups.Add(new GroupedSyncHelpers
                {
                    RootFrame = frameSelection.RootFrame,
                    SyncHelpers = helpers
                });

                allHelpers.AddRange(helpers);
            }

            syncHelpers = allHelpers.ToArray();
            monoBeh.SyncHelpers.RestoreRootFrames(syncHelpers);

            return groups;
        }

        private static bool IsValidHelper(SyncHelper helper)
        {
            return helper != null &&
                   helper.Data != null &&
                   helper.Data.RootFrame != null;
        }

        private string BuildFrameSearchSignature(SyncData rootFrame)
        {
            if (rootFrame == null)
            {
                return string.Empty;
            }

            List<string> parts = new List<string>
            {
                rootFrame.Names?.ClassName,
                rootFrame.Names?.FigmaName,
                rootFrame.GameObject != null ? rootFrame.GameObject.name : null,
                rootFrame.NameHierarchy
            };

            return string.Join(" ", parts.Where(part => part.IsEmpty() == false)).ToLowerInvariant();
        }

        private string BuildFieldSearchSignature(SyncHelper helper, string componentName)
        {
            if (helper == null)
            {
                return string.Empty;
            }

            List<string> parts = new List<string>
            {
                helper.Data?.Names?.FieldName,
                helper.Data?.Names?.FigmaName,
                helper.gameObject != null ? helper.gameObject.name : null,
                componentName
            };

            return string.Join(" ", parts.Where(part => part.IsEmpty() == false)).ToLowerInvariant();
        }

        private static Type[] GetScriptTypes()
        {
            try
            {
                Assembly assembly = Assembly.Load("Assembly-CSharp");

                if (assembly == null)
                {
                    Debug.LogError(FcuLocKey.log_script_generator_assembly_load_failed.Localize());
                    return Array.Empty<Type>();
                }

                Type[] allTypes = assembly.GetTypes();

                Type[] componentTypes = allTypes
                    .Where(t => t.IsClass &&
                                t.IsPublic &&
                                typeof(MonoBehaviour).IsAssignableFrom(t))
                    .ToArray();

                return componentTypes;
            }
            catch (Exception ex)
            {
                Debug.LogError(FcuLocKey.log_script_generator_unexpected_error.Localize(ex.Message));
                return Array.Empty<Type>();
            }
        }
    }
}
