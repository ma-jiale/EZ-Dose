using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#if ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif
#endif

namespace DA_Assets
{
    public static class SpriteUsageFinder
    {
        public static string NormalizePath(string path) => path?.Replace('\\', '/');

        public sealed class UsedSprite : IEquatable<UsedSprite>
        {
            /// <summary>Asset GUID.</summary>
            public string Guid;

            /// <summary>Normalized asset path ("Assets/...").</summary>
            public string Path;

            /// <summary>Asset name.</summary>
            public string Name;

            /// <summary>true if the main asset type is Sprite, false if Texture2D.</summary>
            public bool IsSpriteSubAsset;

            /// <summary>Size in pixels.</summary>
            public Vector2Int Size;

            /// <summary>Whether this asset is marked as Addressable.</summary>
            public bool IsAddressable;

            /// <summary>All places where this sprite/texture is referenced.</summary>
            public List<UsageRef> Usages = new();

            public bool Selected { get; set; }

            // Equality: prefer GUID if available, otherwise fall back to Path.
            public bool Equals(UsedSprite other)
            {
                if (other is null) return false;

                if (!string.IsNullOrEmpty(Guid) && !string.IsNullOrEmpty(other.Guid))
                    return string.Equals(Guid, other.Guid, StringComparison.OrdinalIgnoreCase);

                return string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase);
            }

            public override bool Equals(object obj) => Equals(obj as UsedSprite);

            public override int GetHashCode()
            {
                if (!string.IsNullOrEmpty(Guid))
                    return StringComparer.OrdinalIgnoreCase.GetHashCode(Guid);

                return StringComparer.OrdinalIgnoreCase.GetHashCode(Path ?? string.Empty);
            }
        }

        public enum UsageKind
        {
            Scene,
            Prefab,
            Material,
            AnimatorController,
            AnimationClip,
            TimelinePlayable, 
            SpriteAtlas,
            Tile,    
            ScriptableObject,
            AddressableEntry,  
            AddressableDep   
        }

        public enum RefStrength
        {
            Direct,   // Direct reference (e.g., a field/property holds the sprite).
            Indirect  // Via intermediate asset (e.g., material/anim/atlas).
        }

        public sealed class UsageRef
        {
            /// <summary>Kind of the referring asset.</summary>
            public UsageKind Kind;

            /// <summary>GUID of the referring asset.</summary>
            public string AssetGuid;

            /// <summary>Normalized path of the referring asset.</summary>
            public string AssetPath;

            /// <summary>Full name of the main asset type at referring path.</summary>
            public string AssetType;

            // Contextual hints. Many remain empty when we only use GetDependencies.
            public string HierarchyPath;   // For scenes/prefabs: "Canvas/Icon" (requires deeper scan to fill)
            public string ComponentType;   // e.g., "UnityEngine.UI.Image" (requires deeper scan)
            public string PropertyPath;    // e.g., "Image.sprite" or "SpriteRenderer.sprite"
            public string ShaderProperty;  // For materials: "_MainTex" etc. (requires material inspection)
            public string AnimationBinding;// For clips: binding path/property (requires binding inspection)
            public string TimelineTrack;   // For timeline clips/tracks
            public string AtlasSpriteName; // If sprite comes via a SpriteAtlas with a specific name
            public string Notes;           // Free-form comments

            public RefStrength Strength = RefStrength.Indirect;

            // Addressables context
            public string AddressableGroup;
            public string AddressKey;
            public string[] AddressLabels;
        }

        // --------------------------------------------------------
        // Public API
        // --------------------------------------------------------

