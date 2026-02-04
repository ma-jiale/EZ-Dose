// SpriteDuplicateRemover.cs (Refactored v2)

using DA_Assets.Extensions;
using DA_Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using DA_Assets.FCU.Model;
using DA_Assets.Logging;

namespace DA_Assets.FCU
{
    [Serializable]
    public class SpriteDuplicateRemover : FcuBase
    {
        internal async Task RemoveDuplicates(List<FObject> fobjects, CancellationToken token)
        {
#if UNITY_EDITOR
            // 1. Знаходимо GUID'и всіх спрайтів у цільовій папці
            string spritesPath = monoBeh.Settings.ImageSpritesSettings.SpritesPath;
            string[] allSpriteGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { spritesPath });

            // 2. Отримуємо ПОВНУ інформацію про всі спрайти та ВСІ їх використання
            // Це "джерело правди" про використання ассетів.
            HashSet<SpriteUsageFinder.UsedSprite> allSpritesWithUsage = SpriteUsageFinder.GetUsedSprites_AllAssets(
                allSpriteGuids,
                includeScenes: true,
                includePrefabs: true,
                includeMaterials: true,
                includeAnimation: true,
                includeAtlases: true,
                includeTiles: true,
                includeScriptableObjects: true,
                includeAddressables: true
            );

            if (allSpritesWithUsage.Count == 0)
            {
                Debug.Log(FcuLocKey.log_sprite_duplicate_remover_no_sprites.Localize(spritesPath));
                return;
            }

            // 3. Готуємо дані для SpriteDuplicateFinder
            // Створюємо словник для швидкої заміни "худих" об'єктів на "повні"
            var spriteLookup = allSpritesWithUsage.ToDictionary(
                s => s.Path,
                s => s,
                StringComparer.OrdinalIgnoreCase
            );

            // GetGroups приймає string[], як ви вказали
            string[] allSpritePaths = allSpritesWithUsage.Select(s => s.Path).ToArray();

            // 4. Знаходимо групи дублікатів (отримуємо "худі" групи)
            var sdf = new SpriteDuplicateFinder();
            List<List<SpriteUsageFinder.UsedSprite>> thinGroups = sdf.GetGroups(allSpritePaths, false);

            // 5. "Збагачуємо" групи: замінюємо "худі" UsedSprite на "повні" (з 'Usages')
            List<List<SpriteUsageFinder.UsedSprite>> groups = new List<List<SpriteUsageFinder.UsedSprite>>();
            foreach (var thinGroup in thinGroups)
            {
                var fatGroup = new List<SpriteUsageFinder.UsedSprite>();
                foreach (var thinSprite in thinGroup)
                {
                    // Знаходимо відповідний спрайт, який має повну інформацію
                    if (spriteLookup.TryGetValue(thinSprite.Path, out var fatSprite))
                    {
                        fatGroup.Add(fatSprite);
                    }
                    else
                    {
                        // Про всяк випадок, якщо спрайт не знайшовся (не повинен)
                        fatGroup.Add(thinSprite);
                    }
                }
                groups.Add(fatGroup);
            }

            // 6. Логіка попереднього вибору (тепер працює коректно)
            foreach (var g in groups) // g - це List<SpriteUsageFinder.UsedSprite>
            {
                if (g == null || g.Count == 0) continue;

                SpriteUsageFinder.UsedSprite bestToKeep = null;

                // 6.1. Шукаємо найбільший серед ТИХ, ЩО ВИКОРИСТОВУЮТЬСЯ
                // Цей рядок тепер працює, оскільки 'Usages' заповнено
                var usedInGroup = g.Where(s => s.Usages.Count > 0).ToList();

                if (usedInGroup.Any())
                {
                    bestToKeep = usedInGroup.OrderByDescending(s => (s.Size.x) * (s.Size.y)).FirstOrDefault();
                }

                // 6.2. Якщо ніхто не використовується, просто беремо найбільший з групи
                if (bestToKeep == null)
                {
                    bestToKeep = g.OrderByDescending(s => (s.Size.x) * (s.Size.y)).FirstOrDefault();
                }

                if (bestToKeep == null) continue; // Порожня група

                // 6.3. Позначаємо всі спрайти, ОКРІМ найкращого, як вибрані (для видалення)
                foreach (var sprite in g)
                {
                    sprite.Selected = (sprite != bestToKeep);
                }
            }

            if (groups.Count() < 1)
            {
                Debug.Log(FcuLocKey.log_sprite_duplicate_remover_no_duplicates.Localize());
                return;
            }

            // 7. Показуємо вікно для підтвердження користувачем
            List<List<SpriteUsageFinder.UsedSprite>> processedGroups = null;
            monoBeh.EditorDelegateHolder.ShowSpriteDuplicateFinder(groups, result => processedGroups = result);

            // 8. Чекаємо на відповідь від вікна
            while (processedGroups == null)
            {
                if (token.IsCancellationRequested)
                    return;
                await Task.Delay(100, token);
            }

            // 9. Готуємо карту заміни та список на видалення
            var replaceMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var toDelete = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var group in processedGroups) // group - це List<SpriteUsageFinder.UsedSprite>
            {
                if (group == null || group.Count == 0)
                    continue;

                var keepCandidates = group.Where(x => !x.Selected).ToList();

                if (keepCandidates.Count > 0)
                {
                    var best = keepCandidates.OrderByDescending(s => (s.Size.x) * (s.Size.y)).First();
                    string bestPath = SpriteUsageFinder.NormalizePath(best.Path);

                    foreach (var dub in group)
                    {
                        string path = SpriteUsageFinder.NormalizePath(dub.Path);
                        if (dub.Selected && !path.Equals(bestPath, StringComparison.OrdinalIgnoreCase))
                        {
                            replaceMap[path] = bestPath;
                            toDelete.Add(path);
                        }
                    }
                }
                else
                {
                    foreach (var dub in group)
                    {
                        string path = SpriteUsageFinder.NormalizePath(dub.Path);
                        replaceMap[path] = null;
                        toDelete.Add(path);
                    }
                }
            }

            if (replaceMap.Count == 0 && toDelete.Count == 0)
                return;

            // 10. Оновлюємо fobjects новими шляхами
            foreach (var fobject in fobjects)
            {
                string spritePath = fobject.Data?.SpritePath;
                if (spritePath.IsEmpty()) continue;

                string normalizedPath = SpriteUsageFinder.NormalizePath(spritePath);
                if (replaceMap.TryGetValue(normalizedPath, out string newPath))
                {
                    fobject.Data.SpritePath = newPath;
                }
            }

            // 11. Фізично видаляємо ассети
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (var path in toDelete)
                {
                    if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null)
                    {
                        AssetDatabase.DeleteAsset(path);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
#endif
        }
    }
}
