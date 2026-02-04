using System;
using System.IO;
using UnityEngine;

namespace DA_Assets.FCU
{
    public static class GuidMetaUtility
    {
        public static void WriteGuid(string metaPath, int hash, float scale)
        {
            Rewrite(metaPath, IntFloatGuid.Encode(hash, scale).Value);
        }

        public static bool TryExtractData(string metaPath, out int hash, out float scale)
        {
            hash = 0;
            scale = 0f;

            if (!File.Exists(metaPath))
            {
                return false;
            }

            foreach (string line in File.ReadAllLines(metaPath))
            {
                if (!line.TrimStart().StartsWith("guid:"))
                    continue;

                int colon = line.IndexOf(':');
                if (colon < 0)
                    return false;

                string tail = line.Substring(colon + 1).Trim();
                Guid guid;
                if (!Guid.TryParse(tail, out guid))
                {
                    return false;
                }

                (int h, float s) decoded = IntFloatGuid.Decode(guid);

                hash = decoded.h;
                scale = decoded.s;

                return true;
            }

            return false;
        }

        private static void Rewrite(string metaPath, Guid guid)
        {
            try
            {
                string[] lines = File.ReadAllLines(metaPath);

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].TrimStart().StartsWith("guid:"))
                    {
                        lines[i] = $"guid: {guid:N}";
                        break;
                    }
                }

                File.WriteAllLines(metaPath, lines);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex); 
            }
        }
    }
}