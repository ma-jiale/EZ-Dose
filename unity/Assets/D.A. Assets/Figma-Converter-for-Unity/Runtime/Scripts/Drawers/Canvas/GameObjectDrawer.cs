using DA_Assets.Extensions;
using DA_Assets.FCU.Extensions;
using DA_Assets.FCU.Model;
using DA_Assets.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DA_Assets.FCU.Drawers.CanvasDrawers
{
    [Serializable]
    public class GameObjectDrawer : FcuBase
    {
        private GameObject _tempFrames;

        public void Draw(FObject parent, CancellationToken token)
        {
            _tempFrames = MonoBehExtensions.CreateEmptyGameObject();
            _tempFrames.transform.SetParent(monoBeh.transform);
            _tempFrames.name = "Temp (Remove After Import)";

            Debug.Log(FcuLocKey.log_instantiate_game_objects.Localize());
            DrawFObject(parent, token);
        }

        private void DrawFObject(FObject parent, CancellationToken token)
        {
            for (int i = 0; i < parent.Children.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                FObject fobject = parent.Children[i];

                if (fobject.Data.IsEmpty)
                {
                    FcuLogger.Debug($"InstantiateGameObjects | continue | {fobject.Data.NameHierarchy}", FcuDebugSettingsFlags.LogGameObjectDrawer);
                    continue;
                }

                SyncHelper syncHelper;

                bool dontProcessChilds = false;
                bool alreadyExists = monoBeh.SyncHelpers.IsExistsOnCurrentCanvas(fobject, out syncHelper);

                if (alreadyExists)
                {
                    FcuLogger.Debug($"InstantiateGameObjects | 1 | {fobject.Data.NameHierarchy}", FcuDebugSettingsFlags.LogGameObjectDrawer);
                }
                else if (monoBeh.CurrentProject.HasLocalPrefab(fobject.Data, out SyncHelper localPrefab))
                {
                    FcuLogger.Debug($"InstantiateGameObjects | 2 | {fobject.Data.NameHierarchy}", FcuDebugSettingsFlags.LogGameObjectDrawer);
#if UNITY_EDITOR
                    syncHelper = (SyncHelper)UnityEditor.PrefabUtility.InstantiatePrefab(localPrefab);
#endif
                    int counter = 0;
                    monoBeh.SyncHelpers.SetFcuToAllChilds(syncHelper.gameObject, ref counter, token);

                    SetFigmaIds(fobject, syncHelper, token);

                    dontProcessChilds = true;
                }
                else
                {
                    FcuLogger.Debug($"InstantiateGameObjects | 3 | {fobject.Data.NameHierarchy}", FcuDebugSettingsFlags.LogGameObjectDrawer);
                    syncHelper = MonoBehExtensions.CreateEmptyGameObject().AddComponent<SyncHelper>();
                }

                fobject.SetData(syncHelper, monoBeh);
                fobject.Data.GameObject.name = fobject.Data.Names.ObjectName;

                if (!alreadyExists)
                {
                    monoBeh.Events.OnObjectInstantiate?.Invoke(monoBeh, fobject);
                }

                if (monoBeh.IsUGUI())
                {
                    fobject.Data.GameObject.TryAddComponent(out RectTransform _);
                }
#if UNITY_EDITOR
                if (!UnityEditor.PrefabUtility.IsPartOfPrefabInstance(fobject.Data.GameObject))
#endif
                {
                    fobject.Data.GameObject.transform.SetParent(_tempFrames.transform);
                }

                AddRectGameObject(fobject);

                int goLayer;

                if (fobject.ContainsTag(FcuTag.Blur))
                {
                    goLayer = LayerTools.AddLayer(FcuConfig.BlurredObjectTag);
                }
                else
                {
                    goLayer = monoBeh.Settings.MainSettings.GameObjectLayer;
                }

                fobject.Data.GameObject.layer = goLayer;
                fobject.Data.SiblingIndex = GetSiblingIndex(fobject, parent.Children);

                SetParent(fobject, parent);
                SetParentRect(fobject, parent);

                if (fobject.Children.IsEmpty() || dontProcessChilds)
                    continue;

                DrawFObject(fobject, token);
            }
        }

        private static int GetSiblingIndex(FObject fobject, List<FObject> fobjects)
        {
            int index = fobjects.Select(x => x.Id).ToList().IndexOf(fobject.Id);
            return index;
        }

        private void AddRectGameObject(FObject fobject)
        {
            GameObject rectGameObject = MonoBehExtensions.CreateEmptyGameObject();
            rectGameObject.name = fobject.Data.GameObject.name + " | RectTransform";

            rectGameObject.transform.SetParent(_tempFrames.transform);

            fobject.Data.RectGameObject = rectGameObject;
            fobject.Data.RectGameObject.TryAddComponent(out RectTransform _);

            fobject.Data.RectGameObject.TryAddComponent(out Image rectImg);
            rectImg.color = monoBeh.GraphicHelpers.GetRectTransformColor(fobject);
        }

        private void SetParent(FObject fobject, FObject parent)
        {
            if (!fobject.Data.GameObject.transform.parent.IsPartOfAnyPrefab())
            {
                fobject.Data.ParentTransform = parent.Data.GameObject.transform;
            }
        }

        private void SetParentRect(FObject fobject, FObject parent)
        {
            if (!fobject.Data.RectGameObject.transform.parent.IsPartOfAnyPrefab())
            {
                fobject.Data.ParentTransformRect = parent.Data.RectGameObject.transform;
            }
        }

        private void SetFigmaIds(FObject rootFObject, SyncHelper rootSyncObject, CancellationToken token)
        {
            Dictionary<string, int> items = new Dictionary<string, int>();

            foreach (var childIndex in rootFObject.Data.ChildIndexes)
            {
                token.ThrowIfCancellationRequested();

                if (monoBeh.CurrentProject.TryGetByIndex(childIndex, out FObject childFO))
                {
                    items.Add(childFO.Id, childFO.Data.Hash);
                }
            }

            SyncHelper[] soChilds = rootSyncObject.GetComponentsInChildren<SyncHelper>(true);

            foreach (var soChild in soChilds)
            {
                token.ThrowIfCancellationRequested();

                string idToRemove = null;

                foreach (var item in items)
                {
                    token.ThrowIfCancellationRequested();

                    if (item.Value == soChild.Data.Hash)
                    {
                        idToRemove = item.Key;
                        break;
                    }
                }

                if (idToRemove == null)
                    continue;

                items.Remove(idToRemove);
                soChild.Data.Id = idToRemove;

                if (monoBeh.CurrentProject.TryGetById(idToRemove, out FObject gbi))
                {
                    SetFigmaIds(gbi, soChild, token);
                }
            }
        }

        public async Task DestroyMissing(IEnumerable<SyncData> toRemove, CancellationToken token)
        {
            foreach (SyncData item in toRemove)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    FcuLogger.Debug($"DestroyMissing | {item.NameHierarchy}", FcuDebugSettingsFlags.LogGameObjectDrawer);
                    item.GameObject.Destroy();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(ex);
                }

                await Task.Yield();
            }
        }

        public void ClearTempRectFrames()
        {
            _tempFrames.Destroy();
        }
    }
}
