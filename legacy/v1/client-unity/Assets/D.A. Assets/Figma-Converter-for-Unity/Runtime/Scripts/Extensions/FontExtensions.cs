using DA_Assets.Extensions;
using System.Collections.Generic;
using System.Linq;

namespace DA_Assets.FCU.Extensions
{
    public static class FontExtensions
    {
        public static string FormatFontName(this string value)
        {
            if (value.IsEmpty())
            {
                return "null";
            }

            Dictionary<string, string> weightSynonyms = new Dictionary<string, string>
            {
                { "hairline", "thin" },
                { "ultralight", "extralight" },
                { "light", "light" },
                { "normal", "regular" },
                { "medium", "medium" },
                { "demibold", "semibold" },
                { "bold", "bold" },
                { "ultrabold", "extrabold" },
                { "heavy", "black" },
                { "ultrablack", "extrablack" },
            };

            string formatted = value
                .Replace("SDF", "")
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("_", "")
                .ToLower();

            bool hasWeight = 
                weightSynonyms.Keys.Any(x => formatted.Contains(x)) || 
                weightSynonyms.Values.Any(x => formatted.Contains(x));

            bool hasItalic = formatted.Contains("italic");

            if (hasWeight)
            {
                foreach (var pair in weightSynonyms)
                {
                    if (formatted.Contains(pair.Key))
                    {
                        formatted = formatted.Replace(pair.Key, pair.Value);
                    }
                }
            }
            else if (hasItalic)
            {
                formatted = formatted.Replace("italic", "regularitalic");
            }

            return formatted;
        }
    }
}