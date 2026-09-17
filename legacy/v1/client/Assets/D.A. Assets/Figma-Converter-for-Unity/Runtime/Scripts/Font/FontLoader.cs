using DA_Assets.DAI;
using DA_Assets.FCU.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DA_Assets.Extensions;
using DA_Assets.FCU.Extensions;
using System.Threading;
using System.Threading.Tasks;
using DA_Assets.Logging;
using Resources = UnityEngine.Resources;

#if TextMeshPro
using TMPro;
#endif

namespace DA_Assets.FCU
{
    [Serializable]
    public class FontLoader : FcuBase
    {
        [SerializeField] string ttfFontsPath = "Assets/Fonts/Ttf";
        public string TtfFontsPath { get => ttfFontsPath; set => ttfFontsPath = value; }

        [SerializeField] string tmpFontsPath = "Assets/Fonts/Sdf";
        public string TmpFontsPath { get => tmpFontsPath; set => tmpFontsPath = value; }

        [SerializeField] public List<Font> TtfFonts = new List<Font>();

#if TextMeshPro
        [SerializeField] public List<TMP_FontAsset> TmpFonts = new List<TMP_FontAsset>();
#endif

        public void RemoveDubAndMissingFonts()
        {
            monoBeh.FontLoader.TtfFonts = monoBeh.FontLoader.TtfFonts.Where(x => x != null).ToList();
            monoBeh.FontLoader.TtfFonts = monoBeh.FontLoader.TtfFonts.Distinct().ToList();
#if TextMeshPro
            monoBeh.FontLoader.TmpFonts = monoBeh.FontLoader.TmpFonts.Where(x => x != null).ToList();
            monoBeh.FontLoader.TmpFonts = monoBeh.FontLoader.TmpFonts.Distinct().ToList();
#endif
        }

        public T GetFontFromArray<T>(FObject fobject, List<T> fontArray) where T : UnityEngine.Object
        {
            fobject.Data.HasFontAsset = true;

            T fontItem = null;
            int reason = 0;

            //Search by family, weight and italic
            foreach (T _fontItem in fontArray)
            {
                if (_fontItem.IsDefault())
                    continue;

                if (fobject.FontNameToString(true, true, null, true) == _fontItem.name.FormatFontName())
                {
                    reason = 1;
                    fontItem = _fontItem;
                    break;
                }
            }

            //If font not found, search by family and weight
            if (fontItem == null)
            {
                foreach (T _fontItem in fontArray)
                {
                    if (_fontItem.IsDefault())
                        continue;

                    if (fobject.FontNameToString(true, false, null, true) == _fontItem.name.FormatFontName())
                    {
                        reason = 2;
                        fontItem = _fontItem;
                        break;
                    }
                }
            }

            //If font not found, search by family only
            if (fontItem == null)
            {
                fobject.Data.HasFontAsset = false;

                foreach (T _fontItem in fontArray)
                {
                    if (_fontItem.IsDefault())
                        continue;

                    string fontFamily = fobject.FontNameToString(false, false, null, true);

                    bool contains = _fontItem.name.FormatFontName().Contains(fontFamily);

                    if (contains)
                    {
                        reason = 3;
                        fontItem = _fontItem;
                        break;
                    }
                }
            }

            //If font not found, load default font.
            if (fontItem == null)
            {
                fobject.Data.HasFontAsset = false;

                if (typeof(T) == typeof(Font))
                {
                    reason = 4;
#if UNITY_2022_1_OR_NEWER
                    fontItem = Resources.GetBuiltinResource<T>("LegacyRuntime.ttf");
#else
                    fontItem = Resources.GetBuiltinResource<T>("Arial.ttf");
#endif

                }
                else
                {
                    reason = 5;
                    fontItem = Resources.Load<T>("Fonts & Materials/LiberationSans SDF");
                }
            }

            FcuLogger.Debug($"FontLoader | {fobject.Data.NameHierarchy} | {fontItem} | reason: {reason}", FcuDebugSettingsFlags.LogFontLoader);

            return fontItem;
        }

        public async Task AddToTtfFontsList(CancellationToken token)
        {
            Debug.Log(FcuLocKey.log_start_adding_to_fonts_list.Localize());

            await AddToList(
                monoBeh.FontLoader.TtfFontsPath,
                monoBeh.FontLoader.TtfFonts,
                addedCount =>
                {
                    Debug.Log(FcuLocKey.log_added_total.Localize(addedCount, monoBeh.FontLoader.TtfFonts.Count()));
                }, token);

            RemoveDubAndMissingFonts();
        }
        public async Task AddToTmpMeshFontsList(CancellationToken token)
        {
#if TextMeshPro
            Debug.Log(FcuLocKey.log_start_adding_to_fonts_list.Localize());

            await AddToList(
                monoBeh.FontLoader.TmpFontsPath,
                monoBeh.FontLoader.TmpFonts,
                addedCount =>
                {
                    Debug.Log(FcuLocKey.log_added_total.Localize(addedCount, monoBeh.FontLoader.TmpFonts.Count()));
                }, token);

            RemoveDubAndMissingFonts();
#endif
            await Task.Yield();
        }

        public async Task AddToList<T>(string fontsPath, List<T> list, Action<int> addedCount, CancellationToken token) where T : UnityEngine.Object
        {
            List<T> loadedAssets = new List<T>();
            await LoadAssetFromFolder<T>(fontsPath, x => loadedAssets = x, token);

            int count = 0;

            T asset;

            for (int i = 0; i < loadedAssets.Count; i++)
            {
                asset = loadedAssets[i];

                if (list.Contains(asset) == false)
                {
                    count++;
                    list.Add(asset);
                }

                if (i % 25 == 0)
                {
                    await Task.Yield();
                }
            }

            addedCount.Invoke(count);
        }

        public async Task LoadAssetFromFolder<T>(string fontsPath, Action<List<T>> assets, CancellationToken token) where T : UnityEngine.Object
        {
            List<string> pathes = new List<string>();
            List<T> loadedAssets = new List<T>();

#if UNITY_EDITOR
            string[] guids = UnityEditor.AssetDatabase.FindAssets($"t:{typeof(T).Name}", new string[] { fontsPath.ToRelativePath() });
            pathes = guids.Select(x => UnityEditor.AssetDatabase.GUIDToAssetPath(x)).ToList();

            string path;

            for (int i = 0; i < pathes.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                path = pathes[i];

                T sourceFontFile = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
                loadedAssets.Add(sourceFontFile);

                if (i % 25 == 0)
                {
                    await Task.Yield();
                }
            }
#endif

            assets.Invoke(loadedAssets);
            await Task.Yield();
        }
    }
}
