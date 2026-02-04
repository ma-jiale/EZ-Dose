using System;
using System.Threading;

namespace DA_Assets.FCU
{
    [Serializable]
    public class EditorEventHandlers : FcuBase
    {
        public static void CreateFcu_OnClick() => AssetTools.CreateFcuOnScene();


        public void Auth_OnClick() => monoBeh.Authorizer.Auth();

        public void DownloadProject_OnClick()
        {
            monoBeh.ProjectDownloader.DownloadProjectCts?.Cancel();
            monoBeh.ProjectDownloader.DownloadProjectCts?.Dispose();
            monoBeh.ProjectDownloader.DownloadProjectCts = new CancellationTokenSource();
            monoBeh.ProjectDownloader.DownloadProject(monoBeh.ProjectDownloader.DownloadProjectCts.Token);
        }

        public void ImportSelectedFrames_OnClick() => monoBeh.ProjectImporter.StartImport();

        public void GenerateScripts_OnClick(ScriptGeneratorSelectionContext context) => monoBeh.ScriptGenerator.GenerateScripts(context);

        public void SerializeObjects_OnClick(ScriptGeneratorSelectionContext context) => monoBeh.ScriptGenerator.Serialize(context);

        public void CreatePrefabs_OnClick() => monoBeh.PrefabCreator.CreatePrefabs();

        public void DestroyLastImportedFrames_OnClick() => monoBeh.AssetTools.DestroyLastImportedFrames();

        public void DestroySyncHelpers_OnClick() => monoBeh.SyncHelpers.DestroySyncHelpers();

        public void SetFcuToSyncHelpers_OnClick() => monoBeh.SyncHelpers.SetFcuToAllSyncHelpers();

        public void StopImport_OnClick() => monoBeh.AssetTools.StopAsset(StopAssetReason.Manual);
    }
}