        /// <summary>
        /// Collects ALL usage points of sprites across the project.
        /// Returns a HashSet of UsedSprite, each containing a list of UsageRef entries.
        /// </summary>
        public static HashSet<UsedSprite> GetUsedSprites_AllAssets(
            string[] allSpriteGuids,
            bool includeScenes = true,
            bool includePrefabs = true,
            bool includeMaterials = true,
            bool includeAnimation = true,     // AnimatorController, AnimationClip, Timeline (PlayableAsset)
            bool includeAtlases = true,       // SpriteAtlas
            bool includeTiles = true,         // TileBase (2D Tilemaps)
            bool includeScriptableObjects = false, // Can be slow in big projects
            bool includeAddressables = true
        )
        {
#if !UNITY_EDITOR
            // Outside of the editor, AssetDatabase APIs are unavailable.
            return new HashSet<UsedSprite>();
#else
            // 1) Build an index of sprite candidates from input GUIDs.
            var spriteIndex = BuildSpriteIndex(allSpriteGuids);

            // Quick exit if there are no sprite assets to track.
            if (spriteIndex.Count == 0)
                return new HashSet<UsedSprite>();

            var spritePaths = new HashSet<string>(
                spriteIndex.Values.Select(us => us.Path),
                StringComparer.OrdinalIgnoreCase);

            // 2) Collect root assets (potential referrers) by type.
            var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (includeScenes) AddAssetsByFilter("t:Scene", roots);
            if (includePrefabs) AddAssetsByFilter("t:Prefab", roots);
            if (includeMaterials) AddAssetsByFilter("t:Material", roots);

            if (includeAnimation)
            {
                AddAssetsByFilter("t:AnimatorController", roots);
                AddAssetsByFilter("t:AnimationClip", roots);
                AddAssetsByFilter("t:PlayableAsset", roots); // Timeline
            }

            if (includeAtlases) AddAssetsByFilter("t:SpriteAtlas", roots);
            if (includeTiles) AddAssetsByFilter("t:TileBase", roots);

            if (includeScriptableObjects) AddAssetsByFilter("t:ScriptableObject", roots);

            // 3) For all roots, resolve dependencies and map usages.
            CollectUsagesViaDependencies(roots, spritePaths, spriteIndex);

            // 4) Addressables (optional).
#if ADDRESSABLES
            if (includeAddressables)
            {
                try
                {
                    CollectAddressablesUsages(spritePaths, spriteIndex);
                }
                catch
                {
                    // Silently ignore Addressables issues; keep the rest of results.
                }
            }
#endif

            // 5) Return as a HashSet<UsedSprite>.
            return new HashSet<UsedSprite>(spriteIndex.Values, spriteIndex.Comparer);
#endif // UNITY_EDITOR
        }

        // --------------------------------------------------------
        // Editor-only helpers
        // --------------------------------------------------------
#if UNITY_EDITOR

        /// <summary>
        /// Builds a dictionary of UsedSprite keyed by GUID (when available) or Path.
        /// Ensures Name/Size/IsSpriteSubAsset/IsAddressable are populated.
        /// </summary>
        private static UsedSpriteDictionary BuildSpriteIndex(string[] allSpriteGuids)
        {
            var dict = new UsedSpriteDictionary();

            foreach (var guid in allSpriteGuids ?? Array.Empty<string>())
            {
                var path = NormalizePath(AssetDatabase.GUIDToAssetPath(guid));
                if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Identify main type at the path.
                var mainType = AssetDatabase.GetMainAssetTypeAtPath(path);
                bool isSprite = mainType == typeof(Sprite);
                bool isTex2D = mainType == typeof(Texture2D);

                if (!isSprite && !isTex2D)
                    continue;

                var used = new UsedSprite
                {
                    Guid = guid,
                    Path = path,
                    Name = System.IO.Path.GetFileNameWithoutExtension(path),
                    IsSpriteSubAsset = isSprite,
                    Size = TryGetSize(path, isSprite),
                    IsAddressable = IsPathAddressable(path)
                };

                dict.AddOrGet(used);
            }

            return dict;
        }

