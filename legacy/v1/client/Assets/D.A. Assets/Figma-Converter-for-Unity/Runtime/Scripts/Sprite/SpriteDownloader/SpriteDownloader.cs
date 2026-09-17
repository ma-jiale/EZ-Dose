using DA_Assets.Extensions;
using DA_Assets.FCU.Model;
using DA_Assets.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DA_Assets.DAI;
using UnityEngine;

#if JSONNET_EXISTS
using Newtonsoft.Json;
#endif

namespace DA_Assets.FCU
{
    [Serializable]
    public class SpriteDownloader : FcuBase
    {
        private int _maxConcurrentDownloads = 100;
        private int _maxDownloadAttempts = 3;
        private float _maxChunkSize = 24_000_000;
        private int _maxSpritesCount = 100;
        private int _errorLogSplitLimit = 50;

        public async Task DownloadSprites(List<FObject> fobjects, CancellationToken token)
        {
            List<FObject> uniqueFObjectsToDownload = fobjects
                .Where(x => x.Data.NeedDownload)
                .GroupBy(x => x.Data.Hash)
                .Select(g => g.First())
                .ToList();

            if (uniqueFObjectsToDownload.IsEmpty())
            {
                Debug.Log(FcuLocKey.log_sprite_downloader_no_sprites.Localize());
                return;
            }

            await SpriteDataCalculator.CalculateAndSetSpriteData(uniqueFObjectsToDownload, monoBeh, token);

            Dictionary<ImageFormatScaleKey, List<List<SpriteData>>> chunks = SpriteChunker.CreateChunks(
                uniqueFObjectsToDownload,
                _maxChunkSize,
                _maxSpritesCount);

            List<SpriteData> spritesWithLinks = await FigmaLinkFetcher.FetchLinksAsync(
                chunks,
                monoBeh,
                token);

            ConcurrentBag<FObject> failedObjects = await ConcurrentSpriteDownloader.DownloadAllAsync(
                spritesWithLinks,
                _maxConcurrentDownloads,
                _maxDownloadAttempts,
                monoBeh,
                (current, total) =>
                {
                    monoBeh.EditorDelegateHolder.UpdateProgress?.Invoke(monoBeh, ProgressBarCategory.DownloadingSprites, current);
                },
                token);

            LogFailedDownloads(failedObjects, _errorLogSplitLimit);
        }

        public static void LogFailedDownloads(ConcurrentBag<FObject> failedObjects, int splitLimit)
        {
            if (failedObjects.IsEmpty())
            {
                return;
            }

            List<List<string>> components = failedObjects.Select(x => x.Data.NameHierarchy).Split(splitLimit);

            foreach (List<string> component in components)
            {
                string hierarchies = string.Join("\n", component);
                UnityEngine.Debug.LogError(FcuLocKey.log_malformed_url.Localize(component.Count, hierarchies));
            }
        }
    }

    public struct FigmaImageRequest
    {
#if JSONNET_EXISTS
        [JsonProperty("err")]
#endif
        public string error;
#if JSONNET_EXISTS
        [JsonProperty("images")]
#endif
        // key = id, value = link
        public Dictionary<string, string> images;
    }

    public struct SpriteData
    {
        public FObject FObject { get; set; }
        public string Format { get; set; }
        public string Link { get; set; }
        public float Scale { get; set; }
    }

    public struct ImageFormatScaleKey
    {
        public ImageFormat ImageFormat { get; set; }
        public float Scale { get; set; }
    }
}