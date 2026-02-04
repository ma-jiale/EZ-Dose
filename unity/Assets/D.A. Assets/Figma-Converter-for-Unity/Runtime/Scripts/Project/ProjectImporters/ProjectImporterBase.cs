using DA_Assets.Constants;
using DA_Assets.Extensions;
using DA_Assets.FCU.Extensions;
using DA_Assets.FCU.Model;
using DA_Assets.Logging;
using DA_Assets.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.FCU
{
    [Serializable]
    public abstract class ProjectImporterBase : FcuBase
    {
        private List<FObject> _currentPage;

        public async Task StartImportAsync(CancellationToken token)
        {
            string[] frameIds = GetSelectedFrameIds().ToArray();

            if (frameIds.Length < 1)
            {
                Debug.Log(FcuLocKey.log_nothing_to_import.Localize());
                return;
            }

            await RunImportAsync(frameIds, token);

            try
            {
                token.ThrowIfCancellationRequested();

                ClearAfterImport();

                token.ThrowIfCancellationRequested();

                monoBeh.Events.OnImportComplete?.Invoke(monoBeh);
                monoBeh.AssetTools.ShowRateMe();
                monoBeh.AssetTools.StopAsset(StopAssetReason.End);

            }
            catch (OperationCanceledException ex)
            {
                Debug.LogWarning(ex.Message);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex); 
            }
        }

        protected async Task ImportFrame(CancellationToken token, params string[] frameIds)
        {
            try
            {
                monoBeh.Events.OnImportStart?.Invoke(monoBeh);
                SceneBackuper.TryBackupActiveScene();

                List<FObject> nodes = await monoBeh.ProjectDownloader.DownloadAllNodes(frameIds, token);

                monoBeh.FolderCreator.CreateAll();
                monoBeh.CurrentProject.LastImportedFrames.Clear();

                _currentPage = monoBeh.CurrentProject.CurrentPage;
                _currentPage.Clear();

                FObject virtualPage = new FObject
                {
                    Id = FcuConfig.PARENT_ID,
                    Name = monoBeh.CurrentProject.ProjectName,
                    Children = nodes,
                    Data = new SyncData
                    {
                        GameObject = monoBeh.gameObject,
                        RectGameObject = monoBeh.gameObject,
                        Names = new FNames
                        {
                            ObjectName = FcuTag.Page.ToString(),
                        },
                        Tags = new List<FcuTag>
                        {
                            FcuTag.Page
                        }
                    }
                };

                monoBeh.NameSetter.ClearNames();

                await monoBeh.TagSetter.SetTags(virtualPage, token);
                await ConvertTreeToListAsync(virtualPage, _currentPage, token);
                await monoBeh.ImageTypeSetter.SetImageTypes(_currentPage, token);
                await monoBeh.ImageTypeSetter.SetInsideDownloadableFlags(_currentPage, token);
                await monoBeh.HashGenerator.SetHashes(_currentPage, token);

                await monoBeh.NameSetter.SetNames(_currentPage, FcuNameType.Folder, token);
                await monoBeh.NameSetter.SetNames(_currentPage, FcuNameType.File, token);
                await monoBeh.NameSetter.SetNames(_currentPage, FcuNameType.UssClass, token);

                //Setting root frames before creating game objects to use them in import algorithms.
                monoBeh.CurrentProject.SetRootFrames(_currentPage, token);

                if (monoBeh.IsPlaying())
                {
                    await ShowPreImportWindow(token);
                }

                if (monoBeh.IsPlaying())
                {
                    await LoadPrefabs(token);
                }

                await DrawGameObjects(virtualPage, token);

                //Setting root frames after creating game objects so that the root frames are serialized in the inspector.
                monoBeh.CurrentProject.SetRootFrames(_currentPage, token);
                monoBeh.TagSetter.CountTags(_currentPage);

                await monoBeh.SpritePathSetter.SetSpritePaths(_currentPage, token);
                await monoBeh.SpriteDownloader.DownloadSprites(_currentPage, token);
                await SpriteBatchWriter.Flush(monoBeh, token);

                if (monoBeh.IsPlaying())
                {
                    await monoBeh.SpriteGenerator.GenerateSprites(_currentPage, token);
                }

                if (monoBeh.IsPlaying())
                {
                    await monoBeh.SpriteProcessor.MarkAsSprites(_currentPage, token);
                    await monoBeh.SpriteSlicer.SliceSprites(_currentPage, token);
                }

                await monoBeh.SpriteColorizer.ColorizeSprites(_currentPage, token);

                if (monoBeh.Settings.MainSettings.UseDuplicateFinder)
                {
                    await monoBeh.SpriteDuplicateRemover.RemoveDuplicates(_currentPage, token);
                }

                await monoBeh.FontDownloader.DownloadFonts(_currentPage, token);

                await FinalSteps(virtualPage, _currentPage, token);

                ClearAfterSingleFrameImport();
                SceneBackuper.MakeActiveSceneDirty();
            }
            catch (TaskCanceledException)
            {
                monoBeh.AssetTools.StopAsset(StopAssetReason.TaskCanceled);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                monoBeh.AssetTools.StopAsset(StopAssetReason.Error);
            }
        }

        protected async Task ShowPreImportWindowInternal(CancellationToken token)
        {
            Debug.Log(FcuLocKey.log_import_show_difference_checker.Localize());
            SyncHelper[] syncHelpers = monoBeh.SyncHelpers.GetAllSyncHelpers();

            if (syncHelpers.Length < 1)
            {
                Debug.Log(FcuLocKey.log_import_no_frames_to_sync.Localize());
                return;
            }

            //Setting root frames to existing game objects.
            monoBeh.SyncHelpers.RestoreRootFrames(syncHelpers);

            LayoutUpdaterInput lui = await monoBeh.LayoutUpdateDataCreator
                .Create(_currentPage, syncHelpers
                .ToList(), token);

            LayoutUpdaterOutput luo = default;

            await monoBeh.AssetTools.ReselectFcu(token);
            monoBeh.EditorDelegateHolder.ShowDifferenceChecker(lui, _ => luo = _);

            while (luo.IsDefault())
            {
                token.ThrowIfCancellationRequested();

                await Task.Delay(1000, token);
            }

            List<FObject> tempPage = new List<FObject>();

            foreach (string item in luo.ToImport)
            {
                token.ThrowIfCancellationRequested();

                foreach (FObject fobject in _currentPage)
                {
                    token.ThrowIfCancellationRequested();

                    if (item == fobject.Id)
                    {
                        tempPage.Add(fobject);
                    }
                }
            }

            _currentPage = tempPage;

            await monoBeh.CanvasDrawer.GameObjectDrawer.DestroyMissing(luo.ToRemove, token);
        }

        private List<string> GetSelectedFrameIds()
        {
            List<string> selected = monoBeh.InspectorDrawer.SelectableDocument.Childs
                .SelectMany(si => si.Childs)
                .Where(si => si.Selected)
                .Select(si => si.Id)
                .ToList();

            return selected;
        }

        public async Task ConvertTreeToListAsync(FObject parent, List<FObject> fobjects, CancellationToken token)
        {
            Debug.Log(FcuLocKey.log_convert_tree_to_list.Localize());
            await Task.Run(() => ConvertTreeToList(parent, fobjects, 0, -1, token), token);
        }

        private void ConvertTreeToList(FObject parent, List<FObject> fobjects, int depth, int parentIndex, CancellationToken token)
        {
            foreach (FObject child in parent.Children)
            {
                token.ThrowIfCancellationRequested();

                if (child.Data.IsEmpty || child.ContainsTag(FcuTag.Ignore))
                {
                    child.SetFlagToAllChilds(x => x.Data.IsEmpty = true);
                    continue;
                }

                child.Data.HierarchyLevel = depth + 1;
                child.Data.ParentIndex = parentIndex;

                int currentIndex = fobjects.Count;
                fobjects.Add(child);

                if (parentIndex >= 0 && !parent.ContainsTag(FcuTag.Page))
                {
                    fobjects[parentIndex].Data.ChildIndexes.Add(currentIndex);
                }

                if (child.Data.ForceImage)
                {
                    child.SetFlagToAllChilds(x => x.Data.IsEmpty = true);
                    continue;
                }

                if (child.Children.IsEmpty())
                    continue;

                ConvertTreeToList(child, fobjects, depth + 1, currentIndex, token);
            }
        }

        private void ClearAfterImport()
        {
            if (!monoBeh.IsDebug())
            {
                SyncHelper[] syncHelpers = monoBeh.SyncHelpers.GetAllSyncHelpers();

                Parallel.ForEach(syncHelpers,
                    syncHelper => { ObjectCleaner.ClearByAttribute<ClearAttribute>(syncHelper.Data); });

                monoBeh.ImageTypeSetter.ClearAllIds();
                monoBeh.CanvasDrawer.ButtonDrawer.Buttons.Clear();
            }
        }

        private void ClearAfterSingleFrameImport()
        {
            monoBeh.CanvasDrawer.GameObjectDrawer.ClearTempRectFrames();
            monoBeh.ImageTypeSetter.ClearAllIds();
            monoBeh.CanvasDrawer.ButtonDrawer.Buttons.Clear();
        }

        private static Task<List<string>> GetFrameIds(FigmaProject fileData)
        {
            List<string> ids = new List<string>();

            foreach (FObject page in fileData.Document.Children)
            {
                foreach (FObject node in page.Children)
                {
                    if (node.Type == NodeType.FRAME)
                    {
                        ids.Add(node.Id);
                    }
                    else if (node.Type == NodeType.SECTION)
                    {
                        foreach (FObject sub in node.Children)
                            if (sub.Type == NodeType.FRAME)
                                ids.Add(sub.Id);
                    }
                }
            }

            //FigmaProject fileData = await monoBeh.ProjectDownloader.GetFileStructureAsync(depth: 3);
            //List<string> frameIds = await GetFrameIds(fileData);

            return Task.FromResult(ids);
        }

        protected abstract Task RunImportAsync(string[] frameIds, CancellationToken token);

        protected abstract Task ShowPreImportWindow(CancellationToken token);

        protected abstract Task LoadPrefabs(CancellationToken token);

        protected abstract Task DrawGameObjects(FObject virtualPage, CancellationToken token);

        protected abstract Task FinalSteps(FObject virtualPage, List<FObject> currentPage, CancellationToken token);
    }
}