        /// <summary>
        /// Adds assets matching a search filter to the root set.
        /// </summary>
        private static void AddAssetsByFilter(string filter, HashSet<string> roots)
        {
            foreach (var guid in AssetDatabase.FindAssets(filter))
            {
                var p = NormalizePath(AssetDatabase.GUIDToAssetPath(guid));
                if (!string.IsNullOrEmpty(p) && p.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                    roots.Add(p);
            }
        }

        /// <summary>
        /// Chunks root assets and collects dependencies. For every dependency that is a known sprite path,
        /// adds a UsageRef to the corresponding UsedSprite.
        /// </summary>
        private static void CollectUsagesViaDependencies(
            HashSet<string> roots,
            HashSet<string> spritePaths,
            UsedSpriteDictionary spriteIndex)
        {
            if (roots == null || roots.Count == 0) return;

            const int Chunk = 512;
            var rootArray = roots.ToArray();

            for (int i = 0; i < rootArray.Length; i += Chunk)
            {
                var slice = rootArray.Skip(i).Take(Chunk).ToArray();

                // To avoid repeating the same "referrer -> sprite" mapping too many times
                // within this chunk, we track pairs we've already added.
                var added = new HashSet<(string referrer, string sprite)>(new PairOrdinalIgnoreCaseComparer());

                foreach (var referrerPath in slice)
                {
                    var referrerGuid = AssetDatabase.AssetPathToGUID(referrerPath);
                    var referrerType = AssetDatabase.GetMainAssetTypeAtPath(referrerPath);
                    var kind = GuessUsageKindFromType(referrerType);

                    // For each dependency from this specific referrer:
                    string[] depsForReferrer;
                    try
                    {
                        depsForReferrer = AssetDatabase.GetDependencies(referrerPath, true);
                    }
                    catch
                    {
                        continue;
                    }

                    foreach (var dep in depsForReferrer)
                    {
                        var np = NormalizePath(dep);
                        if (!spritePaths.Contains(np))
                            continue;

                        if (!added.Add((referrerPath, np)))
                            continue;

                        // Locate the UsedSprite entry (by path).
                        if (!spriteIndex.TryGetByPath(np, out var usedSprite))
                            continue;

                        // Identify sprite-vs-texture and mark usage.
                        var depType = AssetDatabase.GetMainAssetTypeAtPath(np);
                        var isSpriteDep = (depType == typeof(Sprite));

                        usedSprite.Usages.Add(new UsageRef
                        {
                            Kind = kind,
                            AssetGuid = referrerGuid,
                            AssetPath = referrerPath,
                            AssetType = referrerType?.FullName ?? "Unknown",
                            Strength = RefStrength.Indirect, // Dependency graph indicates reference, exact field is unknown
                            Notes = isSpriteDep
                                ? "Dependency includes Sprite asset."
                                : "Dependency includes Texture2D with Sprite sub-assets."
                        });
                    }
                }
            }
        }

#if ADDRESSABLES
        /// <summary>
        /// Scans Addressables settings to mark sprites that are addressable or dependencies of addressable entries.
        /// </summary>
        private static void CollectAddressablesUsages(
            HashSet<string> spritePaths,
            UsedSpriteDictionary spriteIndex)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            foreach (var group in settings.groups.Where(g => g != null))
            {
                foreach (var entry in group.entries)
                {
                    var entryPath = NormalizePath(entry.AssetPath);
                    if (string.IsNullOrEmpty(entryPath))
                        continue;

                    var entryGuid = entry.guid;
                    var entryType = AssetDatabase.GetMainAssetTypeAtPath(entryPath);
                    var labels = entry.labels?.ToArray() ?? Array.Empty<string>();

                    // Case A: entry itself IS the sprite/texture path.
                    if (spritePaths.Contains(entryPath) && spriteIndex.TryGetByPath(entryPath, out var directSprite))
                    {
                        directSprite.IsAddressable = true;
                        directSprite.Usages.Add(new UsageRef
                        {
                            Kind = UsageKind.AddressableEntry,
                            AssetGuid = entryGuid,
                            AssetPath = entryPath,
                            AssetType = entryType?.FullName ?? "Unknown",
                            Strength = RefStrength.Direct,
                            AddressableGroup = group.Name,
                            AddressKey = entry.address,
                            AddressLabels = labels,
                            Notes = "Addressable entry points directly to the sprite/texture."
                        });
                    }

                    // Case B: sprite is a dependency of this addressable entry.
                    string[] addrDeps = null;
                    try { addrDeps = AssetDatabase.GetDependencies(entryPath, true); }
                    catch { addrDeps = null; }

                    if (addrDeps == null) continue;

                    foreach (var dep in addrDeps)
                    {
                        var np = NormalizePath(dep);
                        if (!spritePaths.Contains(np))
                            continue;

                        if (!spriteIndex.TryGetByPath(np, out var usedSprite))
                            continue;

                        usedSprite.IsAddressable = true;

                        usedSprite.Usages.Add(new UsageRef
                        {
                            Kind = UsageKind.AddressableDep,
                            AssetGuid = entryGuid,
                            AssetPath = entryPath,
                            AssetType = entryType?.FullName ?? "Unknown",
                            Strength = RefStrength.Indirect,
                            AddressableGroup = group.Name,
                            AddressKey = entry.address,
                            AddressLabels = labels,
                            Notes = "Sprite is a dependency of an Addressable entry."
                        });
                    }
                }
            }
        }
#endif // ADDRESSABLES

        /// <summary>
        /// Attempts to get Sprite/Texture size without importing assets manually.
        /// For Sprite: uses sprite.rect.size; for Texture2D: texture.width/height.
        /// </summary>
        private static Vector2Int TryGetSize(string path, bool isSprite)
        {
            try
            {
                if (isSprite)
                {
                    var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    if (s != null)
                        return new Vector2Int(Mathf.RoundToInt(s.rect.width), Mathf.RoundToInt(s.rect.height));
                }
                else
                {
                    var t = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (t != null)
                        return new Vector2Int(t.width, t.height);
                }
            }
            catch { /* ignore size errors */ }

            return new Vector2Int(0, 0);
        }

