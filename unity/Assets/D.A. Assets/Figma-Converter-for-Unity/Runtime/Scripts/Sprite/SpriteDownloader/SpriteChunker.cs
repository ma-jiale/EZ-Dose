using DA_Assets.FCU.Extensions;
using DA_Assets.FCU.Model;
using System.Collections.Generic;
using System.Linq;

namespace DA_Assets.FCU
{
    public static class SpriteChunker
    {
        public static Dictionary<ImageFormatScaleKey, List<List<SpriteData>>> CreateChunks(
            IEnumerable<FObject> fobjects,
            float maxChunkSize,
            int maxSpritesCount)
        {
            var formatChunks = new Dictionary<ImageFormatScaleKey, List<List<SpriteData>>>();

            var fobjectsByFormatAndScale = fobjects
                .Where(x => x.IsSprite())
                .GroupBy(fobject => new ImageFormatScaleKey
                {
                    ImageFormat = fobject.Data.ImageFormat,
                    Scale = fobject.Data.Scale
                });

            foreach (var group in fobjectsByFormatAndScale)
            {
                ImageFormatScaleKey key = group.Key;
                List<List<SpriteData>> chunks = new List<List<SpriteData>>();
                List<SpriteData> currentChunk = new List<SpriteData>();
                float currentChunkSize = 0;

                foreach (FObject fobject in group)
                {
                    float spriteSize = fobject.Data.MaxSpriteSize.x * fobject.Data.MaxSpriteSize.y * fobject.Data.Scale;

                    if (currentChunk.Any() && (currentChunkSize + spriteSize > maxChunkSize || currentChunk.Count >= maxSpritesCount))
                    {
                        chunks.Add(currentChunk);
                        currentChunk = new List<SpriteData>();
                        currentChunkSize = 0;
                    }

                    currentChunk.Add(new SpriteData
                    {
                        FObject = fobject,
                        Format = key.ImageFormat.ToString(),
                        Scale = key.Scale
                    });
                    currentChunkSize += spriteSize;
                }

                if (currentChunk.Any())
                {
                    chunks.Add(currentChunk);
                }

                formatChunks[key] = chunks;
            }

            return formatChunks;
        }
    }
}
