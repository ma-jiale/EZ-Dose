using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DA_Assets.Logging;

namespace DA_Assets.FCU
{
    public class SpriteDuplicateFinder
    {
        private const int HASH_SIZE = 8;
        private const int THRESH_HASH = 2;
        private const float THRESH_DIFF = 0.08f;

        public List<List<SpriteUsageFinder.UsedSprite>> GetGroups(string[] texPaths, bool log = false)
        {
            var features = new List<Feature>();

            foreach (var path in texPaths)
            {
                try
                {
                    Texture2D tex = null;
#if UNITY_EDITOR
                    tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
#endif
                    if (tex == null)
                        continue;

                    var pix = tex.GetPixels32();
                    int w = tex.width, h = tex.height;

                    if (IsSolid(pix, w, h))
                        continue;

                    string avhash = AvhashImage(pix, w, h);
                    float[] thumbArr = MakeThumbFloatArray(pix, w, h, 64, 64);

                    features.Add(new Feature
                    {
                        Path = path,
                        Name = Path.GetFileName(path),
                        Avhash = avhash,
                        ThumbArr = thumbArr,
                        Width = w,
                        Height = h
                    });
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning(ex.Message);
                }
            }

            var groups = new List<List<int>>();
            for (int i = 0; i < features.Count; ++i)
            {
                bool assigned = false;
                for (int g = 0; g < groups.Count; ++g)
                {
                    bool similar = true;
                    foreach (int idx in groups[g])
                    {
                        if (Hamming(features[i].Avhash, features[idx].Avhash) > THRESH_HASH)
                        {
                            similar = false; break;
                        }
                        if (DiffMetricArr(features[i].ThumbArr, features[idx].ThumbArr) > THRESH_DIFF)
                        {
                            similar = false; break;
                        }
                    }
                    if (similar)
                    {
                        groups[g].Add(i);
                        assigned = true;
                        break;
                    }
                }
                if (!assigned)
                    groups.Add(new List<int> { i });
            }

            var res = new List<List<SpriteUsageFinder.UsedSprite>>();
            foreach (var group in groups)
            {
                if (group.Count < 2) continue;

                res.Add(group.Select(idx =>
                    new SpriteUsageFinder.UsedSprite
                    {
                        Path = features[idx].Path,
                        Size = new Vector2Int(features[idx].Width, features[idx].Height)
                    }).ToList());
            }

            if (log)
            {
                Debug.Log(FcuLocKey.log_sdf_groups_found.Localize(res.Count));
            }
            return res;
        }

        static bool IsSolid(Color32[] pix, int w, int h)
        {
            var first = pix[0];
            for (int i = 1; i < pix.Length; ++i)
            {
                if (pix[i].r != first.r || pix[i].g != first.g || pix[i].b != first.b)
                    return false;
            }
            return true;
        }

        static string AvhashImage(Color32[] pix, int w0, int h0)
        {
            var crop = GetCropAlphaBounds(pix, w0, h0);
            if (!crop.IsValid) return new string('0', HASH_SIZE * HASH_SIZE);

            int srcW = crop.width;
            int srcH = crop.height;
            int srcX0 = crop.xMin;
            int srcY0 = crop.yMin;

            int[] xCoords = new int[HASH_SIZE];
            int[] yCoords = new int[HASH_SIZE];
            for (int i = 0; i < HASH_SIZE; ++i)
            {
                xCoords[i] = srcX0 + Mathf.Clamp(Mathf.RoundToInt((float)i / (HASH_SIZE - 1) * (srcW - 1)), 0, srcW - 1);
                yCoords[i] = srcY0 + Mathf.Clamp(Mathf.RoundToInt((float)i / (HASH_SIZE - 1) * (srcH - 1)), 0, srcH - 1);
            }

            float[] lum = new float[HASH_SIZE * HASH_SIZE];
            for (int y = 0; y < HASH_SIZE; ++y)
            {
                int srcY = yCoords[y];
                for (int x = 0; x < HASH_SIZE; ++x)
                {
                    int srcX = xCoords[x];
                    Color32 c = pix[srcY * w0 + srcX];
                    lum[y * HASH_SIZE + x] = 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
                }
            }

            float avg = 0;
            for (int i = 0; i < lum.Length; ++i)
                avg += lum[i];
            avg /= lum.Length;

            char[] bits = new char[HASH_SIZE * HASH_SIZE];
            for (int i = 0; i < lum.Length; ++i)
                bits[i] = lum[i] > avg ? '1' : '0';

            return new string(bits);
        }

        public static float[] MakeThumbFloatArray(Color32[] pix, int w0, int h0, int outW, int outH)
        {
            var crop = GetCropAlphaBounds(pix, w0, h0);
            if (!crop.IsValid)
            {
                float[] blank = new float[outW * outH * 3];
                for (int i = 0; i < blank.Length; i += 3)
                {
                    blank[i + 0] = 1f;
                    blank[i + 1] = 1f;
                    blank[i + 2] = 1f;
                }
                return blank;
            }

            int srcW = crop.width;
            int srcH = crop.height;
            int srcX0 = crop.xMin;
            int srcY0 = crop.yMin;

            int[] xCoords = new int[outW];
            int[] yCoords = new int[outH];
            for (int i = 0; i < outW; ++i)
                xCoords[i] = srcX0 + Mathf.Clamp(Mathf.RoundToInt((float)i / (outW - 1) * (srcW - 1)), 0, srcW - 1);
            for (int i = 0; i < outH; ++i)
                yCoords[i] = srcY0 + Mathf.Clamp(Mathf.RoundToInt((float)i / (outH - 1) * (srcH - 1)), 0, srcH - 1);

            float[] result = new float[outW * outH * 3];
            for (int y = 0; y < outH; ++y)
            {
                int iy = yCoords[y];
                for (int x = 0; x < outW; ++x)
                {
                    int ix = xCoords[x];
                    Color32 c = pix[iy * w0 + ix];
                    int idx = (y * outW + x) * 3;
                    result[idx + 0] = c.r / 255f;
                    result[idx + 1] = c.g / 255f;
                    result[idx + 2] = c.b / 255f;
                }
            }

            return result;
        }

        static CropRect GetCropAlphaBounds(Color32[] pix, int width, int height)
        {
            int x0 = width, x1 = -1, y0 = height, y1 = -1;

            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    if (pix[y * width + x].a > 0)
                    {
                        if (x < x0) x0 = x;
                        if (x > x1) x1 = x;
                        if (y < y0) y0 = y;
                        if (y > y1) y1 = y;
                    }
                }
            }

            if (x1 < x0 || y1 < y0)
                return new CropRect { xMin = 0, yMin = 0, width = 0, height = 0 };

            return new CropRect
            {
                xMin = x0,
                yMin = y0,
                width = x1 - x0 + 1,
                height = y1 - y0 + 1
            };
        }

        static int Hamming(string a, string b)
        {
            int c = 0;
            for (int i = 0; i < a.Length; ++i)
                if (a[i] != b[i]) c++;
            return c;
        }

        static float DiffMetricArr(float[] a, float[] b)
        {
            float sum = 0;
            for (int i = 0; i < a.Length; ++i)
                sum += Mathf.Abs(a[i] - b[i]);
            return sum / a.Length;
        }

        struct CropRect
        {
            public int xMin, yMin, width, height;
            public bool IsValid => width > 0 && height > 0;
        }

        struct Feature
        {
            public string Path;
            public string Name;
            public string Avhash;
            public float[] ThumbArr;
            public int Width;
            public int Height;
        }
    }
}
