using DA_Assets.Extensions;
using DA_Assets.FCU.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DA_Assets.FCU
{
    [Serializable]
    public class ScriptGeneratorSelectionContext
    {
        private readonly List<ScriptGeneratorFrameSelection> _frames = new List<ScriptGeneratorFrameSelection>();
        private readonly Dictionary<string, ScriptGeneratorFrameSelection> _frameLookup = new Dictionary<string, ScriptGeneratorFrameSelection>();
        private readonly Dictionary<string, ScriptGeneratorFieldSelection> _fieldLookup = new Dictionary<string, ScriptGeneratorFieldSelection>();

        public IReadOnlyList<ScriptGeneratorFrameSelection> Frames => _frames;

        public void SetFrames(IEnumerable<ScriptGeneratorFrameSelection> frames)
        {
            _frames.Clear();

            if (frames != null)
            {
                _frames.AddRange(frames.Where(frame => frame != null));
            }

            RebuildLookups();
        }

        private void RebuildLookups()
        {
            _frameLookup.Clear();
            _fieldLookup.Clear();

            foreach (var frame in _frames)
            {
                string frameId = frame?.FrameId;

                if (frameId.IsEmpty() == false)
                {
                    _frameLookup[frameId] = frame;
                }

                if (frame?.Fields == null)
                {
                    continue;
                }

                foreach (var field in frame.Fields)
                {
                    string fieldId = field?.FieldId;

                    if (fieldId.IsEmpty() == false)
                    {
                        _fieldLookup[fieldId] = field;
                    }
                }
            }
        }

        public ScriptGeneratorFrameSelection GetFrameSelection(SyncData rootFrame)
        {
            string frameId = rootFrame?.Id;

            if (frameId.IsEmpty())
            {
                return null;
            }

            _frameLookup.TryGetValue(frameId, out var selection);
            return selection;
        }

        public ScriptGeneratorFieldSelection GetFieldSelection(SyncHelper helper)
        {
            string fieldId = helper?.Data?.Id;

            if (fieldId.IsEmpty())
            {
                return null;
            }

            _fieldLookup.TryGetValue(fieldId, out var selection);
            return selection;
        }

        public string GetResolvedFieldName(SyncHelper helper)
        {
            string fallback = helper?.Data?.Names?.FieldName;
            var selection = GetFieldSelection(helper);

            if (selection == null)
            {
                return fallback;
            }

            string resolved = selection.GetEffectiveName();
            return resolved.IsEmpty() ? fallback : resolved;
        }

        public string GetResolvedClassName(SyncData rootFrame)
        {
            if (rootFrame == null)
            {
                return null;
            }

            string fallback = rootFrame.Names?.ClassName;
            ScriptGeneratorFrameSelection selection = null;

            string frameId = rootFrame.Id;
            if (frameId.IsEmpty() == false)
            {
                _frameLookup.TryGetValue(frameId, out selection);
            }

            if (selection == null)
            {
                selection = _frames.FirstOrDefault(frame => frame.RootFrame == rootFrame);
            }

            if (selection == null)
            {
                return fallback;
            }

            string resolved = selection.GetEffectiveClassName();
            return resolved.IsEmpty() ? fallback : resolved;
        }

        public string GetResolvedMethodName(SyncHelper helper)
        {
            string fallback = helper?.Data?.Names?.MethodName;
            var selection = GetFieldSelection(helper);
            var method = selection?.MethodSelection;

            if (method == null)
            {
                return fallback;
            }

            string resolved = method.GetEffectiveName();
            return resolved.IsEmpty() ? fallback : resolved;
        }

        public IEnumerable<ScriptGeneratorFrameSelection> GetEnabledFrames()
        {
            return _frames.Where(frame => frame != null && frame.Enabled);
        }

        public IEnumerable<ScriptGeneratorFieldSelection> GetEnabledFields(ScriptGeneratorFrameSelection frame)
        {
            if (frame == null)
            {
                return Array.Empty<ScriptGeneratorFieldSelection>();
            }

            return frame.Fields.Where(field => field != null && field.Enabled);
        }

        public bool HasSelectedFields()
        {
            return GetEnabledFrames().Any(frame => GetEnabledFields(frame).Any());
        }
    }

    [Serializable]
    public class ScriptGeneratorFrameSelection
    {
        public ScriptGeneratorFrameSelection(SyncData rootFrame)
        {
            RootFrame = rootFrame;
            string defaultClassName = rootFrame?.Names?.ClassName;
            if (defaultClassName.IsEmpty())
            {
                defaultClassName = rootFrame?.Names?.FigmaName;
            }

            if (defaultClassName.IsEmpty())
            {
                defaultClassName = rootFrame != null ? rootFrame.GameObject?.name : null;
            }

            if (defaultClassName.IsEmpty())
            {
                defaultClassName = "Frame";
            }

            CustomClassName = defaultClassName;
            SanitizedClassName = defaultClassName;
        }

        public SyncData RootFrame { get; }
        public bool Enabled { get; set; } = true;
        public List<ScriptGeneratorFieldSelection> Fields { get; } = new List<ScriptGeneratorFieldSelection>();
        public string SearchSignature { get; set; }
        public string CustomClassName { get; set; }
        public string SanitizedClassName { get; set; }
        public bool IsClassNameValid { get; set; } = true;
        public string ClassValidationMessage { get; set; }

        public string FrameId => RootFrame?.Id;

        public string DisplayName
        {
            get
            {
                if (RootFrame == null)
                {
                    return "Frame";
                }

                if (RootFrame.Names != null)
                {
                    if (RootFrame.Names.ClassName.IsEmpty() == false)
                    {
                        return RootFrame.Names.ClassName;
                    }

                    if (RootFrame.Names.FigmaName.IsEmpty() == false)
                    {
                        return RootFrame.Names.FigmaName;
                    }
                }

                if (RootFrame.GameObject != null && RootFrame.GameObject.name.IsEmpty() == false)
                {
                    return RootFrame.GameObject.name;
                }

                return "Frame";
            }
        }

        public IEnumerable<ScriptGeneratorFieldSelection> EnabledFields =>
            Fields.Where(field => field != null && field.Enabled);

        public IEnumerable<SyncHelper> EnabledSyncHelpers =>
            EnabledFields.Select(field => field.SyncHelper).Where(helper => helper != null);

        public string GetEffectiveClassName()
        {
            if (IsClassNameValid && SanitizedClassName.IsEmpty() == false)
            {
                return SanitizedClassName;
            }

            if (CustomClassName.IsEmpty() == false)
            {
                return CustomClassName;
            }

            return DisplayName;
        }
    }

    [Serializable]
    public class ScriptGeneratorFieldSelection
    {
        public ScriptGeneratorFieldSelection(SyncHelper syncHelper)
        {
            SyncHelper = syncHelper;

            string defaultName = syncHelper?.Data?.Names?.FieldName;

            if (defaultName.IsEmpty() && syncHelper != null)
            {
                defaultName = syncHelper.gameObject != null ? syncHelper.gameObject.name : syncHelper.name;
            }

            DefaultName = defaultName;
            CustomName = defaultName;
            SanitizedCustomName = defaultName;
        }

        public SyncHelper SyncHelper { get; }
        public bool Enabled { get; set; } = true;
        public string DefaultName { get; }
        public string CustomName { get; set; }
        public string SanitizedCustomName { get; set; }
        public bool IsCustomNameValid { get; set; } = true;
        public string ValidationMessage { get; set; }
        public string ComponentTypeName { get; set; } = nameof(GameObject);
        public Type ComponentType { get; set; } = typeof(GameObject);
        public string SearchSignature { get; set; }
        public string RuntimeId { get; } = Guid.NewGuid().ToString("N");
        public ScriptGeneratorMethodSelection MethodSelection { get; set; }

        public string FieldId => SyncHelper?.Data?.Id;
        public bool HasMethod => MethodSelection != null;

        public string GetEffectiveName()
        {
            if (IsCustomNameValid && SanitizedCustomName.IsEmpty() == false)
            {
                return SanitizedCustomName;
            }

            return DefaultName ?? string.Empty;
        }
    }

    [Serializable]
    public class ScriptGeneratorMethodSelection
    {
        public ScriptGeneratorMethodSelection(SyncHelper syncHelper)
        {
            SyncHelper = syncHelper;

            string defaultName = syncHelper?.Data?.Names?.MethodName;
            if (defaultName.IsEmpty() && syncHelper != null)
            {
                defaultName = syncHelper.Data?.Names?.FieldName;
            }

            DefaultName = defaultName;
            CustomName = defaultName;
            SanitizedCustomName = defaultName;
        }

        public SyncHelper SyncHelper { get; }
        public string DefaultName { get; }
        public string CustomName { get; set; }
        public string SanitizedCustomName { get; set; }
        public bool IsCustomNameValid { get; set; } = true;
        public string ValidationMessage { get; set; }

        public string GetEffectiveName()
        {
            if (IsCustomNameValid && SanitizedCustomName.IsEmpty() == false)
            {
                return SanitizedCustomName;
            }

            return DefaultName ?? string.Empty;
        }
    }
}
