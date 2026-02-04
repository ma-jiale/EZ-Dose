using DA_Assets.Extensions;
using DA_Assets.FCU.Extensions;
using DA_Assets.FCU.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.FCU
{
    [Serializable]
    public class SpritePathSetter : FcuBase
    {
        public async Task SetSpritePaths(List<FObject> fobjects, CancellationToken token)
        {
#if UNITY_EDITOR
            List<FObject> canHasFile = fobjects
                .Where(x => x.IsDownloadableType() || x.IsGenerativeType())
                .ToList();

            await Task.Yield();

            List<FObject> noDuplicates = canHasFile
                .GroupBy(x => x.Data.Hash)
                .Select(x => x.First())
                .ToList();

            await Task.Yield();

            string[] assetSpritePaths;

            if (monoBeh.IsPlaying())
            {
                string filter = $"t:{typeof(Sprite).Name}";

                string[] searchInFolder = new string[]
                {
                    monoBeh.Settings.ImageSpritesSettings.SpritesPath
                };

                assetSpritePaths = UnityEditor.AssetDatabase
                     .FindAssets(filter, searchInFolder)
                     .Select(x => UnityEditor.AssetDatabase.GUIDToAssetPath(x))
                     .ToArray();
            }
            else
            {
                string root = Path.Combine(
                   Application.persistentDataPath,
                   monoBeh.Settings.ImageSpritesSettings.SpritesPath);

                assetSpritePaths = Directory.Exists(root)
                    ? Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
                               .ToArray()
                    : Array.Empty<string>();
            }

            FObject item;
            bool imageFileExists;

            for (int i = 0; i < noDuplicates.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                item = noDuplicates[i];
                imageFileExists = GetSpritePath(item, assetSpritePaths, out string spritePath);

                SetNeedDownloadFileFlag(item, imageFileExists);
                SetNeedGenerateFlag(item, imageFileExists);

                foreach (FObject fobject in canHasFile)
                {
                    if (fobject.Data.Hash == item.Data.Hash)
                    {
                        if (imageFileExists)
                        {
                            fobject.Data.SpritePath = spritePath;
                        }
                        else
                        {
                            fobject.Data.SpritePath = GetSpritePath(item);
                        }
                    }
                }

                if (i % 500 == 0)
                {
                    await Task.Yield();
                }
            }
#endif
        }

        private string GetSpritePath(FObject fobject)
        {
            string spriteDir = fobject.Data.IsMutual
                ? "Mutual"
                : fobject.Data.RootFrame.Names.FolderName;

            string root = monoBeh.IsPlaying()
                ? monoBeh.Settings.ImageSpritesSettings.SpritesPath.GetFullAssetPath()
                : Path.Combine(Application.persistentDataPath,
                               monoBeh.Settings.ImageSpritesSettings.SpritesPath);

            string absoluteFramePath = Path.Combine(root, spriteDir);
            absoluteFramePath.CreateFolderIfNotExists();

            string fileName = fobject.Data.Names.FileName;

            return monoBeh.IsPlaying()
                ? Path.Combine(monoBeh.Settings.ImageSpritesSettings.SpritesPath, spriteDir, fileName)
                : Path.Combine(absoluteFramePath, fileName);                                       
        }

        private bool IsTargetExtension(FObject fobject, string spritePath)
        {
            string spriteExt = Path.GetExtension(spritePath);

            if (spriteExt.StartsWith(".") && spriteExt.Length > 1)
                spriteExt = spriteExt.Remove(0, 1);

            ImageFormat? targetExt = null;

            if (monoBeh.UsingSvgImage())
            {
                if (fobject.CanUseUnityImage(monoBeh))
                {
                    targetExt = ImageFormat.PNG;
                }
            }

            if (targetExt == null)
            {
                targetExt = monoBeh.Settings.ImageSpritesSettings.ImageFormat;
            }

            return spriteExt.ToLower() == targetExt.ToLower();
        }

        public bool GetSpritePath(FObject fobject, string[] spritePathes, out string path)
        {
            foreach (string spritePath in spritePathes)
            {
                if (!IsTargetExtension(fobject, spritePath))
                {
                    continue;
                }

                if (!GuidMetaUtility.TryExtractData(
                    spritePath + ".meta",
                    out int hash1,
                    out float scale1))
                {
                    continue;
                }

                if (hash1 == fobject.Data.Hash)
                {
                    path = spritePath;
                    return true;
                }
            }

            path = null;
            return false;
        }

        private void SetNeedDownloadFileFlag(FObject fobject, bool imageFileExists)
        {
            if (fobject.IsDownloadableType()/* || fobject.IsGenerativeType()*/)
            {
                if (monoBeh.Settings.ImageSpritesSettings.RedownloadSprites)
                {
                    fobject.Data.NeedDownload = true;
                }
                else if (imageFileExists)
                {
                    fobject.Data.NeedDownload = false;
                }
                else
                {
                    fobject.Data.NeedDownload = true;
                }
            }
            else
            {
                fobject.Data.NeedDownload = false;
            }
        }

        private void SetNeedGenerateFlag(FObject fobject, bool imageFileExists)
        {
            if (fobject.IsGenerativeType())
            {
                if (monoBeh.Settings.ImageSpritesSettings.RedownloadSprites)
                {
                    fobject.Data.NeedGenerate = true;
                }
                else if (imageFileExists)
                {
                    fobject.Data.NeedGenerate = false;
                }
                else
                {
                    fobject.Data.NeedGenerate = true;
                }
            }
            else
            {
                fobject.Data.NeedGenerate = false;
            }
        }
    }
}
