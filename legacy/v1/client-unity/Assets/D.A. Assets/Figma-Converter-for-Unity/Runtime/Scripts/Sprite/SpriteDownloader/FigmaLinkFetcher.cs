using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.FCU
{
    public static class FigmaLinkFetcher
    {
        private struct LinkRequestChunk
        {
            public ImageFormatScaleKey Key { get; set; }
            public List<SpriteData> SpriteChunk { get; set; }
        }

        public static async Task<List<SpriteData>> FetchLinksAsync(
            Dictionary<ImageFormatScaleKey, List<List<SpriteData>>> idFormatChunks,
            FigmaConverterUnity monoBeh,
            CancellationToken token)
        {
            List<LinkRequestChunk> requestChunks = idFormatChunks
                .SelectMany(pair => pair.Value.Select(chunk => new LinkRequestChunk
                {
                    Key = pair.Key,
                    SpriteChunk = chunk
                }))
                .ToList();

            if (requestChunks.IsEmpty())
                return new List<SpriteData>();

            var spritesWithLinks = new List<SpriteData>();
            int totalSpriteCount = requestChunks.Sum(c => c.SpriteChunk.Count);

            Debug.Log(FcuLocKey.log_getting_links.Localize(0, totalSpriteCount));
            monoBeh.EditorDelegateHolder.StartProgress?.Invoke(monoBeh, ProgressBarCategory.GettingSpriteLinks, totalSpriteCount, false);

            foreach (LinkRequestChunk request in requestChunks)
            {
                if (token.IsCancellationRequested) break;

                IEnumerable<string> ids = request.SpriteChunk.Select(x => x.FObject.Id);

                DARequest daRequest = RequestCreator.CreateImageLinksRequest(
                    monoBeh.Settings.MainSettings.ProjectUrl,
                    request.Key.ImageFormat.ToLower(),
                    request.Key.Scale,
                    ids,
                    monoBeh.RequestSender.GetRequestHeader(monoBeh.Authorizer.Token));

                DAResult<FigmaImageRequest> result = await monoBeh.RequestSender.SendRequest<FigmaImageRequest>(
                    daRequest,
                    token);

                if (!result.Success)
                {
                    string errorDetails = result.Error.err.IsEmpty() ? "-" : result.Error.err;
                    Debug.LogError(FcuLocKey.log_figma_link_fetch_failed.Localize(result.Error.status, errorDetails));
                    continue;
                }

                var images = result.Object.images ?? new Dictionary<string, string>();

                if (images.IsEmpty() && ids.Any())
                {
                    Debug.LogError(FcuLocKey.log_figma_link_missing_ids.Localize(string.Join(", ", ids)));
                }

                PopulateLinks(request.SpriteChunk, images, monoBeh.Settings.MainSettings.Https);
                spritesWithLinks.AddRange(request.SpriteChunk);
                monoBeh.EditorDelegateHolder.UpdateProgress?.Invoke(monoBeh, ProgressBarCategory.GettingSpriteLinks, spritesWithLinks.Count);
            }

            monoBeh.EditorDelegateHolder.CompleteProgress?.Invoke(monoBeh, ProgressBarCategory.GettingSpriteLinks);
            Debug.Log(FcuLocKey.log_getting_links.Localize(spritesWithLinks.Count, totalSpriteCount));

            return spritesWithLinks;
        }

        private static void PopulateLinks(List<SpriteData> chunk, Dictionary<string, string> images, bool useHttps)
        {
            for (int i = 0; i < chunk.Count; i++)
            {
                var spriteData = chunk[i];
                if (images.TryGetValue(spriteData.FObject.Id, out string link))
                {
                    spriteData.Link = useHttps ? link : link?.Replace("https://", "http://");
                }
                else
                {
                    spriteData.Link = string.Empty;
                }
                chunk[i] = spriteData;
            }
        }
    }
}
