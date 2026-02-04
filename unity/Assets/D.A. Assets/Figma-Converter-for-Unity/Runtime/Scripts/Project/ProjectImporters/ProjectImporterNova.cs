using DA_Assets.FCU.Model;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.FCU
{
    [Serializable]
    public sealed class ProjectImporterNova : ProjectImporterBase
    {
        protected override async Task RunImportAsync(string[] frameIds, CancellationToken token)
        {
            if (monoBeh.Settings.MainSettings.SequentialImport)
            {
                foreach (string frameId in frameIds)
                {
                    await ImportFrame(token, frameId);
                }
            }
            else
            {
                await ImportFrame(token, frameIds);
            }
        }

        protected override async Task ShowPreImportWindow(CancellationToken token)
        {
            if (!monoBeh.Settings.MainSettings.SequentialImport)
            {
                await ShowPreImportWindowInternal(token);
            }
        }

        protected override Task LoadPrefabs(CancellationToken token)
        {
            monoBeh.CurrentProject.LoadLocalPrefabs(token);
            return Task.CompletedTask;
        }

        protected override Task DrawGameObjects(FObject virtualPage, CancellationToken token)
        {
            monoBeh.CanvasDrawer.GameObjectDrawer.Draw(virtualPage, token);
            return Task.CompletedTask;
        }

        protected override async Task FinalSteps(FObject virtualPage, List<FObject> currentPage, CancellationToken token)
        {
#if NOVA_UI_EXISTS
            monoBeh.NovaDrawer.SetupSpace();
            monoBeh.gameObject.transform.localScale = Vector3.one;

            await monoBeh.TransformSetter.SetTransformPos(currentPage);
            await monoBeh.TransformSetter.RestoreParentsRect(currentPage);
            monoBeh.TransformSetter.MoveNovaTransforms(currentPage);

            await monoBeh.TransformSetter.RestoreParents(currentPage);
            await monoBeh.TransformSetter.RestoreNovaFramePositions(currentPage, token);

            await monoBeh.NovaDrawer.DrawToScene(currentPage, token);
            monoBeh.NovaDrawer.EnableScreenSpaceComponent();
#endif
        }
    }
}