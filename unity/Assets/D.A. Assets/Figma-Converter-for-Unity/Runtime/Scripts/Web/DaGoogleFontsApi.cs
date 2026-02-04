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
    [Serializable]
    public class DaGoogleFontsApi : FcuBase
    {
        [SerializeField] public FontSubset FontSubsets = FontSubset.Latin;

        public List<FontSubset> SelectedFontAssets
        {
            get
            {
                List<FontSubset> selectedSubsets = Enum.GetValues(FontSubsets.GetType())
                    .Cast<FontSubset>()
                    .Where(x => FontSubsets.HasFlag(x))
                    .ToList();

                return selectedSubsets;
            }
        }

        private Dictionary<FontSubset, List<FontItem>> googleFontsBySubset = new Dictionary<FontSubset, List<FontItem>>();

        public async Task GetGoogleFontsBySubset(FontSubset fontSubset, CancellationToken token)
        {
            monoBeh.EditorDelegateHolder.StartProgress?.Invoke(monoBeh, ProgressBarCategory.DownloadingGoogleFonts, 0, true);

            try
            {
                List<FontSubset> selectedSubsets = Enum.GetValues(FontSubsets.GetType())
                    .Cast<FontSubset>()
                    .Where(x => FontSubsets.HasFlag(x))
                    .ToList();

                List<FontSubset> missingSubsets = new List<FontSubset>();

                foreach (FontSubset subset in Enum.GetValues(FontSubsets.GetType()))
                {
                    if (FontSubsets.HasFlag(subset) == false)
                        continue;

                    if (googleFontsBySubset.TryGetValue(subset, out var _) == false)
                    {
                        missingSubsets.Add(subset);
                    }
                }

                if (missingSubsets.Count == 0)
                {
                    return;
                }

                foreach (FontSubset missingSubset in missingSubsets)
                {
                    string missingSubsetName = missingSubset.ToLower();

                    if (missingSubsetName == FontSubset.LatinExt.ToLower())
                    {
                        missingSubsetName = "latin-ext";
                    }

                    Debug.Log(FcuLocKey.loading_google_fonts.Localize(missingSubset.ToString()));

                    string gfontsUrl = "https://content-webfonts.googleapis.com/v1/webfonts?subset={0}&key={1}";
                    string url = string.Format(gfontsUrl, missingSubsetName, FcuConfig.GoogleFontsApiKey);

                    DARequest request = new DARequest
                    {
                        RequestType = RequestType.Get,
                        Query = url
                    };

                    DAResult<FontRoot> return0 = await monoBeh.RequestSender.SendRequest<FontRoot>(request, token);

                    if (@return0.Success)
                    {
                        googleFontsBySubset.Add(missingSubset, @return0.Object.Items);
                    }
                    else
                    {
                        Debug.LogError(FcuLocKey.log_google_fonts_subset_failed.Localize(missingSubset.ToString()));
                    }
                }
            }
            finally
            {
                monoBeh.EditorDelegateHolder.CompleteProgress?.Invoke(monoBeh, ProgressBarCategory.DownloadingGoogleFonts);
            }
        }

        public string GetUrlByWeight(FontItem fontItem, int weight, FontStyle fontStyle)
        {
            try
            {
                if (fontStyle == FontStyle.Normal)
                {
                    switch (weight)
                    {
                        case 100: return fontItem.Files["100"];
                        case 200: return fontItem.Files["200"];
                        case 300: return fontItem.Files["300"];
                        case 400: return fontItem.Files["regular"];
                        case 500: return fontItem.Files["500"];
                        case 600: return fontItem.Files["600"];
                        case 700: return fontItem.Files["700"];
                        case 800: return fontItem.Files["800"];
                        case 900: return fontItem.Files["900"];
                    }
                }
                else if (fontStyle == FontStyle.Italic)
                {
                    switch (weight)
                    {
                        case 100: return fontItem.Files["100italic"];
                        case 200: return fontItem.Files["200italic"];
                        case 300: return fontItem.Files["300italic"];
                        case 400: return fontItem.Files["italic"];
                        case 500: return fontItem.Files["500italic"];
                        case 600: return fontItem.Files["600italic"];
                        case 700: return fontItem.Files["700italic"];
                        case 800: return fontItem.Files["800italic"];
                        case 900: return fontItem.Files["900italic"];
                    }
                }
            }
            catch
            {

            }

            return null;
        }

        public FontItem GetFontItem(FontMetadata fontMetadata, FontSubset fontSubset)
        {
            try
            {
                googleFontsBySubset.TryGetValue(fontSubset, out var googleFonts);

                foreach (FontItem item in googleFonts)
                {
                    if (item.Family.ToLower() == fontMetadata.Family.ToLower())
                    {
                        return item;
                    }
                }
            }
            catch
            {

            }

            return default;
        }
    }
}