        /// <summary>
        /// Heuristic: mark a path addressable if an Addressables entry exactly matches it.
        /// (This is a best-effort check for the "IsAddressable" flag.)
        /// </summary>
        private static bool IsPathAddressable(string path)
        {
#if ADDRESSABLES
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return false;

            var guid = AssetDatabase.AssetPathToGUID(path);
            if (string.IsNullOrEmpty(guid)) return false;

            var entry = settings.FindAssetEntry(guid);
            return entry != null;
#else
            return false;
#endif
        }

        /// <summary>
        /// Best-effort mapping from main asset type to UsageKind.
        /// </summary>
        private static UsageKind GuessUsageKindFromType(Type t)
        {
            if (t == typeof(SceneAsset)) return UsageKind.Scene;
            if (t == typeof(GameObject)) return UsageKind.Prefab;
            if (t == typeof(Material)) return UsageKind.Material;
            if (t != null && t.FullName == "UnityEditor.Animations.AnimatorController")
                return UsageKind.AnimatorController;
            if (t == typeof(AnimationClip)) return UsageKind.AnimationClip;

            // Timeline main assets are PlayableAsset (UnityEngine.Playables.PlayableAsset)
            if (t != null && typeof(UnityEngine.Playables.PlayableAsset).IsAssignableFrom(t))
                return UsageKind.TimelinePlayable;

            // SpriteAtlas resides in UnityEngine.U2D (type is internal in some versions; use name check):
            if (t != null && t.FullName == "UnityEngine.U2D.SpriteAtlas")
                return UsageKind.SpriteAtlas;

            // Tiles derive from UnityEngine.Tilemaps.TileBase
            if (t != null && IsSubclassOfFullName(t, "UnityEngine.Tilemaps.TileBase"))
                return UsageKind.Tile;

            // Fallbacks:
            if (t != null && typeof(ScriptableObject).IsAssignableFrom(t))
                return UsageKind.ScriptableObject;

            return UsageKind.ScriptableObject;
        }

        /// <summary>
        /// Checks if a type inherits from a base type specified by FullName (avoids assembly ref headaches).
        /// </summary>
        private static bool IsSubclassOfFullName(Type type, string baseFullName)
        {
            while (type != null && type != typeof(object))
            {
                if (string.Equals(type.FullName, baseFullName, StringComparison.Ordinal))
                    return true;

                type = type.BaseType;
            }
            return false;
        }

        // --------------------------------------------------------
        // Small utility containers & comparers
        // --------------------------------------------------------

        /// <summary>
        /// Dictionary keyed by GUID (if present) or Path as fallback.
        /// Provides quick "get by path" as well.
        /// </summary>
        private sealed class UsedSpriteDictionary
        {
            private readonly Dictionary<string, UsedSprite> byGuid =
                new(StringComparer.OrdinalIgnoreCase);

            private readonly Dictionary<string, UsedSprite> byPath =
                new(StringComparer.OrdinalIgnoreCase);

            public IEqualityComparer<UsedSprite> Comparer => new UsedSpriteEqualityComparer();

            public int Count => byPath.Count;

            public IEnumerable<UsedSprite> Values => byPath.Values;

            public void AddOrGet(UsedSprite us)
            {
                if (!string.IsNullOrEmpty(us.Guid))
                    byGuid[us.Guid] = us;

                if (!string.IsNullOrEmpty(us.Path))
                    byPath[us.Path] = us;
            }

            public bool TryGetByPath(string path, out UsedSprite us)
            {
                return byPath.TryGetValue(path, out us);
            }
        }

        private sealed class UsedSpriteEqualityComparer : IEqualityComparer<UsedSprite>
        {
            public bool Equals(UsedSprite x, UsedSprite y) => x?.Equals(y) ?? (y is null);
            public int GetHashCode(UsedSprite obj) => obj?.GetHashCode() ?? 0;
        }

        private sealed class PairOrdinalIgnoreCaseComparer : IEqualityComparer<(string a, string b)>
        {
            public bool Equals((string a, string b) x, (string a, string b) y) =>
                StringComparer.OrdinalIgnoreCase.Equals(x.a, y.a) &&
                StringComparer.OrdinalIgnoreCase.Equals(x.b, y.b);

            public int GetHashCode((string a, string b) obj)
            {
                unchecked
                {
                    int h1 = StringComparer.OrdinalIgnoreCase.GetHashCode(obj.a ?? string.Empty);
                    int h2 = StringComparer.OrdinalIgnoreCase.GetHashCode(obj.b ?? string.Empty);
                    return (h1 * 397) ^ h2;
                }
            }
        }

#endif // UNITY_EDITOR
    }
}
